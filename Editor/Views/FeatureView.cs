using System;
using DTech.ActorFramework.Diagnostics;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal sealed class FeatureView : IFieldDrawerHost
	{
		private const string RootClass = "feature-view";

		private readonly IDebuggerCommands _commands;
		private readonly FieldDrawerFactory _factory;
		private readonly Foldout _foldout;
		private readonly int _worldId;
		private readonly int _actorId;
		private readonly int _typeId;

		public VisualElement Element => _foldout;
		public int TypeId => _typeId;
		public bool IsRemote => _commands.IsRemote;

		private FieldDrawer _drawer;

		public FeatureView(IDebuggerCommands commands, FeatureTypeInfo type, int worldId, int actorId, int typeId)
		{
			_commands = commands;
			_worldId = worldId;
			_actorId = actorId;
			_typeId = typeId;
			_factory = new FieldDrawerFactory(this);

			_foldout = new Foldout
			{
				text = type?.Name ?? $"#{typeId}",
				tooltip = type?.FullName,
				value = true,
			};
			_foldout.AddToClassList(RootClass);
		}

		public void Update(FieldNode root)
		{
			if (_drawer != null && _drawer.CanUpdate(root))
			{
				_drawer.Update(root);
				return;
			}

			_drawer?.Element.RemoveFromHierarchy();
			_drawer = _factory.Create(root, Array.Empty<int>());
			_foldout.Add(_drawer.Element);
		}

		public void SubmitField(int[] path, FieldNode value)
		{
			_commands.SetField(new SetFieldRequest
			{
				WorldId = _worldId,
				ActorId = _actorId,
				FeatureTypeId = _typeId,
				Path = path,
				Value = value,
			});
		}

		public void SelectActor(int actorId) => _commands.SelectActor(actorId);
	}
}