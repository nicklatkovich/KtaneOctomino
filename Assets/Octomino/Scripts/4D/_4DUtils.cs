using UnityEngine;

namespace _4D {
	public static class Utils {
		public static readonly Vector4 Vector4Ana = new Vector4(0, 0, 0, 1);
		public static readonly Vector4 Vector4Kata = new Vector4(0, 0, 0, -1);

		public static Vector3 ToVector3(Vector2 v2, float z = 0) { return new Vector3(v2.x, v2.y, z); }
		public static Vector4 ToVector4(Vector3 v3, float w = 0) { return new Vector4(v3.x, v3.y, v3.z, w); }
		public static _4D.Vector5 ToVector5(Vector4 v4, float v = 0) { return new _4D.Vector5(v4.x, v4.y, v4.z, v4.w, v); }
		public static Vector3 Flat(Vector4 v4) { return new Vector3(v4.x, v4.y, v4.z); }
		public static Vector4 Div(Vector4 v4, float f) { return new Vector4(v4.x / f, v4.y / f, v4.z / f, v4.w / f); }

		public static Vector3 ToVector3(this Vector4 vec4) { return new Vector3(vec4.x, vec4.y, vec4.z); }

		public static float RandomGaussian() { return Mathf.Sqrt(-2f * Mathf.Log(1f - Random.value)) * Mathf.Cos(2f * Mathf.PI * 1f - Random.value); }
	}
}
