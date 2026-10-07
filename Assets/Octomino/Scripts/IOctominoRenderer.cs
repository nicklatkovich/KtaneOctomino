using UnityEngine;

public interface IOctominoRenderer {
	int Dimensions { get; }
	void CreateFaces(OctominoUtils.FaceProps[] puzzle, Color[] colors);
}
