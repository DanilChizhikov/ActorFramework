using System.Collections.Generic;
using DTech.ActorFramework.Diagnostics;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal sealed class SystemsView
	{
		private const float RowHeight = 20f;

		private readonly ListView _list;
		private readonly List<SystemInfo> _items = new();

		public VisualElement Element => _list;

		public SystemsView()
		{
			_list = new ListView(_items, RowHeight, MakeRow, BindRow)
			{
				selectionType = SelectionType.None,
			};
			_list.style.flexGrow = 1f;
		}

		public void Show(DebugSession session)
		{
			_items.Clear();
			_items.AddRange(session.Systems);
			_list.RefreshItems();
		}

		private static VisualElement MakeRow() => new SystemRow();

		private void BindRow(VisualElement element, int index)
		{
			((SystemRow)element).Bind(index, _items[index]);
		}
	}
}