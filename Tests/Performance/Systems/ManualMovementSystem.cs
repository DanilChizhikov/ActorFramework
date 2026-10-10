namespace DTech.ActorFramework.Tests.Performance
{
	internal sealed class ManualMovementSystem : ActorSystemBase, IUpdateSystem
	{
		private FeaturePool<Position> _positions;
		private FeaturePool<Velocity> _velocities;

		public override void Initialize(ActorWorld world)
		{
			base.Initialize(world);
			_positions = world.GetPool<Position>();
			_velocities = world.GetPool<Velocity>();
		}

		public void Update(float deltaTime)
		{
			for (int i = Query.Count - 1; i >= 0; i--)
			{
				int actorId = Query.GetActorAt(i).Id;
				ref Position position = ref _positions.GetUnchecked(actorId);
				ref Velocity velocity = ref _velocities.GetUnchecked(actorId);
				position.X += velocity.X * deltaTime;
				position.Y += velocity.Y * deltaTime;
				position.Z += velocity.Z * deltaTime;
			}
		}

		protected override void ConfigureQuery(ActorQueryBuilder builder)
		{
			builder.With<Position, Velocity>();
		}
	}
}
