using UnityEngine;

public class CubeComponent : MonoBehaviour {
	public Renderer Renderer;

	private Color _color; public Color Color { get { return _color; } set { Renderer.material.SetColor("_Color", _color = value); } }

	protected void Start() {
		Renderer.material.SetFloat("_Alpha", 0.75f);
	}
}
