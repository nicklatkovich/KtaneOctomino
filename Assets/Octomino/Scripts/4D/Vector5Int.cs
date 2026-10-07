namespace _4D {
	public struct Vector5Int {
		public static Vector5Int FromComponents(int[] components) {
			if (components.Length != 5) throw new System.Exception();
			return new Vector5Int(components[0], components[1], components[2], components[3], components[4]);
		}

		public readonly int x;
		public readonly int y;
		public readonly int z;
		public readonly int w;
		public readonly int v;

		public Vector5Int(int x, int y, int z, int w, int v) { this.x = x; this.y = y; this.z = z; this.w = w; this.v = v; }
		public Vector5Int(Vector4Int v4, int v = 0) : this(v4.x, v4.y, v4.z, v4.w, v) { }
		public int[] GetComponents() { return new[] { x, y, z, w, v }; }
	}
}
