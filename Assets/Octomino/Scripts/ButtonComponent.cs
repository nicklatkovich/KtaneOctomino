using UnityEngine;

public class ButtonComponent : MonoBehaviour {

	public const float DEFAULT_BORDER_SIZE = 0.4f;

	public Renderer Renderer;

	public KMSelectable Selectable;

	private Color _color; public Color Color { get { return _color; } set { Renderer.material.SetColor("_Color", _color = value); } }
	private float _borderSize = -1f; public float BorderSize { get { return _borderSize; } set { Renderer.material.SetFloat("_BorderSize", _borderSize = value); } }

	protected void Start() {
		if (BorderSize < 0f) BorderSize = DEFAULT_BORDER_SIZE;
	}
}
