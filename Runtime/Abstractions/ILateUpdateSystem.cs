namespace DTech.ActorFramework
{
	public interface ILateUpdateSystem : IActorSystem
	{
		void LateUpdate(float deltaTime);
	}
}