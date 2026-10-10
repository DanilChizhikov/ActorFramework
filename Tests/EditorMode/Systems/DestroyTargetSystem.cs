namespace DTech.ActorFramework.Tests.EditorMode
{
	internal sealed class DestroyTargetSystem : ActorSystem<Health>
	{
		public Actor Target { get; set; }
		public int ExecuteCount { get; private set; }

		protected override void Execute(in ActorSystemContext<Health> context)
		{
			ExecuteCount++;

			if (Target == null || !Target.IsAlive)
			{
				return;
			}

			Target.Destroy();
		}
	}
}
