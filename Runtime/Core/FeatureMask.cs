using System;

namespace DTech.ActorFramework
{
	public struct FeatureMask : IEquatable<FeatureMask>
	{
		public const int CAPACITY = 256;

		private const int BITS_PER_WORD = 64;
		private const int WORD_COUNT = 4;

		private ulong _word0;
		private ulong _word1;
		private ulong _word2;
		private ulong _word3;

		public readonly bool IsEmpty => (_word0 | _word1 | _word2 | _word3) == 0UL;

		public void Set(int typeId)
		{
			ulong bit = ToBit(typeId);
			switch (typeId >> 6)
			{
				case 0:
					_word0 |= bit;
					break;
				case 1:
					_word1 |= bit;
					break;
				case 2:
					_word2 |= bit;
					break;
				default:
					_word3 |= bit;
					break;
			}
		}

		public void Unset(int typeId)
		{
			ulong bit = ToBit(typeId);
			switch (typeId >> 6)
			{
				case 0:
					_word0 &= ~bit;
					break;
				case 1:
					_word1 &= ~bit;
					break;
				case 2:
					_word2 &= ~bit;
					break;
				default:
					_word3 &= ~bit;
					break;
			}
		}

		public readonly bool Has(int typeId)
		{
			ulong bit = ToBit(typeId);
			switch (typeId >> 6)
			{
				case 0:
					return (_word0 & bit) != 0UL;
				case 1:
					return (_word1 & bit) != 0UL;
				case 2:
					return (_word2 & bit) != 0UL;
				default:
					return (_word3 & bit) != 0UL;
			}
		}

		public void Clear()
		{
			_word0 = 0UL;
			_word1 = 0UL;
			_word2 = 0UL;
			_word3 = 0UL;
		}

		public readonly bool ContainsAll(in FeatureMask other)
		{
			return (_word0 & other._word0) == other._word0 &&
				(_word1 & other._word1) == other._word1 &&
				(_word2 & other._word2) == other._word2 &&
				(_word3 & other._word3) == other._word3;
		}

		public readonly bool ContainsAny(in FeatureMask other)
		{
			return ((_word0 & other._word0) |
				(_word1 & other._word1) |
				(_word2 & other._word2) |
				(_word3 & other._word3)) != 0UL;
		}

		public readonly bool ContainsNone(in FeatureMask other) => !ContainsAny(in other);

		public readonly int GetSetBitsNonAlloc(int[] results)
		{
			_ = results ?? throw new ArgumentNullException(nameof(results));

			int writtenCount = 0;
			for (int wordIndex = 0; wordIndex < WORD_COUNT; wordIndex++)
			{
				ulong word = GetWord(wordIndex);
				if (word == 0UL)
				{
					continue;
				}

				int offset = wordIndex * BITS_PER_WORD;
				for (int i = 0; i < BITS_PER_WORD; i++)
				{
					if (writtenCount == results.Length)
					{
						return writtenCount;
					}

					if ((word & (1UL << i)) == 0UL)
					{
						continue;
					}

					results[writtenCount] = offset + i;
					writtenCount++;
				}
			}

			return writtenCount;
		}

		public readonly bool Equals(FeatureMask other)
		{
			return _word0 == other._word0 &&
				_word1 == other._word1 &&
				_word2 == other._word2 &&
				_word3 == other._word3;
		}

		public readonly override bool Equals(object obj) => obj is FeatureMask other && Equals(other);

		public readonly override int GetHashCode()
		{
			ulong mixed = _word0;
			mixed = (mixed * 31UL) ^ _word1;
			mixed = (mixed * 31UL) ^ _word2;
			mixed = (mixed * 31UL) ^ _word3;

			return (int)mixed ^ (int)(mixed >> 32);
		}

		private static ulong ToBit(int typeId)
		{
			if (typeId < 0 || typeId >= CAPACITY)
			{
				throw new ArgumentOutOfRangeException(nameof(typeId),
					$"Feature type id must be in [0, {CAPACITY}), but was {typeId}.");
			}

			return 1UL << (typeId & (BITS_PER_WORD - 1));
		}

		private readonly ulong GetWord(int wordIndex)
		{
			switch (wordIndex)
			{
				case 0:
					return _word0;
				case 1:
					return _word1;
				case 2:
					return _word2;
				default:
					return _word3;
			}
		}
	}
}
