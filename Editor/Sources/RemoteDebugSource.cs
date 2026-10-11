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

		private readonly PlayerMessageReceiver _receiver;

		private double _requestTime = NoRequest;

		public RemoteDebugSource()
		{
			_receiver = ScriptableObject.CreateInstance<PlayerMessageReceiver>();
			_receiver.hideFlags = HideFlags.HideAndDontSave;
			_receiver.OnMessage += PollResponseHandler;
			_receiver.OnDisconnected += DisconnectionHandler;

			EditorConnection connection = EditorConnection.instance;
			connection.Initialize();
			connection.Register(DebugMessageIds.PollResponse, _receiver.Receive);
			connection.RegisterDisconnection(_receiver.Disconnect);
		}

		public void Dispose()
		{
			EditorConnection connection = EditorConnection.instance;
			connection.Unregister(DebugMessageIds.PollResponse, _receiver.Receive);
			connection.UnregisterDisconnection(_receiver.Disconnect);
			_receiver.OnMessage -= PollResponseHandler;
			_receiver.OnDisconnected -= DisconnectionHandler;
			UnityEngine.Object.DestroyImmediate(_receiver);
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