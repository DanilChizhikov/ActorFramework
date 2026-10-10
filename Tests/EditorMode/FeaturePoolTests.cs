using NUnit.Framework;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class FeaturePoolTests
	{
		private FeaturePool<Health> _pool;

		[SetUp]
		public void Setup()
		{
			_pool = new FeaturePool<Health>(FeatureType<Health>.Id, 4);
		}

		[Test]
		public void Add_ThenHasAndGetUnchecked_ReturnsStoredValue()
		{
			_pool.Add(2, new Health { Current = 10f, Max = 20f });

			Assert.That(_pool.Has(2), Is.True);
			Assert.That(_pool.Count, Is.EqualTo(1));
			Assert.That(_pool.GetUnchecked(2).Current, Is.EqualTo(10f));
			Assert.That(_pool.GetUnchecked(2).Max, Is.EqualTo(20f));
		}

		[Test]
		public void GetUnchecked_WritesThroughReference()
		{
			_pool.Add(1, new Health { Current = 1f, Max = 2f });

			ref Health health = ref _pool.GetUnchecked(1);
			health.Current = 99f;

			Assert.That(_pool.GetUnchecked(1).Current, Is.EqualTo(99f));
		}

		[Test]
		public void Remove_WithMiddleElement_SwapsLastIntoItsSlot()
		{
			_pool.Add(0, new Health { Current = 0f });
			_pool.Add(1, new Health { Current = 1f });
			_pool.Add(2, new Health { Current = 2f });

			bool removed = _pool.Remove(1);

			Assert.That(removed, Is.True);
			Assert.That(_pool.Count, Is.EqualTo(2));
			Assert.That(_pool.Has(1), Is.False);
			Assert.That(_pool.GetUnchecked(0).Current, Is.EqualTo(0f));
			Assert.That(_pool.GetUnchecked(2).Current, Is.EqualTo(2f));
		}

		[Test]
		public void Remove_ClearsVacatedDenseSlot()
		{
			_pool.Add(0, new Health { Current = 5f });

			_pool.Remove(0);

			Assert.That(_pool.Count, Is.EqualTo(0));
			Assert.That(_pool.Values[0].Current, Is.EqualTo(0f));
		}

		[Test]
		public void Remove_WithAbsentActor_ReturnsFalse()
		{
			Assert.That(_pool.Remove(7), Is.False);
		}

		[Test]
		public void Add_AfterRemove_ReusesActorId()
		{
			_pool.Add(3, new Health { Current = 1f });
			_pool.Remove(3);
			_pool.Add(3, new Health { Current = 2f });

			Assert.That(_pool.Count, Is.EqualTo(1));
			Assert.That(_pool.GetUnchecked(3).Current, Is.EqualTo(2f));
		}

		[Test]
		public void Add_WithHighActorId_GrowsSparseStorage()
		{
			_pool.Add(1000, new Health { Current = 7f });

			Assert.That(_pool.Has(1000), Is.True);
			Assert.That(_pool.GetUnchecked(1000).Current, Is.EqualTo(7f));
			Assert.That(_pool.Has(999), Is.False);
		}

		[Test]
		public void Add_BeyondDenseCapacity_GrowsDenseStorage()
		{
			for (int i = 0; i < 64; i++)
			{
				_pool.Add(i, new Health { Current = i });
			}

			Assert.That(_pool.Count, Is.EqualTo(64));
			Assert.That(_pool.GetUnchecked(63).Current, Is.EqualTo(63f));
		}

		[Test]
		public void Clear_RemovesEverything()
		{
			_pool.Add(0, default);
			_pool.Add(1, default);

			_pool.Clear();

			Assert.That(_pool.Count, Is.EqualTo(0));
			Assert.That(_pool.Has(0), Is.False);
			Assert.That(_pool.Has(1), Is.False);
		}
	}
}
