using System;

namespace DTech.ActorFramework
{
	/// <summary>
	/// Hides a feature field from the Actor Debugger window. Wins over ShowInActorDebugger.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class HideInActorDebuggerAttribute : Attribute
	{
	}
}