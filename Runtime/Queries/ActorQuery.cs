using System;

namespace DTech.ActorFramework
{
	/// <summary>
	/// A live set of the actors matching a feature shape. Membership is maintained by the world on
	/// structural change, so iterating a query is a flat array walk with no matching work per frame.
	/// </summary>
	public sealed class ActorQuery
	{
		private const int DefaultNone = -1;
		private const int MinCapacity = 8;

		private readonly FeatureMask _all;
		private readonly FeatureMask _any;
		private readonly FeatureMask _none;
		private readonly bool _hasAny;
		private readonly bool _includeInactive;

		private int[] _sparse;
		private Actor[] _actors;
		private int _count;

		public int Count => _count;
		public bool IncludeInactive => _includeInactive;

		internal FeatureMask All => _all;
		internal FeatureMask Any => _any;
		internal FeatureMask None => _none;
		internal bool HasAny => _hasAny;

		internal ActorQuery(in ActorQueryKey key, int actorCapacity)
		{
			_all = key.All;
			_any = key.Any;
			_none = key.None;
			_hasAny = key.HasAny;
			_includeInactive = key.IncludeInactive;
			_sparse = new int[actorCapacity < 1 ? 1 : actorCapacity];
			_actors = new Actor[MinCapacity];
			_count = 0;

			FillSparse(0, _sparse.Length);
		}

		public Actor GetActorAt(int index)
		{
			if (index < 0 || index >= _count)
			{
				throw new ArgumentOutOfRangeException(nameof(index),
					$"Index must be in [0, {_count}), but was {index}.");
			}

			return _actors[index];
		}

		public int GetActorsNonAlloc(Actor[] results)
		{
			_ = results ?? throw new ArgumentNullException(nameof(results));

			int writtenCount = _count < results.Length ? _count : results.Length;
			Array.Copy(_actors, results, writtenCount);

			return writtenCount;
		}

		public bool Contains(Actor actor)
		{
			_ = actor ?? throw new ArgumentNullException(nameof(actor));

			int actorId = actor.Id;
			if (actorId < 0 || actorId >= _sparse.Length)
			{
				return false;
			}

			int index = _sparse[actorId];

			return index != DefaultNone && ReferenceEquals(_actors[index], actor);
		}

		/// <summary>
		/// Whether an actor taken from this query still belongs in it. Membership is synced only when a
		/// structural scope ends, so inside a system an actor stays listed after it is destroyed, deactivated
		/// or loses a required feature. Cheap while the actor is unchanged since the last sync.
		/// </summary>
		public bool IsMatching(Actor actor)
		{
			_ = actor ?? throw new ArgumentNullException(nameof(actor));

			return actor.IsAlive && (!actor.IsQueryDirty || Matches(actor));
		}

		internal void Sync(Actor actor)
		{
			if (Matches(actor))
			{
				AddActor(actor);
				return;
			}

			RemoveActor(actor.Id);
		}

		internal void RemoveActor(int actorId)
		{
			if (actorId < 0 || actorId >= _sparse.Length)
			{
				return;
			}

			int index = _sparse[actorId];
			if (index == DefaultNone)
			{
				return;
			}

			int lastIndex = _count - 1;
			if (index != lastIndex)
			{
				Actor moved = _actors[lastIndex];
				_actors[index] = moved;
				_sparse[moved.Id] = index;
			}

			_actors[lastIndex] = null;
			_sparse[actorId] = DefaultNone;
			_count = lastIndex;
		}

		internal void Clear()
		{
			for (int i = 0; i < _count; i++)
			{
				_sparse[_actors[i].Id] = DefaultNone;
				_actors[i] = null;
			}

			_count = 0;
		}

		private bool Matches(Actor actor)
		{
			if (!actor.IsAlive || !actor.IsArmed)
			{
				return false;
			}

			if (!_includeInactive && !actor.IsActive)
			{
				return false;
			}

			ref FeatureMask mask = ref actor.Mask;
			if (!mask.ContainsAll(in _all))
			{
				return false;
			}
			
			if (_hasAny && !mask.ContainsAny(in _any))
			{
				return false;
			}

			return mask.ContainsNone(in _none);
		}

		private void AddActor(Actor actor)
		{
			int actorId = actor.Id;
			EnsureActorCapacity(actorId);

			if (_sparse[actorId] != DefaultNone)
			{
				return;
			}

			if (_count == _actors.Length)
			{
				Array.Resize(ref _actors, _actors.Length * 2);
			}

			_actors[_count] = actor;
			_sparse[actorId] = _count;
			_count++;
		}

		private void EnsureActorCapacity(int actorId)
		{
			if (actorId < _sparse.Length)
			{
				return;
			}

			int previousLength = _sparse.Length;
			int nextLength = previousLength * 2;
			while (nextLength <= actorId)
			{
				nextLength *= 2;
			}

			Array.Resize(ref _sparse, nextLength);
			FillSparse(previousLength, nextLength);
		}

		private void FillSparse(int startIndex, int endIndex)
		{
			for (int i = startIndex; i < endIndex; i++)
			{
				_sparse[i] = DefaultNone;
			}
		}
	}
}