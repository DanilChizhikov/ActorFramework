namespace DTech.ActorFramework.Tests.EditorMode
{
	internal sealed class RegenerationSystem : ActorSystem<Health, Regeneration>
	{
		protected override void Execute(in ActorSystemContext<Health, Regeneration> context)
		{
			ref Health health = ref context.Feature1;
			float next = health.Current + (context.Feature2.PerSecond * context.DeltaTime);
			health.Current = next > health.Max ? health.Max : next;
		}
	}
}
