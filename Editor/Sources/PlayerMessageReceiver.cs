using System;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace DTech.ActorFramework.Editor
{
	internal sealed class PlayerMessageReceiver : ScriptableObject
	{
		public event Action<MessageEventArgs> OnMessage;

		public void Receive(MessageEventArgs args) => OnMessage?.Invoke(args);
	}
}
