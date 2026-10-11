using System;
using System.Collections.Generic;
using DTech.ActorFramework.Diagnostics;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal sealed class ValueFieldDrawer<TValue> : FieldDrawer
	{
		private readonly BaseField<TValue> _field;
		private readonly Func<FieldNode, TValue> _read;
		private readonly Action<FieldNode, TValue> _write;

		private FieldNode _node;

		public ValueFieldDrawer(FieldNode node, int[] path, IFieldDrawerHost host, in ValueFieldBinding<TValue> binding)
			: base(node, path, host)
		{
			_field = binding.Field;
			_read = binding.Read;
			_write = binding.Write;

			SetupField(_field, node);
			_field.SetEnabled(!node.IsReadOnly);
			_field.RegisterValueChangedCallback(ValueChangedHandler);
			AddContent(_field);

			Update(node);
		}

		public override void Update(FieldNode node)
		{
			_node = node;
			if (IsEditingText())
			{
				return;
			}

			TValue value = _read(node);
			if (EqualityComparer<TValue>.Default.Equals(_field.value, value))
			{
				return;
			}

			_field.SetValueWithoutNotify(value);
		}

		private bool IsEditingText()
		{
			var focused = _field.focusController?.focusedElement as VisualElement;

			return focused != null && focused != _field && _field.Contains(focused);
		}

		private void ValueChangedHandler(ChangeEvent<TValue> evt)
		{
			if (evt.target != _field || _node == null)
			{
				return;
			}

			FieldNode value = _node.CloneValue();
			_write(value, evt.newValue);
			Submit(value);
		}
	}
}