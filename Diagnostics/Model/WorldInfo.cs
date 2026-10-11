using System.IO;

namespace DTech.ActorFramework.Diagnostics
{
	internal sealed class WorldInfo
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public Phase Phase { get; set; }
		public int ActorCount { get; set; }
		public int SystemCount { get; set; }
		public int QueryCount { get; set; }
		public int FeatureTypesUsed { get; set; }
		public int FeatureTypesCapacity { get; set; }

		public static void Write(WorldInfo info, BinaryWriter writer)
		{
			writer.Write(info.Id);
			DebugBinary.WriteString(info.Name, writer);
			writer.Write((byte)info.Phase);
			writer.Write(info.ActorCount);
			writer.Write(info.SystemCount);
			writer.Write(info.QueryCount);
			writer.Write(info.FeatureTypesUsed);
			writer.Write(info.FeatureTypesCapacity);
		}

		public static WorldInfo Read(BinaryReader reader)
		{
			return new WorldInfo
			{
				Id = reader.ReadInt32(),
				Name = DebugBinary.ReadString(reader),
				Phase = (Phase)reader.ReadByte(),
				ActorCount = reader.ReadInt32(),
				SystemCount = reader.ReadInt32(),
				QueryCount = reader.ReadInt32(),
				FeatureTypesUsed = reader.ReadInt32(),
				FeatureTypesCapacity = reader.ReadInt32(),
			};
		}
	}
}