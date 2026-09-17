namespace DTech.ActorFramework
{
	public interface IActorFeature
	{
		IActor Actor { get; }
		Phase Phase { get; }
		bool IsInitialized { get; }
		bool IsDisposed { get; }
	}
}