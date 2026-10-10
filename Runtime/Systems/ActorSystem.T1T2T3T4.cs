namespace DTech.ActorFramework
{
	public abstract class ActorSystem<TFeature1, TFeature2, TFeature3, TFeature4> : ActorSystemBase, IUpdateSystem
		where TFeature1 : struct, IActorFeature
		where TFeature2 : struct, IActorFeature
		where TFeature3 : struct, IActorFeature
		where TFeature4 : struct, IActorFeature
	{
		private FeaturePool<TFeature1> _pool1;
		private FeaturePool<TFeature2> _pool2;
		private FeaturePool<TFeature3> _pool3;
		private FeaturePool<TFeature4> _pool4;

		public override void Initialize(ActorWorld world)
		{
			base.Initialize(world);
			_pool1 = world.GetPool<TFeature1>();
			_pool2 = world.GetPool<TFeature2>();
			_pool3 = world.GetPool<TFeature3>();
			_pool4 = world.GetPool<TFeature4>();
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

				Execute(new ActorSystemContext<TFeature1, TFeature2, TFeature3, TFeature4>(
					actor,
					deltaTime,
					_pool1,
					_pool2,
					_pool3,
					_pool4));
			}

			OnAfterUpdate(deltaTime);
		}

		public override void Dispose()
		{
			_pool1 = null;
			_pool2 = null;
			_pool3 = null;
			_pool4 = null;
			base.Dispose();
		}

		protected override void ConfigureQuery(ActorQueryBuilder builder)
		{
			builder.With<TFeature1, TFeature2, TFeature3>();
			builder.With<TFeature4>();
		}

		protected abstract void Execute(in ActorSystemContext<TFeature1, TFeature2, TFeature3, TFeature4> context);

		protected virtual void OnBeforeUpdate(float deltaTime)
		{
		}

		protected virtual void OnAfterUpdate(float deltaTime)
		{
		}
	}
}