using System.IO;

namespace DTech.ActorFramework.Diagnostics
{
	internal sealed class SystemInfo
	{
		public string Name { get; set; }
		public string FullName { get; set; }
		public SystemPhases Phases { get; set; }

		public static void Write(SystemInfo info, BinaryWriter writer)
		{
			DebugBinary.WriteString(info.Name, writer);
			DebugBinary.WriteString(info.FullName, writer);
			writer.Write((byte)info.Phases);
		}

		public static SystemInfo Read(BinaryReader reader)
		{
			return new SystemInfo
			{
				Name = DebugBinary.ReadString(reader),
				FullName = DebugBinary.ReadString(reader),
				Phases = (SystemPhases)reader.ReadByte(),
			};
		}
	}
}