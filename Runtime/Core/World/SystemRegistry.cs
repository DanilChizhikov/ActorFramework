using System;
using System.Collections.Generic;

namespace DTech.ActorFramework
{
	internal sealed class SystemRegistry
	{
		private readonly StructuralChangeQueue _structural;
		private readonly List<IActorSystem> _systems = new();
		private readonly List<IUpdateSystem> _updateSystems = new();
		private readonly List<IFixedUpdateSystem> _fixedUpdateSystems = new();
		private readonly List<ILateUpdateSystem> _lateUpdateSystems = new();

		public int Count => _systems.Count;

		public SystemRegistry(StructuralChangeQueue structural)
		{
			_structural = structural;
		}

		public void Add(IActorSystem system)
		{
			if (_systems.Contains(system))
			{
				throw new InvalidOperationException($"System '{system.GetType().Name}' is already registered.");
			}

			_systems.Add(system);

			if (system is IUpdateSystem updateSystem)
			{
				_updateSystems.Add(updateSystem);
			}

			if (system is IFixedUpdateSystem fixedUpdateSystem)
			{
				_fixedUpdateSystems.Add(fixedUpdateSystem);
			}

			if (system is ILateUpdateSystem lateUpdateSystem)
			{
				_lateUpdateSystems.Add(lateUpdateSystem);
			}
		}

		public bool Remove(IActorSystem system)
		{
			if (!_systems.Remove(system))
			{
				return false;
			}

			if (system is IUpdateSystem updateSystem)
			{
				_updateSystems.Remove(updateSystem);
			}

			if (system is IFixedUpdateSystem fixedUpdateSystem)
			{
				_fixedUpdateSystems.Remove(fixedUpdateSystem);
			}

			if (system is ILateUpdateSystem lateUpdateSystem)
			{
				_lateUpdateSystems.Remove(lateUpdateSystem);
			}

			return true;
		}

		public void InitializeAll(ActorWorld world)
		{
			for (int i = 0; i < _systems.Count; i++)
			{
				_systems[i].Initialize(world);
			}
		}

		public void DisposeAll()
		{
			for (int i = _systems.Count - 1; i >= 0; i--)
			{
				_systems[i].Dispose();
			}

			_systems.Clear();
			_updateSystems.Clear();
			_fixedUpdateSystems.Clear();
			_lateUpdateSystems.Clear();
		}

		public void Update(float deltaTime)
		{
			for (int i = 0; i < _updateSystems.Count; i++)
			{
				IUpdateSystem system = _updateSystems[i];
				_structural.Begin();
				try
				{
					system.Update(deltaTime);
				}
				finally
				{
					_structural.End();
				}
			}
		}

		public void FixedUpdate(float fixedDeltaTime)
		{
			for (int i = 0; i < _fixedUpdateSystems.Count; i++)
			{
				IFixedUpdateSystem system = _fixedUpdateSystems[i];
				_structural.Begin();
				try
				{
					system.FixedUpdate(fixedDeltaTime);
				}
				finally
				{
					_structural.End();
				}
			}
		}

		public void LateUpdate(float deltaTime)
		{
			for (int i = 0; i < _lateUpdateSystems.Count; i++)
			{
				ILateUpdateSystem system = _lateUpdateSystems[i];
				_structural.Begin();
				try
				{
					system.LateUpdate(deltaTime);
				}
				finally
				{
					_structural.End();
				}
			}
		}
	}
}
