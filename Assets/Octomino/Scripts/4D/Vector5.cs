using UnityEngine;

namespace _4D {
	public class Vector5 {
		public static Vector5 FromComponents(float[] components) {
			if (components.Length != 5) throw new System.Exception();
			return new Vector5(components[0], components[1], components[2], components[3], components[4]);
		}

		public readonly float x;
		public readonly float y;
		public readonly float z;
		public readonly float w;
		public readonly float v;

		public Vector4 flat { get { return new Vector4(x, y, z, w); } }

		public Vector5(float x, float y, float z, float w, float v) { this.x = x; this.y = y; this.z = z; this.w = w; this.v = v; }
		public Vector5(Vector4 v4, int v = 0) : this(v4.x, v4.y, v4.z, v4.w, v) { }
		public float[] GetComponents() { return new[] { x, y, z, w, v }; }
	}
}
