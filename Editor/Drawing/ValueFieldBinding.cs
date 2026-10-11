using System;
using DTech.ActorFramework.Diagnostics;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal readonly struct ValueFieldBinding<TValue>
	{
		public BaseField<TValue> Field { get; }
		public Func<FieldNode, TValue> Read { get; }
		public Action<FieldNode, TValue> Write { get; }

		public ValueFieldBinding(BaseField<TValue> field, Func<FieldNode, TValue> read, Action<FieldNode, TValue> write)
		{
			Field = field;
			Read = read;
			Write = write;
		}
	}
}