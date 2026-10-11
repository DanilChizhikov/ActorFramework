using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class ActorWorldTests
	{
		[Test]
		public void Initialize_InitializesSystemsInRegistrationOrder()
		{
			List<string> journal = new();
			ActorWorld world = new();
			world.AddSystem(new RecordingSystem(journal, "first"));
			world.AddSystem(new RecordingSystem(journal, "second"));

			world.Initialize();

			Assert.That(journal, Is.EqualTo(new[] { "first:initialize", "second:initialize" }));
			Assert.That(world.IsInitialized, Is.True);

			world.Dispose();
		}

		[Test]
		public void Dispose_DisposesSystemsInReverseOrder()
		{
			List<string> journal = new();
			ActorWorld world = new();
			world.AddSystem(new RecordingSystem(journal, "first"));
			world.AddSystem(new RecordingSystem(journal, "second"));
			world.Initialize();
			journal.Clear();

			world.Dispose();

			Assert.That(journal, Is.EqualTo(new[] { "second:dispose", "first:dispose" }));
		}

		[Test]
		public void Dispose_DestroysEveryLiveActor()
		{
			ActorWorld world = new();
			world.Initialize();
			Actor first = world.CreateActor();
			Actor second = world.CreateActor();
			second.AddFeature<Health>();

			world.Dispose();

			Assert.That(first.IsAlive, Is.False);
			Assert.That(second.IsAlive, Is.False);
			Assert.That(world.ActorCount, Is.EqualTo(0));
			Assert.That(world.IsDisposed, Is.True);
		}

		[Test]
		public void Dispose_RaisesDestructionEventsForEveryActor()
		{
			ActorWorld world = new();
			world.Initialize();
			world.CreateActor();
			world.CreateActor();

			int destroyedCount = 0;
			world.OnActorDestroyed += destroyed => destroyedCount++;

			world.Dispose();

			Assert.That(destroyedCount, Is.EqualTo(2));
		}

		[Test]
		public void Dispose_CalledTwice_IsIgnored()
		{
			ActorWorld world = new();
			world.Initialize();
			world.Dispose();

			Assert.DoesNotThrow(() => world.Dispose());
		}

		[Test]
		public void CreateActor_AfterDispose_Throws()
		{
			ActorWorld world = new();
			world.Initialize();
			world.Dispose();

			Assert.Throws<ObjectDisposedException>(() => world.CreateActor());
			Assert.Throws<ObjectDisposedException>(() => world.CreateQuery());
			Assert.Throws<ObjectDisposedException>(() => world.Update(1f));
		}

		[Test]
		public void GetActorsNonAlloc_ReturnsLiveActors()
		{
			ActorWorld world = new();
			world.Initialize();
			world.CreateActor();
			world.CreateActor();

			Actor[] results = new Actor[4];
			int count = world.GetActorsNonAlloc(results);

			Assert.That(count, Is.EqualTo(2));

			world.Dispose();
		}

		[Test]
		public void GetFeatureTypeBudget_ReportsRegisteredTypesWithinCapacity()
		{
			ActorWorld world = new();
			world.GetPool<Health>();

			world.GetFeatureTypeBudget(out int used, out int capacity);

			Assert.That(capacity, Is.EqualTo(FeatureMask.CAPACITY));
			Assert.That(used, Is.GreaterThan(0));
			Assert.That(used, Is.LessThanOrEqualTo(capacity));

			world.Dispose();
		}

		[Test]
		public void FeatureTypeIds_AreDistinctPerFeatureType()
		{
			Assert.That(FeatureType<Health>.Id, Is.Not.EqualTo(FeatureType<Regeneration>.Id));
			Assert.That(FeatureType<Health>.Id, Is.EqualTo(FeatureType<Health>.Id));
		}

		[Test]
		public void GetPool_ReturnsTheSameInstanceForTheSameFeature()
		{
			ActorWorld world = new();

			FeaturePool<Health> first = world.GetPool<Health>();
			FeaturePool<Health> second = world.GetPool<Health>();

			Assert.That(second, Is.SameAs(first));

			world.Dispose();
		}

		[Test]
		public void OnFeatureAdded_CarriesTheFeatureTypeId()
		{
			ActorWorld world = new();
			world.Initialize();
			int observedTypeId = -1;
			world.OnFeatureAdded += (actor, typeId) => observedTypeId = typeId;

			world.CreateActor().AddFeature<Health>();

			Assert.That(observedTypeId, Is.EqualTo(FeatureType<Health>.Id));

			world.Dispose();
		}

		[Test]
		public void Dispose_FromInsideSystemUpdate_Throws()
		{
			ActorWorld world = new();
			world.AddSystem(new WorldDisposingSystem());
			world.Initialize();

			Assert.Throws<InvalidOperationException>(() => world.Update(1f));
			Assert.That(world.IsDisposed, Is.False);

			world.Dispose();
		}

		[Test]
		public void CreateActor_FromDestroyHandlerDuringDispose_Throws()
		{
			ActorWorld world = new();
			world.Initialize();
			world.CreateActor();
			world.OnActorDestroyed += destroyed => world.CreateActor();

			Assert.Throws<ObjectDisposedException>(() => world.Dispose());
			Assert.That(world.IsDisposed, Is.True);
			Assert.That(world.ActorCount, Is.EqualTo(0));
		}

		[Test]
		public void Dispose_WhenSystemDisposeThrows_StillDisposesWorld()
		{
			List<string> journal = new();
			ActorWorld world = new();
			world.AddSystem(new RecordingSystem(journal, "first"));
			world.AddSystem(new ThrowingDisposeSystem());
			world.Initialize();
			Actor actor = world.CreateActor();

			Assert.Throws<InvalidOperationException>(() => world.Dispose());
			Assert.That(journal, Does.Contain("first:dispose"));
			Assert.That(actor.IsAlive, Is.False);
			Assert.That(world.IsDisposed, Is.True);
		}

		[Test]
		public void GetPool_AfterDispose_Throws()
		{
			ActorWorld world = new();
			world.Dispose();

			Assert.Throws<ObjectDisposedException>(() => world.GetPool<Health>());
		}

		[Test]
		public void Dispose_WhenDestroyingHandlerAddsFeature_Succeeds()
		{
			ActorWorld world = new();
			world.Initialize();
			Actor first = world.CreateActor();
			Actor second = world.CreateActor();
			world.OnActorDestroying += destroying => destroying.AddFeature<Dead>();

			Assert.DoesNotThrow(() => world.Dispose());
			Assert.That(first.IsAlive, Is.False);
			Assert.That(second.IsAlive, Is.False);
		}

		[Test]
		public void Dispose_WhenDestroyingHandlerThrows_StillDestroysEveryActor()
		{
			ActorWorld world = new();
			world.Initialize();
			Actor first = world.CreateActor();
			Actor second = world.CreateActor();
			world.OnActorDestroying += destroying => throw new InvalidOperationException("Handler failure.");

			Assert.Throws<InvalidOperationException>(() => world.Dispose());
			Assert.That(first.IsAlive, Is.False);
			Assert.That(second.IsAlive, Is.False);
			Assert.That(world.IsDisposed, Is.True);
		}

		[Test]
		public void Update_ReenteredFromSystem_Throws()
		{
			ActorWorld world = new();
			world.AddSystem(new ReentrantUpdateSystem());
			world.Initialize();

			Assert.Throws<InvalidOperationException>(() => world.Update(1f));

			world.Dispose();
		}
	}
}
