using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DTech.ActorFramework.Diagnostics
{
	internal static class DebugFieldSelector
	{
		private const BindingFlags FieldFlags =
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
		private const string BackingFieldSuffix = ">k__BackingField";

		private static readonly Dictionary<Type, DebugField[]> _cache = new();

		public static DebugField[] GetFields(Type type)
		{
			_ = type ?? throw new ArgumentNullException(nameof(type));

			if (_cache.TryGetValue(type, out DebugField[] fields))
			{
				return fields;
			}

			fields = Collect(type);
			_cache.Add(type, fields);

			return fields;
		}

		public static string NicifyName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return name;
			}

			if (name[0] == '<')
			{
				int suffixIndex = name.IndexOf(BackingFieldSuffix, StringComparison.Ordinal);
				if (suffixIndex > 1)
				{
					name = name.Substring(1, suffixIndex - 1);
				}
			}

			if (name.Length > 2 && (name[0] == 'm' || name[0] == 'k') && name[1] == '_')
			{
				name = name.Substring(2);
			}

			name = name.TrimStart('_');
			if (name.Length == 0)
			{
				return name;
			}

			var builder = new StringBuilder(name.Length + 4);
			builder.Append(char.ToUpperInvariant(name[0]));
			for (int i = 1; i < name.Length; i++)
			{
				char current = name[i];
				char previous = name[i - 1];
				if (current == '_')
				{
					builder.Append(' ');
					continue;
				}

				bool isWordStart = char.IsUpper(current) && (char.IsLower(previous) || char.IsDigit(previous) ||
					(char.IsUpper(previous) && i + 1 < name.Length && char.IsLower(name[i + 1])));
				bool isNumberStart = char.IsDigit(current) && char.IsLetter(previous);
				if ((isWordStart || isNumberStart) && previous != '_')
				{
					builder.Append(' ');
				}

				builder.Append(current);
			}

			return builder.ToString();
		}
		
		private static bool IsVisible(FieldInfo field)
		{
			if (field.IsStatic || field.IsDefined(typeof(HideInActorDebuggerAttribute), false))
			{
				return false;
			}

			return field.IsPublic || field.IsDefined(typeof(ShowInActorDebuggerAttribute), false);
		}

		private static DebugField[] Collect(Type type)
		{
			var hierarchy = new List<Type>();
			for (Type current = type; current != null && current != typeof(object) && current != typeof(ValueType);
				current = current.BaseType)
			{
				hierarchy.Add(current);
			}

			var fields = new List<DebugField>();
			for (int i = hierarchy.Count - 1; i >= 0; i--)
			{
				FieldInfo[] declared = hierarchy[i].GetFields(FieldFlags);
				for (int j = 0; j < declared.Length; j++)
				{
					if (IsVisible(declared[j]))
					{
						fields.Add(new DebugField(declared[j]));
					}
				}
			}

			return fields.ToArray();
		}
	}
}