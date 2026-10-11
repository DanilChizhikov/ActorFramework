namespace DTech.ActorFramework.Tests.EditorMode
{
	internal sealed class SpawningSystem : ActorSystem<Health>
	{
		public int ExecuteCount { get; private set; }

		protected override void Execute(in ActorSystemContext<Health> context)
		{
			ExecuteCount++;
			World.BuildActor().With(new Health { Max = 1f }).Build();
		}
	}
}
