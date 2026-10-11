using System.IO;

namespace DTech.ActorFramework.Diagnostics
{
	internal sealed class FeatureDetails
	{
		public int TypeId { get; set; }
		public FieldNode Root { get; set; }

		public static void Write(FeatureDetails details, BinaryWriter writer)
		{
			writer.Write(details.TypeId);
			FieldNode.Write(details.Root, writer);
		}

		public static FeatureDetails Read(BinaryReader reader)
		{
			return new FeatureDetails
			{
				TypeId = reader.ReadInt32(),
				Root = FieldNode.Read(reader),
			};
		}
	}
}