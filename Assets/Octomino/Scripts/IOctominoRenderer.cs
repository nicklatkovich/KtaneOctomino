using UnityEngine;

public interface IOctominoRenderer {
	bool enabled { get; set; }

	int Dimensions { get; }
	void Initialize(OctominoUtils.FaceProps[] puzzle, Color[] colors);
	void Clear();
	void Rotate(Vector2 dv2);
}
