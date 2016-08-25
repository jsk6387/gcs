using UnityEngine;
using UnitySlippyMap.Helpers;
using UnitySlippyMap.Map;
using UnitySlippyMap.UserGUI;
using UnityEngine.UI;
using UnitySlippyMap.MyInput;
using System.Threading;
using System;
using System.IO;
using GcsProject.Controller;
using GcsProject.Model;
public class DroneBehavior : MonoBehaviour {
    public double[] dronePos = new double[2] ;   // drone 경도,위도,고도
    public int key=0;
    public string droneName = "";
    public Transform traceMarker;
    private string nowDate;
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
    private static float zoomScale = 1.02f; // 드론 확대, 축소 
    private ManualResetEvent logEvent;
    private Timer logTimer = null; //드론 로그 기록 타이머

    void Start () {
        
        MapBehaviour map = GameObject.Find("Test").GetComponent<MapBehaviour>();
        dronePos[0]=126.88;
        dronePos[1] = 37.488;
        dronePos = GeoHelpers.WGS84ToRaycastHit(map, dronePos);     //드론의 위도,경도 위치를 화면상 위치로 변환함
        posVec[0] = (float)dronePos[0];                             // vector에 드론 위치 하나씩 저장 x : 경도 ,z : 위도 
        posVec[2] = (float)dronePos[1];
        gameObject.transform.position = posVec;                     // 그 후 gameobject , 즉 드론의 위치를 vector로 설정

        if (int.Parse(droneKey.text) > 0)                           // 드론 마커 줌 크기 조정
            zoomScale += 0.003f * map.getMarkerCnt();
                                                                    // guistyle color 조정
        style.normal.textColor = Color.white;


        nowDate = DateTime.Now.ToString("yy-MM-dd HH-mm");
        logTimer = new Timer(SaveLog, logEvent, 0, 250);

    }

    void Awake()
    {
        controller = GameObject.Find("GameObject").GetComponent<GcsController>();
        dronePanel = GameObject.Find("GameObject").GetComponent<DronePanelBehavior>();
        btnBehavior = GameObject.Find("GameObject").GetComponent<ButtonBehavior>();
        nowDate = DateTime.Now.ToString("yyyy-MM-dd HH-mm");
        droneKey = GameObject.Find("Key").GetComponent<Text>();
        droneKey.text = key.ToString();
        print(droneKey);
        logEvent = new ManualResetEvent(false);

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
    /// <summary>
    /// 드론의 운행정보를 LOG기록.
    /// </summary>
    /// <param name="obj"></param>
    public void SaveLog(object obj)
    {
        GcsModel.DroneStruct model;
        model.drone = controller.GetTraceInfo(key);
            string logDirPath = string.Format("D:\\Logs\\{0}\\{1}", model.drone.bindPort, DateTime.Today.ToString("yyyy-MM-dd"));
            string logFilePath = string.Format("{0}\\{1}_log.txt", logDirPath, nowDate);
            if (!Directory.Exists(logDirPath))
            {
                Directory.CreateDirectory(logDirPath);
            }
            FileInfo file = new FileInfo(logFilePath);
            string logMsg = string.Format("[{0}],{1},{2},{3},{4}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), model.drone.position.latitude
                , model.drone.position.longitude, model.drone.position.altitude, model.drone.groundSpeed);
            if (!file.Exists)
            {
                StreamWriter sw = new StreamWriter(logFilePath);
                sw.WriteLine(logMsg);
                sw.Close();

            }
            else
            {
                using (StreamWriter sw = File.AppendText(logFilePath))
                {
                    sw.WriteLine(logMsg);
                    sw.Close();
                }
            }
        
    }
}
