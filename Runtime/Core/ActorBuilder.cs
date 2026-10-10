namespace DTech.ActorFramework
{
	/// <summary>
	/// Composes an actor before anything observes it: features are written straight into their pools and the
	/// queries are synced once, on Build, instead of once per feature.
	/// A builder that is never built leaves an unarmed actor that no query can see.
	/// </summary>
	public readonly struct ActorBuilder
	{
		private readonly ActorWorld _world;
		private readonly Actor _actor;

		public Actor Actor => _actor;

		internal ActorBuilder(ActorWorld world, Actor actor)
		{
			_world = world;
			_actor = actor;
		}

		public ActorBuilder With<TFeature>(TFeature value = default)
			where TFeature : struct, IActorFeature
		{
			_world.Features.AddSilent(_actor, in value);
			return this;
		}

		public Actor Build() => _world.CompleteBuild(_actor);
	}
}