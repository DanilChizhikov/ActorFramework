using System.Collections.Generic;

namespace DTech.ActorFramework.Tests.EditorMode
{
	internal sealed class RecordingSystem : IActorSystem
	{
		private readonly List<string> _journal;
		private readonly string _name;

		public RecordingSystem(List<string> journal, string name)
		{
			_journal = journal;
			_name = name;
		}

		public void Initialize(ActorWorld world) => _journal.Add($"{_name}:initialize");

		public void Dispose() => _journal.Add($"{_name}:dispose");
	}
}
