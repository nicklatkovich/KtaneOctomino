using UnityEngine;

public interface IOctominoRenderer {
	int Dimensions { get; }
	void Initialize(OctominoUtils.FaceProps[] puzzle, Color[] colors);
}
