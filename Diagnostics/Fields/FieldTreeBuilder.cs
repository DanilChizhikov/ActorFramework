using System;
using System.Collections;

namespace DTech.ActorFramework.Diagnostics
{
	internal static class FieldTreeBuilder
	{
		private const int MaxDepth = 6;
		private const int MaxCollectionElements = 100;

		private const string NullText = "null";

		public static FieldNode BuildRoot(object feature)
		{
			_ = feature ?? throw new ArgumentNullException(nameof(feature));

			return new FieldNode
			{
				Kind = FieldKind.Composite,
				TypeName = TypeNames.GetDisplayName(feature.GetType()),
				Children = BuildChildren(feature, false, 0),
			};
		}

		private static FieldNode[] BuildChildren(object container, bool isReadOnly, int depth)
		{
			DebugField[] fields = DebugFieldSelector.GetFields(container.GetType());
			var children = new FieldNode[fields.Length];
			for (int i = 0; i < fields.Length; i++)
			{
				DebugField field = fields[i];
				FieldNode child = Build(field.Info.GetValue(container), field.FieldType, isReadOnly || field.IsReadOnly,
					depth + 1);

				child.Label = field.Label;
				child.Tooltip = field.Tooltip;
				child.Header = field.Header;
				if (field.HasRange)
				{
					child.Flags |= FieldFlags.HasRange;
					child.RangeMin = field.RangeMin;
					child.RangeMax = field.RangeMax;
				}

				if (field.IsMultiline)
				{
					child.Flags |= FieldFlags.Multiline;
				}

				children[i] = child;
			}

			return children;
		}

		private static FieldNode Build(object value, Type declaredType, bool isReadOnly, int depth)
		{
			Type type = value != null && !declaredType.IsValueType ? value.GetType() : declaredType;
			var node = new FieldNode
			{
				Kind = FieldValueCodec.GetKind(type),
				TypeName = TypeNames.GetDisplayName(type),
				Flags = isReadOnly ? FieldFlags.ReadOnly : FieldFlags.None,
			};

			switch (node.Kind)
			{
				case FieldKind.Composite:
				case FieldKind.Collection:
					BuildNested(value, node, depth);
					break;
				case FieldKind.Text:
					node.Flags |= FieldFlags.ReadOnly;
					node.Text = value is ICollection collection ? $"Count = {collection.Count}" : value?.ToString() ?? NullText;
					break;
				case FieldKind.ActorRef:
				case FieldKind.ObjectRef:
					node.Flags |= FieldFlags.ReadOnly;
					FieldValueCodec.Encode(value, node);
					break;
				default:
					FieldValueCodec.Encode(value, node);
					break;
			}

			return node;
		}

		private static void BuildNested(object value, FieldNode node, int depth)
		{
			if (value == null)
			{
				node.Kind = FieldKind.Null;
				node.Flags |= FieldFlags.ReadOnly;
				node.Text = NullText;
				return;
			}

			if (depth >= MaxDepth)
			{
				node.Kind = FieldKind.Text;
				node.Flags |= FieldFlags.ReadOnly;
				node.Text = value.ToString();
				return;
			}

			bool isReadOnly = node.IsReadOnly;
			if (node.Kind == FieldKind.Composite)
			{
				node.Children = BuildChildren(value, isReadOnly, depth);
				return;
			}

			var list = (IList)value;
			Type elementType = FieldValueCodec.GetElementType(value.GetType());
			int count = Math.Min(list.Count, MaxCollectionElements);

			node.Integer = list.Count;
			node.Children = new FieldNode[count];
			for (int i = 0; i < count; i++)
			{
				FieldNode element = Build(list[i], elementType, isReadOnly, depth + 1);
				element.Label = $"Element {i}";
				node.Children[i] = element;
			}
		}
	}
}