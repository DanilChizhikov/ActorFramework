namespace DTech.ActorFramework
{
	/// <summary>
	/// Marker for actor feature data. Implementations must be structs and must hold data only - all behaviour
	/// lives in systems. Never assign a feature to an IActorFeature local: that boxes it.
	/// Constrained generic calls (where TFeature : struct, IActorFeature) never box.
	/// </summary>
	public interface IActorFeature
	{
	}
}
