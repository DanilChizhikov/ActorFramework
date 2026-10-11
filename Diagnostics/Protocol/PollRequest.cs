using System.IO;

namespace DTech.ActorFramework.Diagnostics
{
	internal sealed class PollRequest
	{
		public const int None = -1;

		public int WorldId { get; set; } = None;
		public int KnownVersion { get; set; } = None;
		public int KnownTypeCount { get; set; }
		public int QueryIndex { get; set; } = None;
		public int SelectedActorId { get; set; } = None;

		public static void Write(PollRequest request, BinaryWriter writer)
		{
			writer.Write(request.WorldId);
			writer.Write(request.KnownVersion);
			writer.Write(request.KnownTypeCount);
			writer.Write(request.QueryIndex);
			writer.Write(request.SelectedActorId);
		}

		public static PollRequest Read(BinaryReader reader)
		{
			return new PollRequest
			{
				WorldId = reader.ReadInt32(),
				KnownVersion = reader.ReadInt32(),
				KnownTypeCount = reader.ReadInt32(),
				QueryIndex = reader.ReadInt32(),
				SelectedActorId = reader.ReadInt32(),
			};
		}

		public static PollRequest FromBytes(byte[] data) => DebugBinary.Deserialize(data, Read);

		public byte[] ToBytes() => DebugBinary.Serialize(this, Write);
	}
}
