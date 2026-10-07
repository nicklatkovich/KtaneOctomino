namespace _4D {
	public struct Vector4Int {
		public static readonly Vector4Int ZERO = new Vector4Int(0, 0, 0, 0);
		public static readonly Vector4Int RIGHT = new Vector4Int(1, 0, 0, 0);
		public static readonly Vector4Int LEFT = new Vector4Int(-1, 0, 0, 0);
		public static readonly Vector4Int UP = new Vector4Int(0, 1, 0, 0);
		public static readonly Vector4Int DOWN = new Vector4Int(0, -1, 0, 0);
		public static readonly Vector4Int FRONT = new Vector4Int(0, 0, 1, 0);
		public static readonly Vector4Int BACK = new Vector4Int(0, 0, -1, 0);
		public static readonly Vector4Int ANA = new Vector4Int(0, 0, 0, 1);
		public static readonly Vector4Int KATA = new Vector4Int(0, 0, 0, -1);

		public static Vector4Int operator *(int num, Vector4Int v4) {
			return new Vector4Int(v4.x * num, v4.y * num, v4.z * num, v4.w * num);
		}

		public readonly int x;
		public readonly int y;
		public readonly int z;
		public readonly int w;
		public Vector4Int(int x, int y, int z, int w) { this.x = x; this.y = y; this.z = z; this.w = w; }
	}
}
