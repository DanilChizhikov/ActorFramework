using System.IO;

namespace DTech.ActorFramework.Diagnostics
{
	internal sealed class FeatureTypeInfo
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string FullName { get; set; }

		public static void Write(FeatureTypeInfo info, BinaryWriter writer)
		{
			writer.Write(info.Id);
			DebugBinary.WriteString(info.Name, writer);
			DebugBinary.WriteString(info.FullName, writer);
		}

		public static FeatureTypeInfo Read(BinaryReader reader)
		{
			return new FeatureTypeInfo
			{
				Id = reader.ReadInt32(),
				Name = DebugBinary.ReadString(reader),
				FullName = DebugBinary.ReadString(reader),
			};
		}
	}
}