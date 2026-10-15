using UnityEngine;

public class RotationButton : MonoBehaviour {
	public KMSelectable Selectable;
	public Renderer SymbolRenderer;

	protected void Start() {
		OnRelease();
	}

	public void OnHeld() {
		SymbolRenderer.material.SetFloat("_BorderSize", 1.1f);
		SymbolRenderer.material.SetColor("_Color", Color.white);
	}

	public void OnRelease() {
		SymbolRenderer.material.SetFloat("_BorderSize", 0.4f);
		SymbolRenderer.material.SetColor("_Color", Color.gray);
	}
}
