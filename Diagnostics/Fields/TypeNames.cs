using System;
using System.Collections.Generic;
using System.Text;

namespace DTech.ActorFramework.Diagnostics
{
	internal static class TypeNames
	{
		private static readonly Dictionary<Type, string> _cache = new();

		public static string GetDisplayName(Type type)
		{
			_ = type ?? throw new ArgumentNullException(nameof(type));

			if (_cache.TryGetValue(type, out string name))
			{
				return name;
			}

			name = Build(type);
			_cache.Add(type, name);

			return name;
		}

		private static string Build(Type type)
		{
			if (type.IsArray)
			{
				return GetDisplayName(type.GetElementType()) + "[]";
			}

			string name = type.Name;
			if (type.IsGenericType)
			{
				int tickIndex = name.IndexOf('`');
				if (tickIndex >= 0)
				{
					name = name.Substring(0, tickIndex);
				}

				var builder = new StringBuilder(name).Append('<');
				Type[] arguments = type.GetGenericArguments();
				for (int i = 0; i < arguments.Length; i++)
				{
					if (i > 0)
					{
						builder.Append(", ");
					}

					builder.Append(GetDisplayName(arguments[i]));
				}

				name = builder.Append('>').ToString();
			}

			if (type.IsNested && !type.IsGenericParameter)
			{
				name = GetDisplayName(type.DeclaringType) + "." + name;
			}

			return name;
		}
	}
}