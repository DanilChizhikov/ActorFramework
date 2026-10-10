using System;
using NUnit.Framework;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class ActorActivationTests
	{
		private ActorWorld _world;
		private object _tokenA;
		private object _tokenB;

		[SetUp]
		public void Setup()
		{
			_world = new ActorWorld();
			_world.Initialize();
			_tokenA = new object();
			_tokenB = new object();
		}

		[TearDown]
		public void TearDown() => _world.Dispose();

		[Test]
		public void CreateActor_WithDefaultActive_IsActive()
		{
			Actor actor = _world.CreateActor();

			Assert.That(actor.IsActive, Is.True);
		}

		[Test]
		public void AddActivator_OnDefaultActiveActor_Throws()
		{
			Actor actor = _world.CreateActor();

			Assert.Throws<InvalidOperationException>(() => actor.AddActivator(_tokenA));
		}

		[Test]
		public void AddDeactivator_OnActiveActor_RaisesDeactivatedOnceAndNoSpuriousActivated()
		{
			Actor actor = _world.CreateActor();
			int activatedCount = 0;
			int deactivatedCount = 0;
			actor.OnActivated += activated => activatedCount++;
			actor.OnDeactivated += deactivated => deactivatedCount++;

			actor.AddDeactivator(_tokenA);

			Assert.That(activatedCount, Is.EqualTo(0), "Lazily arming the activation state must not re-announce activation.");
			Assert.That(deactivatedCount, Is.EqualTo(1));
			Assert.That(actor.IsActive, Is.False);
		}

		[Test]
		public void Deactivators_FromTwoIndependentSources_BothMustBeReleased()
		{
			Actor actor = _world.CreateActor();

			actor.AddDeactivator(_tokenA);
			actor.AddDeactivator(_tokenB);

			Assert.That(actor.DeactivatorCount, Is.EqualTo(2));
			Assert.That(actor.IsActive, Is.False);

			actor.RemoveDeactivator(_tokenA);

			Assert.That(actor.IsActive, Is.False, "A single remaining veto must keep the actor inactive.");

			actor.RemoveDeactivator(_tokenB);

			Assert.That(actor.IsActive, Is.True);
		}

		[Test]
		public void Query_ExcludesInactiveActorsByDefault()
		{
			ActorQuery query = _world.CreateQuery().With<Health>().Build();
			Actor actor = _world.CreateActor();
			actor.AddFeature<Health>();

			Assert.That(query.Count, Is.EqualTo(1));

			actor.AddDeactivator(_tokenA);

			Assert.That(query.Count, Is.EqualTo(0));

			actor.RemoveDeactivator(_tokenA);

			Assert.That(query.Count, Is.EqualTo(1));
		}

		[Test]
		public void Query_WithIncludeInactive_KeepsInactiveActors()
		{
			ActorQuery query = _world.CreateQuery().With<Health>().IncludeInactive().Build();
			Actor actor = _world.CreateActor();
			actor.AddFeature<Health>();

			actor.AddDeactivator(_tokenA);

			Assert.That(query.Count, Is.EqualTo(1));
		}

		[Test]
		public void GetDeactivatorsNonAlloc_ReturnsRegisteredTokens()
		{
			Actor actor = _world.CreateActor();
			actor.AddDeactivator(_tokenA);

			object[] results = new object[4];
			int count = actor.GetDeactivatorsNonAlloc(results);

			Assert.That(count, Is.EqualTo(1));
			Assert.That(results[0], Is.SameAs(_tokenA));
		}

		[Test]
		public void ContainsDeactivator_WithoutAnyTokenUse_ReturnsFalse()
		{
			Actor actor = _world.CreateActor();

			Assert.That(actor.ContainsDeactivator(_tokenA), Is.False);
			Assert.That(actor.DeactivatorCount, Is.EqualTo(0));
		}

		[Test]
		public void StartInactive_IsInactiveUntilActivator()
		{
			Actor actor = _world.BuildActor().StartInactive().Build();

			Assert.That(actor.IsActive, Is.False);

			actor.AddActivator(_tokenA);

			Assert.That(actor.IsActive, Is.True);
		}

		[Test]
		public void StartInactive_RemoveLastActivator_Deactivates()
		{
			Actor actor = _world.BuildActor().StartInactive().Build();
			actor.AddActivator(_tokenA);
			actor.AddActivator(_tokenB);

			actor.RemoveActivator(_tokenA);

			Assert.That(actor.IsActive, Is.True, "A remaining activator must keep the actor active.");

			actor.RemoveActivator(_tokenB);

			Assert.That(actor.IsActive, Is.False);
		}

		[Test]
		public void StartInactive_DeactivatorVetoesActivator()
		{
			Actor actor = _world.BuildActor().StartInactive().Build();
			actor.AddActivator(_tokenA);

			actor.AddDeactivator(_tokenB);

			Assert.That(actor.IsActive, Is.False);

			actor.RemoveDeactivator(_tokenB);

			Assert.That(actor.IsActive, Is.True);
		}

		[Test]
		public void DeactivatorAddedBeforeBuild_KeepsActorInactiveUntilRemoved()
		{
			ActorBuilder builder = _world.BuildActor();
			builder.Actor.AddDeactivator(_tokenA);

			Actor actor = builder.Build();

			Assert.That(actor.IsActive, Is.False);

			actor.RemoveDeactivator(_tokenA);

			Assert.That(actor.IsActive, Is.True);
		}

		[Test]
		public void StartInactive_AfterBuild_Throws()
		{
			ActorBuilder builder = _world.BuildActor();
			builder.Build();

			Assert.Throws<InvalidOperationException>(() => builder.StartInactive());
		}
	}
}
