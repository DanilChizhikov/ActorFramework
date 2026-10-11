using System;

namespace DTech.ActorFramework
{
	/// <summary>
	/// Shows a non-public field of a feature in the Actor Debugger window. Public fields are shown without it.
	/// Apply it with the field target to show an auto-property: [field: ShowInActorDebugger].
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class ShowInActorDebuggerAttribute : Attribute
	{
	}
}