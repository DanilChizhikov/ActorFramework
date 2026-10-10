using System;
using System.Collections.Generic;

namespace DTech.ActorFramework
{
	internal sealed class ActorRegistry
	{
		private const int DEFAULT_CAPACITY = 64;

		private readonly Stack<int> _freeIds = new();

		public int Count => _count;
		public int Capacity => _actorsById.Length;

		private Actor[] _actorsById = new Actor[DEFAULT_CAPACITY];
		private int _nextId;
		private int _count;

		public Actor Create(ActorWorld world)
		{
			int actorId = AllocateId();
			var actor = new Actor(world, actorId);
			_actorsById[actorId] = actor;
			_count++;

			return actor;
		}

		public bool TryGet(int actorId, out Actor actor)
		{
			if (actorId < 0 || actorId >= _actorsById.Length)
			{
				actor = null;

				return false;
			}

			actor = _actorsById[actorId];

			return actor != null;
		}

		public int GetActorsNonAlloc(Actor[] results)
		{
			int writtenCount = 0;
			for (int i = 0; i < _actorsById.Length; i++)
			{
				if (writtenCount == results.Length)
				{
					break;
				}

				Actor actor = _actorsById[i];
				if (actor == null)
				{
					continue;
				}

				results[writtenCount] = actor;
				writtenCount++;
			}

			return writtenCount;
		}

		public void Release(int actorId)
		{
			_actorsById[actorId] = null;
			_freeIds.Push(actorId);
			_count--;
		}

		public void Clear()
		{
			_freeIds.Clear();
			Array.Clear(_actorsById, 0, _actorsById.Length);
			_count = 0;
			_nextId = 0;
		}

		private int AllocateId()
		{
			if (_freeIds.Count > 0)
			{
				return _freeIds.Pop();
			}

			int actorId = _nextId;
			_nextId++;

			if (actorId >= _actorsById.Length)
			{
				Array.Resize(ref _actorsById, _actorsById.Length * 2);
			}

			return actorId;
		}
	}
}
