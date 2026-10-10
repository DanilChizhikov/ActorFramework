using System;

namespace DTech.ActorFramework
{
	/// <summary>
	/// Identity and feature composition of one actor. Feature values live in the world's pools, not here.
	/// Actors are never pooled: a destroyed actor stays destroyed, which is what makes a user-held
	/// reference safe to keep.
	/// </summary>
	public sealed class Actor
	{
		public event Action<Actor> OnActivated;
		public event Action<Actor> OnDeactivated;
		public event Action<Actor> OnDestroyed;

		private readonly ActorWorld _world;
		private readonly int _id;

		private ActivationState _activation;
		private FeatureMask _mask;
		private int _featureCount;
		private bool _isAlive;
		private bool _isActive;
		private bool _isArmed;

		public ActorWorld World => _world;
		public int Id => _id;
		public bool IsAlive => _isAlive;
		public bool IsActive => _isActive;
		public int FeatureCount => _featureCount;
		public int ActivatorCount => _activation?.ActivatorCount ?? 0;
		public int DeactivatorCount => _activation?.DeactivatorCount ?? 0;

		internal ref FeatureMask Mask => ref _mask;

		internal bool IsQueryDirty { get; set; }

		internal Actor(ActorWorld world, int id)
		{
			_world = world ?? throw new ArgumentNullException(nameof(world));
			_id = id;
			_isAlive = true;
		}

		public bool HasFeature<TFeature>()
			where TFeature : struct, IActorFeature
		{
			return _mask.Has(FeatureType<TFeature>.Id);
		}

		/// <summary>
		/// Reference to this actor's feature value. Throws when the feature is absent.
		/// The reference is invalidated by the next structural change - never hold it across one.
		/// </summary>
		public ref TFeature GetFeature<TFeature>()
			where TFeature : struct, IActorFeature
		{
			ThrowIfNotAlive();

			return ref _world.Features.Get<TFeature>(this);
		}

		public bool TryGetFeature<TFeature>(out TFeature feature)
			where TFeature : struct, IActorFeature
		{
			return _world.Features.TryGet(this, out feature);
		}

		/// <summary>
		/// Adds a feature. Throws when the actor already has it.
		/// The returned reference is invalidated by the next structural change.
		/// </summary>
		public ref TFeature AddFeature<TFeature>(TFeature value = default)
			where TFeature : struct, IActorFeature
		{
			ThrowIfNotAlive();

			return ref _world.Features.Add(this, value);
		}

		public ref TFeature GetOrAddFeature<TFeature>()
			where TFeature : struct, IActorFeature
		{
			ThrowIfNotAlive();

			return ref _world.Features.GetOrAdd<TFeature>(this);
		}

		public bool RemoveFeature<TFeature>()
			where TFeature : struct, IActorFeature
		{
			ThrowIfNotAlive();

			return _world.Features.Remove<TFeature>(this);
		}

		public int GetFeatureTypeIdsNonAlloc(int[] results) => _mask.GetSetBitsNonAlloc(results);

		public void Destroy() => _world.DestroyActor(this);

		public bool ContainsActivator<TToken>(TToken token)
			where TToken : class
		{
			_ = token ?? throw new ArgumentNullException(nameof(token));

			return _activation != null && _activation.ContainsActivator(token);
		}

		public void AddActivator<TToken>(TToken token)
			where TToken : class
		{
			_ = token ?? throw new ArgumentNullException(nameof(token));
			ThrowIfNotAlive();
			EnsureActivationState().AddActivator(token);
		}

		public bool RemoveActivator<TToken>(TToken token)
			where TToken : class
		{
			_ = token ?? throw new ArgumentNullException(nameof(token));

			return _activation != null && _activation.RemoveActivator(token);
		}

		public int GetActivatorsNonAlloc(object[] results)
		{
			_ = results ?? throw new ArgumentNullException(nameof(results));

			return _activation?.GetActivatorsNonAlloc(results) ?? 0;
		}

		public bool ContainsDeactivator<TToken>(TToken token)
			where TToken : class
		{
			_ = token ?? throw new ArgumentNullException(nameof(token));

			return _activation != null && _activation.ContainsDeactivator(token);
		}

		public void AddDeactivator<TToken>(TToken token)
			where TToken : class
		{
			_ = token ?? throw new ArgumentNullException(nameof(token));
			ThrowIfNotAlive();
			EnsureActivationState().AddDeactivator(token);
		}

		public bool RemoveDeactivator<TToken>(TToken token)
			where TToken : class
		{
			_ = token ?? throw new ArgumentNullException(nameof(token));

			return _activation != null && _activation.RemoveDeactivator(token);
		}

		public int GetDeactivatorsNonAlloc(object[] results)
		{
			_ = results ?? throw new ArgumentNullException(nameof(results));

			return _activation?.GetDeactivatorsNonAlloc(results) ?? 0;
		}

		public override string ToString() => $"Actor({_id})";

		internal void Arm()
		{
			if (_isArmed)
			{
				return;
			}

			_isArmed = true;
			if (_activation != null)
			{
				_activation.Arm();
				return;
			}
			
			SetActive(true);
		}

		internal void IncrementFeatureCount() => _featureCount++;

		internal void DecrementFeatureCount() => _featureCount--;

		internal void MarkDestroyed() => _isAlive = false;

		internal void ResetActivation()
		{
			_isArmed = false;
			_activation?.Reset();
			SetActive(false);
		}

		internal void ClearMask()
		{
			_mask.Clear();
			_featureCount = 0;
		}

		internal void RaiseDestroyed() => OnDestroyed?.Invoke(this);

		internal void ClearEvents()
		{
			OnActivated = null;
			OnDeactivated = null;
			OnDestroyed = null;
		}

		private ActivationState EnsureActivationState()
		{
			if (_activation != null)
			{
				return _activation;
			}

			_activation = new ActivationState();

			_activation.OnActivated += ActivationActivatedHandler;
			_activation.OnDeactivated += ActivationDeactivatedHandler;

			if (_isArmed)
			{
				_activation.Arm();
			}

			return _activation;
		}

		private void SetActive(bool isActive)
		{
			if (_isActive == isActive)
			{
				return;
			}

			_isActive = isActive;
			_world.Structural.Begin();
			try
			{
				_world.Structural.MarkDirty(this);
				if (isActive)
				{
					OnActivated?.Invoke(this);
				}
				else
				{
					OnDeactivated?.Invoke(this);
				}
			}
			finally
			{
				_world.Structural.End();
			}
		}

		private void ThrowIfNotAlive()
		{
			if (_isAlive)
			{
				return;
			}

			throw new InvalidOperationException($"Actor '{_id}' is destroyed.");
		}

		private void ActivationActivatedHandler() => SetActive(true);

		private void ActivationDeactivatedHandler() => SetActive(false);
	}
}