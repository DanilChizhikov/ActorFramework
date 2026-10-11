using DTech.ActorFramework.Diagnostics;

namespace DTech.ActorFramework.Editor
{
	internal interface IDebuggerCommands
	{
		bool IsRemote { get; }

		void SetField(SetFieldRequest request);
		void SelectActor(int actorId);
		void ShowQueryActors(int queryIndex);
	}
}