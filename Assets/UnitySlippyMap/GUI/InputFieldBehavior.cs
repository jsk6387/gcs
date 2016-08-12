using UnityEngine;
using System;
using UnitySlippyMap.Map;
using UnityEngine.UI;
using UnitySlippyMap.Helpers;
public class InputFieldBehavior : MonoBehaviour {

    // Use this for initialization
    private int index=0;
    public InputField input=null;
    private double[] pos = new double[2];
    private double[] exPos = new double[2];
    private double[] exVal=new double[2];
    void Start()
    {
        input = gameObject.GetComponent<InputField>();
        input.onEndEdit.AddListener(delegate { ChangeInput(input); });
    }
    /// <summary>
    /// inputField의 값이 변경될때 이벤트
    /// </summary>
    /// <param name="input"></param>
    void ChangeInput(InputField input)
    {
        MapBehaviour map = GameObject.Find("Test").GetComponent<MapBehaviour>();
        if (gameObject.transform.localPosition.x == 140)    //Long 값이 바뀔 때
            setMarkerLong(map, input);
        else if (gameObject.transform.localPosition.x == 240)   //Lat 값이 바뀔 때
            setMarkerLat(map, input);
        else if (gameObject.transform.localPosition.x == 340)   // Alt 값이 바뀔 때
        {
            setMarkerAlt(map,input);
        }
    }
    /// <summary>
    /// 마커의 고도변경시 고도설정
    /// </summary>
    /// <param name="map"></param>
    /// <param name="input"></param>
    public void setMarkerAlt(MapBehaviour map,InputField input)
    {
        index = (int)((gameObject.transform.localPosition.y + 17) / (-map.getContentRowNum()));
        map.getMarkerAlt()[index] =double.Parse( input.text);
    }
    /// <summary>
    /// 마커의 위도변경시 위도설정
    /// </summary>
    /// <param name="map"></param>
    /// <param name="input"></param>
    public void setMarkerLat(MapBehaviour map,InputField input)
    {
        index = (int)((gameObject.transform.localPosition.y + 17) / (-map.getContentRowNum()));// 값이 수정된 textfield의 index얻기
        exPos[0] = map.getMarkerLong(index);
        exPos[1] = map.getMarkerLat(index);
        exPos = GeoHelpers.WGS84ToRaycastHit(map, exPos);
        index = (int)((gameObject.transform.localPosition.y + 17) / (-map.getContentRowNum()));
        map.getMarkerLat()[index] = Math.Round(double.Parse(input.text), 6, MidpointRounding.AwayFromZero);
        setMarkerPos(map, index,exPos, 2);
    }
    /// <summary>
    /// 마커의 경도변경시 경도설정
    /// </summary>
    /// <param name="map"></param>
    /// <param name="input"></param>
    public void setMarkerLong(MapBehaviour map,InputField input)
    {
        index = (int)((gameObject.transform.localPosition.y + 17) / (-map.getContentRowNum()));// 값이 수정된 textfield의 index얻기
        exPos[0] = map.getMarkerLong(index);
        exPos[1] = map.getMarkerLat(index);
        exPos = GeoHelpers.WGS84ToRaycastHit(map, exPos);
        print("index:"+index); 
        map.getMarkerLong()[index] =Math.Round( double.Parse( input.text),6,MidpointRounding.AwayFromZero);
        setMarkerPos(map, index,exPos, 1);
    }
	
	// Update is called once per frame
	void Update () {
	}
    /// <summary>
    /// set Marker Postion depending on changed input
    /// </summary>
    void setMarkerPos(MapBehaviour map,int index,double[] exPosition, int fieldType)
    {
        pos[0] = map.getMarkerLong(index);
        pos[1] = map.getMarkerLat(index);
        GameObject[] gos = GameObject.FindGameObjectsWithTag("Marker");

        foreach (GameObject go in gos)
        {
            switch (fieldType)
            {
                case 1: // Longtitude changed
                    if (Math.Round(go.transform.position.x, 3, MidpointRounding.AwayFromZero) == Math.Round(exPos[0], 3, MidpointRounding.AwayFromZero))
                    {
                        Destroy(go);
                        map.drawMarker(pos);
                       // print(pos[0]);
                    }
                    break;
                case 2: //Latitude changed
                    //print(Math.Round(go.transform.position.z, 3, MidpointRounding.AwayFromZero) +"/" + Math.Round(exPos[1], 3, MidpointRounding.AwayFromZero));
                    if (Math.Round(go.transform.position.z, 3, MidpointRounding.AwayFromZero) == Math.Round(exPos[1], 3, MidpointRounding.AwayFromZero))
                    {
                        Destroy(go);
                        map.drawMarker(pos);
                        //print(pos[1]);
                    }
                    break;
            }
        }
    }
}
