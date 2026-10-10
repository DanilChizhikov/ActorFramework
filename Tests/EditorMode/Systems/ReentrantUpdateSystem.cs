namespace DTech.ActorFramework.Tests.EditorMode
{
	internal sealed class ReentrantUpdateSystem : IUpdateSystem
	{
		private ActorWorld _world;

		public void Initialize(ActorWorld world) => _world = world;

		public void Update(float deltaTime) => _world.Update(deltaTime);

		public void Dispose() => _world = null;
	}
}
