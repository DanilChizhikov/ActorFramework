using System;

namespace DTech.ActorFramework.Diagnostics
{
	[Flags]
	internal enum FieldFlags : byte
	{
		None = 0,
		ReadOnly = 1 << 0,
		Multiline = 1 << 1,
		HasRange = 1 << 2,
	}
}