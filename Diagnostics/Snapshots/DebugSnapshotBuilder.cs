using System;
using System.Collections.Generic;

namespace DTech.ActorFramework.Diagnostics
{
	internal static class DebugSnapshotBuilder
	{
		public static readonly Guid SessionId = Guid.NewGuid();

		private static readonly List<ActorWorld> _worlds = new();
		private static readonly int[] _typeIdBuffer = new int[FeatureMask.CAPACITY];
		private static readonly int[] _emptyIds = Array.Empty<int>();
		private static readonly Comparison<ActorSummary> _byId = (left, right) => left.Id.CompareTo(right.Id);

		public static PollResponse Build(PollRequest request)
		{
			_ = request ?? throw new ArgumentNullException(nameof(request));

			ActorWorldTracker.GetWorlds(_worlds);
			try
			{
				var response = new PollResponse
				{
					SessionId = SessionId,
					Worlds = BuildWorlds(),
					NewTypes = BuildNewTypes(request.KnownTypeCount),
				};

				ActorWorld world = FindWorld(request.WorldId);
				if (world == null)
				{
					return response;
				}

				response.World = BuildWorld(world, request);
				if (request.SelectedActorId != PollRequest.NONE)
				{
					response.Actor = BuildActor(world, request.SelectedActorId);
				}

				return response;
			}
			finally
			{
				_worlds.Clear();
			}
		}

		private static WorldInfo[] BuildWorlds()
		{
			var infos = new WorldInfo[_worlds.Count];
			for (int i = 0; i < infos.Length; i++)
			{
				ActorWorld world = _worlds[i];
				world.GetFeatureTypeBudget(out int used, out int capacity);
				infos[i] = new WorldInfo
				{
					Id = world.DebugId,
					Name = world.Name,
					Phase = world.Phase,
					ActorCount = world.ActorCount,
					SystemCount = world.SystemCount,
					QueryCount = world.QueryCount,
					FeatureTypesUsed = used,
					FeatureTypesCapacity = capacity,
				};
			}

			return infos;
		}

		private static FeatureTypeInfo[] BuildNewTypes(int knownTypeCount)
		{
			int count = FeatureTypeRegistry.Count;
			int start = knownTypeCount < 0 || knownTypeCount > count ? 0 : knownTypeCount;

			var infos = new FeatureTypeInfo[count - start];
			for (int i = 0; i < infos.Length; i++)
			{
				int typeId = start + i;
				Type type = FeatureTypeRegistry.GetFeatureType(typeId);
				infos[i] = new FeatureTypeInfo
				{
					Id = typeId,
					Name = TypeNames.GetDisplayName(type),
					FullName = type.FullName,
				};
			}

			return infos;
		}

		private static ActorWorld FindWorld(int worldId)
		{
			for (int i = 0; i < _worlds.Count; i++)
			{
				if (_worlds[i].DebugId == worldId)
				{
					return _worlds[i];
				}
			}

			return null;
		}

		private static WorldSnapshot BuildWorld(ActorWorld world, PollRequest request)
		{
			var snapshot = new WorldSnapshot
			{
				WorldId = world.DebugId,
				Version = world.StructureVersion,
				Systems = BuildSystems(world),
				Queries = BuildQueries(world),
			};

			if (snapshot.Version != request.KnownVersion)
			{
				snapshot.Actors = BuildActors(world, request.QueryIndex);
			}

			return snapshot;
		}

		private static ActorSummary[] BuildActors(ActorWorld world, int queryIndex)
		{
			ActorSummary[] summaries;
			if (queryIndex >= 0 && queryIndex < world.Queries.Count)
			{
				ActorQuery query = world.Queries.GetAt(queryIndex);
				summaries = new ActorSummary[query.Count];
				for (int i = 0; i < summaries.Length; i++)
				{
					summaries[i] = BuildSummary(query.GetActorAt(i));
				}

				Array.Sort(summaries, _byId);

				return summaries;
			}

			var actors = new Actor[world.ActorCount];
			int count = world.GetActorsNonAlloc(actors);
			summaries = new ActorSummary[count];
			for (int i = 0; i < count; i++)
			{
				summaries[i] = BuildSummary(actors[i]);
			}

			return summaries;
		}

		private static ActorSummary BuildSummary(Actor actor)
		{
			return new ActorSummary
			{
				Id = actor.Id,
				IsActive = actor.IsActive,
				FeatureTypeIds = CopyFeatureTypeIds(actor),
			};
		}

