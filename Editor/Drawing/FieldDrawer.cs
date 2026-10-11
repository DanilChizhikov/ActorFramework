using DTech.ActorFramework.Diagnostics;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal abstract class FieldDrawer
	{
		private const string RootClass = "field-drawer";
		private const string HeaderClass = "field-drawer__header";

		private readonly VisualElement _root;
		private readonly FieldKind _kind;
		private readonly FieldFlags _flags;
		private readonly int _enumCount;

		public VisualElement Element => _root;

		protected IFieldDrawerHost Host { get; }
		protected int[] Path { get; }

		protected FieldDrawer(FieldNode node, int[] path, IFieldDrawerHost host)
		{
			_kind = node.Kind;
			_flags = node.Flags;
			_enumCount = node.EnumNames?.Length ?? 0;
			Host = host;
			Path = path;

			_root = new VisualElement();
			_root.AddToClassList(RootClass);
			if (string.IsNullOrEmpty(node.Header))
			{
				return;
			}

			var header = new Label(node.Header);
			header.AddToClassList(HeaderClass);
			_root.Add(header);
		}

		public virtual bool CanUpdate(FieldNode node)
		{
			return node.Kind == _kind && node.Flags == _flags && (node.EnumNames?.Length ?? 0) == _enumCount;
		}

		public abstract void Update(FieldNode node);

		protected static void SetupField(VisualElement field, FieldNode node)
		{
			field.AddToClassList(BaseField<int>.alignedFieldUssClassName);
			field.tooltip = string.IsNullOrEmpty(node.Tooltip) ? node.TypeName : node.Tooltip;
		}

		protected void AddContent(VisualElement content) => _root.Add(content);

		protected void Submit(FieldNode value) => Host.SubmitField(Path, value);
	}
}