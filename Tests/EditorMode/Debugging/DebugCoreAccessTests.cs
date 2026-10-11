using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class DebugCoreAccessTests
	{
		private ActorWorld _world;

		[SetUp]
		public void Setup()
		{
			_world = new ActorWorld();
		}

		[TearDown]
		public void TearDown()
		{
			_world.Dispose();
		}

		[Test]
		public void GetBoxed_ReturnsFeatureValue()
		{
			Actor actor = _world.BuildActor().With(new Health { Current = 3f, Max = 9f }).Build();
			_world.Features.TryGetPool(FeatureType<Health>.Id, out IFeaturePool pool);

			var health = (Health)pool.GetBoxed(actor.Id);

			Assert.That(health.Current, Is.EqualTo(3f));
			Assert.That(health.Max, Is.EqualTo(9f));
		}

		[Test]
		public void SetBoxed_WritesFeatureValue()
		{
			Actor actor = _world.BuildActor().With(new Health { Current = 3f }).Build();
			_world.Features.TryGetPool(FeatureType<Health>.Id, out IFeaturePool pool);

			pool.SetBoxed(actor.Id, new Health { Current = 42f, Max = 50f });

			Assert.That(actor.GetFeature<Health>().Current, Is.EqualTo(42f));
			Assert.That(actor.GetFeature<Health>().Max, Is.EqualTo(50f));
		}

		[Test]
		public void SetBoxed_WithForeignType_Throws()
		{
			Actor actor = _world.BuildActor().With(new Health()).Build();
			_world.Features.TryGetPool(FeatureType<Health>.Id, out IFeaturePool pool);

			Assert.Throws<InvalidCastException>(() => pool.SetBoxed(actor.Id, new Position()));
		}

		[Test]
		public void GetBoxed_WithAbsentActor_Throws()
		{
			Actor actor = _world.BuildActor().With(new Health()).Build();
			Actor other = _world.CreateActor();
			_world.Features.TryGetPool(FeatureType<Health>.Id, out IFeaturePool pool);

			Assert.That(pool.Has(actor.Id), Is.True);
			Assert.Throws<KeyNotFoundException>(() => pool.GetBoxed(other.Id));
			Assert.Throws<KeyNotFoundException>(() => pool.SetBoxed(other.Id, new Health()));
		}

		[Test]
		public void TryGetPool_WithUnusedTypeId_ReturnsFalse()
		{
			Assert.That(_world.Features.TryGetPool(FeatureMask.CAPACITY - 1, out IFeaturePool pool), Is.False);
			Assert.That(pool, Is.Null);
			Assert.That(_world.Features.TryGetPool(-1, out _), Is.False);
		}

		[Test]
		public void StructureVersion_ChangesOnStructuralChanges()
		{
			int version = _world.StructureVersion;
			Actor actor = _world.CreateActor();
			Assert.That(_world.StructureVersion, Is.Not.EqualTo(version), "create");

			version = _world.StructureVersion;
			actor.AddFeature(new Health());
			Assert.That(_world.StructureVersion, Is.Not.EqualTo(version), "add feature");

			version = _world.StructureVersion;
			actor.RemoveFeature<Health>();
			Assert.That(_world.StructureVersion, Is.Not.EqualTo(version), "remove feature");

			version = _world.StructureVersion;
			var token = new object();
			actor.AddDeactivator(token);
			Assert.That(_world.StructureVersion, Is.Not.EqualTo(version), "deactivate");

			version = _world.StructureVersion;
			actor.Destroy();
			Assert.That(_world.StructureVersion, Is.Not.EqualTo(version), "destroy");
		}

		[Test]
		public void StructureVersion_DoesNotChangeOnReadsAndValueWrites()
		{
			Actor actor = _world.BuildActor().With(new Health()).Build();
			int version = _world.StructureVersion;

			actor.GetFeature<Health>().Current = 5f;
			actor.TryGetFeature(out Health _);
			_world.GetActorsNonAlloc(new Actor[4]);

			Assert.That(_world.StructureVersion, Is.EqualTo(version));
		}

		[Test]
		public void Registries_ExposeSystemsAndQueries()
		{
			var system = new CountingSystem();
			_world.AddSystem(system);
			ActorQuery query = _world.CreateQuery().With<Health>().Without<Dead>().Build();

			Assert.That(_world.Systems.GetAt(0), Is.SameAs(system));
			Assert.That(_world.Queries.GetAt(DebugTestUtility.IndexOfQuery(_world, query)), Is.SameAs(query));
			Assert.That(query.All.Has(FeatureType<Health>.Id), Is.True);
			Assert.That(query.None.Has(FeatureType<Dead>.Id), Is.True);
			Assert.That(query.HasAny, Is.False);
		}
	}
}
