using System;

namespace DTech.ActorFramework
{
	public interface IActivatableActorFeature : IActorFeature
	{
		event Action<IActivatableActorFeature> OnActivated;
		event Action<IActivatableActorFeature> OnDeactivated;

		ActivationBehaviourType ActivationBehaviour { get; }
		bool IsActive { get; }
		int ActivatorCount { get; }
		int DeactivatorCount { get; }
		
		bool ContainsActivator<TToken>(TToken token)
			where TToken : class;
		void AddActivator<TToken>(TToken token)
			where TToken : class;
		bool RemoveActivator<TToken>(TToken token)
			where TToken : class;
		int GetActivatorsNonAlloc(object[] results);
		bool ContainsDeactivator<TToken>(TToken token)
			where TToken : class;
		void AddDeactivator<TToken>(TToken token)
			where TToken : class;
		bool RemoveDeactivator<TToken>(TToken token)
			where TToken : class;
		int GetDeactivatorsNonAlloc(object[] results);
	}
}