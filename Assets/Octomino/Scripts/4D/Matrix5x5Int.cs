using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace _4D {
	public class Matrix5x5Int {
		public static readonly Matrix5x5Int IDENTITY = new Matrix5x5Int();
		public static readonly Matrix5x5Int ROTATION_XY = Rotation(0, 1);
		public static readonly Matrix5x5Int ROTATION_XZ = Rotation(0, 2);
		public static readonly Matrix5x5Int ROTATION_XW = Rotation(0, 3);
		public static readonly Matrix5x5Int ROTATION_YX = Rotation(1, 0);
		public static readonly Matrix5x5Int ROTATION_YZ = Rotation(1, 2);
		public static readonly Matrix5x5Int ROTATION_YW = Rotation(1, 3);
		public static readonly Matrix5x5Int ROTATION_ZX = Rotation(2, 0);
		public static readonly Matrix5x5Int ROTATION_ZY = Rotation(2, 1);
		public static readonly Matrix5x5Int ROTATION_ZW = Rotation(2, 3);
		public static readonly Matrix5x5Int ROTATION_WX = Rotation(3, 0);
		public static readonly Matrix5x5Int ROTATION_WY = Rotation(3, 1);
		public static readonly Matrix5x5Int ROTATION_WZ = Rotation(3, 2);

		public static Matrix5x5Int GetRandomRotation() {
			Queue<int> axles = new Queue<int>(Enumerable.Range(0, 4).OrderBy(_ => Random.value));
			Matrix5x5Int res = Matrix5x5Int.IDENTITY;
			int pivot1 = axles.Dequeue();
			int count1 = pivot1 == 0 ? Random.Range(0, 2) * 2 : Random.Range(0, 4);
			if (count1 == 0) {}
			else if (count1 == 1) res *= Rotation(0, pivot1);
			else if (count1 == 2) {
				if (pivot1 == 0) {
					int tempAxis = Random.Range(1, 4);
					res *= Rotation(0, tempAxis) * Rotation(0, tempAxis);
				} else {
					res *= Rotation(0, pivot1) * Rotation(0, pivot1);
				}
			} else res *= Rotation(pivot1, 0);
			int pivot2 = axles.Dequeue();
			int pivot3 = axles.Dequeue();
			int count2 = Random.Range(0, 4);
			for (int i = 0; i < count2; i++) res *= Rotation(pivot2, pivot3);
			int pivot4 = axles.Dequeue();
			int count3 = Random.Range(0, 4);
			for (int i = 0; i < count3; i++) res *= Rotation(pivot2, pivot4);
			return res;
		}

		public static Matrix5x5Int GetRandomXYZRotation() {
			Queue<int> axles = new Queue<int>(Enumerable.Range(0, 3).OrderBy(_ => Random.value));
			Matrix5x5Int res = Matrix5x5Int.IDENTITY;
			int pivot1 = axles.Dequeue();
			int count1 = pivot1 == 0 ? Random.Range(0, 2) * 2 : Random.Range(0, 4);
			if (count1 == 0) {}
			else if (count1 == 1) res *= Rotation(0, pivot1);
			else if (count1 == 2) {
				if (pivot1 == 0) {
					int tempAxis = Random.Range(1, 3);
					res *= Rotation(0, tempAxis) * Rotation(0, tempAxis);
				} else {
					res *= Rotation(0, pivot1) * Rotation(0, pivot1);
				}
			} else res *= Rotation(pivot1, 0);
			int pivot2 = axles.Dequeue();
			int pivot3 = axles.Dequeue();
			int count2 = Random.Range(0, 4);
			for (int i = 0; i < count2; i++) res *= Rotation(pivot2, pivot3);
			return res;
		}

		public static Matrix5x5Int operator *(Matrix5x5Int m5a, Matrix5x5Int m5b) {
			Matrix5x5Int result = new Matrix5x5Int();
			for (int row = 0; row < 5; row++) {
				for (int column = 0; column < 5; column++) result._data[row][column] = Enumerable.Range(0, 5).Select(i => m5a._data[row][i] * m5b._data[i][column]).Sum();
			}
			return result;
		}

		public static bool operator ==(Matrix5x5Int m5a, Matrix5x5Int m5b) {
			for (int row = 0; row < 5; row++) {
				for (int col = 0; col < 5; col++) {
					if (m5a[row, col] != m5b[row, col]) return false;
				}
			}
			return true;
		}

		public static bool operator !=(Matrix5x5Int m5a, Matrix5x5Int m5b) {
			return !(m5a == m5b);
		}

		public override bool Equals(object obj) {
			Matrix5x5Int other = obj as Matrix5x5Int;
			if (other == null) return false;
			return this == other;
		}

		public override int GetHashCode() {
			unchecked {
				int res = 0;
				for (int row = 0; row < 5; row++) {
					for (int col = 0; col < 5; col++) {
						res = res * 23 + this[row, col].GetHashCode();
					}
				}
				return res;
			}
		}

		public static Matrix5x5Int Translation(Vector4Int v4) {
			Matrix5x5Int result = new Matrix5x5Int();
			result._data[4][0] = v4.x;
			result._data[4][1] = v4.y;
			result._data[4][2] = v4.z;
			result._data[4][3] = v4.w;
			return result;
		}

		public static Matrix5x5Int Rotation(int componentAIndex, int componentBIndex) {
			if (componentAIndex == componentBIndex) return Matrix5x5Int.IDENTITY;
			if (componentAIndex < 0 || componentBIndex < 0 || componentAIndex > 3 || componentBIndex > 3) throw new System.Exception();
			Matrix5x5Int result = new Matrix5x5Int();
			result._data[componentAIndex][componentAIndex] = 0;
			result._data[componentAIndex][componentBIndex] = 1;
			result._data[componentBIndex][componentAIndex] = -1;
			result._data[componentBIndex][componentBIndex] = 0;
			return result;
		}

		public static Vector5Int operator *(Vector5Int v5, Matrix5x5Int m5) {
			int[] v5c = v5.GetComponents();
			return Vector5Int.FromComponents(Enumerable.Range(0, 5).Select(i => Enumerable.Range(0, 5).Select(j => v5c[j] * m5._data[j][i]).Sum()).ToArray());
		}

		public int this[int row, int column] { get { return _data[row][column]; } }
		private int[][] _data;
		public Matrix5x5Int() { _data = Enumerable.Range(0, 5).Select(r => Enumerable.Range(0, 5).Select(c => r == c ? 1 : 0).ToArray()).ToArray(); }
	}
}
