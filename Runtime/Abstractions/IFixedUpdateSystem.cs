namespace DTech.ActorFramework
{
	public interface IFixedUpdateSystem : IActorSystem
	{
		void FixedUpdate(float fixedDeltaTime);
	}
}