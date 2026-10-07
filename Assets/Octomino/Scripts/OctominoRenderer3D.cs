using UnityEngine;
using MultiDimVectorFloat = MultiDimVector<float>;

public class OctominoRenderer3D : MonoBehaviour, IOctominoRenderer {
	public const int DIMENSIONS = 3;
	public int Dimensions { get { return DIMENSIONS; } }

	public CubeComponent CubePrefab;

	public Transform CubesContainer;

	public void Initialize(OctominoUtils.FaceProps[] puzzle, Color[] colors) {
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
		}
		CubesContainer.localRotation = Random.rotation;
	}
}
