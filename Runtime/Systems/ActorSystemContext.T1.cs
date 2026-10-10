namespace DTech.ActorFramework
{
	/// <summary>
	/// Per-actor arguments of a typed system. Every feature property resolves through the pool on each access,
	/// so take it once into a ref local. Feature references are invalidated by the next structural change.
	/// </summary>
	public readonly struct ActorSystemContext<TFeature1>
		where TFeature1 : struct, IActorFeature
	{
		private readonly FeaturePool<TFeature1> _pool1;

		public Actor Actor { get; }
		public float DeltaTime { get; }
		public ref TFeature1 Feature1 => ref _pool1.GetUnchecked(Actor.Id);

		internal ActorSystemContext(Actor actor, float deltaTime, FeaturePool<TFeature1> pool1)
		{
			Actor = actor;
			DeltaTime = deltaTime;
			_pool1 = pool1;
		}
	}
}
