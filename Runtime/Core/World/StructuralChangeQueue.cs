using System;

namespace DTech.ActorFramework
{
	internal sealed class StructuralChangeQueue
	{
		private const int DefaultCapacity = 32;
		private const int MaxFlushPasses = 64;

		private readonly ActorWorld _world;
		private readonly ActorRegistry _actors;
		private readonly QueryRegistry _queries;

		public bool IsInScope => _depth > 0;
		public int Version => _version;

		private int[] _dirtyActorIds = new int[DefaultCapacity];
		private int[] _dirtyScratch = new int[DefaultCapacity];
		private int[] _pendingDestroyIds = new int[DefaultCapacity];
		private int[] _pendingDestroyScratch = new int[DefaultCapacity];
		private int _dirtyCount;
		private int _pendingDestroyCount;
		private int _depth;
		private int _version;
		private bool _isFlushing;

		public StructuralChangeQueue(ActorWorld world, ActorRegistry actors, QueryRegistry queries)
		{
			_world = world;
			_actors = actors;
			_queries = queries;
		}

		public void Begin() => _depth++;

		public void End()
		{
			_depth--;
			if (_depth != 0)
			{
				return;
			}

			Flush();
		}

		public void MarkDirty(Actor actor)
		{
			if (actor.IsQueryDirty)
			{
				return;
			}

			actor.IsQueryDirty = true;

			if (_dirtyCount == _dirtyActorIds.Length)
			{
				Array.Resize(ref _dirtyActorIds, _dirtyActorIds.Length * 2);
			}

			_dirtyActorIds[_dirtyCount] = actor.Id;
			_dirtyCount++;
		}

		public void EnqueueDestroy(int actorId)
		{
			if (_pendingDestroyCount == _pendingDestroyIds.Length)
			{
				Array.Resize(ref _pendingDestroyIds, _pendingDestroyIds.Length * 2);
			}

			_pendingDestroyIds[_pendingDestroyCount] = actorId;
			_pendingDestroyCount++;
		}

		public void Clear()
		{
			_dirtyCount = 0;
			_pendingDestroyCount = 0;
			_depth = 0;
			_isFlushing = false;
		}

		private void Flush()
		{
			if (_isFlushing || (_pendingDestroyCount == 0 && _dirtyCount == 0))
			{
				return;
			}

			_isFlushing = true;
			_version++;
			_depth++;
			try
			{
				int passCount = 0;
				while (_pendingDestroyCount > 0 || _dirtyCount > 0)
				{
					passCount++;
					if (passCount > MaxFlushPasses)
					{
						throw new InvalidOperationException(
							$"Structural changes did not converge after {MaxFlushPasses} passes. " +
							"A listener is most likely dirtying actors from inside a structural event.");
					}

					FlushPendingDestroys();
					FlushDirtyActors();
				}
			}
			finally
			{
				_depth--;
				_isFlushing = false;
			}
		}

		private void FlushPendingDestroys()
		{
			int count = _pendingDestroyCount;
			if (count == 0)
			{
				return;
			}

			if (_pendingDestroyScratch.Length < count)
			{
				_pendingDestroyScratch = new int[_pendingDestroyIds.Length];
			}

			Array.Copy(_pendingDestroyIds, _pendingDestroyScratch, count);
			_pendingDestroyCount = 0;

			int index = 0;
			try
			{
				for (; index < count; index++)
				{
					if (!_actors.TryGet(_pendingDestroyScratch[index], out Actor actor))
					{
						continue;
					}

					_world.TeardownActor(actor);
				}
			}
			finally
			{
				for (int i = index + 1; i < count; i++)
				{
					EnqueueDestroy(_pendingDestroyScratch[i]);
				}
			}
		}

		private void FlushDirtyActors()
		{
			int count = _dirtyCount;
			if (count == 0)
			{
				return;
			}

			if (_dirtyScratch.Length < count)
			{
				_dirtyScratch = new int[_dirtyActorIds.Length];
			}

			Array.Copy(_dirtyActorIds, _dirtyScratch, count);
			_dirtyCount = 0;

			for (int i = 0; i < count; i++)
			{
				if (!_actors.TryGet(_dirtyScratch[i], out Actor actor))
				{
					continue;
				}

				actor.IsQueryDirty = false;
				_queries.Sync(actor);
			}
		}
	}
}