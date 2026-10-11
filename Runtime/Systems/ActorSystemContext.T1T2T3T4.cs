namespace DTech.ActorFramework
{
	public readonly struct ActorSystemContext<TFeature1, TFeature2, TFeature3, TFeature4>
		where TFeature1 : struct, IActorFeature
		where TFeature2 : struct, IActorFeature
		where TFeature3 : struct, IActorFeature
		where TFeature4 : struct, IActorFeature
	{
		private readonly FeaturePool<TFeature1> _pool1;
		private readonly FeaturePool<TFeature2> _pool2;
		private readonly FeaturePool<TFeature3> _pool3;
		private readonly FeaturePool<TFeature4> _pool4;

		public Actor Actor { get; }
		public float DeltaTime { get; }
		public ref TFeature1 Feature1 => ref _pool1.GetUnchecked(Actor.Id);
		public ref TFeature2 Feature2 => ref _pool2.GetUnchecked(Actor.Id);
		public ref TFeature3 Feature3 => ref _pool3.GetUnchecked(Actor.Id);
		public ref TFeature4 Feature4 => ref _pool4.GetUnchecked(Actor.Id);

		internal ActorSystemContext(
			Actor actor,
			float deltaTime,
			FeaturePool<TFeature1> pool1,
			FeaturePool<TFeature2> pool2,
			FeaturePool<TFeature3> pool3,
			FeaturePool<TFeature4> pool4)
		{
			Actor = actor;
			DeltaTime = deltaTime;
			_pool1 = pool1;
			_pool2 = pool2;
			_pool3 = pool3;
			_pool4 = pool4;
		}
	}
}
