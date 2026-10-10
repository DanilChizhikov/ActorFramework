namespace DTech.ActorFramework.Tests.EditorMode
{
	internal sealed class CountingSystem : ActorSystem<Health>
	{
		public int ExecuteCount { get; private set; }
		public int LastQueryCount { get; private set; }

		protected override void OnBeforeUpdate(float deltaTime)
		{
			LastQueryCount = Query.Count;
		}

		protected override void Execute(in ActorSystemContext<Health> context)
		{
			ExecuteCount++;
		}
	}
}
