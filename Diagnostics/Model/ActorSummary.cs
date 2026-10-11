using System.IO;

namespace DTech.ActorFramework.Diagnostics
{
	internal sealed class ActorSummary
	{
		public int Id { get; set; }
		public bool IsActive { get; set; }
		public int[] FeatureTypeIds { get; set; }

		public static void Write(ActorSummary summary, BinaryWriter writer)
		{
			writer.Write(summary.Id);
			writer.Write(summary.IsActive);

			int[] typeIds = summary.FeatureTypeIds;
			writer.Write((ushort)typeIds.Length);
			for (int i = 0; i < typeIds.Length; i++)
			{
				writer.Write((ushort)typeIds[i]);
			}
		}

		public static ActorSummary Read(BinaryReader reader)
		{
			int id = reader.ReadInt32();
			bool isActive = reader.ReadBoolean();

			var typeIds = new int[reader.ReadUInt16()];
			for (int i = 0; i < typeIds.Length; i++)
			{
				typeIds[i] = reader.ReadUInt16();
			}

			return new ActorSummary
			{
				Id = id,
				IsActive = isActive,
				FeatureTypeIds = typeIds,
			};
		}
	}
}