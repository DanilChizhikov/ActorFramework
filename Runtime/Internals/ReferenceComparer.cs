using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DTech.ActorFramework
{
	internal sealed class ReferenceComparer : IEqualityComparer<object>
	{
		public static readonly ReferenceComparer Instance = new();

		private ReferenceComparer()
		{
		}

		public new bool Equals(object x, object y) => ReferenceEquals(x, y);

		public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
	}
}