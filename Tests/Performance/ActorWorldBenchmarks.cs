using NUnit.Framework;
using Unity.PerformanceTesting;

namespace DTech.ActorFramework.Tests.Performance
{
	[TestFixture]
	internal sealed class ActorWorldBenchmarks
	{
		private const int ACTOR_COUNT = 10000;
		private const int CHURN_COUNT = 1000;
		private const float DELTA_TIME = 0.016f;

		[Test]
		[Performance]
		public void Iterate_TenThousandActors_OneFeature_TypedSystem()
		{
			ActorWorld world = new();
			world.AddSystem(new LifetimeSystem());
			world.Initialize();
			PopulateMovers(world, ACTOR_COUNT);

			Measure.Method(() => world.Update(DELTA_TIME))
				.WarmupCount(5)
				.MeasurementCount(50)
				.GC()
				.Run();

			world.Dispose();
		}

		[Test]
		[Performance]
		public void Iterate_TenThousandActors_TwoFeatures_TypedSystem()
		{
			ActorWorld world = new();
			world.AddSystem(new MovementSystem());
			world.Initialize();
			PopulateMovers(world, ACTOR_COUNT);

			Measure.Method(() => world.Update(DELTA_TIME))
				.WarmupCount(5)
				.MeasurementCount(50)
				.GC()
				.Run();

			world.Dispose();
		}

		[Test]
		[Performance]
		public void Iterate_TenThousandActors_ThreeFeatures_TypedSystem()
		{
			ActorWorld world = new();
			world.AddSystem(new DamageSystem());
			world.Initialize();
			PopulateMovers(world, ACTOR_COUNT);

			Measure.Method(() => world.Update(DELTA_TIME))
				.WarmupCount(5)
				.MeasurementCount(50)
				.GC()
				.Run();

			world.Dispose();
		}

		[Test]
		[Performance]
		public void Iterate_TenThousandActors_TwoFeatures_ManualSystem()
		{
			ActorWorld world = new();
			world.AddSystem(new ManualMovementSystem());
			world.Initialize();
			PopulateMovers(world, ACTOR_COUNT);

			Measure.Method(() => world.Update(DELTA_TIME))
				.WarmupCount(5)
				.MeasurementCount(50)
				.GC()
				.Run();

			world.Dispose();
		}

		[Test]
		[Performance]
		public void Spawn_TenThousandActors_CreateActorThenAddFeature()
		{
			ActorWorld world = null;

			Measure.Method(() =>
				{
					for (int i = 0; i < ACTOR_COUNT; i++)
					{
						Actor actor = world.CreateActor();
						actor.AddFeature(new Position());
						actor.AddFeature(new Velocity { X = 1f });
						actor.AddFeature(new Health { Max = 100f });
					}
				})
				.SetUp(() => world = CreateWorldWithQueries())
				.CleanUp(() => world.Dispose())
				.WarmupCount(2)
				.MeasurementCount(10)
				.GC()
				.Run();
		}

		[Test]
		[Performance]
		public void Spawn_TenThousandActors_ActorBuilder()
		{
			ActorWorld world = null;

			Measure.Method(() =>
				{
					for (int i = 0; i < ACTOR_COUNT; i++)
					{
						world.BuildActor()
							.With(new Position())
							.With(new Velocity { X = 1f })
							.With(new Health { Max = 100f })
							.Build();
					}
				})
				.SetUp(() => world = CreateWorldWithQueries())
				.CleanUp(() => world.Dispose())
				.WarmupCount(2)
				.MeasurementCount(10)
				.GC()
				.Run();
		}

		[Test]
		[Performance]
		public void Churn_DestroyAndRespawn_OneThousandActors_WithEightQueries()
		{
			ActorWorld world = CreateWorldWithQueries();
			world.Initialize();
			PopulateMovers(world, ACTOR_COUNT);

			Actor[] buffer = new Actor[ACTOR_COUNT];

			Measure.Method(() =>
				{
					int count = world.GetActorsNonAlloc(buffer);
					int churn = count < CHURN_COUNT ? count : CHURN_COUNT;
					for (int i = 0; i < churn; i++)
					{
						buffer[i].Destroy();
					}

					for (int i = 0; i < churn; i++)
					{
						world.BuildActor()
							.With(new Position())
							.With(new Velocity { X = 1f })
							.With(new Health { Max = 100f })
							.With(new Lifetime { Remaining = 1f })
							.Build();
					}
				})
				.WarmupCount(3)
				.MeasurementCount(20)
				.GC()
				.Run();

			world.Dispose();
		}

		private static ActorWorld CreateWorldWithQueries()
		{
			ActorWorld world = new();

			world.CreateQuery().With<Position>().Build();
			world.CreateQuery().With<Position, Velocity>().Build();
			world.CreateQuery().With<Health>().Build();
			world.CreateQuery().With<Health>().Without<Dead>().Build();
			world.CreateQuery().With<Lifetime>().Build();
			world.CreateQuery().With<Health, Damage>().Build();
			world.CreateQuery().WithAny<PlayerTag, EnemyTag>().Build();
			world.CreateQuery().With<Position, Velocity, Health>().Build();

			return world;
		}

		private static void PopulateMovers(ActorWorld world, int count)
		{
			for (int i = 0; i < count; i++)
			{
				world.BuildActor()
					.With(new Position())
					.With(new Velocity { X = 1f, Y = 1f, Z = 1f })
					.With(new Health { Current = 100f, Max = 100f })
					.With(new Damage { Amount = 1f })
					.With(new Lifetime { Remaining = 10f })
					.Build();
			}
		}
	}
}
