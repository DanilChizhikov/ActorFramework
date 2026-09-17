using System;

namespace DTech.ActorFramework
{
	public interface IActor
	{
		event Action<IActor> OnInitialized;
		event Action<IActor> OnDisposed;
		event Action<IActor, IActorFeature> OnFeatureAdded;
		
		Phase Phase { get; }
		bool IsInitialized { get; }
		bool IsDisposed { get; }
		int FeatureCount { get; }

		void Initialize();
		void Tick(float deltaTime);
		bool HasFeature<TFeature>()
			where TFeature : IActorFeature;
		bool TryGetFeature<TFeature>(out TFeature feature)
			where TFeature : IActorFeature;
		TFeature GetFeature<TFeature>()
			where TFeature : IActorFeature;
		TFeature GetFeatureOrException<TFeature>()
			where TFeature : IActorFeature;
		int GetFeaturesNonAlloc<TFeature>(TFeature[] results)
			where TFeature : IActorFeature;
	}
}