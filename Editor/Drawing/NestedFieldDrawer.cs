using System;
using System.Collections.Generic;
using DTech.ActorFramework.Diagnostics;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal sealed class NestedFieldDrawer : FieldDrawer
	{
		private const string FoldoutClass = "field-drawer__foldout";
		private const string NoteClass = "field-drawer__note";
		private const string EmptyText = "No visible fields";

		private readonly FieldDrawerFactory _factory;
		private readonly VisualElement _container;
		private readonly Foldout _foldout;
		private readonly Label _note;
		private readonly List<FieldDrawer> _children = new();
		private readonly bool _isCollection;

		public NestedFieldDrawer(FieldNode node, int[] path, IFieldDrawerHost host, FieldDrawerFactory factory)
			: base(node, path, host)
		{
			_factory = factory;
			_isCollection = node.Kind == FieldKind.Collection;

			if (path.Length == 0)
			{
				_container = new VisualElement();
			}
			else
			{
				_foldout = new Foldout { value = false };
				_foldout.AddToClassList(FoldoutClass);
				_foldout.tooltip = node.TypeName;
				_container = _foldout;
			}

			_note = new Label();
			_note.AddToClassList(NoteClass);
			AddContent(_container);

			FieldNode[] children = node.Children ?? Array.Empty<FieldNode>();
			for (int i = 0; i < children.Length; i++)
			{
				FieldDrawer child = _factory.Create(children[i], CreateChildPath(i));
				_children.Add(child);
				_container.Add(child.Element);
			}

			_container.Add(_note);
			Update(node);
		}

		public override bool CanUpdate(FieldNode node)
		{
			return base.CanUpdate(node) && (node.Children?.Length ?? 0) == _children.Count;
		}

		public override void Update(FieldNode node)
		{
			FieldNode[] children = node.Children ?? Array.Empty<FieldNode>();
			for (int i = 0; i < _children.Count; i++)
			{
				FieldDrawer child = _children[i];
				if (child.CanUpdate(children[i]))
				{
					child.Update(children[i]);
					continue;
				}

				FieldDrawer replacement = _factory.Create(children[i], CreateChildPath(i));
				_container.Insert(_container.IndexOf(child.Element), replacement.Element);
				child.Element.RemoveFromHierarchy();
				_children[i] = replacement;
			}

			if (_foldout != null)
			{
				_foldout.text = _isCollection ? $"{node.Label} [{node.Integer}]" : node.Label;
			}

			UpdateNote(node);
		}

		private void UpdateNote(FieldNode node)
		{
			string text = null;
			if (_isCollection && node.Integer > _children.Count)
			{
				text = $"… {node.Integer - _children.Count} more";
			}
			else if (!_isCollection && _children.Count == 0)
			{
				text = EmptyText;
			}

			_note.text = text;
			_note.style.display = text == null ? DisplayStyle.None : DisplayStyle.Flex;
		}

		private int[] CreateChildPath(int index)
		{
			var path = new int[Path.Length + 1];
			Array.Copy(Path, path, Path.Length);
			path[Path.Length] = index;

			return path;
		}
	}
}