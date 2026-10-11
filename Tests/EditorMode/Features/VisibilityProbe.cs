namespace DTech.ActorFramework.Tests.EditorMode
{
	internal struct VisibilityProbe : IActorFeature
	{
		public readonly int ReadOnlyValue;

		[field: ShowInActorDebugger]
		public int AutoProperty { get; set; }
		public int ShownPrivate => _shownPrivate;
		public int HiddenPrivate => _hiddenPrivate;

		public static int StaticValue;

		public int Visible;

		[HideInActorDebugger]
		public int HiddenPublic;

		[ShowInActorDebugger]
		private int _shownPrivate;

		private int _hiddenPrivate;

		public VisibilityProbe(int readOnlyValue, int shownPrivate, int hiddenPrivate)
		{
			this = default;
			ReadOnlyValue = readOnlyValue;
			_shownPrivate = shownPrivate;
			_hiddenPrivate = hiddenPrivate;
		}
	}
}
