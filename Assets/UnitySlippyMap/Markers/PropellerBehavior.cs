using UnityEngine;
using System.Collections;

public class PropellerBehavior : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        //프로펠러 돌리기
        gameObject.transform.Rotate(Vector3.forward * Time.deltaTime*2000);
	}
}
