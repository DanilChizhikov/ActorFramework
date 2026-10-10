namespace DTech.ActorFramework.Tests.Performance
{
	internal sealed class MovementSystem : ActorSystem<Position, Velocity>
	{
		protected override void Execute(in ActorSystemContext<Position, Velocity> context)
		{
			ref Position position = ref context.Feature1;
			ref Velocity velocity = ref context.Feature2;
			float deltaTime = context.DeltaTime;
			position.X += velocity.X * deltaTime;
			position.Y += velocity.Y * deltaTime;
			position.Z += velocity.Z * deltaTime;
		}
	}
}
