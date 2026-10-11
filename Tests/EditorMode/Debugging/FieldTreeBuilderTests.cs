using DTech.ActorFramework.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class FieldTreeBuilderTests
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
		public void BuildRoot_FlatStruct_ProducesLeafPerField()
		{
			FieldNode root = FieldTreeBuilder.BuildRoot(new Health { Current = 3f, Max = 9f });

			Assert.That(root.Kind, Is.EqualTo(FieldKind.Composite));
			Assert.That(root.Children, Has.Length.EqualTo(2));
			Assert.That(root.Children[0].Label, Is.EqualTo("Current"));
			Assert.That(root.Children[0].Kind, Is.EqualTo(FieldKind.Float));
			Assert.That(root.Children[0].Float, Is.EqualTo(3d));
			Assert.That(root.Children[1].Float, Is.EqualTo(9d));
		}

		[Test]
		public void BuildRoot_TagFeature_HasNoChildren()
		{
			FieldNode root = FieldTreeBuilder.BuildRoot(new PlayerTag());

			Assert.That(root.Kind, Is.EqualTo(FieldKind.Composite));
			Assert.That(root.Children, Is.Empty);
		}

		[Test]
		public void BuildRoot_NestedStruct_ProducesComposite()
		{
			FieldNode offset = GetChild(BuildLoadout(), nameof(Loadout.Offset));

			Assert.That(offset.Kind, Is.EqualTo(FieldKind.Composite));
			Assert.That(offset.Children, Has.Length.EqualTo(2));
			Assert.That(offset.Children[0].Float, Is.EqualTo(1d));
			Assert.That(offset.Children[1].Float, Is.EqualTo(2d));
		}

		[Test]
		public void BuildRoot_Array_ProducesCollectionWithElements()
		{
			FieldNode slots = GetChild(BuildLoadout(), nameof(Loadout.Slots));

			Assert.That(slots.Kind, Is.EqualTo(FieldKind.Collection));
			Assert.That(slots.Integer, Is.EqualTo(3L));
			Assert.That(slots.Children, Has.Length.EqualTo(3));
			Assert.That(slots.Children[2].Kind, Is.EqualTo(FieldKind.Integer));
			Assert.That(slots.Children[2].Integer, Is.EqualTo(30L));
			Assert.That(slots.Children[2].Label, Is.EqualTo("Element 2"));
		}

		[Test]
		public void BuildRoot_NullArray_ProducesReadOnlyNull()
		{
			FieldNode root = FieldTreeBuilder.BuildRoot(new Loadout());
			FieldNode slots = root.Children[DebugTestUtility.IndexOfField(typeof(Loadout), nameof(Loadout.Slots))];

			Assert.That(slots.Kind, Is.EqualTo(FieldKind.Null));
			Assert.That(slots.IsReadOnly, Is.True);
		}

		[Test]
		public void BuildRoot_Enum_ProducesNamesAndValue()
		{
			FieldNode mode = GetChild(BuildLoadout(), nameof(Loadout.Mode));

			Assert.That(mode.Kind, Is.EqualTo(FieldKind.Enum));
			Assert.That(mode.Integer, Is.EqualTo((long)LoadoutMode.Stealth));
			Assert.That(mode.EnumNames, Is.EqualTo(new[] { "Idle", "Combat", "Stealth" }));
			Assert.That(mode.EnumValues, Is.EqualTo(new[] { 0L, 1L, 2L }));
		}

		[Test]
		public void BuildRoot_AliveActor_ProducesActorRef()
		{
			Actor target = _world.CreateActor();
			FieldNode node = GetChild(FieldTreeBuilder.BuildRoot(new Loadout { Target = target }), nameof(Loadout.Target));

			Assert.That(node.Kind, Is.EqualTo(FieldKind.ActorRef));
			Assert.That(node.Integer, Is.EqualTo((long)target.Id));
			Assert.That(node.IsReadOnly, Is.True);
		}

		[Test]
		public void BuildRoot_DestroyedActor_ProducesEmptyActorRef()
		{
			Actor target = _world.CreateActor();
			target.Destroy();
			FieldNode node = GetChild(FieldTreeBuilder.BuildRoot(new Loadout { Target = target }), nameof(Loadout.Target));

			Assert.That(node.Integer, Is.EqualTo(-1L));
			Assert.That(node.Text, Does.Contain("destroyed"));
		}

		[Test]
		public void BuildRoot_Vector3_ProducesComponents()
		{
			FieldNode direction = GetChild(BuildLoadout(), nameof(Loadout.Direction));

			Assert.That(direction.Kind, Is.EqualTo(FieldKind.Vector3));
			Assert.That(direction.Components, Is.EqualTo(new[] { 0d, 1d, 0d }));
		}

		[Test]
		public void BuildRoot_ReadOnlyField_IsFlaggedReadOnly()
		{
			FieldNode root = FieldTreeBuilder.BuildRoot(new VisibilityProbe(7, 1, 2));
			FieldNode node = root.Children[DebugTestUtility.IndexOfField(typeof(VisibilityProbe),
				nameof(VisibilityProbe.ReadOnlyValue))];

			Assert.That(node.IsReadOnly, Is.True);
			Assert.That(node.Integer, Is.EqualTo(7L));
		}

		private static FieldNode BuildLoadout()
		{
			return FieldTreeBuilder.BuildRoot(new Loadout
			{
				Offset = new Position { X = 1f, Y = 2f },
				Slots = new[] { 10, 20, 30 },
				Mode = LoadoutMode.Stealth,
				Title = "Main",
				Direction = Vector3.up,
			});
		}

		private static FieldNode GetChild(FieldNode root, string fieldName)
		{
			return root.Children[DebugTestUtility.IndexOfField(typeof(Loadout), fieldName)];
		}
	}
}
