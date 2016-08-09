using UnityEngine;
using UnitySlippyMap.Helpers;
using UnitySlippyMap.Map;
using UnitySlippyMap.UserGUI;
using UnityEngine.UI;
using UnitySlippyMap.DroneStruct;
using GcsProject.Controller;
public class DroneBehavior : MonoBehaviour {
    public double[] dronePos = new double[2] ;   // drone 경도,위도,고도
    public int key=0;
    public string droneName = "";
    public Transform traceMarker;
    private GameObject[] gos;
    private GcsController controller;
    private PositionDouble dronePosition;
    private static Vector3 lastHitPosition = Vector3.zero;
    private Vector3 posVec = new Vector3(0, 0, 0);  // drone 의 위치벡터
    private Text droneKey;
	// Use this for initialization

	void Start () {
        //getDronePos();
        MapBehaviour map = GameObject.Find("Test").GetComponent<MapBehaviour>();
        DronePanelBehavior DPB = GameObject.Find("GameObject").GetComponent<DronePanelBehavior>();
        dronePos[0]=126.88;
        dronePos[1] = 37.488;
        dronePos = GeoHelpers.WGS84ToRaycastHit(map, dronePos); //드론의 위도,경도 위치를 화면상 위치로 변환함
        posVec[0] = (float)dronePos[0];      // vector에 드론 위치 하나씩 저장 x : 경도 ,z : 위도 
        posVec[2] = (float)dronePos[1];
        gameObject.transform.position = posVec;      // 그 후 gameobject , 즉 드론의 위치를 vector로 설정
        droneKey = GameObject.Find("Key").GetComponent<Text>();
        droneKey.text=key.ToString();

	}
	public void OnMouseDown()       // 드론을 마우스로 클릭했을 때 이벤트함수 
    {
        ButtonBehavior btnBehavior = GameObject.Find("GameObject").GetComponent<ButtonBehavior>();
        controller = GameObject.Find("GameObject").GetComponent<GcsController>();
        controller.GetDroneInfo(key);
        btnBehavior.doClear();
    }
	// Update is called once per frame
	void Update () {
        SaveLoadBehavior SLB = GameObject.Find("GameObject").GetComponent<SaveLoadBehavior>();  
        if (Input.GetMouseButton(0) && !SLB.usingUI)
        {
            gos = GameObject.FindGameObjectsWithTag("Drone");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo))
            {
                Vector3 displacement = Vector3.zero;
                if (lastHitPosition != Vector3.zero)
                {
                    displacement = hitInfo.point - lastHitPosition;
                }
                lastHitPosition = new Vector3(hitInfo.point.x, hitInfo.point.y, hitInfo.point.z);

                if (displacement != Vector3.zero)
                {
                    // update the marker position
                    foreach (GameObject go in gos)
                    {
                        go.transform.position += new Vector3(displacement.x, 0, displacement.z);
                    }
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // reset the last hit position
            lastHitPosition = Vector3.zero;
        }

    }
    /// <summary>
    /// Drone 지도위에 그리기
    /// </summary>
    /// <param name="pos"></param>
    public void drawDrone(double[] pos)       
    {
        posVec[0] = (float)pos[0];
        posVec[2] = (float)pos[1];
        gameObject.transform.position = posVec;
    }
    /// <summary>
    /// Drone trace pos위치에 그리기
    /// </summary>
    /// <param name="pos"></param>
    public void drawTraceMarker(double[] pos)                   // trace 그리기
    {

        Transform marker = Instantiate(traceMarker);
        marker.name = "traceMarker_" + key;
        posVec[0] = (float)pos[0];
        posVec[2] = (float)pos[1];
        marker.transform.position = posVec;
    }
}
