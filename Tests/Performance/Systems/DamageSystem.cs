namespace DTech.ActorFramework.Tests.Performance
{
	internal sealed class DamageSystem : ActorSystem<Health, Damage, Lifetime>
	{
		protected override void Execute(in ActorSystemContext<Health, Damage, Lifetime> context)
		{
			float deltaTime = context.DeltaTime;
			context.Feature1.Current -= context.Feature2.Amount * deltaTime;
			context.Feature3.Remaining -= deltaTime;
		}
	}
}
