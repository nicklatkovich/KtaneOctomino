using System.Collections.Generic;
using System.Linq;
using _4D;
using UnityEngine;
using UnityEngine.Assertions;
using MultiDimVectorFloat = MultiDimVector<float>;

public class OctominoRenderer4D : MonoBehaviour, IOctominoRenderer {
	public const int DIMENSIONS = 4;
	public const float FOV = 90f;
	public const float MOUSE_MAX_ROTATION_ANGLE = 45; // deg

	public int Dimensions { get { return DIMENSIONS; } }

	public TesseractComponent TesseractPrefab;

	public Transform TesseractsContainer;
	public Transform RendererContainer;

	private readonly List<TesseractComponent> _tesseracts = new List<TesseractComponent>();

	public void Initialize(OctominoUtils.FaceProps[] puzzle, Color[] colors) {
		Clear();
		float containerScale;
		MultiDimVectorFloat facesOffset;
		OctominoUtils.CalculatePuzzleOffsetAndScale(DIMENSIONS, puzzle, out containerScale, out facesOffset);
		Vector4 offset = facesOffset.ToVector4();
		foreach (OctominoUtils.FaceProps props in puzzle) {
			TesseractComponent tesseract = Instantiate(TesseractPrefab);
			tesseract.transform.parent = TesseractsContainer;
			tesseract.transform.localPosition = Vector3.zero;
			tesseract.transform.localRotation = Quaternion.identity;
			tesseract.transform.localScale = Vector3.one;
			tesseract.Color = colors[props.index];
			tesseract.Position = props.position.ToFloat().ToVector4() - offset;
			_tesseracts.Add(tesseract);
		}
		Matrix5x5 scaleMatrix = Matrix5x5.Scale(containerScale);
		// Matrix5x5 rotationMatrix = SlightRotation();
		Matrix5x5 rotationMatrix = Matrix5x5.IDENTITY;
		Matrix5x5 translationMatrix = Matrix5x5.Translation(Utils.Vector4Ana * 0.5f / Mathf.Sin(FOV / 2f * Mathf.Deg2Rad));
		Matrix5x5 perspectiveMatrix = Matrix5x5.Perpective(FOV, .01f, 100f);
		Matrix5x5 transformMatrix = scaleMatrix * rotationMatrix * translationMatrix * perspectiveMatrix;
		List<Vector3> transformedVertices = new List<Vector3>();
		foreach (TesseractComponent t in _tesseracts) {
			Vector3[] tesseractVertices;
			t.Transform(transformMatrix, out tesseractVertices);
			transformedVertices.AddRange(tesseractVertices);
		}
		ScaleAndOffsetRenderer(transformedVertices.ToArray());
	}

	public void Clear() {
		foreach (TesseractComponent tesseract in _tesseracts) Destroy(tesseract.gameObject);
		_tesseracts.Clear();
	}

	public void Rotate(Vector2 dv2) {
#if UNITY_EDITOR
		RendererContainer.Rotate(Vector3.back, dv2.x, Space.World);
#else
		RendererContainer.Rotate(Vector3.down, dv2.x, Space.World);
#endif
		RendererContainer.Rotate(Vector3.right, dv2.y, Space.World);
	}

	private Matrix5x5 SlightRotation() {
		float maxRads = 15 * Mathf.Deg2Rad;
		int[] components = Enumerable.Range(0, 4).ToArray().Shuffle();
		Matrix5x5 rotation1 = Matrix5x5.Rotation(components[0], components[1], (Random.Range(0, 2) - 0.5f) * maxRads);
		Matrix5x5 rotation2 = Matrix5x5.Rotation(components[2], components[3], (Random.Range(0, 2) - 0.5f) * maxRads);
		return rotation1 * rotation2;
	}

	private void ScaleAndOffsetRenderer(Vector3[] vertices) {
		// TODO: rewrite using bounding 3D sphere algorithm
		float maxDistance = 0;
		Vector3 offset = Vector3.zero;
		for (int i = 0; i < vertices.Length; i++) {
			for (int j = i + 1; j < vertices.Length; j++) {
				float dist = Vector3.Distance(vertices[i], vertices[j]);
				if (dist > maxDistance) {
					maxDistance = dist;
					offset = (vertices[i] + vertices[j]) / 2f;
				}
			}
		}
		Assert.IsTrue(maxDistance > 0, "4D Octomino: Failed to scale and offset renderer");
		float maxRadius = maxDistance / 2f;
		bool fit = true;
		foreach (Vector3 vertex in vertices) {
			if (Vector3.Distance(vertex, offset) > maxRadius + Mathf.Epsilon) {
				fit = false;
				break;
			}
		}
		float scale = 1f / maxDistance;
		if (!fit) RecalculateScaleAndOffsetUsingCuboid(vertices, out scale, out offset);
		TesseractsContainer.localScale = Vector3.one * scale;
		TesseractsContainer.localPosition = -offset;

	}

	private void RecalculateScaleAndOffsetUsingCuboid(Vector3[] vertices, out float scale, out Vector3 offset) {
		Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 max = -min;
		foreach (Vector3 v in vertices) {
			max = new Vector3(Mathf.Max(max.x, v.x), Mathf.Max(max.y, v.y), Mathf.Max(max.z, v.z));
			min = new Vector3(Mathf.Min(min.x, v.x), Mathf.Min(min.y, v.y), Mathf.Min(min.z, v.z));
		}
		float diameter = Vector3.Distance(min, max);
		scale = 1 / diameter;
		offset = (max + min) / 2f;
	}
}