		private static int[] CopyFeatureTypeIds(Actor actor)
		{
			int count = actor.GetFeatureTypeIdsNonAlloc(_typeIdBuffer);

			return CopyBuffer(count);
		}

		private static int[] CopyMask(FeatureMask mask)
		{
			int count = mask.GetSetBitsNonAlloc(_typeIdBuffer);

			return CopyBuffer(count);
		}

		private static int[] CopyBuffer(int count)
		{
			if (count == 0)
			{
				return _emptyIds;
			}

			var ids = new int[count];
			Array.Copy(_typeIdBuffer, ids, count);

			return ids;
		}

		private static SystemInfo[] BuildSystems(ActorWorld world)
		{
			SystemRegistry systems = world.Systems;
			var infos = new SystemInfo[systems.Count];
			for (int i = 0; i < infos.Length; i++)
			{
				IActorSystem system = systems.GetAt(i);
				Type type = system.GetType();
				var phases = SystemPhases.None;
				if (system is IUpdateSystem)
				{
					phases |= SystemPhases.Update;
				}

				if (system is IFixedUpdateSystem)
				{
					phases |= SystemPhases.FixedUpdate;
				}

				if (system is ILateUpdateSystem)
				{
					phases |= SystemPhases.LateUpdate;
				}

				infos[i] = new SystemInfo
				{
					Name = TypeNames.GetDisplayName(type),
					FullName = type.FullName,
					Phases = phases,
				};
			}

			return infos;
		}

		private static QueryInfo[] BuildQueries(ActorWorld world)
		{
			QueryRegistry queries = world.Queries;
			var infos = new QueryInfo[queries.Count];
			for (int i = 0; i < infos.Length; i++)
			{
				ActorQuery query = queries.GetAt(i);
				infos[i] = new QueryInfo
				{
					Count = query.Count,
					IncludeInactive = query.IncludeInactive,
					All = CopyMask(query.All),
					Any = query.HasAny ? CopyMask(query.Any) : _emptyIds,
					None = CopyMask(query.None),
				};
			}

			return infos;
		}

		private static ActorDetails BuildActor(ActorWorld world, int actorId)
		{
			if (!world.TryGetActor(actorId, out Actor actor))
			{
				return null;
			}

			var tokens = new object[Math.Max(actor.ActivatorCount, actor.DeactivatorCount)];

			return new ActorDetails
			{
				Id = actor.Id,
				IsAlive = actor.IsAlive,
				IsActive = actor.IsActive,
				Activators = DescribeTokens(tokens, actor.GetActivatorsNonAlloc(tokens)),
				Deactivators = DescribeTokens(tokens, actor.GetDeactivatorsNonAlloc(tokens)),
				Features = BuildFeatures(world, actor),
			};
		}

		private static string[] DescribeTokens(object[] tokens, int count)
		{
			var descriptions = new string[count];
			for (int i = 0; i < count; i++)
			{
				object token = tokens[i];
				Type type = token.GetType();
				string text = token.ToString();
				descriptions[i] = text == type.ToString() ? TypeNames.GetDisplayName(type) : text;
				tokens[i] = null;
			}

			return descriptions;
		}

		private static FeatureDetails[] BuildFeatures(ActorWorld world, Actor actor)
		{
			int[] typeIds = CopyFeatureTypeIds(actor);
			var features = new FeatureDetails[typeIds.Length];
			for (int i = 0; i < typeIds.Length; i++)
			{
				int typeId = typeIds[i];
				features[i] = new FeatureDetails
				{
					TypeId = typeId,
					Root = BuildFeatureRoot(world, actor, typeId),
				};
			}

			return features;
		}

		private static FieldNode BuildFeatureRoot(ActorWorld world, Actor actor, int typeId)
		{
			try
			{
				if (!world.Features.TryGetPool(typeId, out IFeaturePool pool))
				{
					return CreateError("Feature pool is missing.");
				}

				return FieldTreeBuilder.BuildRoot(pool.GetBoxed(actor.Id));
			}
			catch (Exception exception)
			{
				return CreateError(exception.Message);
			}
		}

		private static FieldNode CreateError(string message)
		{
			return new FieldNode
			{
				Kind = FieldKind.Text,
				Flags = FieldFlags.ReadOnly,
				Label = "Error",
				Text = message,
			};
		}
	}
}