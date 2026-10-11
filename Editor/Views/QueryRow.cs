using System;
using DTech.ActorFramework.Diagnostics;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal sealed class QueryRow : VisualElement
	{
		private const string RootClass = "debug-row";
		private const string IndexClass = "debug-row__index";
		private const string CountClass = "debug-row__count";
		private const string NameClass = "debug-row__name";

		private readonly Action<int> _showActors;
		private readonly Label _index;
		private readonly Label _count;
		private readonly Label _description;

		private int _queryIndex = DebugSession.None;

		public QueryRow(Action<int> showActors)
		{
			_showActors = showActors;
			AddToClassList(RootClass);

			_index = new Label();
			_index.AddToClassList(IndexClass);
			_count = new Label();
			_count.AddToClassList(CountClass);
			_description = new Label();
			_description.AddToClassList(NameClass);

			Add(_index);
			Add(_count);
			Add(_description);
			Add(new Button(ShowActorsClickedHandler) { text = "Actors", tooltip = "Show the actors of this query" });
		}

		public void Bind(int index, QueryInfo query, DebugSession session)
		{
			_queryIndex = index;
			_index.text = $"#{index}";
			_count.text = query.Count.ToString();
			_count.tooltip = "Matching actors";
			_description.text = session.DescribeQuery(query);
		}

		private void ShowActorsClickedHandler()
		{
			if (_queryIndex == DebugSession.None)
			{
				return;
			}

			_showActors(_queryIndex);
		}
	}
}