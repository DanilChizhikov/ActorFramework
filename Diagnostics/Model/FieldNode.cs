using System.IO;

namespace DTech.ActorFramework.Diagnostics
{
	internal sealed class FieldNode
	{
		public FieldKind Kind { get; set; }
		public FieldFlags Flags { get; set; }
		public string Label { get; set; }
		public string Tooltip { get; set; }
		public string Header { get; set; }
		public string TypeName { get; set; }
		public float RangeMin { get; set; }
		public float RangeMax { get; set; }
		public long Integer { get; set; }
		public double Float { get; set; }
		public string Text { get; set; }
		public double[] Components { get; set; }
		public string[] EnumNames { get; set; }
		public long[] EnumValues { get; set; }
		public FieldNode[] Children { get; set; }
		public bool IsReadOnly => (Flags & FieldFlags.ReadOnly) != 0;
		public bool IsMultiline => (Flags & FieldFlags.Multiline) != 0;
		public bool HasRange => (Flags & FieldFlags.HasRange) != 0;

		public static void Write(FieldNode node, BinaryWriter writer)
		{
			writer.Write((byte)node.Kind);
			writer.Write((byte)node.Flags);
			DebugBinary.WriteString(node.Label, writer);
			DebugBinary.WriteString(node.Tooltip, writer);
			DebugBinary.WriteString(node.Header, writer);
			DebugBinary.WriteString(node.TypeName, writer);
			writer.Write(node.RangeMin);
			writer.Write(node.RangeMax);
			writer.Write(node.Integer);
			writer.Write(node.Float);
			DebugBinary.WriteString(node.Text, writer);
			DebugBinary.WriteArray(node.Components, writer, DebugBinary.WriteDouble);
			DebugBinary.WriteArray(node.EnumNames, writer, DebugBinary.WriteString);
			DebugBinary.WriteArray(node.EnumValues, writer, DebugBinary.WriteInt64);
			DebugBinary.WriteArray(node.Children, writer, Write);
		}

		public static FieldNode Read(BinaryReader reader)
		{
			return new FieldNode
			{
				Kind = (FieldKind)reader.ReadByte(),
				Flags = (FieldFlags)reader.ReadByte(),
				Label = DebugBinary.ReadString(reader),
				Tooltip = DebugBinary.ReadString(reader),
				Header = DebugBinary.ReadString(reader),
				TypeName = DebugBinary.ReadString(reader),
				RangeMin = reader.ReadSingle(),
				RangeMax = reader.ReadSingle(),
				Integer = reader.ReadInt64(),
				Float = reader.ReadDouble(),
				Text = DebugBinary.ReadString(reader),
				Components = DebugBinary.ReadArray(reader, DebugBinary.ReadDouble),
				EnumNames = DebugBinary.ReadArray(reader, DebugBinary.ReadString),
				EnumValues = DebugBinary.ReadArray(reader, DebugBinary.ReadInt64),
				Children = DebugBinary.ReadArray(reader, Read),
			};
		}

		public FieldNode CloneValue()
		{
			return new FieldNode
			{
				Kind = Kind,
				Integer = Integer,
				Float = Float,
				Text = Text,
				Components = (double[])Components?.Clone(),
			};
		}
	}
}