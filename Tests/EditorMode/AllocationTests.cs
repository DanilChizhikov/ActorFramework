using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class AllocationTests
	{
		private const int ACTOR_COUNT = 512;

		private ActorWorld _world;
		private ActorQuery _query;
		private FeaturePool<Health> _pool;

		[SetUp]
		public void Setup()
		{
			_world = new ActorWorld();
			_world.AddSystem(new RegenerationSystem());
			_world.AddSystem(new ManualHealthSystem());
			_world.Initialize();

			for (int i = 0; i < ACTOR_COUNT; i++)
			{
				_world.BuildActor()
					.With(new Health { Current = 0f, Max = 1000f })
					.With(new Regeneration { PerSecond = 1f })
					.Build();
			}

			_query = _world.CreateQuery().With<Health>().Build();
			_pool = _world.GetPool<Health>();
			
			for (int i = 0; i < 4; i++)
			{
				_world.Update(0.016f);
			}
		}

		[TearDown]
		public void TearDown() => _world.Dispose();

		[Test]
		public void Update_InSteadyState_DoesNotAllocate()
		{
			Assert.That(() => _world.Update(0.016f), Is.Not.AllocatingGCMemory());
		}

		[Test]
		public void QueryIteration_DoesNotAllocate()
		{
			Assert.That(
				() =>
				{
					float total = 0f;
					for (int i = _query.Count - 1; i >= 0; i--)
					{
						Actor actor = _query.GetActorAt(i);
						total += _pool.GetUnchecked(actor.Id).Current;
					}
				},
				Is.Not.AllocatingGCMemory());
		}

		[Test]
		public void FeatureAccess_DoesNotAllocate()
		{
			Actor actor = _query.GetActorAt(0);

			Assert.That(
				() =>
				{
					if (actor.HasFeature<Health>())
					{
						ref Health health = ref actor.GetFeature<Health>();
						health.Current += 1f;
					}
				},
				Is.Not.AllocatingGCMemory());
		}
	}
}
