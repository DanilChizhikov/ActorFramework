using System;
using NUnit.Framework;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class FeatureMaskTests
	{
		[Test]
		public void Set_ThenHas_ReturnsTrue()
		{
			FeatureMask mask = default;

			mask.Set(0);
			mask.Set(63);
			mask.Set(64);
			mask.Set(255);

			Assert.That(mask.Has(0), Is.True);
			Assert.That(mask.Has(63), Is.True);
			Assert.That(mask.Has(64), Is.True);
			Assert.That(mask.Has(255), Is.True);
			Assert.That(mask.Has(1), Is.False);
			Assert.That(mask.IsEmpty, Is.False);
		}

		[Test]
		public void Unset_ThenHas_ReturnsFalse()
		{
			FeatureMask mask = default;
			mask.Set(130);

			mask.Unset(130);

			Assert.That(mask.Has(130), Is.False);
			Assert.That(mask.IsEmpty, Is.True);
		}

		[Test]
		public void Set_WithIdOutOfRange_Throws()
		{
			FeatureMask mask = default;

			Assert.Throws<ArgumentOutOfRangeException>(() => mask.Set(FeatureMask.CAPACITY));
			Assert.Throws<ArgumentOutOfRangeException>(() => mask.Set(-1));
		}

		[Test]
		public void ContainsAll_WithSubset_ReturnsTrue()
		{
			FeatureMask mask = default;
			mask.Set(1);
			mask.Set(70);
			mask.Set(200);

			FeatureMask subset = default;
			subset.Set(1);
			subset.Set(200);

			Assert.That(mask.ContainsAll(in subset), Is.True);
		}

		[Test]
		public void ContainsAll_WithMissingBit_ReturnsFalse()
		{
			FeatureMask mask = default;
			mask.Set(1);

			FeatureMask other = default;
			other.Set(1);
			other.Set(2);

			Assert.That(mask.ContainsAll(in other), Is.False);
		}

		[Test]
		public void ContainsAll_WithEmptyOther_ReturnsTrue()
		{
			FeatureMask mask = default;
			mask.Set(5);
			FeatureMask empty = default;

			Assert.That(mask.ContainsAll(in empty), Is.True);
		}

		[Test]
		public void ContainsAny_WithEmptyOther_ReturnsFalse()
		{
			FeatureMask mask = default;
			mask.Set(5);
			FeatureMask empty = default;

			Assert.That(mask.ContainsAny(in empty), Is.False);
			Assert.That(mask.ContainsNone(in empty), Is.True);
		}

		[Test]
		public void GetSetBitsNonAlloc_ReturnsAscendingIds()
		{
			FeatureMask mask = default;
			mask.Set(3);
			mask.Set(64);
			mask.Set(191);

			int[] results = new int[8];
			int count = mask.GetSetBitsNonAlloc(results);

			Assert.That(count, Is.EqualTo(3));
			Assert.That(results[0], Is.EqualTo(3));
			Assert.That(results[1], Is.EqualTo(64));
			Assert.That(results[2], Is.EqualTo(191));
		}

		[Test]
		public void GetSetBitsNonAlloc_WithSmallBuffer_TruncatesToBufferLength()
		{
			FeatureMask mask = default;
			mask.Set(1);
			mask.Set(2);
			mask.Set(3);

			int[] results = new int[2];
			int count = mask.GetSetBitsNonAlloc(results);

			Assert.That(count, Is.EqualTo(2));
		}

		[Test]
		public void Equals_WithSameBits_ReturnsTrueAndSameHashCode()
		{
			FeatureMask left = default;
			left.Set(7);
			left.Set(250);

			FeatureMask right = default;
			right.Set(250);
			right.Set(7);

			Assert.That(left.Equals(right), Is.True);
			Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
		}
	}
}
