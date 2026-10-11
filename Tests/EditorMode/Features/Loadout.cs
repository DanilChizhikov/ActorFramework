using UnityEngine;

namespace DTech.ActorFramework.Tests.EditorMode
{
	internal struct Loadout : IActorFeature
	{
		public Position Offset;
		public int[] Slots;
		public LoadoutMode Mode;
		public Actor Target;
		public string Title;
		public Vector3 Direction;
	}
}
