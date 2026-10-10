using System;

namespace DTech.ActorFramework
{
	/// <summary>
	/// Owns actors, feature pools, queries and systems. Not thread-safe: every member is main-thread only.
	/// </summary>
	public sealed class ActorWorld : IDisposable
	{
		public event Action<Actor> OnActorCreated;
		public event Action<Actor> OnActorDestroying;
		public event Action<Actor> OnActorDestroyed;
		public event Action<Actor, int> OnFeatureAdded;
		public event Action<Actor, int> OnFeatureRemoved;

		private readonly ActorRegistry _actors;
		private readonly QueryRegistry _queries;
		private readonly StructuralChangeQueue _structural;
		private readonly FeatureStore _features;
		private readonly SystemRegistry _systems;

		public Phase Phase => _phase;
		public int ActorCount => _actors.Count;
		public int SystemCount => _systems.Count;
		public int QueryCount => _queries.Count;
		public bool IsInitialized => _phase == Phase.Initialized;
		public bool IsDisposed => _phase == Phase.Disposed;

		private Phase _phase = Phase.Created;
		private bool _isDisposing;

		internal FeatureStore Features => _features;
		internal StructuralChangeQueue Structural => _structural;

		public ActorWorld()
		{
			_actors = new ActorRegistry();
			_queries = new QueryRegistry(_actors);
			_structural = new StructuralChangeQueue(this, _actors, _queries);
			_features = new FeatureStore(this, _actors, _structural);
			_systems = new SystemRegistry(_structural);
		}

		public void Dispose()
		{
			if (_phase == Phase.Disposed || _isDisposing)
			{
				return;
			}

			_isDisposing = true;

			_systems.DisposeAll();

			_structural.Begin();
			try
			{
				for (int i = 0; i < _actors.Capacity; i++)
				{
					if (!_actors.TryGet(i, out Actor actor))
					{
						continue;
					}

					DestroyActorCore(actor);
				}
			}
			finally
			{
				_structural.End();
			}

			_features.Clear();
			_queries.Clear();
			_actors.Clear();
			_structural.Clear();

			_phase = Phase.Disposed;
			_isDisposing = false;

			OnActorCreated = null;
			OnActorDestroying = null;
			OnActorDestroyed = null;
			OnFeatureAdded = null;
			OnFeatureRemoved = null;
		}

		/// <summary>
		/// Initializes every system registered so far, in registration order. Systems added afterwards are
		/// initialized on registration.
		/// </summary>
		public void Initialize()
		{
			ThrowIfDisposed();

			if (_phase == Phase.Initialized)
			{
				return;
			}

			_systems.InitializeAll(this);
			_phase = Phase.Initialized;
		}

		public Actor CreateActor()
		{
			ThrowIfDisposed();

			Actor actor = _actors.Create(this);

			_structural.Begin();
			try
			{
				actor.Arm();
				_structural.MarkDirty(actor);
				OnActorCreated?.Invoke(actor);
			}
			finally
			{
				_structural.End();
			}

			return actor;
		}

		/// <summary>
		/// Spawn fast path: writes every feature first and syncs queries once, instead of once per feature.
		/// </summary>
		public ActorBuilder BuildActor()
		{
			ThrowIfDisposed();

			return new ActorBuilder(this, _actors.Create(this));
		}

		public bool DestroyActor(Actor actor)
		{
			_ = actor ?? throw new ArgumentNullException(nameof(actor));
			ThrowIfDisposed();

			if (!ReferenceEquals(actor.World, this))
			{
				throw new InvalidOperationException($"Actor '{actor.Id}' does not belong to this world.");
			}

			return DestroyActorCore(actor);
		}

		public bool TryGetActor(int actorId, out Actor actor) => _actors.TryGet(actorId, out actor);

		public int GetActorsNonAlloc(Actor[] results)
		{
			_ = results ?? throw new ArgumentNullException(nameof(results));

			return _actors.GetActorsNonAlloc(results);
		}

		/// <summary>
		/// The dense pool of one feature type, created on first use. Iterate it directly from a hot system.
		/// </summary>
		public FeaturePool<TFeature> GetPool<TFeature>()
			where TFeature : struct, IActorFeature
		{
			return _features.GetPool<TFeature>();
		}

