using NUnit.Framework;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class ActorQueryTests
	{
		private ActorWorld _world;

		[SetUp]
		public void Setup()
		{
			_world = new ActorWorld();
			_world.Initialize();
		}

		[TearDown]
		public void TearDown() => _world.Dispose();

		[Test]
		public void Query_WhenFeatureAdded_ContainsActor()
		{
			ActorQuery query = _world.CreateQuery().With<Health>().Build();
			Actor actor = _world.CreateActor();

			Assert.That(query.Count, Is.EqualTo(0));

			actor.AddFeature<Health>();

			Assert.That(query.Count, Is.EqualTo(1));
			Assert.That(query.GetActorAt(0), Is.SameAs(actor));
		}

		[Test]
		public void Query_WhenFeatureRemoved_DropsActor()
		{
			ActorQuery query = _world.CreateQuery().With<Health>().Build();
			Actor actor = _world.CreateActor();
			actor.AddFeature<Health>();

			actor.RemoveFeature<Health>();

			Assert.That(query.Count, Is.EqualTo(0));
			Assert.That(query.Contains(actor), Is.False);
		}

		[Test]
		public void Query_CreatedAfterActors_BackfillsExistingActors()
		{
			Actor actor = _world.CreateActor();
			actor.AddFeature<Health>();

			ActorQuery query = _world.CreateQuery().With<Health>().Build();

			Assert.That(query.Count, Is.EqualTo(1));
			Assert.That(query.GetActorAt(0), Is.SameAs(actor));
		}

		[Test]
		public void Query_WithoutFilter_ExcludesActorsHoldingTheExcludedFeature()
		{
			ActorQuery query = _world.CreateQuery().With<Health>().Without<Dead>().Build();
			Actor actor = _world.CreateActor();
			actor.AddFeature<Health>();

			Assert.That(query.Count, Is.EqualTo(1));

			actor.AddFeature<Dead>();

			Assert.That(query.Count, Is.EqualTo(0));

			actor.RemoveFeature<Dead>();

			Assert.That(query.Count, Is.EqualTo(1));
		}

		[Test]
		public void Query_WithAny_MatchesEitherFeature()
		{
			ActorQuery query = _world.CreateQuery().WithAny<PlayerTag, EnemyTag>().Build();

			Actor player = _world.CreateActor();
			player.AddFeature<PlayerTag>();
			Actor enemy = _world.CreateActor();
			enemy.AddFeature<EnemyTag>();
			Actor neither = _world.CreateActor();
			neither.AddFeature<Health>();

			Assert.That(query.Count, Is.EqualTo(2));
			Assert.That(query.Contains(player), Is.True);
			Assert.That(query.Contains(enemy), Is.True);
			Assert.That(query.Contains(neither), Is.False);
		}

		[Test]
		public void Query_WithNoAnyConstraint_IsUnconstrainedRatherThanEmpty()
		{
			ActorQuery query = _world.CreateQuery().With<Health>().Build();
			Actor actor = _world.CreateActor();
			actor.AddFeature<Health>();

			Assert.That(query.Count, Is.EqualTo(1), "An empty Any mask must mean unconstrained, not 'matches nothing'.");
		}

		[Test]
		public void Build_WithIdenticalShape_ReturnsTheSameQueryInstance()
		{
			ActorQuery first = _world.CreateQuery().With<Health>().Without<Dead>().Build();
			ActorQuery second = _world.CreateQuery().With<Health>().Without<Dead>().Build();

			Assert.That(second, Is.SameAs(first));
			Assert.That(_world.QueryCount, Is.EqualTo(1));
		}

		[Test]
		public void Build_WithDifferentShape_ReturnsDistinctQueries()
		{
			ActorQuery first = _world.CreateQuery().With<Health>().Build();
			ActorQuery second = _world.CreateQuery().With<Health>().Without<Dead>().Build();

			Assert.That(second, Is.Not.SameAs(first));
			Assert.That(_world.QueryCount, Is.EqualTo(2));
		}

		[Test]
		public void Build_DifferingOnlyByIncludeInactive_ReturnsDistinctQueries()
		{
			ActorQuery active = _world.CreateQuery().With<Health>().Build();
			ActorQuery all = _world.CreateQuery().With<Health>().IncludeInactive().Build();

			Assert.That(all, Is.Not.SameAs(active));
		}

		[Test]
		public void Query_WhenActorDestroyed_DropsIt()
		{
			ActorQuery query = _world.CreateQuery().With<Health>().Build();
			Actor actor = _world.CreateActor();
			actor.AddFeature<Health>();

			actor.Destroy();

			Assert.That(query.Count, Is.EqualTo(0));
		}

		[Test]
		public void GetActorsNonAlloc_WithSmallBuffer_TruncatesToBufferLength()
		{
			ActorQuery query = _world.CreateQuery().With<Health>().Build();
			for (int i = 0; i < 5; i++)
			{
				_world.CreateActor().AddFeature<Health>();
			}

			Actor[] results = new Actor[3];
			int count = query.GetActorsNonAlloc(results);

			Assert.That(count, Is.EqualTo(3));
			Assert.That(results[0], Is.Not.Null);
		}

		[Test]
		public void Contains_StaleActorWithReusedId_ReturnsFalse()
		{
			ActorQuery query = _world.CreateQuery().With<Health>().Build();
			Actor first = _world.CreateActor();
			first.AddFeature<Health>();
			first.Destroy();

			Actor second = _world.CreateActor();
			second.AddFeature<Health>();

			Assert.That(second.Id, Is.EqualTo(first.Id));
			Assert.That(query.Contains(first), Is.False);
			Assert.That(query.Contains(second), Is.True);
		}

		[Test]
		public void IncludeInactiveQuery_DoesNotSeeUnbuiltActor()
		{
			ActorBuilder builder = _world.BuildActor().With(new Health());

			ActorQuery query = _world.CreateQuery().With<Health>().IncludeInactive().Build();

			Assert.That(query.Count, Is.EqualTo(0));

			builder.Build();

			Assert.That(query.Count, Is.EqualTo(1));
		}
	}
}
