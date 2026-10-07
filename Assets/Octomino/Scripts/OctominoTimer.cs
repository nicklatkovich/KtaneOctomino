using UnityEngine;

public class OctominoTimer : MonoBehaviour {
	public Renderer Renderer;

	private float _value; public float Value { get { return _value; } set { Renderer.material.SetFloat("_Timer", _value = value); } }
}
