using System;
using DTech.ActorFramework.Diagnostics;

namespace DTech.ActorFramework.Editor
{
	internal sealed class LocalDebugSource : IDebugSource
	{
		public event Action<PollResponse> OnResponse;

		private const int PollIntervalMS = 200;

		public bool IsRemote => false;
		public bool IsBusy => false;
		public int PollIntervalMs => PollIntervalMS;

		public void Poll(PollRequest request) => OnResponse?.Invoke(DebugSnapshotBuilder.Build(request));

		public void SetField(SetFieldRequest request) => FieldValueApplier.TryApply(request);
	}
}