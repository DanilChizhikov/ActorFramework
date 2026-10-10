using NUnit.Framework;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class ActorBuilderTests
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
		public void Build_AppliesEveryFeature()
		{
			Actor actor = _world.BuildActor()
				.With(new Health { Current = 5f, Max = 10f })
				.With(new Position { X = 1f, Y = 2f })
				.With(new Velocity { X = 3f, Y = 4f })
				.Build();

			Assert.That(actor.FeatureCount, Is.EqualTo(3));
			Assert.That(actor.GetFeature<Health>().Max, Is.EqualTo(10f));
			Assert.That(actor.GetFeature<Position>().X, Is.EqualTo(1f));
			Assert.That(actor.GetFeature<Velocity>().Y, Is.EqualTo(4f));
		}

		[Test]
		public void Build_AddsTheActorToMatchingQueries()
		{
			ActorQuery query = _world.CreateQuery().With<Health, Position>().Build();

			Actor actor = _world.BuildActor()
				.With(new Health())
				.With(new Position())
				.Build();

			Assert.That(query.Count, Is.EqualTo(1));
			Assert.That(query.GetActorAt(0), Is.SameAs(actor));
		}

		[Test]
		public void Build_RaisesActorCreatedBeforeFeatureEvents()
		{
			int createdCount = 0;
			int featureAddedCount = 0;
			int featureAddedAfterCreated = 0;

			_world.OnActorCreated += actor => createdCount++;
			_world.OnFeatureAdded += (actor, typeId) =>
			{
				featureAddedCount++;
				if (createdCount > 0)
				{
					featureAddedAfterCreated++;
				}
			};

			_world.BuildActor()
				.With(new Health())
				.With(new Position())
				.Build();

			Assert.That(createdCount, Is.EqualTo(1));
			Assert.That(featureAddedCount, Is.EqualTo(2));
			Assert.That(featureAddedAfterCreated, Is.EqualTo(2));
		}

		[Test]
		public void Build_ArmsActivationOnce()
		{
			int activatedCount = 0;

			ActorBuilder builder = _world.BuildActor();
			builder.Actor.OnActivated += actor => activatedCount++;

			Actor built = builder.With(new Health()).Build();

			Assert.That(built.IsActive, Is.True);
			Assert.That(activatedCount, Is.EqualTo(1));
		}

		[Test]
		public void Build_WithDuplicateFeature_Throws()
		{
			ActorBuilder builder = _world.BuildActor().With(new Health());

			Assert.Throws<System.InvalidOperationException>(() => builder.With(new Health()));
		}
	}
}