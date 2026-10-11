using System;
using System.Collections.Generic;
using DTech.ActorFramework.Diagnostics;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal sealed class ActorDetailsView
	{
		private const string RootClass = "actor-details";
		private const string TitleClass = "actor-details__title";
		private const string StateClass = "actor-details__state";
		private const string StateActiveClass = "actor-details__state--active";
		private const string StateInactiveClass = "actor-details__state--inactive";
		private const string StateDestroyedClass = "actor-details__state--destroyed";
		private const string TokensClass = "actor-details__tokens";
		private const string MessageClass = "actor-details__message";
		private const string FeaturesClass = "actor-details__features";

		private readonly IDebuggerCommands _commands;
		private readonly ScrollView _root;
		private readonly VisualElement _content;
		private readonly Label _title;
		private readonly Label _state;
		private readonly Label _activators;
		private readonly Label _deactivators;
		private readonly Label _message;
		private readonly VisualElement _features;
		private readonly List<FeatureView> _featureViews = new();

		public VisualElement Element => _root;

		private int _worldId = DebugSession.None;
		private int _actorId = DebugSession.None;

		public ActorDetailsView(IDebuggerCommands commands)
		{
			_commands = commands ?? throw new ArgumentNullException(nameof(commands));

			_root = new ScrollView(ScrollViewMode.Vertical);
			_root.AddToClassList(RootClass);
			_root.AddToClassList(InspectorElement.ussClassName);

			_message = new Label();
			_message.AddToClassList(MessageClass);
			_root.Add(_message);

			_content = new VisualElement();
			_root.Add(_content);

			var header = new VisualElement();
			header.style.flexDirection = FlexDirection.Row;
			_title = new Label();
			_title.AddToClassList(TitleClass);
			_state = new Label();
			_state.AddToClassList(StateClass);
			header.Add(_title);
			header.Add(_state);
			_content.Add(header);

			_activators = new Label();
			_activators.AddToClassList(TokensClass);
			_deactivators = new Label();
			_deactivators.AddToClassList(TokensClass);
			_content.Add(_activators);
			_content.Add(_deactivators);

			_features = new VisualElement();
			_features.AddToClassList(FeaturesClass);
			_content.Add(_features);

			ShowMessage("Select an actor.");
		}

		public void Show(DebugSession session)
		{
			int worldId = session.SelectedWorldId;
			int actorId = session.SelectedActorId;
			if (worldId != _worldId || actorId != _actorId)
			{
				ClearFeatures();
				_worldId = worldId;
				_actorId = actorId;
			}

			if (actorId == DebugSession.None)
			{
				ShowMessage("Select an actor.");
				return;
			}

			ActorDetails details = session.SelectedActor;
			if (details == null)
			{
				ShowMessage(session.IsSelectedActorMissing ? $"Actor {actorId} is destroyed." : "Loading…");
				return;
			}

			_message.style.display = DisplayStyle.None;
			_content.style.display = DisplayStyle.Flex;

			_title.text = $"Actor {details.Id}";
			UpdateState(details);
			_activators.text = DescribeTokens("Activators", details.Activators);
			_deactivators.text = DescribeTokens("Deactivators", details.Deactivators);
			UpdateFeatures(session, details.Features ?? Array.Empty<FeatureDetails>());
		}

		private static string DescribeTokens(string title, string[] tokens)
		{
			int count = tokens?.Length ?? 0;

			return count == 0 ? $"{title}: none" : $"{title} ({count}): {string.Join(", ", tokens)}";
		}

		private void UpdateState(ActorDetails details)
		{
			bool isDestroyed = !details.IsAlive;
			bool isActive = details.IsAlive && details.IsActive;
			_state.text = isDestroyed ? "Destroyed" : isActive ? "Active" : "Inactive";
			_state.EnableInClassList(StateActiveClass, isActive);
			_state.EnableInClassList(StateInactiveClass, !isActive && !isDestroyed);
			_state.EnableInClassList(StateDestroyedClass, isDestroyed);
		}

		private void UpdateFeatures(DebugSession session, FeatureDetails[] features)
		{
			if (!HasSameFeatures(features))
			{
				ClearFeatures();
				for (int i = 0; i < features.Length; i++)
				{
					int typeId = features[i].TypeId;
					var view = new FeatureView(_commands, session.GetFeatureType(typeId), _worldId, _actorId, typeId);
					_featureViews.Add(view);
					_features.Add(view.Element);
				}
			}

			for (int i = 0; i < features.Length; i++)
			{
				_featureViews[i].Update(features[i].Root);
			}
		}

		private bool HasSameFeatures(FeatureDetails[] features)
		{
			if (features.Length != _featureViews.Count)
			{
				return false;
			}

			for (int i = 0; i < features.Length; i++)
			{
				if (features[i].TypeId != _featureViews[i].TypeId)
				{
					return false;
				}
			}

			return true;
		}

		private void ClearFeatures()
		{
			_featureViews.Clear();
			_features.Clear();
		}

		private void ShowMessage(string text)
		{
			_message.text = text;
			_message.style.display = DisplayStyle.Flex;
			_content.style.display = DisplayStyle.None;
		}
	}
}