		public void PreallocateFeatures<TFeature>(int capacity)
			where TFeature : struct, IActorFeature
		{
			ThrowIfDisposed();

			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(capacity), $"Capacity must not be negative, but was {capacity}.");
			}

			_features.GetPool<TFeature>().EnsureDenseCapacity(capacity);
		}

		/// <summary>
		/// Feature type ids are global and capped. Assert against this at startup to fail fast rather than
		/// discover the cap as a TypeInitializationException mid-session.
		/// </summary>
		public void GetFeatureTypeBudget(out int used, out int capacity)
		{
			used = FeatureTypeRegistry.Count;
			capacity = FeatureTypeRegistry.MAX_FEATURE_TYPES;
		}

		public ActorQueryBuilder CreateQuery()
		{
			ThrowIfDisposed();

			return new ActorQueryBuilder(this);
		}

		public void AddSystem(IActorSystem system)
		{
			_ = system ?? throw new ArgumentNullException(nameof(system));
			ThrowIfDisposed();

			_systems.Add(system);

			if (_phase == Phase.Initialized)
			{
				system.Initialize(this);
			}
		}

		/// <summary>
		/// Unregisters a system without disposing it - the caller keeps ownership.
		/// </summary>
		public bool RemoveSystem(IActorSystem system)
		{
			_ = system ?? throw new ArgumentNullException(nameof(system));

			return _systems.Remove(system);
		}

		public void Update(float deltaTime)
		{
			ThrowIfNotRunnable();
			_systems.Update(deltaTime);
		}

		public void FixedUpdate(float fixedDeltaTime)
		{
			ThrowIfNotRunnable();
			_systems.FixedUpdate(fixedDeltaTime);
		}

		public void LateUpdate(float deltaTime)
		{
			ThrowIfNotRunnable();
			_systems.LateUpdate(deltaTime);
		}

		internal ActorQuery GetOrCreateQuery(in ActorQueryKey key)
		{
			ThrowIfDisposed();

			return _queries.GetOrCreate(in key);
		}

		internal Actor CompleteBuild(Actor actor)
		{
			FeatureMask snapshot = actor.Mask;

			_structural.Begin();
			try
			{
				actor.Arm();
				_structural.MarkDirty(actor);
				OnActorCreated?.Invoke(actor);

				for (int typeId = 0; typeId < _features.PoolCapacity; typeId++)
				{
					if (!snapshot.Has(typeId))
					{
						continue;
					}

					OnFeatureAdded?.Invoke(actor, typeId);
				}
			}
			finally
			{
				_structural.End();
			}

			return actor;
		}

		internal void TeardownActor(Actor actor)
		{
			int actorId = actor.Id;

			_queries.RemoveActor(actorId);

			FeatureMask snapshot = actor.Mask;
			for (int typeId = 0; typeId < _features.PoolCapacity; typeId++)
			{
				if (!snapshot.Has(typeId))
				{
					continue;
				}

				OnFeatureRemoved?.Invoke(actor, typeId);
				_features.RemoveFromPool(typeId, actorId);
			}

			actor.ClearMask();
			_actors.Release(actorId);

			actor.RaiseDestroyed();
			OnActorDestroyed?.Invoke(actor);
			actor.ClearEvents();
		}

		internal void RaiseFeatureAdded(Actor actor, int typeId) => OnFeatureAdded?.Invoke(actor, typeId);

		internal void RaiseFeatureRemoved(Actor actor, int typeId) => OnFeatureRemoved?.Invoke(actor, typeId);

		internal void ThrowIfDisposed()
		{
			if (_phase != Phase.Disposed)
			{
				return;
			}

			throw new ObjectDisposedException(nameof(ActorWorld));
		}

		private bool DestroyActorCore(Actor actor)
		{
			if (!actor.IsAlive)
			{
				return false;
			}

			_structural.Begin();
			try
			{
				OnActorDestroying?.Invoke(actor);
				actor.MarkDestroyed();
				actor.ResetActivation();
				_structural.EnqueueDestroy(actor.Id);
			}
			finally
			{
				_structural.End();
			}

			return true;
		}

		private void ThrowIfNotRunnable()
		{
			ThrowIfDisposed();

			if (_phase == Phase.Initialized)
			{
				return;
			}

			throw new InvalidOperationException(
				$"{nameof(ActorWorld)} must be initialized before it is updated. Call {nameof(Initialize)} first.");
		}
	}
}
