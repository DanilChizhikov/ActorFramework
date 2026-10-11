namespace DTech.ActorFramework
{
	public interface IUpdateSystem : IActorSystem
	{
		void Update(float deltaTime);
	}
}