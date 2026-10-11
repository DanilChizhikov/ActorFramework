using System.IO;

namespace DTech.ActorFramework.Diagnostics
{
	internal sealed class WorldSnapshot
	{
		public int WorldId { get; set; }
		public int Version { get; set; }
		public ActorSummary[] Actors { get; set; }
		public SystemInfo[] Systems { get; set; }
		public QueryInfo[] Queries { get; set; }

		public static void Write(WorldSnapshot snapshot, BinaryWriter writer)
		{
			writer.Write(snapshot.WorldId);
			writer.Write(snapshot.Version);
			DebugBinary.WriteArray(snapshot.Actors, writer, ActorSummary.Write);
			DebugBinary.WriteArray(snapshot.Systems, writer, SystemInfo.Write);
			DebugBinary.WriteArray(snapshot.Queries, writer, QueryInfo.Write);
		}

		public static WorldSnapshot Read(BinaryReader reader)
		{
			return new WorldSnapshot
			{
				WorldId = reader.ReadInt32(),
				Version = reader.ReadInt32(),
				Actors = DebugBinary.ReadArray(reader, ActorSummary.Read),
				Systems = DebugBinary.ReadArray(reader, SystemInfo.Read),
				Queries = DebugBinary.ReadArray(reader, QueryInfo.Read),
			};
		}
	}
}