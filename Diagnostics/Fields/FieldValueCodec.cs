using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DTech.ActorFramework.Diagnostics
{
	internal static class FieldValueCodec
	{
		private const int NoActor = -1;

		private static readonly Dictionary<Type, string[]> _enumNames = new();
		private static readonly Dictionary<Type, long[]> _enumValues = new();

		public static FieldKind GetKind(Type type)
		{
			_ = type ?? throw new ArgumentNullException(nameof(type));

			if (type == typeof(bool))
			{
				return FieldKind.Bool;
			}

			if (type.IsEnum)
			{
				return type.IsDefined(typeof(FlagsAttribute), false) ? FieldKind.Flags : FieldKind.Enum;
			}

			if (type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort) ||
				type == typeof(int) || type == typeof(uint) || type == typeof(long))
			{
				return FieldKind.Integer;
			}

			if (type == typeof(ulong))
			{
				return FieldKind.UInteger;
			}

			if (type == typeof(float) || type == typeof(double))
			{
				return FieldKind.Float;
			}

			if (type == typeof(char))
			{
				return FieldKind.Char;
			}

			if (type == typeof(string))
			{
				return FieldKind.String;
			}

			if (TryGetUnityKind(type, out FieldKind unityKind))
			{
				return unityKind;
			}

			if (type == typeof(Actor))
			{
				return FieldKind.ActorRef;
			}

			if (typeof(Object).IsAssignableFrom(type))
			{
				return FieldKind.ObjectRef;
			}

			if (IsCollection(type))
			{
				return FieldKind.Collection;
			}

			return IsOpaque(type) ? FieldKind.Text : FieldKind.Composite;
		}

		public static bool IsCollection(Type type)
		{
			return type.IsArray ? type.GetArrayRank() == 1 : typeof(IList).IsAssignableFrom(type);
		}

		public static Type GetElementType(Type collectionType)
		{
			if (collectionType.IsArray)
			{
				return collectionType.GetElementType();
			}

			Type[] interfaces = collectionType.GetInterfaces();
			for (int i = 0; i < interfaces.Length; i++)
			{
				Type candidate = interfaces[i];
				if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IList<>))
				{
					return candidate.GetGenericArguments()[0];
				}
			}

			return typeof(object);
		}

		public static void Encode(object value, FieldNode node)
		{
			switch (node.Kind)
			{
				case FieldKind.Integer:
					node.Integer = Convert.ToInt64(value);
					break;
				
				case FieldKind.UInteger:
					node.Integer = unchecked((long)(ulong)value);
					break;
				
				case FieldKind.Float:
					node.Float = Convert.ToDouble(value);
					break;
				
				case FieldKind.Bool:
					node.Integer = (bool)value ? 1L : 0L;
					break;
				
				case FieldKind.Char:
					node.Integer = (char)value;
					break;
				
				case FieldKind.String:
					node.Text = (string)value;
					break;
				
				case FieldKind.Enum:
				case FieldKind.Flags:
					EncodeEnum(value, node);
					break;
				
				case FieldKind.LayerMask:
					node.Integer = ((LayerMask)value).value;
					break;
				
				case FieldKind.ActorRef:
					EncodeActor(value as Actor, node);
					break;
				
				case FieldKind.ObjectRef:
					EncodeObject(value as Object, node);
					break;
				default:
					node.Components = EncodeComponents(value, node.Kind);
					break;
			}
		}

		public static object Decode(FieldNode node, Type type)
		{
			_ = node ?? throw new ArgumentNullException(nameof(node));
			_ = type ?? throw new ArgumentNullException(nameof(type));

			switch (node.Kind)
			{
				case FieldKind.Integer:
					return DecodeInteger(node.Integer, type);
				case FieldKind.UInteger:
					return unchecked((ulong)node.Integer);
				case FieldKind.Float:
					return type == typeof(float) ? (object)(float)node.Float : node.Float;
				case FieldKind.Bool:
					return node.Integer != 0L;
				case FieldKind.Char:
					return (char)node.Integer;
				case FieldKind.String:
					return node.Text;
				case FieldKind.Enum:
				case FieldKind.Flags:
					return Enum.ToObject(type, node.Integer);
				case FieldKind.LayerMask:
					return (LayerMask)(int)node.Integer;
				case FieldKind.Vector2:
					return new Vector2(GetFloat(node, 0), GetFloat(node, 1));
				case FieldKind.Vector3:
					return new Vector3(GetFloat(node, 0), GetFloat(node, 1), GetFloat(node, 2));
				case FieldKind.Vector4:
					return new Vector4(GetFloat(node, 0), GetFloat(node, 1), GetFloat(node, 2), GetFloat(node, 3));
				case FieldKind.Vector2Int:
					return new Vector2Int(GetInt(node, 0), GetInt(node, 1));
				case FieldKind.Vector3Int:
					return new Vector3Int(GetInt(node, 0), GetInt(node, 1), GetInt(node, 2));
				case FieldKind.Quaternion:
					return Quaternion.Euler(GetFloat(node, 0), GetFloat(node, 1), GetFloat(node, 2));
				case FieldKind.Color:
					return DecodeColor(node, type);
				case FieldKind.Rect:
					return DecodeRect(node, type);
				case FieldKind.Bounds:
					return DecodeBounds(node, type);
				default:
					throw new NotSupportedException($"Field kind '{node.Kind}' cannot be written.");
			}
		}

		private static bool TryGetUnityKind(Type type, out FieldKind kind)
		{
			if (type == typeof(Vector2))
			{
				kind = FieldKind.Vector2;
			}
			else if (type == typeof(Vector3))
			{
				kind = FieldKind.Vector3;
			}
			else if (type == typeof(Vector4))
			{
				kind = FieldKind.Vector4;
			}
			else if (type == typeof(Vector2Int))
			{
				kind = FieldKind.Vector2Int;
			}
			else if (type == typeof(Vector3Int))
			{
				kind = FieldKind.Vector3Int;
			}
			else if (type == typeof(Quaternion))
			{
				kind = FieldKind.Quaternion;
			}
			else if (type == typeof(Color) || type == typeof(Color32))
			{
				kind = FieldKind.Color;
			}
			else if (type == typeof(Rect) || type == typeof(RectInt))
			{
				kind = FieldKind.Rect;
			}
			else if (type == typeof(Bounds) || type == typeof(BoundsInt))
			{
				kind = FieldKind.Bounds;
			}
			else if (type == typeof(LayerMask))
			{
				kind = FieldKind.LayerMask;
			}
			else
			{
				kind = FieldKind.Null;
				return false;
			}

			return true;
		}

		private static bool IsOpaque(Type type)
		{
			if (type.IsPrimitive || type.IsPointer || type.IsInterface || type.IsAbstract || type == typeof(object) ||
				type == typeof(decimal) || typeof(Delegate).IsAssignableFrom(type))
			{
				return true;
			}

			if (type == typeof(ActorWorld) || type == typeof(ActorQuery))
			{
				return true;
			}

			string ns = type.Namespace;

			return ns != null && (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal));
		}

		private static void EncodeEnum(object value, FieldNode node)
		{
			Type type = value.GetType();
			node.Integer = ToInt64(value, Enum.GetUnderlyingType(type));

			if (!_enumNames.TryGetValue(type, out string[] names))
			{
				names = Enum.GetNames(type);
				Array values = Enum.GetValues(type);
				var longs = new long[values.Length];
				for (int i = 0; i < longs.Length; i++)
				{
					longs[i] = ToInt64(values.GetValue(i), Enum.GetUnderlyingType(type));
				}

				_enumNames.Add(type, names);
				_enumValues.Add(type, longs);
			}

			node.EnumNames = names;
			node.EnumValues = _enumValues[type];
		}

		private static long ToInt64(object value, Type underlyingType)
		{
			return underlyingType == typeof(ulong) ? unchecked((long)Convert.ToUInt64(value)) : Convert.ToInt64(value);
		}

		private static void EncodeActor(Actor actor, FieldNode node)
		{
			if (actor == null)
			{
				node.Integer = NoActor;
				return;
			}

			node.Integer = actor.IsAlive ? actor.Id : NoActor;
			node.Text = actor.IsAlive ? actor.ToString() : $"{actor} (destroyed)";
		}

		private static void EncodeObject(Object value, FieldNode node)
		{
			if (value == null)
			{
				return;
			}

			node.Integer = value.GetInstanceID();
			node.Text = value.name;
		}

		private static double[] EncodeComponents(object value, FieldKind kind)
		{
			switch (value)
			{
				case Vector2 vector:
					return new double[] { vector.x, vector.y };
				case Vector3 vector:
					return new double[] { vector.x, vector.y, vector.z };
				case Vector4 vector:
					return new double[] { vector.x, vector.y, vector.z, vector.w };
				case Vector2Int vector:
					return new double[] { vector.x, vector.y };
				case Vector3Int vector:
					return new double[] { vector.x, vector.y, vector.z };
				case Quaternion rotation:
					Vector3 euler = rotation.eulerAngles;
					return new double[] { euler.x, euler.y, euler.z };
				case Color color:
					return new double[] { color.r, color.g, color.b, color.a };
				case Color32 color32:
					Color converted = color32;
					return new double[] { converted.r, converted.g, converted.b, converted.a };
				case Rect rect:
					return new double[] { rect.x, rect.y, rect.width, rect.height };
				case RectInt rect:
					return new double[] { rect.x, rect.y, rect.width, rect.height };
				case Bounds bounds:
					return new double[]
					{
						bounds.center.x, bounds.center.y, bounds.center.z, bounds.size.x, bounds.size.y, bounds.size.z,
					};
				case BoundsInt bounds:
					return new double[]
					{
						bounds.position.x, bounds.position.y, bounds.position.z, bounds.size.x, bounds.size.y,
						bounds.size.z,
					};
				default:
					throw new NotSupportedException($"Field kind '{kind}' has no component encoding.");
			}
		}

		private static object DecodeInteger(long value, Type type)
		{
			if (type == typeof(sbyte))
			{
				return (sbyte)Clamp(value, sbyte.MinValue, sbyte.MaxValue);
			}

			if (type == typeof(byte))
			{
				return (byte)Clamp(value, byte.MinValue, byte.MaxValue);
			}

			if (type == typeof(short))
			{
				return (short)Clamp(value, short.MinValue, short.MaxValue);
			}

			if (type == typeof(ushort))
			{
				return (ushort)Clamp(value, ushort.MinValue, ushort.MaxValue);
			}

			if (type == typeof(int))
			{
				return (int)Clamp(value, int.MinValue, int.MaxValue);
			}

			if (type == typeof(uint))
			{
				return (uint)Clamp(value, uint.MinValue, uint.MaxValue);
			}

			return value;
		}

		private static long Clamp(long value, long min, long max)
		{
			return value < min ? min : value > max ? max : value;
		}

		private static object DecodeColor(FieldNode node, Type type)
		{
			var color = new Color(GetFloat(node, 0), GetFloat(node, 1), GetFloat(node, 2), GetFloat(node, 3));

			return type == typeof(Color32) ? (object)(Color32)color : color;
		}

		private static object DecodeRect(FieldNode node, Type type)
		{
			if (type == typeof(RectInt))
			{
				return new RectInt(GetInt(node, 0), GetInt(node, 1), GetInt(node, 2), GetInt(node, 3));
			}

			return new Rect(GetFloat(node, 0), GetFloat(node, 1), GetFloat(node, 2), GetFloat(node, 3));
		}

		private static object DecodeBounds(FieldNode node, Type type)
		{
			if (type == typeof(BoundsInt))
			{
				var position = new Vector3Int(GetInt(node, 0), GetInt(node, 1), GetInt(node, 2));
				var sizeInt = new Vector3Int(GetInt(node, 3), GetInt(node, 4), GetInt(node, 5));
				return new BoundsInt(position, sizeInt);
			}

			var center = new Vector3(GetFloat(node, 0), GetFloat(node, 1), GetFloat(node, 2));
			var size = new Vector3(GetFloat(node, 3), GetFloat(node, 4), GetFloat(node, 5));
			return new Bounds(center, size);
		}

		private static float GetFloat(FieldNode node, int index) => (float)GetComponent(node, index);

		private static int GetInt(FieldNode node, int index) => (int)Math.Round(GetComponent(node, index));

		private static double GetComponent(FieldNode node, int index)
		{
			double[] components = node.Components;
			if (components == null || index >= components.Length)
			{
				throw new ArgumentException($"Field kind '{node.Kind}' needs at least {index + 1} components.", nameof(node));
			}

			return components[index];
		}
	}
}