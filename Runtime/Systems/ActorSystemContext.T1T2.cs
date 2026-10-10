namespace DTech.ActorFramework
{
	public readonly struct ActorSystemContext<TFeature1, TFeature2>
		where TFeature1 : struct, IActorFeature
		where TFeature2 : struct, IActorFeature
	{
		private readonly FeaturePool<TFeature1> _pool1;
		private readonly FeaturePool<TFeature2> _pool2;

		public Actor Actor { get; }
		public float DeltaTime { get; }
		public ref TFeature1 Feature1 => ref _pool1.GetUnchecked(Actor.Id);
		public ref TFeature2 Feature2 => ref _pool2.GetUnchecked(Actor.Id);

		internal ActorSystemContext(Actor actor, float deltaTime, FeaturePool<TFeature1> pool1, FeaturePool<TFeature2> pool2)
		{
			Actor = actor;
			DeltaTime = deltaTime;
			_pool1 = pool1;
			_pool2 = pool2;
		}
	}
}
