namespace DTech.ActorFramework
{
	public abstract class ActorSystem<TFeature1, TFeature2> : ActorSystemBase, IUpdateSystem
		where TFeature1 : struct, IActorFeature
		where TFeature2 : struct, IActorFeature
	{
		private FeaturePool<TFeature1> _pool1;
		private FeaturePool<TFeature2> _pool2;

		public override void Initialize(ActorWorld world)
		{
			base.Initialize(world);
			_pool1 = world.GetPool<TFeature1>();
			_pool2 = world.GetPool<TFeature2>();
		}

		public virtual void Update(float deltaTime)
		{
			OnBeforeUpdate(deltaTime);

			ActorQuery query = Query;
			for (int i = query.Count - 1; i >= 0; i--)
			{
				Actor actor = query.GetActorAt(i);
				if (!query.IsMatching(actor))
				{
					continue;
				}

				Execute(new ActorSystemContext<TFeature1, TFeature2>(actor, deltaTime, _pool1, _pool2));
			}

			OnAfterUpdate(deltaTime);
		}

		public override void Dispose()
		{
			_pool1 = null;
			_pool2 = null;
			base.Dispose();
		}

		protected override void ConfigureQuery(ActorQueryBuilder builder)
		{
			builder.With<TFeature1, TFeature2>();
		}

		protected abstract void Execute(in ActorSystemContext<TFeature1, TFeature2> context);

		protected virtual void OnBeforeUpdate(float deltaTime)
		{
		}

		protected virtual void OnAfterUpdate(float deltaTime)
		{
		}
	}
}