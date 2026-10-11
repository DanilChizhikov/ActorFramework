using System.IO;

namespace DTech.ActorFramework.Diagnostics
{
	internal sealed class SetFieldRequest
	{
		public int WorldId { get; set; }
		public int ActorId { get; set; }
		public int FeatureTypeId { get; set; }
		public int[] Path { get; set; }
		public FieldNode Value { get; set; }

		public static SetFieldRequest FromBytes(byte[] data) => DebugBinary.Deserialize(data, Read);

		public byte[] ToBytes() => DebugBinary.Serialize(this, Write);
		
		private static void Write(SetFieldRequest request, BinaryWriter writer)
		{
			writer.Write(request.WorldId);
			writer.Write(request.ActorId);
			writer.Write(request.FeatureTypeId);
			DebugBinary.WriteArray(request.Path, writer, DebugBinary.WriteInt32);
			FieldNode.Write(request.Value, writer);
		}

		private static SetFieldRequest Read(BinaryReader reader)
		{
			return new SetFieldRequest
			{
				WorldId = reader.ReadInt32(),
				ActorId = reader.ReadInt32(),
				FeatureTypeId = reader.ReadInt32(),
				Path = DebugBinary.ReadArray(reader, DebugBinary.ReadInt32),
				Value = FieldNode.Read(reader),
			};
		}
	}
}