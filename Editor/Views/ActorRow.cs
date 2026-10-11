using System.Text;
using DTech.ActorFramework.Diagnostics;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal sealed class ActorRow : VisualElement
	{
		private const string RootClass = "actor-row";
		private const string StatusClass = "actor-row__status";
		private const string StatusActiveClass = "actor-row__status--active";
		private const string IdClass = "actor-row__id";
		private const string FeaturesClass = "actor-row__features";

		private readonly StringBuilder _builder = new();
		private readonly VisualElement _status;
		private readonly Label _id;
		private readonly Label _features;

		public ActorRow()
		{
			AddToClassList(RootClass);

			_status = new VisualElement();
			_status.AddToClassList(StatusClass);
			_id = new Label();
			_id.AddToClassList(IdClass);
			_features = new Label();
			_features.AddToClassList(FeaturesClass);

			Add(_status);
			Add(_id);
			Add(_features);
		}

		public void Bind(ActorSummary summary, DebugSession session)
		{
			_status.EnableInClassList(StatusActiveClass, summary.IsActive);
			_status.tooltip = summary.IsActive ? "Active" : "Inactive";
			_id.text = $"Actor {summary.Id}";

			_builder.Clear();
			int[] typeIds = summary.FeatureTypeIds;
			for (int i = 0; i < typeIds.Length; i++)
			{
				if (i > 0)
				{
					_builder.Append(", ");
				}

				_builder.Append(session.GetFeatureTypeName(typeIds[i]));
			}

			_features.text = _builder.ToString();
		}
	}
}