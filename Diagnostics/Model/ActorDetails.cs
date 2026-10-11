using System.IO;

namespace DTech.ActorFramework.Diagnostics
{
	internal sealed class ActorDetails
	{
		public int Id { get; set; }
		public bool IsAlive { get; set; }
		public bool IsActive { get; set; }
		public string[] Activators { get; set; }
		public string[] Deactivators { get; set; }
		public FeatureDetails[] Features { get; set; }

		public static void Write(ActorDetails details, BinaryWriter writer)
		{
			writer.Write(details.Id);
			writer.Write(details.IsAlive);
			writer.Write(details.IsActive);
			DebugBinary.WriteArray(details.Activators, writer, DebugBinary.WriteString);
			DebugBinary.WriteArray(details.Deactivators, writer, DebugBinary.WriteString);
			DebugBinary.WriteArray(details.Features, writer, FeatureDetails.Write);
		}

		public static ActorDetails Read(BinaryReader reader)
		{
			return new ActorDetails
			{
				Id = reader.ReadInt32(),
				IsAlive = reader.ReadBoolean(),
				IsActive = reader.ReadBoolean(),
				Activators = DebugBinary.ReadArray(reader, DebugBinary.ReadString),
				Deactivators = DebugBinary.ReadArray(reader, DebugBinary.ReadString),
				Features = DebugBinary.ReadArray(reader, FeatureDetails.Read),
			};
		}
	}
}