using UnityEngine;
using System.Collections;
using GcsProject.Controller;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using UnitySlippyMap.UserGUI;
public class TextFieldBehavior : MonoBehaviour,IPointerDownHandler {
    private int selected;
    private GcsController controller;
    private Text droneKey;
	// Use this for initialization
	void Start () {
	
	}
	void Awake()
    {
        controller = GameObject.Find("GameObject").GetComponent<GcsController>();
        droneKey = GameObject.Find("Key").GetComponent<Text>();
    }
	// Update is called once per frame
	void Update () {
	
	}
    /// <summary>
    /// 마우스 클릭 이벤트
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(PointerEventData eventData)
    {
        selected = (int)-(gameObject.transform.localPosition.y + 15) / 15;
        print(selected);
        controller.GetDroneInfo(selected - 1);
        droneKey.text = (selected - 1).ToString();
    }
}
