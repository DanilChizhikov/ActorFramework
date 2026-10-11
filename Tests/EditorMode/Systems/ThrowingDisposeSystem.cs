using System;

namespace DTech.ActorFramework.Tests.EditorMode
{
	internal sealed class ThrowingDisposeSystem : IActorSystem
	{
		public void Initialize(ActorWorld world)
		{
		}

		public void Dispose() => throw new InvalidOperationException("Dispose failure.");
	}
}
