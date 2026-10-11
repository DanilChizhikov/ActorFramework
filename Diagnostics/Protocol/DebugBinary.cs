using System;
using System.IO;
using System.Text;

namespace DTech.ActorFramework.Diagnostics
{
	internal static class DebugBinary
	{
		public static byte[] Serialize<T>(T value, Action<T, BinaryWriter> write)
		{
			using var stream = new MemoryStream();
			using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
			{
				write(value, writer);
			}

			return stream.ToArray();
		}

		public static T Deserialize<T>(byte[] data, Func<BinaryReader, T> read)
		{
			_ = data ?? throw new ArgumentNullException(nameof(data));

			using var stream = new MemoryStream(data, false);
			using var reader = new BinaryReader(stream, Encoding.UTF8);

			return read(reader);
		}

		public static void WriteString(string value, BinaryWriter writer)
		{
			writer.Write(value != null);
			if (value != null)
			{
				writer.Write(value);
			}
		}

		public static string ReadString(BinaryReader reader)
		{
			return reader.ReadBoolean() ? reader.ReadString() : null;
		}

		public static void WriteInt32(int value, BinaryWriter writer) => writer.Write(value);

		public static int ReadInt32(BinaryReader reader) => reader.ReadInt32();

		public static void WriteInt64(long value, BinaryWriter writer) => writer.Write(value);

		public static long ReadInt64(BinaryReader reader) => reader.ReadInt64();

		public static void WriteDouble(double value, BinaryWriter writer) => writer.Write(value);

		public static double ReadDouble(BinaryReader reader) => reader.ReadDouble();

		public static void WriteArray<T>(T[] items, BinaryWriter writer, Action<T, BinaryWriter> write)
		{
			if (items == null)
			{
				writer.Write(-1);
				return;
			}

			writer.Write(items.Length);
			for (int i = 0; i < items.Length; i++)
			{
				write(items[i], writer);
			}
		}

		public static T[] ReadArray<T>(BinaryReader reader, Func<BinaryReader, T> read)
		{
			int length = reader.ReadInt32();
			if (length < 0)
			{
				return null;
			}

			var items = new T[length];
			for (int i = 0; i < length; i++)
			{
				items[i] = read(reader);
			}

			return items;
		}

		public static void WriteNullable<T>(T item, BinaryWriter writer, Action<T, BinaryWriter> write)
			where T : class
		{
			writer.Write(item != null);
			if (item != null)
			{
				write(item, writer);
			}
		}

		public static T ReadNullable<T>(BinaryReader reader, Func<BinaryReader, T> read)
			where T : class
		{
			return reader.ReadBoolean() ? read(reader) : null;
		}
	}
}