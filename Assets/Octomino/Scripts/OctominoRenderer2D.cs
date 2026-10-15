using System.Collections.Generic;
using UnityEngine;
using MultiDimVectorFloat = MultiDimVector<float>;

public class OctominoRenderer2D : MonoBehaviour, IOctominoRenderer {
	public const int DIMENSIONS = 2;
	public int Dimensions { get { return DIMENSIONS; } }

	public FaceComponent FacePrefab;

	public Transform FacesContainer;

	private readonly List<FaceComponent> _faces = new List<FaceComponent>();

	public void Initialize(OctominoUtils.FaceProps[] puzzle, Color[] colors) {
		Clear();
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
			_faces.Add(face);
		}
		FacesContainer.localRotation = Quaternion.Euler(0, 45f + Random.Range(0, 4) * 90f, 0);
	}

	public void Clear() {
		foreach (FaceComponent face in _faces) Destroy(face.gameObject);
		_faces.Clear();
	}

	public void Rotate(Vector2 dv2) {
		FacesContainer.localRotation *= Quaternion.Euler(0, dv2.x, 0);
	}
}
