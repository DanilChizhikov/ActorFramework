using System;
using DTech.ActorFramework.Diagnostics;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace DTech.ActorFramework.Editor
{
	internal sealed class RemoteDebugSource : IDebugSource, IDisposable
	{
		public event Action<PollResponse> OnResponse;

		private const int PollIntervalMS = 500;
		private const double ResponseTimeoutSeconds = 3d;
		private const double NoRequest = -1d;

		public bool IsRemote => true;
		public bool IsBusy => _requestTime >= 0d &&
			EditorApplication.timeSinceStartup - _requestTime < ResponseTimeoutSeconds;
		public int PollIntervalMs => PollIntervalMS;

		private double _requestTime = NoRequest;

		public RemoteDebugSource()
		{
			EditorConnection connection = EditorConnection.instance;
			connection.Initialize();
			connection.Register(DebugMessageIds.PollResponse, PollResponseHandler);
			connection.RegisterDisconnection(DisconnectionHandler);
		}

		public void Dispose()
		{
			EditorConnection connection = EditorConnection.instance;
			connection.Unregister(DebugMessageIds.PollResponse, PollResponseHandler);
			connection.UnregisterDisconnection(DisconnectionHandler);
			OnResponse = null;
		}

		public void Poll(PollRequest request)
		{
			if (EditorConnection.instance.TrySend(DebugMessageIds.PollRequest, request.ToBytes()))
			{
				_requestTime = EditorApplication.timeSinceStartup;
			}
		}

		public void SetField(SetFieldRequest request)
		{
			EditorConnection.instance.TrySend(DebugMessageIds.SetFieldRequest, request.ToBytes());
		}

		private void PollResponseHandler(MessageEventArgs args)
		{
			_requestTime = NoRequest;

			PollResponse response;
			try
			{
				response = PollResponse.FromBytes(args.data);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				return;
			}

			OnResponse?.Invoke(response);
		}

		private void DisconnectionHandler(int playerId) => _requestTime = NoRequest;
	}
}