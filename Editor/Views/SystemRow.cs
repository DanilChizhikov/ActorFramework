using DTech.ActorFramework.Diagnostics;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal sealed class SystemRow : VisualElement
	{
		private const string RootClass = "debug-row";
		private const string IndexClass = "debug-row__index";
		private const string NameClass = "debug-row__name";
		private const string BadgeClass = "debug-row__badge";

		private readonly Label _index;
		private readonly Label _name;
		private readonly Label _update;
		private readonly Label _fixedUpdate;
		private readonly Label _lateUpdate;

		public SystemRow()
		{
			AddToClassList(RootClass);

			_index = new Label();
			_index.AddToClassList(IndexClass);
			_name = new Label();
			_name.AddToClassList(NameClass);
			Add(_index);
			Add(_name);

			_update = CreateBadge("Update");
			_fixedUpdate = CreateBadge("FixedUpdate");
			_lateUpdate = CreateBadge("LateUpdate");
		}

		public void Bind(int index, SystemInfo system)
		{
			_index.text = $"#{index}";
			_name.text = system.Name;
			_name.tooltip = system.FullName;
			SetVisible(_update, (system.Phases & SystemPhases.Update) != 0);
			SetVisible(_fixedUpdate, (system.Phases & SystemPhases.FixedUpdate) != 0);
			SetVisible(_lateUpdate, (system.Phases & SystemPhases.LateUpdate) != 0);
		}

		private static void SetVisible(VisualElement element, bool isVisible)
		{
			element.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
		}

		private Label CreateBadge(string text)
		{
			var badge = new Label(text);
			badge.AddToClassList(BadgeClass);
			Add(badge);

			return badge;
		}
	}
}