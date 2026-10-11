namespace DTech.ActorFramework.Diagnostics
{
	internal enum FieldKind : byte
	{
		Null = 0,
		Text = 1,
		Integer = 2,
		UInteger = 3,
		Float = 4,
		Bool = 5,
		String = 6,
		Char = 7,
		Enum = 8,
		Flags = 9,
		Vector2 = 10,
		Vector3 = 11,
		Vector4 = 12,
		Vector2Int = 13,
		Vector3Int = 14,
		Quaternion = 15,
		Color = 16,
		Rect = 17,
		Bounds = 18,
		LayerMask = 19,
		ObjectRef = 20,
		ActorRef = 21,
		Composite = 22,
		Collection = 23,
	}
}