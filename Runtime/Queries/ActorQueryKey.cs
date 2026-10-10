using System;

namespace DTech.ActorFramework
{
	internal readonly struct ActorQueryKey : IEquatable<ActorQueryKey>
	{
		public FeatureMask All { get; }
		public FeatureMask Any { get; }
		public FeatureMask None { get; }
		public bool HasAny { get; }
		public bool IncludeInactive { get; }

		public ActorQueryKey(
			in FeatureMask all,
			in FeatureMask any,
			in FeatureMask none,
			bool hasAny,
			bool includeInactive)
		{
			All = all;
			Any = any;
			None = none;
			HasAny = hasAny;
			IncludeInactive = includeInactive;
		}

		public bool Equals(ActorQueryKey other)
		{
			return HasAny == other.HasAny &&
				IncludeInactive == other.IncludeInactive &&
				All.Equals(other.All) &&
				Any.Equals(other.Any) &&
				None.Equals(other.None);
		}

		public override bool Equals(object obj) => obj is ActorQueryKey other && Equals(other);

		public override int GetHashCode()
		{
			int hashCode = All.GetHashCode();
			hashCode = (hashCode * 397) ^ Any.GetHashCode();
			hashCode = (hashCode * 397) ^ None.GetHashCode();
			hashCode = (hashCode * 397) ^ (HasAny ? 1 : 0);
			hashCode = (hashCode * 397) ^ (IncludeInactive ? 1 : 0);

			return hashCode;
		}
	}
}
