using System;
using System.Collections.Generic;

namespace DTech.ActorFramework
{
	internal sealed class FeatureStore
	{
		private const int DEFAULT_POOL_CAPACITY = 16;

		private readonly ActorWorld _world;
		private readonly ActorRegistry _actors;
		private readonly StructuralChangeQueue _structural;

		public int PoolCapacity => _pools.Length;

		private IFeaturePool[] _pools = new IFeaturePool[DEFAULT_POOL_CAPACITY];

		public FeatureStore(ActorWorld world, ActorRegistry actors, StructuralChangeQueue structural)
		{
			_world = world;
			_actors = actors;
			_structural = structural;
		}

		public FeaturePool<TFeature> GetPool<TFeature>()
			where TFeature : struct, IActorFeature
		{
			int typeId = FeatureType<TFeature>.Id;
			EnsurePoolCapacity(typeId);

			IFeaturePool pool = _pools[typeId];
			if (pool != null)
			{
				return (FeaturePool<TFeature>)pool;
			}

			FeaturePool<TFeature> created = new FeaturePool<TFeature>(typeId, _actors.Capacity);
			_pools[typeId] = created;

			return created;
		}

		public ref TFeature Get<TFeature>(Actor actor)
			where TFeature : struct, IActorFeature
		{
			int typeId = FeatureType<TFeature>.Id;
			if (!actor.Mask.Has(typeId))
			{
				throw new KeyNotFoundException($"Actor '{actor.Id}' has no feature '{typeof(TFeature).Name}'.");
			}

			return ref GetPool<TFeature>().GetUnchecked(actor.Id);
		}

		public bool TryGet<TFeature>(Actor actor, out TFeature feature)
			where TFeature : struct, IActorFeature
		{
			int typeId = FeatureType<TFeature>.Id;
			if (!actor.Mask.Has(typeId))
			{
				feature = default;

				return false;
			}

			feature = GetPool<TFeature>().GetUnchecked(actor.Id);

			return true;
		}

		public ref TFeature Add<TFeature>(Actor actor, TFeature value)
			where TFeature : struct, IActorFeature
		{
			_world.ThrowIfNotAcceptingChanges();

			int typeId = FeatureType<TFeature>.Id;
			ref FeatureMask mask = ref actor.Mask;
			if (mask.Has(typeId))
			{
				throw new InvalidOperationException(
					$"Actor '{actor.Id}' already has feature '{typeof(TFeature).Name}'.");
			}

			FeaturePool<TFeature> pool = GetPool<TFeature>();
			pool.Add(actor.Id, in value);
			mask.Set(typeId);
			actor.IncrementFeatureCount();

			_structural.Begin();
			try
			{
				_structural.MarkDirty(actor);
				_world.RaiseFeatureAdded(actor, typeId);
			}
			finally
			{
				_structural.End();
			}

			return ref pool.GetUnchecked(actor.Id);
		}

		public ref TFeature GetOrAdd<TFeature>(Actor actor)
			where TFeature : struct, IActorFeature
		{
			int typeId = FeatureType<TFeature>.Id;
			if (actor.Mask.Has(typeId))
			{
				return ref GetPool<TFeature>().GetUnchecked(actor.Id);
			}

			return ref Add<TFeature>(actor, default);
		}

		public bool Remove<TFeature>(Actor actor)
			where TFeature : struct, IActorFeature
		{
			_world.ThrowIfDisposed();

			int typeId = FeatureType<TFeature>.Id;
			ref FeatureMask mask = ref actor.Mask;
			if (!mask.Has(typeId))
			{
				return false;
			}

			_structural.Begin();
			try
			{
				_world.RaiseFeatureRemoved(actor, typeId);

				if (!mask.Has(typeId))
				{
					return true;
				}

				mask.Unset(typeId);
				actor.DecrementFeatureCount();
				GetPool<TFeature>().Remove(actor.Id);
				_structural.MarkDirty(actor);
			}
			finally
			{
				_structural.End();
			}

			return true;
		}

		public void AddSilent<TFeature>(Actor actor, in TFeature value)
			where TFeature : struct, IActorFeature
		{
			_world.ThrowIfNotBuildable(actor);

			int typeId = FeatureType<TFeature>.Id;
			ref FeatureMask mask = ref actor.Mask;
			if (mask.Has(typeId))
			{
				throw new InvalidOperationException(
					$"Actor '{actor.Id}' already has feature '{typeof(TFeature).Name}'.");
			}

			GetPool<TFeature>().Add(actor.Id, in value);
			mask.Set(typeId);
			actor.IncrementFeatureCount();
		}

		public void RemoveFromPool(int typeId, int actorId) => _pools[typeId].Remove(actorId);

		public void Clear()
		{
			for (int i = 0; i < _pools.Length; i++)
			{
				_pools[i]?.Clear();
			}

			Array.Clear(_pools, 0, _pools.Length);
		}

		private void EnsurePoolCapacity(int typeId)
		{
			if (typeId < _pools.Length)
			{
				return;
			}

			int nextLength = _pools.Length * 2;
			while (nextLength <= typeId)
			{
				nextLength *= 2;
			}

			if (nextLength > FeatureTypeRegistry.MAX_FEATURE_TYPES)
			{
				nextLength = FeatureTypeRegistry.MAX_FEATURE_TYPES;
			}

			Array.Resize(ref _pools, nextLength);
		}
	}
}
