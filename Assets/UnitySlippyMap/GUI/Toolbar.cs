using UnityEngine;
using System.Collections;

public class Toolbar : MonoBehaviour {
    float rotateAngle = 0f;
	// Use this for initialization
	void Start () {
        if (GUILayout.RepeatButton("Right", GUILayout.ExpandHeight(true)))
        {
            rotateAngle = 0.1f;

            Camera.main.transform.RotateAround(Vector3.zero, Vector3.up, rotateAngle);

        }
        if (Event.current.type == EventType.Repaint)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            
                //pressed = true;
        }
        if (GUILayout.RepeatButton("Left", GUILayout.ExpandHeight(true)))
        {
            rotateAngle = -0.1f;
            Camera.main.transform.RotateAround(Vector3.zero, Vector3.up, rotateAngle);
        }
        if (Event.current.type == EventType.Repaint)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            
        }
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
