using System;
using DTech.ActorFramework.Diagnostics;

namespace DTech.ActorFramework.Editor
{
	internal interface IDebugSource
	{
		event Action<PollResponse> OnResponse;

		bool IsRemote { get; }
		bool IsBusy { get; }
		int PollIntervalMs { get; }

		void Poll(PollRequest request);
		void SetField(SetFieldRequest request);
	}
}