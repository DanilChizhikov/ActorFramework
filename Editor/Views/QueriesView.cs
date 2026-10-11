using System;
using System.Collections.Generic;
using DTech.ActorFramework.Diagnostics;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal sealed class QueriesView
	{
		private const float RowHeight = 22f;

		private readonly IDebuggerCommands _commands;
		private readonly ListView _list;
		private readonly List<QueryInfo> _items = new();

		public VisualElement Element => _list;

		private DebugSession _session;

		public QueriesView(IDebuggerCommands commands)
		{
			_commands = commands ?? throw new ArgumentNullException(nameof(commands));
			_list = new ListView(_items, RowHeight, MakeRow, BindRow)
			{
				selectionType = SelectionType.None,
			};
			_list.style.flexGrow = 1f;
		}

		public void Show(DebugSession session)
		{
			_session = session;
			_items.Clear();
			_items.AddRange(session.Queries);
			_list.RefreshItems();
		}

		private VisualElement MakeRow() => new QueryRow(_commands.ShowQueryActors);

		private void BindRow(VisualElement element, int index)
		{
			((QueryRow)element).Bind(index, _items[index], _session);
		}
	}
}