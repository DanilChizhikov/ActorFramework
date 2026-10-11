using DTech.ActorFramework.Diagnostics;

namespace DTech.ActorFramework.Editor
{
	internal interface IFieldDrawerHost
	{
		bool IsRemote { get; }

		void SubmitField(int[] path, FieldNode value);
		void SelectActor(int actorId);
	}
}