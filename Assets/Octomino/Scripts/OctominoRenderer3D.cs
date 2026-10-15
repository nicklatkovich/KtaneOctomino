using System.Collections.Generic;
using UnityEngine;
using MultiDimVectorFloat = MultiDimVector<float>;

public class OctominoRenderer3D : MonoBehaviour, IOctominoRenderer {
	public const int DIMENSIONS = 3;
	public const float MOUSE_MAX_ROTATION_ANGLE = 30; // deg

	public int Dimensions { get { return DIMENSIONS; } }

	public CubeComponent CubePrefab;

	public Transform CubesContainer;

	private readonly List<CubeComponent> _cubes = new List<CubeComponent>();

	public void Initialize(OctominoUtils.FaceProps[] puzzle, Color[] colors) {
		Clear();
		float containerScale;
		MultiDimVectorFloat facesOffset;
		OctominoUtils.CalculatePuzzleOffsetAndScale(DIMENSIONS, puzzle, out containerScale, out facesOffset);
		Vector3 offset = new Vector3(facesOffset[0], facesOffset[1], facesOffset[2]);
		CubesContainer.localScale = Vector3.one * containerScale;
		foreach (OctominoUtils.FaceProps props in puzzle) {
			CubeComponent cube = Instantiate(CubePrefab);
			cube.transform.parent = CubesContainer;
			Vector3 pos = new Vector3(props.position[0], props.position[1], props.position[2]);
			cube.transform.localPosition = pos - offset;
			cube.transform.localRotation = Quaternion.identity;
			cube.transform.localScale = Vector3.one;
			cube.Color = colors[props.index];
			_cubes.Add(cube);
		}
		CubesContainer.localRotation = Random.rotation;
	}

	public void Clear() {
		foreach (CubeComponent cube in _cubes) Destroy(cube.gameObject);
		_cubes.Clear();
	}

	public void Rotate(Vector2 dv2) {
		CubesContainer.Rotate(Vector3.back, dv2.x, Space.World);
		CubesContainer.Rotate(Vector3.right, dv2.y, Space.World);
	}
}
