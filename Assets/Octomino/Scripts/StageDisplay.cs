using UnityEngine;

public class StageDisplay : MonoBehaviour {
	public Renderer Renderer;
	public KMSelectable Selectable;

	private Color _outerColor_0; public Color OuterColor_0 { get { return _outerColor_0; } set { Renderer.material.SetColor("_OuterColor_0", _outerColor_0 = value); } }
	private Color _outerColor_1; public Color OuterColor_1 { get { return _outerColor_1; } set { Renderer.material.SetColor("_OuterColor_1", _outerColor_1 = value); } }
	private Color _outerColor_2; public Color OuterColor_2 { get { return _outerColor_2; } set { Renderer.material.SetColor("_OuterColor_2", _outerColor_2 = value); } }

	private int _outerColorsCount;
	public int OuterColorsCount { get { return _outerColorsCount; } set { Renderer.material.SetInt("_OuterColorsCount", _outerColorsCount = value); } }

	private float _progress; public float Progress { get { return _progress; } set { Renderer.material.SetFloat("_Progress", _progress = value); } }
	private float _resetProgress; public float ResetProgress { get { return _resetProgress; } set { Renderer.material.SetFloat("_ResetProgress", _resetProgress = value); } }
}
