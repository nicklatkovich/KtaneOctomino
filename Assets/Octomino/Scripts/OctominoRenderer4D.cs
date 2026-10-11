using System.Collections.Generic;
using _4D;
using UnityEngine;
using MultiDimVectorFloat = MultiDimVector<float>;

public class OctominoRenderer4D : MonoBehaviour, IOctominoRenderer {
	public const int DIMENSIONS = 4;
	public const float MOUSE_MAX_ROTATION_ANGLE = 90; // deg

	public int Dimensions { get { return DIMENSIONS; } }

	public TesseractComponent TesseractPrefab;

	public Transform TesseractsContainer;

	private readonly List<TesseractComponent> _tesseracts = new List<TesseractComponent>();
	private Matrix5x5 _scaleMatrix;
	private Matrix5x5 _rotationMatrix;
	private Matrix5x5 _perspectiveMatrix;

	protected void Start() {
		_rotationMatrix = Matrix5x5.RandomRotation();
		// _rotationMatrix = Matrix5x5.IDENTITY;
		_perspectiveMatrix = Matrix5x5.Perpective(120f, .01f, 100f);
	}

	protected void Update() {
		float x = Mathf.Clamp01(Input.mousePosition.x / Screen.width) * 2f - 1f;
		float y = Mathf.Clamp01(Input.mousePosition.y / Screen.height) * 2f - 1f;
		TesseractsContainer.localRotation = Quaternion.Euler(y * MOUSE_MAX_ROTATION_ANGLE, 0, -x * MOUSE_MAX_ROTATION_ANGLE);
		// _rotationMatrix *= Matrix5x5.Rotation(0, 3, Time.deltaTime);
		// _rotationMatrix = Matrix5x5.Rotation(0, 1, -x * MOUSE_MAX_ROTATION_ANGLE * Mathf.Deg2Rad) * Matrix5x5.Rotation(2, 3, y * MOUSE_MAX_ROTATION_ANGLE * Mathf.Deg2Rad);
		foreach (TesseractComponent t in _tesseracts) t.Transform(_scaleMatrix * _rotationMatrix * Matrix5x5.Translation(Utils.Vector4Ana * .708f) * _perspectiveMatrix);
	}

	public void Initialize(OctominoUtils.FaceProps[] puzzle, Color[] colors) {
		Clear();
		float containerScale;
		MultiDimVectorFloat facesOffset;
		OctominoUtils.CalculatePuzzleOffsetAndScale(DIMENSIONS, puzzle, out containerScale, out facesOffset);
		Vector4 offset = facesOffset.ToVector4();
		_scaleMatrix = Matrix5x5.Scale(containerScale);
		// CubesContainer.localScale = Vector3.one * containerScale;
		foreach (OctominoUtils.FaceProps props in puzzle) {
			TesseractComponent tesseract = Instantiate(TesseractPrefab);
			tesseract.transform.parent = TesseractsContainer;
			// Vector3 pos = new Vector3(props.position[0], props.position[1], props.position[2]);
			tesseract.transform.localPosition = Vector3.zero;
			tesseract.transform.localRotation = Quaternion.identity;
			tesseract.transform.localScale = Vector3.one;
			tesseract.Color = colors[props.index];
			tesseract.Position = props.position.ToFloat().ToVector4() - offset;
			_tesseracts.Add(tesseract);
		}
		// CubesContainer.localRotation = _rotation;
	}

	public void Clear() {
		foreach (TesseractComponent tesseract in _tesseracts) Destroy(tesseract.gameObject);
		_tesseracts.Clear();
	}
}
