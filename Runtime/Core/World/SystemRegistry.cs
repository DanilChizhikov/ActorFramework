using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace DTech.ActorFramework
{
	internal sealed class SystemRegistry
	{
		private static readonly Action<IUpdateSystem, float> _invokeUpdate =
			(system, deltaTime) => system.Update(deltaTime);
		private static readonly Action<IFixedUpdateSystem, float> _invokeFixedUpdate =
			(system, fixedDeltaTime) => system.FixedUpdate(fixedDeltaTime);
		private static readonly Action<ILateUpdateSystem, float> _invokeLateUpdate =
			(system, deltaTime) => system.LateUpdate(deltaTime);

		private readonly StructuralChangeQueue _structural;
		private readonly List<IActorSystem> _systems = new();
		private readonly SystemPhase<IUpdateSystem> _updateSystems = new();
		private readonly SystemPhase<IFixedUpdateSystem> _fixedUpdateSystems = new();
		private readonly SystemPhase<ILateUpdateSystem> _lateUpdateSystems = new();

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

		public IActorSystem GetAt(int index) => _systems[index];

		public void InitializeAll(ActorWorld world)
		{
			for (int i = 0; i < _systems.Count; i++)
			{
				_systems[i].Initialize(world);
			}
		}

		public void DisposeAll()
		{
			ExceptionDispatchInfo firstException = null;
			for (int i = _systems.Count - 1; i >= 0; i--)
			{
				try
				{
					_systems[i].Dispose();
				}
				catch (Exception exception)
				{
					firstException ??= ExceptionDispatchInfo.Capture(exception);
				}
			}

			_systems.Clear();
			_updateSystems.Clear();
			_fixedUpdateSystems.Clear();
			_lateUpdateSystems.Clear();

			firstException?.Throw();
		}

		public void Update(float deltaTime) => _updateSystems.Run(_invokeUpdate, deltaTime, _structural);

		public void FixedUpdate(float fixedDeltaTime) =>
			_fixedUpdateSystems.Run(_invokeFixedUpdate, fixedDeltaTime, _structural);

		public void LateUpdate(float deltaTime) => _lateUpdateSystems.Run(_invokeLateUpdate, deltaTime, _structural);
	}
}