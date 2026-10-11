using DTech.ActorFramework.Diagnostics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DTech.ActorFramework.Editor
{
	internal sealed class ObjectRefFieldDrawer : FieldDrawer
	{
		private readonly ObjectField _objectField;
		private readonly TextField _textField;

		public ObjectRefFieldDrawer(FieldNode node, int[] path, IFieldDrawerHost host)
			: base(node, path, host)
		{
			if (host.IsRemote)
			{
				_textField = new TextField(node.Label) { isReadOnly = true };
				SetupField(_textField, node);
				AddContent(_textField);
			}
			else
			{
				_objectField = new ObjectField(node.Label) { objectType = typeof(Object), allowSceneObjects = true };
				SetupField(_objectField, node);
				_objectField.RegisterValueChangedCallback(ObjectChangedHandler);
				AddContent(_objectField);
			}

			Update(node);
		}

		public override void Update(FieldNode node)
		{
			if (_objectField != null)
			{
				Object target = node.Integer != 0 ? EditorUtility.InstanceIDToObject((int)node.Integer) : null;
				if (_objectField.value != target)
				{
					_objectField.SetValueWithoutNotify(target);
				}

				return;
			}

			string text = $"{node.Text ?? "None"} ({node.TypeName})";
			if (_textField.value != text)
			{
				_textField.SetValueWithoutNotify(text);
			}
		}

		private void ObjectChangedHandler(ChangeEvent<Object> evt)
		{
			_objectField.SetValueWithoutNotify(evt.previousValue);
		}
	}
}