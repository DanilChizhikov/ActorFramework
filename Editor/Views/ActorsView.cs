using System;
using System.Collections.Generic;
using DTech.ActorFramework.Diagnostics;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal sealed class ActorsView
	{
		private const string RootClass = "actors-view";
		private const string QueryChipClass = "actors-view__query-chip";
		private const string ListClass = "actors-view__list";
		private const float RowHeight = 20f;
		private const float ListWidth = 260f;

		private readonly IDebuggerCommands _commands;
		private readonly VisualElement _root;
		private readonly ToolbarSearchField _search;
		private readonly EnumField _filter;
		private readonly VisualElement _queryChip;
		private readonly Label _queryLabel;
		private readonly ListView _list;
		private readonly ActorDetailsView _details;
		private readonly List<ActorSummary> _items = new();

		public VisualElement Element => _root;

		private DebugSession _session;
		private int _shownRevision = -1;
		private int _syncedActorId = DebugSession.None;

		public ActorsView(IDebuggerCommands commands)
		{
			_commands = commands ?? throw new ArgumentNullException(nameof(commands));

			_root = new VisualElement();
			_root.AddToClassList(RootClass);
			_root.style.flexGrow = 1f;

			var toolbar = new Toolbar();
			_search = new ToolbarSearchField();
			_search.style.flexGrow = 1f;
			_search.tooltip = "Actor id (12 or #12) or part of a feature name";
			_search.RegisterValueChangedCallback(SearchChangedHandler);
			_filter = new EnumField(ActorFilter.All);
			_filter.RegisterValueChangedCallback(FilterChangedHandler);
			toolbar.Add(_search);
			toolbar.Add(_filter);
			_root.Add(toolbar);

			_queryChip = new VisualElement();
			_queryChip.AddToClassList(QueryChipClass);
			_queryLabel = new Label();
			_queryLabel.style.flexGrow = 1f;
			_queryChip.Add(_queryLabel);
			_queryChip.Add(new Button(ClearQueryClickedHandler) { text = "×", tooltip = "Show all actors" });
			_root.Add(_queryChip);

			var split = new TwoPaneSplitView(0, ListWidth, TwoPaneSplitViewOrientation.Horizontal);
			split.style.flexGrow = 1f;
			_list = new ListView(_items, RowHeight, MakeRow, BindRow)
			{
				selectionType = SelectionType.Single,
			};
			_list.AddToClassList(ListClass);
			_list.selectionChanged += SelectionChangedHandler;
			_details = new ActorDetailsView(commands);
			split.Add(_list);
			split.Add(_details.Element);
			_root.Add(split);
		}

		public void Show(DebugSession session)
		{
			_session = session;
			if (session.ActorsRevision != _shownRevision)
			{
				RebuildItems();
			}
			else if (session.SelectedActorId != _syncedActorId)
			{
				SyncSelection();
			}

			UpdateQueryChip();
			_details.Show(session);
		}

		private static VisualElement MakeRow() => new ActorRow();

		private static bool MatchesFilter(ActorSummary actor, ActorFilter filter)
		{
			switch (filter)
			{
				case ActorFilter.Active:
					return actor.IsActive;
				case ActorFilter.Inactive:
					return !actor.IsActive;
				default:
					return true;
			}
		}

		private void BindRow(VisualElement element, int index)
		{
			((ActorRow)element).Bind(_items[index], _session);
		}

		private void RebuildItems()
		{
			_items.Clear();
			if (_session == null)
			{
				_list.RefreshItems();
				return;
			}

			_shownRevision = _session.ActorsRevision;
			var search = new ActorSearch(_search.value);
			var filter = (ActorFilter)_filter.value;
			ActorSummary[] actors = _session.Actors;
			for (int i = 0; i < actors.Length; i++)
			{
				ActorSummary actor = actors[i];
				if (MatchesFilter(actor, filter) && search.Matches(actor, _session))
				{
					_items.Add(actor);
				}
			}

			_list.RefreshItems();
			_syncedActorId = DebugSession.None;
			SyncSelection();
		}

		private void SyncSelection()
		{
			int actorId = _session.SelectedActorId;
			int index = _items.FindIndex(item => item.Id == actorId);
			if (index < 0)
			{
				_list.SetSelectionWithoutNotify(Array.Empty<int>());
			}
			else
			{
				_list.SetSelectionWithoutNotify(new[] { index });
				if (actorId != _syncedActorId)
				{
					_list.ScrollToItem(index);
				}
			}

			_syncedActorId = actorId;
		}

		private void UpdateQueryChip()
		{
			int queryIndex = _session.QueryIndex;
			QueryInfo[] queries = _session.Queries;
			bool hasQuery = queryIndex >= 0;
			_queryChip.style.display = hasQuery ? DisplayStyle.Flex : DisplayStyle.None;
			if (!hasQuery)
			{
				return;
			}

			_queryLabel.text = queryIndex < queries.Length
				? $"Query #{queryIndex}: {_session.DescribeQuery(queries[queryIndex])}"
				: $"Query #{queryIndex}";
		}

		private void SearchChangedHandler(ChangeEvent<string> evt) => RebuildItems();

		private void FilterChangedHandler(ChangeEvent<Enum> evt) => RebuildItems();

		private void ClearQueryClickedHandler() => _commands.ShowQueryActors(DebugSession.None);

		private void SelectionChangedHandler(IEnumerable<object> selection)
		{
			foreach (object item in selection)
			{
				if (item is ActorSummary summary)
				{
					_commands.SelectActor(summary.Id);
				}

				return;
			}
		}
	}
}