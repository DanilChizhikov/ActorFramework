using System.IO;

namespace DTech.ActorFramework.Diagnostics
{
	internal sealed class QueryInfo
	{
		public int Count { get; set; }
		public bool IncludeInactive { get; set; }
		public int[] All { get; set; }
		public int[] Any { get; set; }
		public int[] None { get; set; }

		public static void Write(QueryInfo info, BinaryWriter writer)
		{
			writer.Write(info.Count);
			writer.Write(info.IncludeInactive);
			DebugBinary.WriteArray(info.All, writer, DebugBinary.WriteInt32);
			DebugBinary.WriteArray(info.Any, writer, DebugBinary.WriteInt32);
			DebugBinary.WriteArray(info.None, writer, DebugBinary.WriteInt32);
		}

		public static QueryInfo Read(BinaryReader reader)
		{
			return new QueryInfo
			{
				Count = reader.ReadInt32(),
				IncludeInactive = reader.ReadBoolean(),
				All = DebugBinary.ReadArray(reader, DebugBinary.ReadInt32),
				Any = DebugBinary.ReadArray(reader, DebugBinary.ReadInt32),
				None = DebugBinary.ReadArray(reader, DebugBinary.ReadInt32),
			};
		}
	}
}