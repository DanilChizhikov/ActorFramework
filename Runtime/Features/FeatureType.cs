namespace DTech.ActorFramework
{
	internal static class FeatureType<TFeature>
		where TFeature : struct, IActorFeature
	{
		public static readonly int Id = FeatureTypeRegistry.Register(typeof(TFeature));
	}
}
