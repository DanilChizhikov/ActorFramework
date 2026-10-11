using System;
using System.Reflection;
using UnityEngine;

namespace DTech.ActorFramework.Diagnostics
{
	internal sealed class DebugField
	{
		public FieldInfo Info { get; }
		public string Label { get; }
		public string Tooltip { get; }
		public string Header { get; }
		public bool IsReadOnly { get; }
		public bool IsMultiline { get; }
		public bool HasRange { get; }
		public float RangeMin { get; }
		public float RangeMax { get; }
		public Type FieldType => Info.FieldType;

		public DebugField(FieldInfo info)
		{
			Info = info ?? throw new ArgumentNullException(nameof(info));
			Label = DebugFieldSelector.NicifyName(info.Name);
			Tooltip = info.GetCustomAttribute<TooltipAttribute>()?.tooltip;
			Header = info.GetCustomAttribute<HeaderAttribute>()?.header;
			IsReadOnly = info.IsInitOnly;
			IsMultiline = info.IsDefined(typeof(TextAreaAttribute), false) ||
				info.IsDefined(typeof(MultilineAttribute), false);

			RangeAttribute range = info.GetCustomAttribute<RangeAttribute>();
			HasRange = range != null;
			RangeMin = range?.min ?? 0f;
			RangeMax = range?.max ?? 0f;
		}
	}
}