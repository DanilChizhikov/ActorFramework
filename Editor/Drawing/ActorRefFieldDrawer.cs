using DTech.ActorFramework.Diagnostics;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal sealed class ActorRefFieldDrawer : FieldDrawer
	{
		private const string ButtonClass = "field-drawer__actor-link";
		private const string NoneText = "None";

		private readonly Button _button;

		private int _actorId = DebugSession.None;

		public ActorRefFieldDrawer(FieldNode node, int[] path, IFieldDrawerHost host)
			: base(node, path, host)
		{
			var row = new VisualElement();
			row.AddToClassList(BaseField<int>.ussClassName);
			SetupField(row, node);

			var label = new Label(node.Label);
			label.AddToClassList(BaseField<int>.labelUssClassName);
			row.Add(label);

			_button = new Button(ClickedHandler);
			_button.AddToClassList(BaseField<int>.inputUssClassName);
			_button.AddToClassList(ButtonClass);
			row.Add(_button);

			AddContent(row);
			Update(node);
		}

		public override void Update(FieldNode node)
		{
			_actorId = (int)node.Integer;
			_button.text = node.Text ?? NoneText;
			_button.SetEnabled(_actorId >= 0);
		}

		private void ClickedHandler()
		{
			if (_actorId < 0)
			{
				return;
			}

			Host.SelectActor(_actorId);
		}
	}
}