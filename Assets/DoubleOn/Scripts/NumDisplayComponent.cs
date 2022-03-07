using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumDisplayComponent : MonoBehaviour {
	public const float UNLIT_MULTIPLIER = 0.2f;

	public TextMesh BackText;
	public TextMesh FrontText;

	public void SetColor(Color cl) {
		BackText.color = new Color(cl.r * UNLIT_MULTIPLIER, cl.g * UNLIT_MULTIPLIER, cl.b * UNLIT_MULTIPLIER, 1.0f);
		FrontText.color = cl;
	}
}
