namespace DTech.ActorFramework.Tests.EditorMode
{
	internal sealed class SelfDestroyingSystem : ActorSystem<Health>
	{
		protected override void Execute(in ActorSystemContext<Health> context)
		{
			context.Actor.Destroy();
		}
	}
}
