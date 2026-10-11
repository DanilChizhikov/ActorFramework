using System;
using System.Collections.Generic;
using System.Text;
using DTech.ActorFramework.Diagnostics;

namespace DTech.ActorFramework.Editor
{
	internal sealed class DebugSession
	{
		public const int None = PollRequest.None;

		private readonly List<FeatureTypeInfo> _types = new();

		public WorldInfo[] Worlds { get; private set; } = Array.Empty<WorldInfo>();
		public WorldInfo SelectedWorld { get; private set; }
		public ActorSummary[] Actors { get; private set; } = Array.Empty<ActorSummary>();
		public int ActorsRevision { get; private set; }
		public SystemInfo[] Systems { get; private set; } = Array.Empty<SystemInfo>();
		public QueryInfo[] Queries { get; private set; } = Array.Empty<QueryInfo>();
		public int SelectedActorId { get; private set; } = None;
		public ActorDetails SelectedActor { get; private set; }
		public bool IsSelectedActorMissing { get; private set; }
		public int QueryIndex { get; private set; } = None;
		public bool HasResponse { get; private set; }
		public string PreferredWorldName { get; set; }
		public int SelectedWorldId => SelectedWorld?.Id ?? None;

		private Guid _sessionId;
		private int _knownVersion = None;
		private int _requestedWorldId = None;
		private int _requestedActorId = None;
		private int _requestedQueryIndex = None;

		public PollRequest CreateRequest()
		{
			_requestedWorldId = SelectedWorldId;
			_requestedActorId = SelectedActorId;
			_requestedQueryIndex = QueryIndex;

			return new PollRequest
			{
				WorldId = _requestedWorldId,
				KnownVersion = _knownVersion,
				KnownTypeCount = _types.Count,
				QueryIndex = _requestedQueryIndex,
				SelectedActorId = _requestedActorId,
			};
		}

		public bool Apply(PollResponse response)
		{
			_ = response ?? throw new ArgumentNullException(nameof(response));

			if (response.SessionId != _sessionId)
			{
				bool isFirstResponse = _sessionId == Guid.Empty;
				Reset();
				_sessionId = response.SessionId;
				if (!isFirstResponse)
				{
					return false;
				}
			}

			HasResponse = true;
			MergeTypes(response.NewTypes);
			Worlds = response.Worlds ?? Array.Empty<WorldInfo>();

			WorldInfo world = ResolveWorld();
			if ((world?.Id ?? None) != SelectedWorldId)
			{
				SwitchWorld(world);
				return world == null;
			}

			SelectedWorld = world;
			if (world == null)
			{
				return true;
			}

			if (_requestedWorldId != world.Id)
			{
				return false;
			}

			ApplyWorld(response.World);
			if (_requestedActorId == SelectedActorId && SelectedActorId != None)
			{
				SelectedActor = response.Actor;
				IsSelectedActorMissing = response.Actor == null;
			}

			return true;
		}

		public void Reset()
		{
			_sessionId = Guid.Empty;
			_types.Clear();
			Worlds = Array.Empty<WorldInfo>();
			HasResponse = false;
			SwitchWorld(null);
		}

		public void SelectWorld(int worldId)
		{
			for (int i = 0; i < Worlds.Length; i++)
			{
				if (Worlds[i].Id != worldId)
				{
					continue;
				}

				SwitchWorld(Worlds[i]);
				return;
			}
		}

		public void SelectActor(int actorId)
		{
			if (actorId == SelectedActorId)
			{
				return;
			}

			SelectedActorId = actorId;
			SelectedActor = null;
			IsSelectedActorMissing = false;
		}

		public void SetQueryFilter(int queryIndex)
		{
			if (queryIndex == QueryIndex)
			{
				return;
			}

			QueryIndex = queryIndex;
			_knownVersion = None;
		}

		public FeatureTypeInfo GetFeatureType(int typeId)
		{
			return typeId >= 0 && typeId < _types.Count ? _types[typeId] : null;
		}

		public string GetFeatureTypeName(int typeId)
		{
			return GetFeatureType(typeId)?.Name ?? $"#{typeId}";
		}

		public string DescribeQuery(QueryInfo query)
		{
			var builder = new StringBuilder();
			AppendTypes(builder, "All", query.All);
			AppendTypes(builder, "Any", query.Any);
			AppendTypes(builder, "None", query.None);
			if (query.IncludeInactive)
			{
				AppendSeparator(builder).Append("+inactive");
			}

			return builder.Length == 0 ? "Every actor" : builder.ToString();
		}

		private static StringBuilder AppendSeparator(StringBuilder builder)
		{
			return builder.Length == 0 ? builder : builder.Append(" · ");
		}

		private void MergeTypes(FeatureTypeInfo[] newTypes)
		{
			if (newTypes == null)
			{
				return;
			}

			for (int i = 0; i < newTypes.Length; i++)
			{
				FeatureTypeInfo info = newTypes[i];
				while (_types.Count <= info.Id)
				{
					_types.Add(null);
				}

				_types[info.Id] = info;
			}
		}

		private WorldInfo ResolveWorld()
		{
			WorldInfo byName = null;
			for (int i = 0; i < Worlds.Length; i++)
			{
				WorldInfo world = Worlds[i];
				if (world.Id == SelectedWorldId)
				{
					return world;
				}

				if (byName == null && world.Name == PreferredWorldName)
				{
					byName = world;
				}
			}

			return byName ?? (Worlds.Length > 0 ? Worlds[0] : null);
		}

		private void SwitchWorld(WorldInfo world)
		{
			SelectedWorld = world;
			if (world != null)
			{
				PreferredWorldName = world.Name;
			}

			Actors = Array.Empty<ActorSummary>();
			ActorsRevision++;
			Systems = Array.Empty<SystemInfo>();
			Queries = Array.Empty<QueryInfo>();
			SelectedActorId = None;
			SelectedActor = null;
			IsSelectedActorMissing = false;
			QueryIndex = None;
			_knownVersion = None;
		}

		private void AppendTypes(StringBuilder builder, string title, int[] typeIds)
		{
			if (typeIds == null || typeIds.Length == 0)
			{
				return;
			}

			AppendSeparator(builder).Append(title).Append(": ");
			for (int i = 0; i < typeIds.Length; i++)
			{
				if (i > 0)
				{
					builder.Append(", ");
				}

				builder.Append(GetFeatureTypeName(typeIds[i]));
			}
		}

		private void ApplyWorld(WorldSnapshot snapshot)
		{
			if (snapshot == null || snapshot.WorldId != SelectedWorldId)
			{
				return;
			}

			Systems = snapshot.Systems ?? Array.Empty<SystemInfo>();
			Queries = snapshot.Queries ?? Array.Empty<QueryInfo>();

			if (snapshot.Actors == null || _requestedQueryIndex != QueryIndex)
			{
				return;
			}

			Actors = snapshot.Actors;
			ActorsRevision++;
			_knownVersion = snapshot.Version;
		}
	}
}