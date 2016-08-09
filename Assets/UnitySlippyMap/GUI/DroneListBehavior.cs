using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnitySlippyMap.Helpers;
using UnitySlippyMap.Map;
public class DroneListBehavior : MonoBehaviour {


    private int droneNum;   // 지도상의 드론 갯수 
    private int exNum;      // exDroneNum
    private GameObject[] drones;
    public Text txtLabel;
	// Use this for initialization
	void Start () {
        droneNum = 0;
        exNum = -1;
	}
	
	// Update is called once per frame
	void Update () {
        drones = GameObject.FindGameObjectsWithTag("Drone");
        droneNum = drones.Length;
        if (droneNum > 0 && droneNum != exNum)
        {
            exNum = droneNum;
            drawDroneList(drones, droneNum);
        }
	}
    /// <summary>
    /// 드론 목록 그리기
    /// </summary>
    /// <param name="drones"></param>
    public void drawDroneList(GameObject[] drones, int droneCnt)
    {
        double[] pos;
        MapBehaviour map = GameObject.Find("Test").GetComponent<MapBehaviour>();
        pos = GeoHelpers.ScreenpointToWGS84(map, drones[droneCnt-1].transform.position);
        InputField droneName = GameObject.Find("Drone name").GetComponent<InputField>();
        RectTransform droneList = GameObject.Find("List").GetComponent<RectTransform>();
        Text order = Instantiate(txtLabel);
        order.GetComponent<RectTransform>().position = new Vector3(40, -15 - 15 * droneCnt, 0);
        order.text = droneCnt.ToString();
        order.transform.SetParent(droneList.transform, false);

        Text name = Instantiate(txtLabel);
        name.GetComponent<RectTransform>().position = new Vector3(60, -15 - 15 * droneCnt, 0);
        DroneBehavior droneData = drones[droneCnt-1].GetComponent<DroneBehavior>();
        if (droneData != null)
        {
            name.text = droneData.droneName;
        }
        name.transform.SetParent(droneList.transform, false);

        Text longtitude = Instantiate(txtLabel);
        longtitude.GetComponent<RectTransform>().position = new Vector3(100, -15 - 15 * droneCnt, 0);
        longtitude.text = pos[0].ToString();
        longtitude.fontSize = 9;
        longtitude.transform.SetParent(droneList.transform, false);

        Text latitude = Instantiate(txtLabel);
        latitude.GetComponent<RectTransform>().position = new Vector3(150, -15 - 15 * droneCnt, 0);
        latitude.text = pos[1].ToString();
        latitude.fontSize = 9;
        latitude.transform.SetParent(droneList.transform, false);
    }
}
