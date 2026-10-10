using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class ActorLifetimeTests
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
		public void CreateActor_ProducesAliveActiveActor()
		{
			Actor actor = _world.CreateActor();

			Assert.That(actor.IsAlive, Is.True);
			Assert.That(actor.IsActive, Is.True);
			Assert.That(actor.FeatureCount, Is.EqualTo(0));
			Assert.That(_world.ActorCount, Is.EqualTo(1));
		}

		[Test]
		public void AddFeature_ThenHasAndGetFeature_ReturnsValue()
		{
			Actor actor = _world.CreateActor();

			actor.AddFeature(new Health { Current = 50f, Max = 100f });

			Assert.That(actor.HasFeature<Health>(), Is.True);
			Assert.That(actor.FeatureCount, Is.EqualTo(1));
			Assert.That(actor.GetFeature<Health>().Current, Is.EqualTo(50f));
		}

		[Test]
		public void GetFeature_WritesThroughReference()
		{
			Actor actor = _world.CreateActor();
			actor.AddFeature(new Health { Current = 50f, Max = 100f });

			ref Health health = ref actor.GetFeature<Health>();
			health.Current = 1f;

			Assert.That(actor.GetFeature<Health>().Current, Is.EqualTo(1f));
		}

		[Test]
		public void AddFeature_WhenAlreadyPresent_Throws()
		{
			Actor actor = _world.CreateActor();
			actor.AddFeature<Health>();

			Assert.Throws<InvalidOperationException>(() => actor.AddFeature<Health>());
		}

		[Test]
		public void GetFeature_WhenAbsent_Throws()
		{
			Actor actor = _world.CreateActor();

			Assert.Throws<KeyNotFoundException>(() => actor.GetFeature<Health>());
		}

		[Test]
		public void TryGetFeature_WhenAbsent_ReturnsFalse()
		{
			Actor actor = _world.CreateActor();

			Assert.That(actor.TryGetFeature(out Health _), Is.False);
		}

		[Test]
		public void GetOrAddFeature_WhenAbsent_AddsDefault()
		{
			Actor actor = _world.CreateActor();

			ref Health health = ref actor.GetOrAddFeature<Health>();
			health.Max = 42f;

			Assert.That(actor.FeatureCount, Is.EqualTo(1));
			Assert.That(actor.GetFeature<Health>().Max, Is.EqualTo(42f));
		}

		[Test]
		public void RemoveFeature_DropsItAndReportsTrue()
		{
			Actor actor = _world.CreateActor();
			actor.AddFeature<Health>();

			bool removed = actor.RemoveFeature<Health>();

			Assert.That(removed, Is.True);
			Assert.That(actor.HasFeature<Health>(), Is.False);
			Assert.That(actor.FeatureCount, Is.EqualTo(0));
		}

		[Test]
		public void RemoveFeature_WhenAbsent_ReturnsFalse()
		{
			Actor actor = _world.CreateActor();

			Assert.That(actor.RemoveFeature<Health>(), Is.False);
		}

		[Test]
		public void Destroy_MarksActorNotAliveAndDropsItFromWorld()
		{
			Actor actor = _world.CreateActor();
			actor.AddFeature<Health>();

			actor.Destroy();

			Assert.That(actor.IsAlive, Is.False);
			Assert.That(_world.ActorCount, Is.EqualTo(0));
			Assert.That(_world.TryGetActor(actor.Id, out Actor _), Is.False);
		}

		[Test]
		public void Destroy_ThenMutatingMembers_Throw()
		{
			Actor actor = _world.CreateActor();
			actor.Destroy();

			Assert.Throws<InvalidOperationException>(() => actor.AddFeature<Health>());
			Assert.Throws<InvalidOperationException>(() => actor.RemoveFeature<Health>());
			Assert.Throws<InvalidOperationException>(() => actor.AddActivator(this));
		}

		[Test]
		public void Destroy_CalledTwice_ReturnsFalseTheSecondTime()
		{
			Actor actor = _world.CreateActor();

			Assert.That(_world.DestroyActor(actor), Is.True);
			Assert.That(_world.DestroyActor(actor), Is.False);
		}

		[Test]
		public void Destroy_RaisesActorEventsInOrder()
		{
			List<string> journal = new();
			Actor actor = _world.CreateActor();
			actor.AddFeature<Health>();

			_world.OnActorDestroying += destroyed => journal.Add("destroying");
			_world.OnFeatureRemoved += (destroyed, typeId) => journal.Add("feature-removed");
			actor.OnDestroyed += destroyed => journal.Add("actor-destroyed");
			_world.OnActorDestroyed += destroyed => journal.Add("world-destroyed");

			actor.Destroy();

			Assert.That(journal, Is.EqualTo(new[] { "destroying", "feature-removed", "actor-destroyed", "world-destroyed" }));
		}

		[Test]
		public void CreateActor_AfterDestroy_ReusesIdButNotTheStaleReference()
		{
			Actor first = _world.CreateActor();
			int reusedId = first.Id;
			first.Destroy();

			Actor second = _world.CreateActor();

			Assert.That(second.Id, Is.EqualTo(reusedId));
			Assert.That(second, Is.Not.SameAs(first));
			Assert.That(first.IsAlive, Is.False);
			Assert.That(second.IsAlive, Is.True);
		}

		[Test]
		public void DestroyActor_FromAnotherWorld_Throws()
		{
			ActorWorld other = new();
			Actor actor = other.CreateActor();

			Assert.Throws<InvalidOperationException>(() => _world.DestroyActor(actor));

			other.Dispose();
		}
	}
}
