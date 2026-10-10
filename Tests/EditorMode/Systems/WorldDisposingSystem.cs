namespace DTech.ActorFramework.Tests.EditorMode
{
	internal sealed class WorldDisposingSystem : IUpdateSystem
	{
		private ActorWorld _world;

		public void Initialize(ActorWorld world) => _world = world;

		public void Update(float deltaTime) => _world.Dispose();

		public void Dispose() => _world = null;
	}
}
