using System;
using System.IO;

namespace DTech.ActorFramework.Diagnostics
{
	internal sealed class PollResponse
	{
		private const int GuidSize = 16;

		public Guid SessionId { get; set; }
		public WorldInfo[] Worlds { get; set; }
		public FeatureTypeInfo[] NewTypes { get; set; }
		public WorldSnapshot World { get; set; }
		public ActorDetails Actor { get; set; }

		public static PollResponse FromBytes(byte[] data) => DebugBinary.Deserialize(data, Read);

		public byte[] ToBytes() => DebugBinary.Serialize(this, Write);
		
		private static void Write(PollResponse response, BinaryWriter writer)
		{
			writer.Write(response.SessionId.ToByteArray());
			DebugBinary.WriteArray(response.Worlds, writer, WorldInfo.Write);
			DebugBinary.WriteArray(response.NewTypes, writer, FeatureTypeInfo.Write);
			DebugBinary.WriteNullable(response.World, writer, WorldSnapshot.Write);
			DebugBinary.WriteNullable(response.Actor, writer, ActorDetails.Write);
		}

		private static PollResponse Read(BinaryReader reader)
		{
			return new PollResponse
			{
				SessionId = new Guid(reader.ReadBytes(GuidSize)),
				Worlds = DebugBinary.ReadArray(reader, WorldInfo.Read),
				NewTypes = DebugBinary.ReadArray(reader, FeatureTypeInfo.Read),
				World = DebugBinary.ReadNullable(reader, WorldSnapshot.Read),
				Actor = DebugBinary.ReadNullable(reader, ActorDetails.Read),
			};
		}
	}
}