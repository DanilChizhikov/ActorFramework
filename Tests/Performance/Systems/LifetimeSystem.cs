namespace DTech.ActorFramework.Tests.Performance
{
	internal sealed class LifetimeSystem : ActorSystem<Lifetime>
	{
		protected override void Execute(in ActorSystemContext<Lifetime> context)
		{
			context.Feature1.Remaining -= context.DeltaTime;
		}
	}
}
