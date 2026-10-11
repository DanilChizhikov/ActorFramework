using DTech.ActorFramework.Diagnostics;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal sealed class ReadOnlyFieldDrawer : FieldDrawer
	{
		private readonly TextField _field;

		public ReadOnlyFieldDrawer(FieldNode node, int[] path, IFieldDrawerHost host)
			: base(node, path, host)
		{
			_field = new TextField(node.Label) { isReadOnly = true };
			SetupField(_field, node);
			AddContent(_field);

			Update(node);
		}

		public override void Update(FieldNode node)
		{
			string text = node.Text ?? "null";
			if (_field.value != text)
			{
				_field.SetValueWithoutNotify(text);
			}
		}
	}
}