using UnityEngine;
using UnitySlippyMap.Helpers;
using UnitySlippyMap.Map;
using UnitySlippyMap.UserGUI;
using UnityEngine.UI;
using UnitySlippyMap.MyInput;
using GcsProject.Controller;
public class DroneBehavior : MonoBehaviour {
    public double[] dronePos = new double[2] ;   // drone 경도,위도,고도
    public int key=0;
    public string droneName = "";
    public Transform traceMarker;
    private GameObject[] gos;
    private GcsController controller;
    private DronePanelBehavior dronePanel;
    private ButtonBehavior btnBehavior;
    private static Vector3 lastHitPosition = Vector3.zero;
    private Vector3 posVec = new Vector3(0, 0, 0);  // drone 의 위치벡터
    private Text droneKey;
    private Rect doWindowDrone;
    private bool renderWindowDrone;
    public GUIStyle style;
    private static float zoomScale = 1.015f; // 드론 확대, 축소 비율
    // Use this for initialization
    
    void Start () {
        //getDronePos();
        MapBehaviour map = GameObject.Find("Test").GetComponent<MapBehaviour>();
        dronePos[0]=126.88;
        dronePos[1] = 37.488;
        dronePos = GeoHelpers.WGS84ToRaycastHit(map, dronePos); //드론의 위도,경도 위치를 화면상 위치로 변환함
        posVec[0] = (float)dronePos[0];      // vector에 드론 위치 하나씩 저장 x : 경도 ,z : 위도 
        posVec[2] = (float)dronePos[1];
        gameObject.transform.position = posVec;      // 그 후 gameobject , 즉 드론의 위치를 vector로 설정
        
        // 드론 마커 줌 크기 조정
        if (int.Parse(droneKey.text) > 0)
            zoomScale += 0.003f * map.getMarkerCnt();
        // guistyle color 조정
        style.normal.textColor = Color.white;
    }

    void Awake()
    {
        controller = GameObject.Find("GameObject").GetComponent<GcsController>();
        dronePanel = GameObject.Find("GameObject").GetComponent<DronePanelBehavior>();
        btnBehavior = GameObject.Find("GameObject").GetComponent<ButtonBehavior>();
        droneKey = GameObject.Find("Key").GetComponent<Text>();
        droneKey.text = key.ToString();
        print(droneKey);
    }
    /// <summary>
    /// 드론 클릭시 운행정보 창 전환
    /// </summary>
	public void OnMouseDown()       
    {
        controller.GetDroneInfo(key);
        btnBehavior.doClearPlan();
        droneKey.text = key.ToString();
    }

    public void OnMouseOver()
    {
        controller.GetDroneID(key);
        renderWindowDrone = true;
    }
    public void OnMouseExit()
    {
        renderWindowDrone = false;
    }
    void OnGUI()
    {
        if(renderWindowDrone)
        {
            GUI.color = Color.cyan;
            doWindowDrone = GUI.Window(0, new Rect(Input.mousePosition.x, Screen.height - Input.mousePosition.y - 120, 180, 100), DoWindowDrone, "Drone Info");
        }
    }
    
    void DoWindowDrone(int WindowID)
    {
        GUI.Label(new Rect(20, 20, 80, 80), " Name : " + droneName + "\n System ID : " + dronePanel.getID()[0]
                + "\n Component ID : " + dronePanel.getID()[1], style);
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

        // 지도 축소, 확대할 떄 드론마커의 크기비율
        if (MapInput.isZoom && MapInput.lastZoomFactor < 0)
        {
            gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x * zoomScale,
                gameObject.transform.localScale.y * zoomScale, gameObject.transform.localScale.z * zoomScale);
            print(gameObject.transform.localScale.x);
        }
        else if (MapInput.isZoom && MapInput.lastZoomFactor > 0)
        {
            gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x / zoomScale,
                gameObject.transform.localScale.y / zoomScale, gameObject.transform.localScale.z / zoomScale);
            print(gameObject.transform.localScale.x);
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
