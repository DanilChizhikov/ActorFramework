using System.Collections.Generic;
using NUnit.Framework;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class ActorWorldTrackerTests
	{
		[Test]
		public void Constructor_RegistersWorld()
		{
			var world = new ActorWorld();
			var worlds = new List<ActorWorld>();

			ActorWorldTracker.GetWorlds(worlds);

			Assert.That(worlds, Has.Member(world));
			Assert.That(ActorWorldTracker.TryGet(world.DebugId, out ActorWorld found), Is.True);
			Assert.That(found, Is.SameAs(world));

			world.Dispose();
		}

		[Test]
		public void Dispose_UnregistersWorld()
		{
			var world = new ActorWorld();
			var worlds = new List<ActorWorld>();

			world.Dispose();
			ActorWorldTracker.GetWorlds(worlds);

			Assert.That(worlds, Has.No.Member(world));
			Assert.That(ActorWorldTracker.TryGet(world.DebugId, out _), Is.False);
		}

		[Test]
		public void Name_WithoutName_UsesNumberedDefault()
		{
			var world = new ActorWorld();

			Assert.That(world.Name, Is.EqualTo($"ActorWorld #{world.DebugId}"));
			Assert.That(world.ToString(), Is.EqualTo(world.Name));

			world.Dispose();
		}

		[Test]
		public void Name_WithName_KeepsIt()
		{
			var world = new ActorWorld("Gameplay");

			Assert.That(world.Name, Is.EqualTo("Gameplay"));

			world.Dispose();
		}

		[Test]
		public void DebugId_IsUniquePerWorld()
		{
			var first = new ActorWorld();
			var second = new ActorWorld();

			Assert.That(first.DebugId, Is.Not.EqualTo(second.DebugId));

			first.Dispose();
			second.Dispose();
		}
	}
}
