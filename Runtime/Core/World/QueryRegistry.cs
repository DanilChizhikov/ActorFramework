using System.Collections.Generic;

namespace DTech.ActorFramework
{
	internal sealed class QueryRegistry
	{
		private readonly ActorRegistry _actors;
		private readonly List<ActorQuery> _queries = new();
		private readonly Dictionary<ActorQueryKey, ActorQuery> _queriesByKey = new();

		public int Count => _queries.Count;

		public QueryRegistry(ActorRegistry actors)
		{
			_actors = actors;
		}

		public ActorQuery GetOrCreate(in ActorQueryKey key)
		{
			if (_queriesByKey.TryGetValue(key, out ActorQuery existing))
			{
				return existing;
			}

			ActorQuery query = new ActorQuery(in key, _actors.Capacity);
			_queriesByKey.Add(key, query);
			_queries.Add(query);

			for (int i = 0; i < _actors.Capacity; i++)
			{
				if (!_actors.TryGet(i, out Actor actor))
				{
					continue;
				}

				query.Sync(actor);
			}

			return query;
		}

		public ActorQuery GetAt(int index) => _queries[index];

		public void Sync(Actor actor)
		{
			for (int i = 0; i < _queries.Count; i++)
			{
				_queries[i].Sync(actor);
			}
		}

		public void RemoveActor(int actorId)
		{
			for (int i = 0; i < _queries.Count; i++)
			{
				_queries[i].RemoveActor(actorId);
			}
		}

		public void Clear()
		{
			for (int i = 0; i < _queries.Count; i++)
			{
				_queries[i].Clear();
			}

			_queries.Clear();
			_queriesByKey.Clear();
		}
	}
}