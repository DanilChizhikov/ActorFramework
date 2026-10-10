using System;

namespace DTech.ActorFramework
{
	public interface IActorSystem : IDisposable
	{
		void Initialize(ActorWorld world);
	}
}