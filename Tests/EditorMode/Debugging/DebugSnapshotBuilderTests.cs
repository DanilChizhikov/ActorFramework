using System;
using DTech.ActorFramework.Diagnostics;
using NUnit.Framework;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class DebugSnapshotBuilderTests
	{
		private ActorWorld _world;

		[SetUp]
		public void Setup()
		{
			_world = new ActorWorld("Snapshot World");
		}

		[TearDown]
		public void TearDown()
		{
			_world.Dispose();
		}

		[Test]
		public void Build_ListsTrackedWorld()
		{
			PollResponse response = DebugSnapshotBuilder.Build(new PollRequest());

			WorldInfo info = Array.Find(response.Worlds, world => world.Id == _world.DebugId);
			Assert.That(info, Is.Not.Null);
			Assert.That(info.Name, Is.EqualTo("Snapshot World"));
			Assert.That(response.SessionId, Is.EqualTo(DebugSnapshotBuilder.SessionId));
			Assert.That(response.World, Is.Null);
		}

		[Test]
		public void Build_WithWorld_ReturnsActorsAndTheirFeatures()
		{
			Actor actor = _world.BuildActor().With(new Health()).With(new PlayerTag()).Build();

			PollResponse response = DebugSnapshotBuilder.Build(new PollRequest { WorldId = _world.DebugId });

			Assert.That(response.World.Version, Is.EqualTo(_world.StructureVersion));
			Assert.That(response.World.Actors, Has.Length.EqualTo(1));
			ActorSummary summary = response.World.Actors[0];
			Assert.That(summary.Id, Is.EqualTo(actor.Id));
			Assert.That(summary.IsActive, Is.True);
			Assert.That(summary.FeatureTypeIds,
				Is.EquivalentTo(new[] { FeatureType<Health>.Id, FeatureType<PlayerTag>.Id }));
		}

		[Test]
		public void Build_WithKnownVersion_OmitsActors()
		{
			_world.CreateActor();

			PollResponse response = DebugSnapshotBuilder.Build(new PollRequest
			{
				WorldId = _world.DebugId,
				KnownVersion = _world.StructureVersion,
			});

			Assert.That(response.World, Is.Not.Null);
			Assert.That(response.World.Actors, Is.Null);
		}

		[Test]
		public void Build_WithQueryIndex_ReturnsOnlyMembers()
		{
			ActorQuery query = _world.CreateQuery().With<Health>().Build();
			Actor member = _world.BuildActor().With(new Health()).Build();
			_world.CreateActor();

			PollResponse response = DebugSnapshotBuilder.Build(new PollRequest
			{
				WorldId = _world.DebugId,
				QueryIndex = DebugTestUtility.IndexOfQuery(_world, query),
			});

			Assert.That(response.World.Actors, Has.Length.EqualTo(1));
			Assert.That(response.World.Actors[0].Id, Is.EqualTo(member.Id));
		}

		[Test]
		public void Build_DescribesSystemsAndQueries()
		{
			_world.AddSystem(new CountingSystem());
			_world.CreateQuery().With<Health>().Without<Dead>().Build();

			PollResponse response = DebugSnapshotBuilder.Build(new PollRequest { WorldId = _world.DebugId });

			SystemInfo system = Array.Find(response.World.Systems, info => info.Name == nameof(CountingSystem));
			Assert.That(system, Is.Not.Null);
			Assert.That(system.Phases, Is.EqualTo(SystemPhases.Update));

			QueryInfo query = Array.Find(response.World.Queries,
				info => info.None.Length == 1 && info.None[0] == FeatureType<Dead>.Id);
			Assert.That(query, Is.Not.Null);
			Assert.That(query.All, Is.EqualTo(new[] { FeatureType<Health>.Id }));
		}

		[Test]
		public void Build_WithSelectedActor_ReturnsDetails()
		{
			var token = new object();
			Actor actor = _world.BuildActor().With(new Health { Current = 4f, Max = 8f }).Build();
			actor.AddDeactivator(token);

			PollResponse response = DebugSnapshotBuilder.Build(new PollRequest
			{
				WorldId = _world.DebugId,
				SelectedActorId = actor.Id,
			});

			ActorDetails details = response.Actor;
			Assert.That(details.Id, Is.EqualTo(actor.Id));
			Assert.That(details.IsActive, Is.False);
			Assert.That(details.Deactivators, Has.Length.EqualTo(1));
			Assert.That(details.Features, Has.Length.EqualTo(1));
			Assert.That(details.Features[0].TypeId, Is.EqualTo(FeatureType<Health>.Id));
			Assert.That(details.Features[0].Root.Children[0].Float, Is.EqualTo(4d));
		}

		[Test]
		public void Build_WithMissingActor_ReturnsNoDetails()
		{
			Actor actor = _world.CreateActor();
			actor.Destroy();

			PollResponse response = DebugSnapshotBuilder.Build(new PollRequest
			{
				WorldId = _world.DebugId,
				SelectedActorId = actor.Id,
			});

			Assert.That(response.Actor, Is.Null);
		}

		[Test]
		public void Build_WithKnownTypeCount_SendsOnlyNewTypes()
		{
			_ = FeatureType<Health>.Id;

			PollResponse full = DebugSnapshotBuilder.Build(new PollRequest());
			PollResponse delta = DebugSnapshotBuilder.Build(new PollRequest { KnownTypeCount = full.NewTypes.Length });

			Assert.That(full.NewTypes[FeatureType<Health>.Id].Name, Is.EqualTo(nameof(Health)));
			Assert.That(delta.NewTypes, Is.Empty);
		}
	}
}
