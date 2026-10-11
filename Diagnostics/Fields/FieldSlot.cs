using System;
using System.Collections;
using System.Reflection;

namespace DTech.ActorFramework.Diagnostics
{
	internal readonly struct FieldSlot
	{
		private readonly FieldInfo _field;
		private readonly int _index;

		public object Container { get; }
		public Type DeclaredType { get; }
		public bool IsReadOnly { get; }

		private FieldSlot(object container, FieldInfo field, int index, Type declaredType, bool isReadOnly)
		{
			Container = container;
			_field = field;
			_index = index;
			DeclaredType = declaredType;
			IsReadOnly = isReadOnly;
		}

		public static bool TryCreate(object container, int index, out FieldSlot slot)
		{
			Type containerType = container.GetType();
			if (FieldValueCodec.IsCollection(containerType))
			{
				var list = (IList)container;
				if (index < 0 || index >= list.Count)
				{
					slot = default;
					return false;
				}

				slot = new FieldSlot(container, null, index, FieldValueCodec.GetElementType(containerType), list.IsReadOnly);
				return true;
			}

			DebugField[] fields = DebugFieldSelector.GetFields(containerType);
			if (index < 0 || index >= fields.Length)
			{
				slot = default;
				return false;
			}

			DebugField field = fields[index];
			slot = new FieldSlot(container, field.Info, -1, field.FieldType, field.IsReadOnly);
			return true;
		}

		public object GetValue() => _field != null ? _field.GetValue(Container) : ((IList)Container)[_index];

		public void SetValue(object value)
		{
			if (_field != null)
			{
				_field.SetValue(Container, value);
				return;
			}

			((IList)Container)[_index] = value;
		}
	}
}