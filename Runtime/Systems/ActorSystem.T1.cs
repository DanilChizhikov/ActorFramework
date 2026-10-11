namespace DTech.ActorFramework
{
	/// <summary>
	/// Typed tier: one virtual call per actor per frame, and no lookup work beyond a sparse index.
	/// Override ConfigureQuery and call base to narrow the query further.
	/// </summary>
	public abstract class ActorSystem<TFeature1> : ActorSystemBase, IUpdateSystem
		where TFeature1 : struct, IActorFeature
	{
		private FeaturePool<TFeature1> _pool1;

		public override void Initialize(ActorWorld world)
		{
			base.Initialize(world);
			_pool1 = world.GetPool<TFeature1>();
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

				Execute(new ActorSystemContext<TFeature1>(actor, deltaTime, _pool1));
			}

			OnAfterUpdate(deltaTime);
		}

		public override void Dispose()
		{
			_pool1 = null;
			base.Dispose();
		}

		protected override void ConfigureQuery(ActorQueryBuilder builder)
		{
			builder.With<TFeature1>();
		}

		protected abstract void Execute(in ActorSystemContext<TFeature1> context);

		protected virtual void OnBeforeUpdate(float deltaTime)
		{
		}

		protected virtual void OnAfterUpdate(float deltaTime)
		{
		}
	}
}