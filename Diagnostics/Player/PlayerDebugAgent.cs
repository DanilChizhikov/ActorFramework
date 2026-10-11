using System;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Scripting;

namespace DTech.ActorFramework.Diagnostics
{
	internal static class PlayerDebugAgent
	{
		[Preserve]
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize()
		{
			if (Application.isEditor)
			{
				return;
			}

			PlayerConnection connection = PlayerConnection.instance;
			connection.Register(DebugMessageIds.PollRequest, PollRequestHandler);
			connection.Register(DebugMessageIds.SetFieldRequest, SetFieldRequestHandler);
		}

		private static void PollRequestHandler(MessageEventArgs args)
		{
			try
			{
				PollResponse response = DebugSnapshotBuilder.Build(PollRequest.FromBytes(args.data));
				PlayerConnection.instance.TrySend(DebugMessageIds.PollResponse, response.ToBytes());
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private static void SetFieldRequestHandler(MessageEventArgs args)
		{
			try
			{
				FieldValueApplier.TryApply(SetFieldRequest.FromBytes(args.data));
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
	}
}