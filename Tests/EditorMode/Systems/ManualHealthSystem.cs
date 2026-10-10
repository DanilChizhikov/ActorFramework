namespace DTech.ActorFramework.Tests.EditorMode
{
	internal sealed class ManualHealthSystem : ActorSystemBase, IUpdateSystem
	{
		private FeaturePool<Health> _pool;

		public int ExecuteCount { get; private set; }

		public override void Initialize(ActorWorld world)
		{
			base.Initialize(world);
			_pool = world.GetPool<Health>();
		}

		public void Update(float deltaTime)
		{
			for (int i = Query.Count - 1; i >= 0; i--)
			{
				Actor actor = Query.GetActorAt(i);
				if (!Query.IsMatching(actor))
				{
					continue;
				}

				ref Health health = ref _pool.GetUnchecked(actor.Id);
				health.Current += deltaTime;
				ExecuteCount++;
			}
		}

		protected override void ConfigureQuery(ActorQueryBuilder builder)
		{
			builder.With<Health>();
		}
	}
}
