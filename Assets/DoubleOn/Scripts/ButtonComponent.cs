using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonComponent : MonoBehaviour {
	public Renderer ButtonRenderer;
	public KMSelectable Selectable;

	private Color _color1;
	public Color Color1 { get { return _color1; } set { if (_color1 == value) return; _color1 = value; ButtonRenderer.material.SetColor("_Color_1", value); } }

	private Color _color2;
	public Color Color2 { get { return _color2; } set { if (_color2 == value) return; _color2 = value; ButtonRenderer.material.SetColor("_Color_2", value); } }

	private void Start() {
		ButtonRenderer.material.SetVector("_MainTexOffset", new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)));
		ButtonRenderer.material.SetVector("_AltTexOffset", new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)));
	}
}
