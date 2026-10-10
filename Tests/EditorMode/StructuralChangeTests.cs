using System;
using NUnit.Framework;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class StructuralChangeTests
	{
		private ActorWorld _world;

		[SetUp]
		public void Setup()
		{
			_world = new ActorWorld();
		}

		[TearDown]
		public void TearDown() => _world.Dispose();

		[Test]
		public void Update_RunsTypedSystemForEachMatchingActor()
		{
			RegenerationSystem system = new();
			_world.AddSystem(system);
			_world.Initialize();

			Actor actor = _world.BuildActor()
				.With(new Health { Current = 10f, Max = 100f })
				.With(new Regeneration { PerSecond = 5f })
				.Build();

			_world.Update(1f);

			Assert.That(actor.GetFeature<Health>().Current, Is.EqualTo(15f));
		}

		[Test]
		public void Update_ClampsThroughRefWrites()
		{
			RegenerationSystem system = new();
			_world.AddSystem(system);
			_world.Initialize();

			Actor actor = _world.BuildActor()
				.With(new Health { Current = 99f, Max = 100f })
				.With(new Regeneration { PerSecond = 50f })
				.Build();

			_world.Update(1f);

			Assert.That(actor.GetFeature<Health>().Current, Is.EqualTo(100f));
		}

		[Test]
		public void Update_SkipsActorsMissingOneOfTheRequiredFeatures()
		{
			CountingSystem system = new();
			_world.AddSystem(system);
			_world.Initialize();

			_world.CreateActor().AddFeature<Health>();
			_world.CreateActor();

			_world.Update(1f);

			Assert.That(system.ExecuteCount, Is.EqualTo(1));
		}

		[Test]
		public void Update_WhenSystemDestroysAnotherActor_SkipsItWithinTheSameLoop()
		{
			DestroyTargetSystem system = new();
			_world.AddSystem(system);
			_world.Initialize();

			Actor target = _world.CreateActor();
			target.AddFeature<Health>();
			Actor other = _world.CreateActor();
			other.AddFeature<Health>();
			system.Target = target;

			_world.Update(1f);

			Assert.That(system.ExecuteCount, Is.EqualTo(1), "The destroyed actor must not be executed.");
			Assert.That(target.IsAlive, Is.False);
			Assert.That(_world.ActorCount, Is.EqualTo(1));
		}

		[Test]
		public void Update_WhenSystemSpawnsActors_DoesNotExtendTheCurrentIteration()
		{
			SpawningSystem system = new();
			_world.AddSystem(system);
			_world.Initialize();

			_world.CreateActor().AddFeature<Health>();

			_world.Update(1f);

			Assert.That(system.ExecuteCount, Is.EqualTo(1), "Query membership must stay frozen for the whole system update.");
			Assert.That(_world.ActorCount, Is.EqualTo(2));
		}

		[Test]
		public void Update_AfterSpawningUpdate_SeesTheNewActors()
		{
			CountingSystem counting = new();
			SpawningSystem spawning = new();
			_world.AddSystem(spawning);
			_world.AddSystem(counting);
			_world.Initialize();

			_world.CreateActor().AddFeature<Health>();

			_world.Update(1f);

			Assert.That(counting.LastQueryCount, Is.EqualTo(2),
				"The spawning system ran first, so the counting system must already see the flushed membership.");
		}

		[Test]
		public void Update_RunsManualSystem()
		{
			ManualHealthSystem system = new();
			_world.AddSystem(system);
			_world.Initialize();

			Actor actor = _world.CreateActor();
			actor.AddFeature(new Health { Current = 1f, Max = 10f });

			_world.Update(2f);

			Assert.That(system.ExecuteCount, Is.EqualTo(1));
			Assert.That(actor.GetFeature<Health>().Current, Is.EqualTo(3f));
		}

		[Test]
		public void AddSystem_AfterInitialize_InitializesImmediately()
		{
			_world.Initialize();
			CountingSystem system = new();

			_world.AddSystem(system);

			_world.CreateActor().AddFeature<Health>();
			_world.Update(1f);

			Assert.That(system.ExecuteCount, Is.EqualTo(1));
		}

		[Test]
		public void AddSystem_Twice_Throws()
		{
			CountingSystem system = new();
			_world.AddSystem(system);

			Assert.Throws<InvalidOperationException>(() => _world.AddSystem(system));
		}

		[Test]
		public void RemoveSystem_StopsItFromRunning()
		{
			CountingSystem system = new();
			_world.AddSystem(system);
			_world.Initialize();
			_world.CreateActor().AddFeature<Health>();

			Assert.That(_world.RemoveSystem(system), Is.True);

			_world.Update(1f);

			Assert.That(system.ExecuteCount, Is.EqualTo(0));
		}

		[Test]
		public void FlushStructuralChanges_WhenAListenerNeverConverges_ThrowsInsteadOfHanging()
		{
			ActorWorld world = new();
			world.Initialize();
			Action<Actor> handler = destroyed => world.CreateActor().Destroy();
			world.OnActorDestroyed += handler;

			Actor actor = world.CreateActor();

			Assert.Throws<InvalidOperationException>(() => actor.Destroy());

			world.OnActorDestroyed -= handler;
		}

		[Test]
		public void Update_WhenSystemRemovesFeatureFromAnotherQueuedActor_SkipsIt()
		{
			RemoveFeatureTargetSystem system = new();
			_world.AddSystem(system);
			_world.Initialize();

			Actor target = _world.CreateActor();
			target.AddFeature<Health>();
			_world.CreateActor().AddFeature<Health>();
			system.Target = target;

			Assert.DoesNotThrow(() => _world.Update(1f));
			Assert.That(system.ExecuteCount, Is.EqualTo(1), "The stripped actor must not be executed.");
			Assert.That(target.HasFeature<Health>(), Is.False);
		}

		[Test]
		public void Update_WhenSystemDeactivatesAnotherActor_SkipsIt()
		{
			DeactivateTargetSystem system = new();
			_world.AddSystem(system);
			_world.Initialize();

			Actor target = _world.CreateActor();
			target.AddFeature<Health>();
			_world.CreateActor().AddFeature<Health>();
			system.Target = target;

			_world.Update(1f);

			Assert.That(system.ExecuteCount, Is.EqualTo(1), "The deactivated actor must not be executed.");
			Assert.That(target.IsActive, Is.False);
		}

		[Test]
		public void RemoveSystem_ItselfDuringUpdate_DoesNotSkipNextSystem()
		{
			SystemRemovingSystem remover = new();
			UpdateCounterSystem counter = new();
			remover.SystemToRemove = remover;
			_world.AddSystem(remover);
			_world.AddSystem(counter);
			_world.Initialize();

			_world.Update(1f);

			Assert.That(counter.UpdateCount, Is.EqualTo(1));
			Assert.That(_world.SystemCount, Is.EqualTo(1));
		}

		[Test]
		public void RemoveSystem_OfLaterSystemDuringUpdate_StopsItThisFrame()
		{
			SystemRemovingSystem remover = new();
			UpdateCounterSystem counter = new();
			remover.SystemToRemove = counter;
			_world.AddSystem(remover);
			_world.AddSystem(counter);
			_world.Initialize();

			_world.Update(1f);

			Assert.That(counter.UpdateCount, Is.EqualTo(0));
		}

		[Test]
		public void AddSystem_DuringUpdate_RunsFromNextFrame()
		{
			SystemAddingSystem adder = new();
			UpdateCounterSystem counter = new();
			adder.SystemToAdd = counter;
			_world.AddSystem(adder);
			_world.Initialize();

			_world.Update(1f);

			Assert.That(counter.UpdateCount, Is.EqualTo(0));

			_world.Update(1f);

			Assert.That(counter.UpdateCount, Is.EqualTo(1));
		}

		[Test]
		public void FlushStructuralChanges_WhenDestroyHandlerThrows_TearsDownEveryActor()
		{
			_world.AddSystem(new SelfDestroyingSystem());
			_world.Initialize();
			_world.CreateActor().AddFeature<Health>();
			_world.CreateActor().AddFeature<Health>();

			int handlerCallCount = 0;
			_world.OnActorDestroyed += destroyed =>
			{
				handlerCallCount++;
				if (handlerCallCount == 1)
				{
					throw new InvalidOperationException("Handler failure.");
				}
			};

			Assert.Throws<InvalidOperationException>(() => _world.Update(1f));

			_world.CreateActor();

			Assert.That(handlerCallCount, Is.EqualTo(2), "The actor left in the queue must be torn down by the next flush.");
			Assert.That(_world.ActorCount, Is.EqualTo(1));
			Assert.That(_world.GetPool<Health>().Count, Is.EqualTo(0));
		}
	}
}
