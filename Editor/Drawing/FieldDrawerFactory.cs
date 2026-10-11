using System;
using System.Collections.Generic;
using DTech.ActorFramework.Diagnostics;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.ActorFramework.Editor
{
	internal sealed class FieldDrawerFactory
	{
		private const int MaxMaskBits = 32;

		private readonly IFieldDrawerHost _host;

		public FieldDrawerFactory(IFieldDrawerHost host)
		{
			_host = host ?? throw new ArgumentNullException(nameof(host));
		}

		public FieldDrawer Create(FieldNode node, int[] path)
		{
			switch (node.Kind)
			{
				case FieldKind.Composite:
				case FieldKind.Collection:
					return new NestedFieldDrawer(node, path, _host, this);
				case FieldKind.ActorRef:
					return new ActorRefFieldDrawer(node, path, _host);
				case FieldKind.ObjectRef:
					return new ObjectRefFieldDrawer(node, path, _host);
				case FieldKind.Enum when node.EnumNames?.Length > 0:
					return CreateEnum(node, path);
				case FieldKind.Flags when node.EnumNames?.Length > 0:
					return CreateFlags(node, path);
				default:
					return CreateValue(node, path) ?? new ReadOnlyFieldDrawer(node, path, _host);
			}
		}

		private static float GetFloat(FieldNode node, int index)
		{
			double[] components = node.Components;

			return components != null && index < components.Length ? (float)components[index] : 0f;
		}

		private static int GetInt(FieldNode node, int index) => Mathf.RoundToInt(GetFloat(node, index));

		private static string FindEnumName(string[] names, long[] values, long value)
		{
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i] == value)
				{
					return names[i];
				}
			}

			return value.ToString();
		}

		private FieldDrawer CreateValue(FieldNode node, int[] path)
		{
			string label = node.Label;
			switch (node.Kind)
			{
				case FieldKind.Integer:
				case FieldKind.UInteger:
					return node.HasRange
						? Bind(node, path, new ValueFieldBinding<int>(
							new SliderInt(label, (int)node.RangeMin, (int)node.RangeMax) { showInputField = true },
							n => (int)n.Integer, (n, v) => n.Integer = v))
						: Bind(node, path, new ValueFieldBinding<long>(
							new LongField(label) { isDelayed = true },
							n => n.Integer, (n, v) => n.Integer = v));
				case FieldKind.Float:
					return node.HasRange
						? Bind(node, path, new ValueFieldBinding<float>(
							new Slider(label, node.RangeMin, node.RangeMax) { showInputField = true },
							n => (float)n.Float, (n, v) => n.Float = v))
						: Bind(node, path, new ValueFieldBinding<double>(
							new DoubleField(label) { isDelayed = true },
							n => n.Float, (n, v) => n.Float = v));
				case FieldKind.Bool:
					return Bind(node, path, new ValueFieldBinding<bool>(
						new Toggle(label),
						n => n.Integer != 0L, (n, v) => n.Integer = v ? 1L : 0L));
				case FieldKind.String:
					return Bind(node, path, new ValueFieldBinding<string>(
						new TextField(label) { isDelayed = true, multiline = node.IsMultiline },
						n => n.Text ?? string.Empty, (n, v) => n.Text = v));
				case FieldKind.Char:
					return Bind(node, path, new ValueFieldBinding<string>(
						new TextField(label) { isDelayed = true, maxLength = 1 },
						n => n.Integer == 0L ? string.Empty : ((char)n.Integer).ToString(),
						(n, v) => n.Integer = string.IsNullOrEmpty(v) ? 0L : v[0]));
				case FieldKind.Vector2:
					return Bind(node, path, new ValueFieldBinding<Vector2>(
						new Vector2Field(label),
						n => new Vector2(GetFloat(n, 0), GetFloat(n, 1)),
						(n, v) => n.Components = new double[] { v.x, v.y }));
				case FieldKind.Vector3:
				case FieldKind.Quaternion:
					return Bind(node, path, new ValueFieldBinding<Vector3>(
						new Vector3Field(label),
						n => new Vector3(GetFloat(n, 0), GetFloat(n, 1), GetFloat(n, 2)),
						(n, v) => n.Components = new double[] { v.x, v.y, v.z }));
				case FieldKind.Vector4:
					return Bind(node, path, new ValueFieldBinding<Vector4>(
						new Vector4Field(label),
						n => new Vector4(GetFloat(n, 0), GetFloat(n, 1), GetFloat(n, 2), GetFloat(n, 3)),
						(n, v) => n.Components = new double[] { v.x, v.y, v.z, v.w }));
				case FieldKind.Vector2Int:
					return Bind(node, path, new ValueFieldBinding<Vector2Int>(
						new Vector2IntField(label),
						n => new Vector2Int(GetInt(n, 0), GetInt(n, 1)),
						(n, v) => n.Components = new double[] { v.x, v.y }));
				case FieldKind.Vector3Int:
					return Bind(node, path, new ValueFieldBinding<Vector3Int>(
						new Vector3IntField(label),
						n => new Vector3Int(GetInt(n, 0), GetInt(n, 1), GetInt(n, 2)),
						(n, v) => n.Components = new double[] { v.x, v.y, v.z }));
				case FieldKind.Color:
					return Bind(node, path, new ValueFieldBinding<Color>(
						new ColorField(label) { showAlpha = true },
						n => new Color(GetFloat(n, 0), GetFloat(n, 1), GetFloat(n, 2), GetFloat(n, 3)),
						(n, v) => n.Components = new double[] { v.r, v.g, v.b, v.a }));
				case FieldKind.Rect:
					return Bind(node, path, new ValueFieldBinding<Rect>(
						new RectField(label),
						n => new Rect(GetFloat(n, 0), GetFloat(n, 1), GetFloat(n, 2), GetFloat(n, 3)),
						(n, v) => n.Components = new double[] { v.x, v.y, v.width, v.height }));
				case FieldKind.Bounds:
					return Bind(node, path, new ValueFieldBinding<Bounds>(
						new BoundsField(label),
						n => new Bounds(
							new Vector3(GetFloat(n, 0), GetFloat(n, 1), GetFloat(n, 2)),
							new Vector3(GetFloat(n, 3), GetFloat(n, 4), GetFloat(n, 5))),
						(n, v) => n.Components = new double[]
						{
							v.center.x, v.center.y, v.center.z, v.size.x, v.size.y, v.size.z,
						}));
				case FieldKind.LayerMask:
					return Bind(node, path, new ValueFieldBinding<int>(
						new LayerMaskField(label),
						n => (int)n.Integer, (n, v) => n.Integer = v));
				default:
					return null;
			}
		}

		private FieldDrawer CreateEnum(FieldNode node, int[] path)
		{
			string[] names = node.EnumNames;
			long[] values = node.EnumValues;

			return Bind(node, path, new ValueFieldBinding<string>(
				new PopupField<string>(node.Label, new List<string>(names), 0),
				n => FindEnumName(names, values, n.Integer),
				(n, v) =>
				{
					int index = Array.IndexOf(names, v);
					if (index >= 0)
					{
						n.Integer = values[index];
					}
				}));
		}

		private FieldDrawer CreateFlags(FieldNode node, int[] path)
		{
			var choices = new List<string>();
			var bits = new List<long>();
			long representedBits = 0L;
			for (int i = 0; i < node.EnumNames.Length && bits.Count < MaxMaskBits; i++)
			{
				long value = node.EnumValues[i];
				if (value == 0L || (value & (value - 1L)) != 0L || (representedBits & value) != 0L)
				{
					continue;
				}

				choices.Add(node.EnumNames[i]);
				bits.Add(value);
				representedBits |= value;
			}

			return Bind(node, path, new ValueFieldBinding<int>(
				new MaskField(node.Label, choices, 0),
				n =>
				{
					int mask = 0;
					for (int i = 0; i < bits.Count; i++)
					{
						if ((n.Integer & bits[i]) != 0L)
						{
							mask |= 1 << i;
						}
					}

					return mask;
				},
				(n, v) =>
				{
					long result = n.Integer & ~representedBits;
					for (int i = 0; i < bits.Count; i++)
					{
						if ((v & (1 << i)) != 0)
						{
							result |= bits[i];
						}
					}

					n.Integer = result;
				}));
		}

		private FieldDrawer Bind<TValue>(FieldNode node, int[] path, in ValueFieldBinding<TValue> binding)
		{
			return new ValueFieldDrawer<TValue>(node, path, _host, in binding);
		}
	}
}