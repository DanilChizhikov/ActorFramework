namespace DTech.ActorFramework.Tests.EditorMode
{
	internal sealed class SystemAddingSystem : IUpdateSystem
	{
		private ActorWorld _world;

		public IActorSystem SystemToAdd { get; set; }

		public void Initialize(ActorWorld world) => _world = world;

		public void Update(float deltaTime)
		{
			if (SystemToAdd == null)
			{
				return;
			}

			_world.AddSystem(SystemToAdd);
			SystemToAdd = null;
		}

		public void Dispose() => _world = null;
	}
}
