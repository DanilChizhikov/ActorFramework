using System;
using System.Collections.Generic;

namespace DTech.ActorFramework
{
	/// <summary>
	/// Sparse set of one feature type. Dense storage, O(1) add / remove / lookup.
	/// Removal swaps the last element into the freed slot, so dense order is never stable.
	/// </summary>
	public sealed class FeaturePool<TFeature> : IFeaturePool
		where TFeature : struct, IActorFeature
	{
		private const int None = -1;
		private const int MinDenseCapacity = 4;

		private int[] _sparse;
		private int[] _denseActorIds;
		private TFeature[] _denseValues;
		private int _count;

		public int TypeId { get; }

		public int Count => _count;

		/// <summary>
		/// Dense values, valid for indices [0, Count).
		/// Never cache this array across a structural change - it is replaced when the pool grows.
		/// </summary>
		public TFeature[] Values => _denseValues;

		/// <summary>
		/// Actor ids parallel to Values. Same caching rule applies.
		/// </summary>
		public int[] ActorIds => _denseActorIds;

		internal FeaturePool(int typeId, int actorCapacity, int denseCapacity = MinDenseCapacity)
		{
			TypeId = typeId;
			_sparse = new int[actorCapacity < 1 ? 1 : actorCapacity];
			_denseActorIds = new int[denseCapacity < MinDenseCapacity ? MinDenseCapacity : denseCapacity];
			_denseValues = new TFeature[_denseActorIds.Length];
			_count = 0;

			FillSparse(0, _sparse.Length);
		}

		public bool Has(int actorId) => actorId >= 0 && actorId < _sparse.Length && _sparse[actorId] != None;

		/// <summary>
		/// Reference to the value of actorId. The caller must have checked Has.
		/// The reference is invalidated by the next structural change on the world - never hold it across one.
		/// </summary>
		public ref TFeature GetUnchecked(int actorId) => ref _denseValues[_sparse[actorId]];

		/// <summary>
		/// Reference to the value at a dense index. Same invalidation rule as GetUnchecked.
		/// </summary>
		public ref TFeature GetAt(int denseIndex)
		{
			if (denseIndex < 0 || denseIndex >= _count)
			{
				throw new ArgumentOutOfRangeException(nameof(denseIndex),
					$"Dense index must be in [0, {_count}), but was {denseIndex}.");
			}

			return ref _denseValues[denseIndex];
		}

		public int GetActorIdAt(int denseIndex)
		{
			if (denseIndex < 0 || denseIndex >= _count)
			{
				throw new ArgumentOutOfRangeException(nameof(denseIndex),
					$"Dense index must be in [0, {_count}), but was {denseIndex}.");
			}

			return _denseActorIds[denseIndex];
		}

		internal void Add(int actorId, in TFeature value)
		{
			EnsureActorCapacity(actorId);

			if (_sparse[actorId] != None)
			{
				throw new InvalidOperationException(
					$"Actor '{actorId}' already has feature '{typeof(TFeature).Name}'.");
			}

			if (_count == _denseValues.Length)
			{
				GrowDense(_denseValues.Length * 2);
			}

			_denseActorIds[_count] = actorId;
			_denseValues[_count] = value;
			_sparse[actorId] = _count;
			_count++;
		}

		internal bool Remove(int actorId)
		{
			if (!Has(actorId))
			{
				return false;
			}

			int denseIndex = _sparse[actorId];
			int lastIndex = _count - 1;
			if (denseIndex != lastIndex)
			{
				int movedActorId = _denseActorIds[lastIndex];
				_denseActorIds[denseIndex] = movedActorId;
				_denseValues[denseIndex] = _denseValues[lastIndex];
				_sparse[movedActorId] = denseIndex;
			}
			
			_denseActorIds[lastIndex] = None;
			_denseValues[lastIndex] = default;
			_sparse[actorId] = None;
			_count = lastIndex;

			return true;
		}

		internal void EnsureDenseCapacity(int capacity)
		{
			if (capacity <= _denseValues.Length)
			{
				return;
			}

			GrowDense(capacity);
		}

		internal void EnsureActorCapacity(int actorId)
		{
			if (actorId < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(actorId), $"Actor id must not be negative, but was {actorId}.");
			}

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

		internal void Clear()
		{
			for (int i = 0; i < _count; i++)
			{
				_sparse[_denseActorIds[i]] = None;
				_denseActorIds[i] = None;
				_denseValues[i] = default;
			}

			_count = 0;
		}

		private void GrowDense(int capacity)
		{
			Array.Resize(ref _denseActorIds, capacity);
			Array.Resize(ref _denseValues, capacity);
		}

		private void FillSparse(int startIndex, int endIndex)
		{
			for (int i = startIndex; i < endIndex; i++)
			{
				_sparse[i] = None;
			}
		}

		private void ThrowIfMissing(int actorId)
		{
			if (Has(actorId))
			{
				return;
			}

			throw new KeyNotFoundException($"Actor '{actorId}' has no feature '{typeof(TFeature).Name}'.");
		}

		bool IFeaturePool.Has(int actorId) => Has(actorId);

		bool IFeaturePool.Remove(int actorId) => Remove(actorId);

		void IFeaturePool.Clear() => Clear();

		object IFeaturePool.GetBoxed(int actorId)
		{
			ThrowIfMissing(actorId);

			return GetUnchecked(actorId);
		}

		void IFeaturePool.SetBoxed(int actorId, object value)
		{
			ThrowIfMissing(actorId);

			GetUnchecked(actorId) = (TFeature)value;
		}
	}
}