using System;

namespace DTech.ActorFramework
{
	internal static class FeatureTypeRegistry
	{
		public const int MAX_FEATURE_TYPES = FeatureMask.CAPACITY;

		private const int INITIAL_CAPACITY = 32;

		private static readonly object _lock = new();

		private static Type[] _types = new Type[INITIAL_CAPACITY];
		private static int _count;

		public static int Count
		{
			get
			{
				lock (_lock)
				{
					return _count;
				}
			}
		}

		public static int Register(Type featureType)
		{
			_ = featureType ?? throw new ArgumentNullException(nameof(featureType));

			lock (_lock)
			{
				if (_count == MAX_FEATURE_TYPES)
				{
					throw new InvalidOperationException(
						$"Cannot register feature type '{featureType.Name}': the limit of " +
						$"{MAX_FEATURE_TYPES} feature types is reached.");
				}

				if (_count == _types.Length)
				{
					Array.Resize(ref _types, _types.Length * 2);
				}

				int typeId = _count;
				_types[typeId] = featureType;
				_count++;

				return typeId;
			}
		}

		public static Type GetFeatureType(int typeId)
		{
			lock (_lock)
			{
				if (typeId < 0 || typeId >= _count)
				{
					throw new ArgumentOutOfRangeException(nameof(typeId),
						$"Feature type id must be in [0, {_count}), but was {typeId}.");
				}

				return _types[typeId];
			}
		}
	}
}