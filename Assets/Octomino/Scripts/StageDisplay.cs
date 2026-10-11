using UnityEngine;

public class StageDisplay : MonoBehaviour {
	public Renderer Renderer;

	private Color _outerColor; public Color OuterColor { get { return _outerColor; } set { Renderer.material.SetColor("_OuterColor", _outerColor = value); } }
	private float _progress; public float Progress { get { return _progress; } set { Renderer.material.SetFloat("_Progress", _progress = value); } }
	private float _resetProgress; public float ResetProgress { get { return _resetProgress; } set { Renderer.material.SetFloat("_ResetProgress", _resetProgress = value); } }
}
