using UnityEngine;
using MultiDimVectorFloat = MultiDimVector<float>;

public class OctominoRenderer2D : MonoBehaviour, IOctominoRenderer {
	public const int DIMENSIONS = 2;
	public int Dimensions { get { return DIMENSIONS; } }

	public FaceComponent FacePrefab;

	public Transform FacesContainer;

	public void CreateFaces(OctominoUtils.FaceProps[] puzzle, Color[] colors) {
		float containerScale;
		MultiDimVectorFloat facesOffset;
		OctominoUtils.CalculatePuzzleOffsetAndScale(DIMENSIONS, puzzle, out containerScale, out facesOffset);
		FacesContainer.localScale = Vector3.one * containerScale;
		foreach (OctominoUtils.FaceProps props in puzzle) {
			FaceComponent face = Instantiate(FacePrefab);
			face.transform.parent = FacesContainer;
			face.transform.localPosition = new Vector3(props.position[0], 0, props.position[1]) - new Vector3(facesOffset[0], 0, facesOffset[1]);
			face.transform.localRotation = Quaternion.identity;
			face.transform.localScale = Vector3.one;
			face.Color = colors[props.index];
		}
		FacesContainer.localRotation = Quaternion.Euler(0, 45f + Random.Range(0, 4) * 90f, 0);
	}
}
