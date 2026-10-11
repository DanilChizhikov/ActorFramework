using System;
using DTech.ActorFramework.Diagnostics;

namespace DTech.ActorFramework.Tests.EditorMode
{
	internal static class DebugTestUtility
	{
		public static int IndexOfField(Type type, string fieldName)
		{
			DebugField[] fields = DebugFieldSelector.GetFields(type);
			int index = Array.FindIndex(fields, field => field.Info.Name == fieldName);
			if (index < 0)
			{
				throw new ArgumentException($"Field '{fieldName}' is not visible on '{type.Name}'.", nameof(fieldName));
			}

			return index;
		}

		public static int IndexOfQuery(ActorWorld world, ActorQuery query)
		{
			for (int i = 0; i < world.Queries.Count; i++)
			{
				if (ReferenceEquals(world.Queries.GetAt(i), query))
				{
					return i;
				}
			}

			throw new ArgumentException("Query does not belong to the world.", nameof(query));
		}
	}
}
