using System;
using System.Collections.Generic;

namespace DTech.ActorFramework
{
	internal sealed class SystemPhase<TSystem>
		where TSystem : class
	{
		private const int NotRunning = -1;

		private readonly List<TSystem> _systems = new();

		private int _cursor = NotRunning;
		private int _end;

		public void Add(TSystem system) => _systems.Add(system);

		public void Remove(TSystem system)
		{
			int index = _systems.IndexOf(system);
			if (index < 0)
			{
				return;
			}

			_systems.RemoveAt(index);

			if (_cursor == NotRunning)
			{
				return;
			}

			if (index <= _cursor)
			{
				_cursor--;
			}

			if (index < _end)
			{
				_end--;
			}
		}

		public void Run(Action<TSystem, float> invoke, float deltaTime, StructuralChangeQueue structural)
		{
			if (_cursor != NotRunning)
			{
				throw new InvalidOperationException(
					$"Systems of '{typeof(TSystem).Name}' are already running. An update phase cannot be re-entered.");
			}

			_end = _systems.Count;
			try
			{
				for (_cursor = 0; _cursor < _end; _cursor++)
				{
					TSystem system = _systems[_cursor];
					structural.Begin();
					try
					{
						invoke(system, deltaTime);
					}
					finally
					{
						structural.End();
					}
				}
			}
			finally
			{
				_cursor = NotRunning;
			}
		}

		public void Clear()
		{
			_systems.Clear();
			_end = 0;
		}
	}
}