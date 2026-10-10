namespace DTech.ActorFramework.Tests.EditorMode
{
	internal sealed class RemoveFeatureTargetSystem : ActorSystem<Health>
	{
		public Actor Target { get; set; }
		public int ExecuteCount { get; private set; }

		protected override void Execute(in ActorSystemContext<Health> context)
		{
			ExecuteCount++;

			if (Target == null || context.Actor == Target || !Target.HasFeature<Health>())
			{
				return;
			}

			Target.RemoveFeature<Health>();
		}
	}
}
