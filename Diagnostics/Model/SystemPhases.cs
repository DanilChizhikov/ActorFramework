using System;

namespace DTech.ActorFramework.Diagnostics
{
	[Flags]
	internal enum SystemPhases : byte
	{
		None = 0,
		Update = 1 << 0,
		FixedUpdate = 1 << 1,
		LateUpdate = 1 << 2,
	}
}