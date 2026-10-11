namespace DTech.ActorFramework
{
	internal interface IFeaturePool
	{
		int TypeId { get; }
		int Count { get; }

		bool Has(int actorId);
		bool Remove(int actorId);
		void Clear();
		object GetBoxed(int actorId);
		void SetBoxed(int actorId, object value);
	}
}