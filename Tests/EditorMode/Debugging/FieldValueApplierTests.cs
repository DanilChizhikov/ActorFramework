using DTech.ActorFramework.Diagnostics;
using NUnit.Framework;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class FieldValueApplierTests
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
		public void TryApply_TopLevelField_WritesFeature()
		{
			Actor actor = _world.BuildActor().With(new Health { Current = 1f, Max = 10f }).Build();
			SetFieldRequest request = CreateRequest<Health>(actor,
				new FieldNode { Kind = FieldKind.Float, Float = 42d },
				DebugTestUtility.IndexOfField(typeof(Health), nameof(Health.Current)));

			Assert.That(FieldValueApplier.TryApply(request), Is.True);
			Assert.That(actor.GetFeature<Health>().Current, Is.EqualTo(42f));
			Assert.That(actor.GetFeature<Health>().Max, Is.EqualTo(10f));
		}

		[Test]
		public void TryApply_NestedStructField_WritesBackThroughParent()
		{
			Actor actor = _world.BuildActor().With(new Loadout { Offset = new Position { X = 1f, Y = 2f } }).Build();
			SetFieldRequest request = CreateRequest<Loadout>(actor,
				new FieldNode { Kind = FieldKind.Float, Float = 5d },
				DebugTestUtility.IndexOfField(typeof(Loadout), nameof(Loadout.Offset)),
				DebugTestUtility.IndexOfField(typeof(Position), nameof(Position.Y)));

			Assert.That(FieldValueApplier.TryApply(request), Is.True);
			Assert.That(actor.GetFeature<Loadout>().Offset.X, Is.EqualTo(1f));
			Assert.That(actor.GetFeature<Loadout>().Offset.Y, Is.EqualTo(5f));
		}

		[Test]
		public void TryApply_ArrayElement_WritesElement()
		{
			Actor actor = _world.BuildActor().With(new Loadout { Slots = new[] { 1, 2, 3 } }).Build();
			SetFieldRequest request = CreateRequest<Loadout>(actor,
				new FieldNode { Kind = FieldKind.Integer, Integer = 7L },
				DebugTestUtility.IndexOfField(typeof(Loadout), nameof(Loadout.Slots)), 1);

			Assert.That(FieldValueApplier.TryApply(request), Is.True);
			Assert.That(actor.GetFeature<Loadout>().Slots, Is.EqualTo(new[] { 1, 7, 3 }));
		}

		[Test]
		public void TryApply_Enum_WritesEnumValue()
		{
			Actor actor = _world.BuildActor().With(new Loadout()).Build();
			SetFieldRequest request = CreateRequest<Loadout>(actor,
				new FieldNode { Kind = FieldKind.Enum, Integer = (long)LoadoutMode.Combat },
				DebugTestUtility.IndexOfField(typeof(Loadout), nameof(Loadout.Mode)));

			Assert.That(FieldValueApplier.TryApply(request), Is.True);
			Assert.That(actor.GetFeature<Loadout>().Mode, Is.EqualTo(LoadoutMode.Combat));
		}

		[Test]
		public void TryApply_PrivateShownField_WritesField()
		{
			Actor actor = _world.BuildActor().With(new VisibilityProbe(0, 1, 2)).Build();
			SetFieldRequest request = CreateRequest<VisibilityProbe>(actor,
				new FieldNode { Kind = FieldKind.Integer, Integer = 11L },
				DebugTestUtility.IndexOfField(typeof(VisibilityProbe), "_shownPrivate"));

			Assert.That(FieldValueApplier.TryApply(request), Is.True);
			Assert.That(actor.GetFeature<VisibilityProbe>().ShownPrivate, Is.EqualTo(11));
		}

		[Test]
		public void TryApply_ReadOnlyField_ReturnsFalse()
		{
			Actor actor = _world.BuildActor().With(new VisibilityProbe(3, 0, 0)).Build();
			SetFieldRequest request = CreateRequest<VisibilityProbe>(actor,
				new FieldNode { Kind = FieldKind.Integer, Integer = 9L },
				DebugTestUtility.IndexOfField(typeof(VisibilityProbe), nameof(VisibilityProbe.ReadOnlyValue)));

			Assert.That(FieldValueApplier.TryApply(request), Is.False);
			Assert.That(actor.GetFeature<VisibilityProbe>().ReadOnlyValue, Is.EqualTo(3));
		}

		[Test]
		public void TryApply_DestroyedActor_ReturnsFalse()
		{
			Actor actor = _world.BuildActor().With(new Health()).Build();
			SetFieldRequest request = CreateRequest<Health>(actor,
				new FieldNode { Kind = FieldKind.Float, Float = 1d }, 0);
			actor.Destroy();

			Assert.That(FieldValueApplier.TryApply(request), Is.False);
		}

		[Test]
		public void TryApply_OutOfRangePath_ReturnsFalse()
		{
			Actor actor = _world.BuildActor().With(new Health()).Build();
			SetFieldRequest request = CreateRequest<Health>(actor,
				new FieldNode { Kind = FieldKind.Float, Float = 1d }, 99);

			Assert.That(FieldValueApplier.TryApply(request), Is.False);
		}

		private SetFieldRequest CreateRequest<TFeature>(Actor actor, FieldNode value, params int[] path)
			where TFeature : struct, IActorFeature
		{
			return new SetFieldRequest
			{
				WorldId = _world.DebugId,
				ActorId = actor.Id,
				FeatureTypeId = FeatureType<TFeature>.Id,
				Path = path,
				Value = value,
			};
		}
	}
}
