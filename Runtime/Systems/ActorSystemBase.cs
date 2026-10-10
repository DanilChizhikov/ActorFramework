using System;

namespace DTech.ActorFramework
{
	/// <summary>
	/// Manual tier: gives a system its world and its query and gets out of the way. Walk
	/// ActorQuery or the feature pools directly when you want zero dispatch inside the loop.
	/// Iterate backwards and skip actors whose Actor.IsAlive is false - a system may destroy
	/// actors that are still listed in the query it is iterating.
	/// </summary>
	public abstract class ActorSystemBase : IActorSystem
	{
		protected ActorWorld World { get; private set; }
		protected ActorQuery Query { get; private set; }

		public virtual void Initialize(ActorWorld world)
		{
			_ = world ?? throw new ArgumentNullException(nameof(world));

			World = world;

			ActorQueryBuilder builder = world.CreateQuery();
			ConfigureQuery(builder);
			Query = builder.Build();
		}

		public virtual void Dispose()
		{
			World = null;
			Query = null;
		}

		protected abstract void ConfigureQuery(ActorQueryBuilder builder);
	}
}