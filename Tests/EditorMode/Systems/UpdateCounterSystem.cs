namespace DTech.ActorFramework.Tests.EditorMode
{
	internal sealed class UpdateCounterSystem : IUpdateSystem
	{
		public int UpdateCount { get; private set; }

		public void Initialize(ActorWorld world)
		{
		}

		public void Update(float deltaTime) => UpdateCount++;

		public void Dispose()
		{
		}
	}
}
