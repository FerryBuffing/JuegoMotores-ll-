using UnityEngine;
using System.Collections;

public class Speedometer : MonoBehaviour {

	public Texture2D dialTex;
	public Texture2D needleTex;
	public Vector2 dialPos = new Vector2(0, 0);
	public float topSpeed = 200;
	public float stopAngle = -211;
	public float topSpeedAngle = 31;
	
	private float speed = 0;

	public void SetSpeed(float _speed){
		speed = _speed;
	}
	
	void OnGUI() {
		GUI.DrawTexture(new Rect(dialPos.x, dialPos.y, dialTex.width, dialTex.height), dialTex);
		Vector2 centre = new Vector2(dialPos.x + dialTex.width / 2, dialPos.y + dialTex.height / 2);
		float speedFraction = speed / topSpeed;
		float needleAngle = Mathf.Lerp(stopAngle, topSpeedAngle, speedFraction);
		Matrix4x4 savedMatrix = GUI.matrix;
		GUIUtility.RotateAroundPivot(needleAngle, centre);
		GUI.DrawTexture(new Rect(centre.x, centre.y - needleTex.height / 2, needleTex.width, needleTex.height), needleTex);
		GUI.matrix = savedMatrix;
	}
}
