namespace DTech.ActorFramework.Tests.EditorMode
{
	internal sealed class SystemRemovingSystem : IUpdateSystem
	{
		private ActorWorld _world;

		public IActorSystem SystemToRemove { get; set; }

		public void Initialize(ActorWorld world) => _world = world;

		public void Update(float deltaTime)
		{
			if (SystemToRemove == null)
			{
				return;
			}

			_world.RemoveSystem(SystemToRemove);
			SystemToRemove = null;
		}

		public void Dispose() => _world = null;
	}
}
