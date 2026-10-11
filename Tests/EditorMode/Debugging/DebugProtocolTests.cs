using DTech.ActorFramework.Diagnostics;
using NUnit.Framework;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class DebugProtocolTests
	{
		[Test]
		public void PollRequest_RoundTrips()
		{
			var request = new PollRequest
			{
				WorldId = 3,
				KnownVersion = 17,
				KnownTypeCount = 5,
				QueryIndex = 2,
				SelectedActorId = 41,
			};

			PollRequest copy = PollRequest.FromBytes(request.ToBytes());

			Assert.That(copy.WorldId, Is.EqualTo(3));
			Assert.That(copy.KnownVersion, Is.EqualTo(17));
			Assert.That(copy.KnownTypeCount, Is.EqualTo(5));
			Assert.That(copy.QueryIndex, Is.EqualTo(2));
			Assert.That(copy.SelectedActorId, Is.EqualTo(41));
		}

		[Test]
		public void PollResponse_RoundTrips()
		{
			var world = new ActorWorld("Protocol World");
			Actor target = world.CreateActor();
			Actor actor = world.BuildActor()
				.With(new Loadout { Slots = new[] { 1, 2 }, Mode = LoadoutMode.Combat, Target = target, Title = "Main" })
				.Build();
			world.CreateQuery().With<Loadout>().Build();
			PollResponse response = DebugSnapshotBuilder.Build(new PollRequest
			{
				WorldId = world.DebugId,
				SelectedActorId = actor.Id,
			});

			PollResponse copy = PollResponse.FromBytes(response.ToBytes());
			world.Dispose();

			Assert.That(copy.SessionId, Is.EqualTo(response.SessionId));
			Assert.That(copy.Worlds, Has.Length.EqualTo(response.Worlds.Length));
			Assert.That(copy.NewTypes, Has.Length.EqualTo(response.NewTypes.Length));
			Assert.That(copy.World.WorldId, Is.EqualTo(response.World.WorldId));
			Assert.That(copy.World.Actors, Has.Length.EqualTo(2));
			Assert.That(copy.World.Queries, Has.Length.EqualTo(response.World.Queries.Length));
			Assert.That(copy.Actor.Id, Is.EqualTo(actor.Id));

			FieldNode expected = response.Actor.Features[0].Root;
			FieldNode actual = copy.Actor.Features[0].Root;
			AssertNodesEqual(actual, expected);
		}

		[Test]
		public void SetFieldRequest_RoundTrips()
		{
			var request = new SetFieldRequest
			{
				WorldId = 1,
				ActorId = 2,
				FeatureTypeId = 3,
				Path = new[] { 0, 4 },
				Value = new FieldNode { Kind = FieldKind.Vector3, Components = new[] { 1d, 2d, 3d } },
			};

			SetFieldRequest copy = SetFieldRequest.FromBytes(request.ToBytes());

			Assert.That(copy.WorldId, Is.EqualTo(1));
			Assert.That(copy.ActorId, Is.EqualTo(2));
			Assert.That(copy.FeatureTypeId, Is.EqualTo(3));
			Assert.That(copy.Path, Is.EqualTo(new[] { 0, 4 }));
			Assert.That(copy.Value.Kind, Is.EqualTo(FieldKind.Vector3));
			Assert.That(copy.Value.Components, Is.EqualTo(new[] { 1d, 2d, 3d }));
		}

		private static void AssertNodesEqual(FieldNode actual, FieldNode expected)
		{
			Assert.That(actual.Kind, Is.EqualTo(expected.Kind));
			Assert.That(actual.Flags, Is.EqualTo(expected.Flags));
			Assert.That(actual.Label, Is.EqualTo(expected.Label));
			Assert.That(actual.TypeName, Is.EqualTo(expected.TypeName));
			Assert.That(actual.Integer, Is.EqualTo(expected.Integer));
			Assert.That(actual.Float, Is.EqualTo(expected.Float));
			Assert.That(actual.Text, Is.EqualTo(expected.Text));
			Assert.That(actual.Components, Is.EqualTo(expected.Components));
			Assert.That(actual.EnumNames, Is.EqualTo(expected.EnumNames));
			Assert.That(actual.EnumValues, Is.EqualTo(expected.EnumValues));
			Assert.That(actual.Children?.Length, Is.EqualTo(expected.Children?.Length));
			for (int i = 0; i < (expected.Children?.Length ?? 0); i++)
			{
				AssertNodesEqual(actual.Children[i], expected.Children[i]);
			}
		}
	}
}
