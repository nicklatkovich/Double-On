using UnityEngine;

public class ConnectionComponent : MonoBehaviour {
	public Renderer SelfRenderer;

	private Color _color1;
	public Color Color1 { get { return _color1; } set { if (_color1 == value) return; _color1 = value; UpdateColor1(); } }

	private Color _color2;
	public Color Color2 { get { return _color2; } set { if (_color2 == value) return; _color2 = value; UpdateColor2(); } }

	private void Start() {
		SelfRenderer.material.SetVector("_MainTexOffset", new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)));
		SelfRenderer.material.SetVector("_AltTexOffset", new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)));
		UpdateColors();
	}

	public void UpdateColors() {
		UpdateColor1();
		UpdateColor2();
	}

	private void UpdateColor1() {
		SelfRenderer.material.SetColor("_Color_1", _color1);
	}

	private void UpdateColor2() {
		SelfRenderer.material.SetColor("_Color_2", _color2);
	}
}
