using System.Collections.Generic;
using System.Linq;
using _4D;
using UnityEngine;

public class TesseractComponent : MonoBehaviour {
	public Renderer Renderer;
	public MeshFilter MeshFilter;

	public Vector4 Position;

	private bool _initialized = false;
	private Color _color; public Color Color { get { return _color; } set { Renderer.material.SetColor("_Color", _color = value); } }
	private readonly Vector4[] _vertices = OctominoUtils.TESSERACT_POINTS.Select(point => point.ToVector4()).ToArray();

	public void Start() {
		if (!_initialized) Initialize();
		Renderer.material.SetFloat("_Alpha", 0.25f);
	}

	public void Transform(_4D.Matrix5x5 parentTransfrom, out Vector3[] transformedVertices) {
		if (!_initialized) Initialize();
		Mesh mesh = MeshFilter.mesh;
		_4D.Matrix5x5 m5 = _4D.Matrix5x5.Scale(.9f) * _4D.Matrix5x5.Translation(Position) * parentTransfrom;
		transformedVertices = _vertices.Select(v4 => (v4 * m5).ToVector3()).ToArray();
		mesh.vertices = BuildMeshVertices(transformedVertices);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
	}

	private void Initialize() {
		Mesh mesh = MeshFilter.mesh;
		mesh.Clear();
		Vector3[] meshVertices = BuildMeshVertices(Enumerable.Repeat(Vector3.zero, _vertices.Length).ToArray());
		mesh.vertices = meshVertices;
		Vector2[] faceUVs = new Vector2[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
		mesh.uv = Enumerable.Range(0, mesh.vertices.Length).Select(i => faceUVs[i % 4]).ToArray();
		mesh.triangles = BuildMeshTriangles(meshVertices.Length);
		Bounds meshBounds = new Bounds { center = Vector3.zero, size = Vector3.one };
		mesh.bounds = meshBounds;
		_initialized = true;
	}

	private Vector3[] BuildMeshVertices(Vector3[] v3s) {
		List<Vector3> vertices = new List<Vector3>();
		const int dimensions = 4;
		int pointCount = 1 << dimensions;
		for (int dimensionA = 0; dimensionA < dimensions; dimensionA++) {
			for (int dimensionB = dimensionA + 1; dimensionB < dimensions; dimensionB++) {
				int variableMask = (1 << dimensionA) | (1 << dimensionB);
				for (int fixedMask = 0; fixedMask < pointCount; fixedMask++) {
					if ((fixedMask & variableMask) != 0) continue;
					vertices.Add(v3s[fixedMask]);
					vertices.Add(v3s[fixedMask | (1 << dimensionA)]);
					vertices.Add(v3s[fixedMask | (1 << dimensionB)]);
					vertices.Add(v3s[fixedMask | variableMask]);
				}
			}
		}
		return vertices.ToArray();
	}

	private int[] BuildMeshTriangles(int vertexCount) {
		List<int> triangles = new List<int>();
		for (int i = 0; i < vertexCount; i += 4) triangles.AddRange(new int[] { i, i + 2, i + 1, i + 2, i + 3, i + 1 });
		return triangles.ToArray();
	}
}
