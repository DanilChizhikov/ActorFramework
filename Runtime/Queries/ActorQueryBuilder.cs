using System;

namespace DTech.ActorFramework
{
	public sealed class ActorQueryBuilder
	{
		private readonly ActorWorld _world;

		private FeatureMask _all;
		private FeatureMask _any;
		private FeatureMask _none;
		private bool _hasAny;
		private bool _includeInactive;

		internal ActorQueryBuilder(ActorWorld world)
		{
			_world = world ?? throw new ArgumentNullException(nameof(world));
		}

		public ActorQueryBuilder With<TFeature>()
			where TFeature : struct, IActorFeature
		{
			_all.Set(FeatureType<TFeature>.Id);

			return this;
		}

		public ActorQueryBuilder With<TFeature1, TFeature2>()
			where TFeature1 : struct, IActorFeature
			where TFeature2 : struct, IActorFeature
		{
			_all.Set(FeatureType<TFeature1>.Id);
			_all.Set(FeatureType<TFeature2>.Id);

			return this;
		}

		public ActorQueryBuilder With<TFeature1, TFeature2, TFeature3>()
			where TFeature1 : struct, IActorFeature
			where TFeature2 : struct, IActorFeature
			where TFeature3 : struct, IActorFeature
		{
			_all.Set(FeatureType<TFeature1>.Id);
			_all.Set(FeatureType<TFeature2>.Id);
			_all.Set(FeatureType<TFeature3>.Id);

			return this;
		}

		public ActorQueryBuilder Without<TFeature>()
			where TFeature : struct, IActorFeature
		{
			_none.Set(FeatureType<TFeature>.Id);

			return this;
		}

		public ActorQueryBuilder Without<TFeature1, TFeature2>()
			where TFeature1 : struct, IActorFeature
			where TFeature2 : struct, IActorFeature
		{
			_none.Set(FeatureType<TFeature1>.Id);
			_none.Set(FeatureType<TFeature2>.Id);

			return this;
		}

		public ActorQueryBuilder WithAny<TFeature>()
			where TFeature : struct, IActorFeature
		{
			_any.Set(FeatureType<TFeature>.Id);
			_hasAny = true;

			return this;
		}

		public ActorQueryBuilder WithAny<TFeature1, TFeature2>()
			where TFeature1 : struct, IActorFeature
			where TFeature2 : struct, IActorFeature
		{
			_any.Set(FeatureType<TFeature1>.Id);
			_any.Set(FeatureType<TFeature2>.Id);
			_hasAny = true;

			return this;
		}

		public ActorQueryBuilder WithAny<TFeature1, TFeature2, TFeature3>()
			where TFeature1 : struct, IActorFeature
			where TFeature2 : struct, IActorFeature
			where TFeature3 : struct, IActorFeature
		{
			_any.Set(FeatureType<TFeature1>.Id);
			_any.Set(FeatureType<TFeature2>.Id);
			_any.Set(FeatureType<TFeature3>.Id);
			_hasAny = true;

			return this;
		}

		/// <summary>
		/// Inactive actors are excluded by default, so a system never pays an activation check per actor.
		/// </summary>
		public ActorQueryBuilder IncludeInactive()
		{
			_includeInactive = true;

			return this;
		}

		public ActorQuery Build()
		{
			ActorQueryKey key = new ActorQueryKey(in _all, in _any, in _none, _hasAny, _includeInactive);

			return _world.GetOrCreateQuery(in key);
		}
	}
}