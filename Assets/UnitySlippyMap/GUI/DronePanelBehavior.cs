using UnityEngine;
using UnityEngine.UI;
using GcsProject.Controller;
using System.Collections.Generic;
using UnitySlippyMap.Helpers;
using UnitySlippyMap.Map;
using UnitySlippyMap.DroneStruct;
using GcsProject.Model;

namespace UnitySlippyMap.UserGUI
{
    public class DronePanelBehavior : MonoBehaviour
    {

        #region Variables
        
        public bool runOnce = true;
        public Rect doWindowAdd; // 드론추가 창
        public Rect doWindowConnect; // 드론 연결 창
        public Rect doWindowWarnings; // 경고 창
        public Rect doWindowLoad; // Load 창
        public GUIStyle style;  
        public Transform droneMarker;
        private static bool renderAdd = false;       //드론추가창을 on/off
        private static bool renderConnect = false;
        private static bool renderNameSame = false;
        private static bool renderIDSame = false;
        private static bool renderError = false;
        private static bool renderLoadList = false;
        private string ip = "";
        private string gcsPort = "";
        private string port = "";
        private string systemID = "";
        private string componentID = "";
        private string droneName = "";
        private int selected;
        private int listCnt;
        private List<string> ipList;
        private List<string> nameList;
        private List<string> compList;
        private List<string> sysList;
        private List<string> portList;
        private List<string> gcsPortList;
        private GUIStyle windowStyle = new GUIStyle();
        private InputField droneTxt;    // unity 의 drone name input field
        private Text altTxt;            
        private Text longTxt;
        private Text latTxt;
        private Text sysID;
        private Text compID;
        private Text groundSpeed;
        private Text[] rpm = new Text[4];
        private InputField[] acc = new InputField[3];
        private InputField[] gyro = new InputField[3];
        private InputField[] mag = new InputField[3];
        private GameObject target;
        private GameObject[] targetArr;
        private byte[] id = new byte[2];
        private bool renderConflict = false;
        private bool renderSocketErr=false;
        #endregion
        // Use this for initialization
        void Start()
        {
            style.normal.textColor = Color.white;   // 시작할때 text color white로 바꿈
            selected = 0;
            listCnt = 0;
            ipList = new List<string>() ;
            nameList=new List<string>();
            compList=new List<string>();
            sysList=new List<string> ();
            portList=new List<string> ();
            gcsPortList=new List<string>();
        }
        public void getLoadList(List<ConnectList.ConnectStruct> list)
        {
            foreach (var data in list)
            {
                ipList.Add(data.ip);
                nameList.Add(data.name);
                compList.Add(data.componentId.ToString());
                sysList.Add(data.systemId.ToString());
                portList.Add(data.bindPort.ToString());
                gcsPortList.Add(data.gcsPort.ToString());
            }
            listCnt = ipList.Count;
        }
        /// <summary>
        /// +,- 버튼 입력에 대한 이벤트
        /// </summary>
        /// <param name="key"></param>
        public void getKey(string key)
        {
            GcsController controller = GameObject.Find("GameObject").GetComponent<GcsController>();
            Text droneKey = GameObject.Find("Key").GetComponent<Text>();
            switch (key)
            {
                case "+":  // 드론 추가
                    renderAdd = true;
                    break;
                case "-":    // 해당 키에 대한 드론 삭제
                    controller.RemoveDrone(int.Parse(droneKey.text));
                    deleteDrone(droneKey.text);
                    setDroneInfo();
                    break;
            }
        }
        /// <summary>
        /// Delete drones on the map by key
        /// </summary>
        /// <param name="key"></param>
        public void deleteDrone(string key)
        {
            ButtonBehavior btnBehavior = GameObject.Find("GameObject").GetComponent<ButtonBehavior>();

            btnBehavior.DeleteObjects("GPSfield");
            btnBehavior.DeleteObjects("Order");
            target = GameObject.Find("drone_" + key);
            Destroy(target);
            targetArr = GameObject.FindGameObjectsWithTag("Trace");
            foreach(GameObject go in targetArr)
            {
                if (go.name == "traceMarker_" + key)
                    Destroy(go);
            }

        }
        /// <summary>
        /// connect창 끄고 키기.
        /// </summary>
        /// <param name="show"></param>
        public void setRenderConnect(bool show)
        {
            renderConnect = show;
        }
        void OnGUI()
        {
            if (renderAdd)   // Add창
            {
                GUI.color = Color.cyan;
                doWindowAdd = GUI.Window(0, new Rect(Screen.width - 600, 100, 300, 150), DoWindowAdd, "Add Drone");
            }
            if (renderConnect)
            {
                GUI.color = Color.cyan;
                doWindowConnect = GUI.Window(1, new Rect(Screen.width - 600, 100, 200, 90), DoWindowConnect, "Connecting...");
            }
            if (renderNameSame)
            {
                GUI.color = Color.cyan;
                doWindowWarnings = GUI.Window(2, new Rect(Screen.width - 600, 100, 180, 80), DoWindowWarnings, "Warnings!");
            }
            if (renderIDSame)
            {
                GUI.color = Color.cyan;
                doWindowWarnings = GUI.Window(3, new Rect(Screen.width - 600, 100, 180, 80), DoWindowWarnings, "Warnings!");
            }
            if (renderError)
            {
                GUI.color = Color.cyan;
                doWindowWarnings = GUI.Window(4, new Rect(Screen.width - 600, 100, 180, 80), DoWindowWarnings, "Warnings!");
            }
            if(renderLoadList)
            {
                GUI.color = Color.cyan;
                doWindowLoad = GUI.Window(5, new Rect(Screen.width - 600, 100, 220, 80+20*listCnt), DoWindowLoad, "Drone Load");
            }
            if(renderConflict)
            {
                GUI.color = Color.cyan;
                doWindowWarnings = GUI.Window(6, new Rect(Screen.width - 600, 100, 180, 80), DoWindowWarnings, " Error !");
            }
            if (renderSocketErr)
            {
                GUI.color = Color.cyan;
                doWindowWarnings = GUI.Window(7, new Rect(Screen.width - 600, 100, 180, 80), DoWindowWarnings, " Error !");
            }
        }

        /// <summary>
        /// 추가 버튼 눌렀을 떄 그려지는 Add 창
        /// </summary>
        /// <param name="WindowID"></param>
        void DoWindowAdd(int WindowID)
        {
            SaveLoadBehavior SLB = GameObject.Find("GameObject").GetComponent<SaveLoadBehavior>();
            SLB.usingUI = true;
            GUI.Label(new Rect(20, 30, 20, 20), "IP", style);
            ip = GUI.TextField(new Rect(50, 30, 100, 20), ip);
            GUI.Label(new Rect(180, 30, 20, 20), "Port", style);
            port = GUI.TextField(new Rect(210, 30, 60, 20), port);
            GUI.Label(new Rect(20, 60, 50, 20), "System ID", style);
            systemID = GUI.TextField(new Rect(90, 60, 30, 20), systemID);
            GUI.Label(new Rect(140, 60, 90, 20), "Component ID", style);
            componentID = GUI.TextField(new Rect(240, 60, 30, 20), componentID);
            GUI.Label(new Rect(20, 90, 30, 20), "Name ", style);
            droneName = GUI.TextField(new Rect(60, 90, 60, 20), droneName);
            GUI.Label(new Rect(160, 90, 50, 20), "GCS Port ", style);
            gcsPort = GUI.TextField(new Rect(220, 90, 50, 20), gcsPort);
            if(GUI.Button(new Rect(70,120,50,20),"Load"))
            {
                renderAdd = false;
                renderLoadList=true;
            }
            if (GUI.Button(new Rect(130, 120, 40, 20), "Add"))   //Add버튼누를 때
            {
                SLB.usingUI = false;
                renderAdd = false;
                renderConnect = true;
                runOnce = true;
            }
            if (GUI.Button(new Rect(180, 120, 50, 20), "Close"))
            {
                SLB.usingUI = false;
                renderAdd = false;
                initInput();
                droneName = "";
            }
        }
        /// <summary>
        /// Initiallize input strings in window
        /// </summary>
        public void initInput()
        {
            ip = "";
            port = "";
            systemID = "";
            componentID = "";
            gcsPort = "";
        }
        /// <summary>
        /// display Connecting windows
        /// </summary>
        /// <param name="WindowID"></param>
        void DoWindowConnect(int WindowID)      // 연결중뜨는 창
        {
            SaveLoadBehavior SLB = GameObject.Find("GameObject").GetComponent<SaveLoadBehavior>();
            GcsController controller =GameObject.Find("GameObject").GetComponent<GcsController>();
            if (runOnce)
            {
                SLB.usingUI = true;
                GUI.Label(new Rect(20, 30, 100, 20), "Try Connecting ...", style);
                int testConnect;    // 테스트,
                testConnect = (int)controller.Connect(ip, int.Parse(port), int.Parse(systemID), int.Parse(componentID), droneName,int.Parse(gcsPort));

                switch (testConnect)
                {
                    case 1:
                        renderNameSame = true;
                        renderConnect = false;
                        break;
                    case 2:
                        renderIDSame = true;
                        renderConnect = false;
                        break;
                    case 3:
                        renderError = true;
                        renderConnect = false;
                        break;
                    case 4:
                        renderConflict = true;
                        renderConnect = false;
                        break;
                    case 5:
                        renderSocketErr = true;
                        renderConnect = false;
                        break;
                }
                if (GUI.Button(new Rect(130, 60, 50, 20), "Cancel"))
                {
                    SLB.usingUI = false;
                    renderConnect = false;
                }
                runOnce = false;
            }
        }


        /// <summary>
        /// 드론 이름이나 ID가 중복일 때 뜨는 경고창
        /// </summary>
        /// <param name="WindowID"></param>
        void DoWindowWarnings(int WindowID)
        {
            switch(WindowID)
            {
                case 2:
                    GUI.Label(new Rect(40, 20, 100, 20), "Same Name !");
                    break;
                case 3:
                    GUI.Label(new Rect(60, 20, 100, 20), "Same ID !");
                    break;
                case 4:
                    GUI.Label(new Rect(60, 20, 100, 20), "Errors !");
                    break;
                case 6:
                    GUI.Label(new Rect(60, 20, 100, 20), "Conflict GCS Port !");
                    break;
                case 7:
                    GUI.Label(new Rect(60, 20, 100, 20), "Socket Error !");
                    break;
            }
            if (GUI.Button(new Rect(70, 50, 40, 20), "OK"))
            {
                renderIDSame = false;
                renderNameSame = false;
                renderError = false;
                renderAdd = true;
                renderSocketErr = false;
                renderConflict = false;
            }
        }
        /// <summary>
        /// 드론 load
        /// </summary>
        /// <param name="WindowID"></param>
        void DoWindowLoad(int WindowID)
        {
            selected = GUI.SelectionGrid(new Rect(20, 20, 180, 20 * listCnt),selected,nameList.ToArray(),1);
            if(GUI.Button(new Rect(140,40+20*listCnt,40,20),"Load"))
            {
                droneName = nameList[selected];
                ip = ipList[selected];
                componentID = compList[selected];
                systemID = sysList[selected];
                port = portList[selected];
                gcsPort = gcsPortList[selected];
                renderLoadList = false;
                renderAdd = true;
            }
        }
       

        // Update is called once per frame
        void Update()
        {
           
        }
        /// <summary>
        /// drone Panel의 텍스트 component들을 매칭시킨다.
        /// </summary>
        public void connectComplete(int droneKey)
        {
            SaveLoadBehavior SLB = GameObject.Find("GameObject").GetComponent<SaveLoadBehavior>();
            GcsController controller = GameObject.Find("GameObject").GetComponent<GcsController>();
            drawDroneMarker(droneKey);
            controller.AddConnectList(saveDrone());
            SLB.usingUI = false;
            renderConnect = false; //창 닫음
        }
        /// <summary>
        /// 연결 성공시 자동으로 드론을 저장
        /// </summary>
        /// <param name="data"></param>
        private ConnectList.ConnectStruct saveDrone()
        {
            ConnectList.ConnectStruct data = new ConnectList.ConnectStruct();
            data.ip = ip;
            data.bindPort = int.Parse(port);
            data.componentId = int.Parse(componentID);
            data.systemId = int.Parse(systemID);
            data.name = droneName;
            data.gcsPort = int.Parse(gcsPort);
            initInput();
            return data;
        }
        /// <summary>
        /// 드론 마커 생성
        /// </summary>
        public void drawDroneMarker(int droneKey)
        {
            Transform newDrone = Instantiate(droneMarker);  // droneMarker 생성. 
            Vector3 vec = new Vector3(0, 0, 0);
            newDrone.transform.position = vec;
            newDrone.name = "drone_" + droneKey;
            DroneBehavior droneBehavior = newDrone.GetComponent<DroneBehavior>();
            droneBehavior.key = droneKey;
            print(droneName);
            droneBehavior.droneName = droneName;
            droneName = "";
        }

        /// <summary>
        /// key값으로 해당 드론 position update 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="pos"></param>
        public void setDronePosByKey(int key,double[] pos)
        {
            DroneBehavior drone = GameObject.Find("drone_" + key).GetComponent<DroneBehavior>();
            MapBehaviour map = GameObject.Find("Test").GetComponent<MapBehaviour>();
            pos = GeoHelpers.WGS84ToRaycastHit(map, pos);     // drone 경도,위도를 screen 좌표로 변환
            drone.drawDrone(pos);
        }

        /// <summary>
        /// key값으로 해당 드론의 traceMarker 그리기
        /// </summary>
        /// <param name="key"></param>
        /// <param name="pos"></param>
       public void setTraceMarkerByKey(int key,double[] pos)
        {
            DroneBehavior drone = GameObject.Find("drone_" + key).GetComponent<DroneBehavior>();
            MapBehaviour map = GameObject.Find("Test").GetComponent<MapBehaviour>();
            pos = GeoHelpers.WGS84ToRaycastHit(map, pos);
            drone.drawTraceMarker(pos);
        }

        /// <summary>
        /// drone 패널에 drone정보 모두 update 
        /// </summary>
        /// <param name="drone"></param>
        public void setDroneInfo(DroneInfo drone)
        {
            if (drone != null)
            {
                droneTxt = GameObject.Find("Drone name").GetComponent<InputField>();
                sysID = GameObject.Find("SysID").GetComponent<Text>();
                compID = GameObject.Find("CompID").GetComponent<Text>();
                altTxt = GameObject.Find("droneAlt").GetComponent<Text>();
                longTxt = GameObject.Find("droneLong").GetComponent<Text>();
                latTxt = GameObject.Find("droneLat").GetComponent<Text>();
                groundSpeed = GameObject.Find("groundSpeed").GetComponent<Text>();
                rpm[0] = GameObject.Find("RPM 1").GetComponent<Text>();
                rpm[1] = GameObject.Find("RPM 2").GetComponent<Text>();
                rpm[2] = GameObject.Find("RPM 3").GetComponent<Text>();
                rpm[3] = GameObject.Find("RPM 4").GetComponent<Text>();
                acc[0] = GameObject.Find("Acc_x").GetComponent<InputField>();
                acc[1] = GameObject.Find("Acc_y").GetComponent<InputField>();
                acc[2] = GameObject.Find("Acc_z").GetComponent<InputField>();
                gyro[0] = GameObject.Find("Gyro_x").GetComponent<InputField>();
                gyro[1] = GameObject.Find("Gyro_y").GetComponent<InputField>();
                gyro[2] = GameObject.Find("Gyro_z").GetComponent<InputField>();
                mag[0] = GameObject.Find("Mag_x").GetComponent<InputField>();
                mag[1] = GameObject.Find("Mag_y").GetComponent<InputField>();
                mag[2] = GameObject.Find("Mag_z").GetComponent<InputField>();
                droneTxt.text = drone.name;
                sysID.text = "System ID : " + drone.systemID;
                compID.text = "Component ID : " + drone.componentID;
                altTxt.text = "Altitude : " + drone.altitude+" m";
                longTxt.text = "Longtitude : " + drone.longtitude;
                latTxt.text = "Latitude : " + drone.latitude;
                groundSpeed.text = "Ground Speed : " + drone.droneInfo.groundspeed*3.6+" km/h";
                /*
                     * RPM 관련 데이터는 아직 정의되지 않은 상태이므로 임시 조치함
                    rpm[0].text = "RPM 1 : " + drone.droneInfo.rpm[0];
                    rpm[1].text = "RPM 2 : " + drone.droneInfo.rpm[1];
                    rpm[2].text = "RPM 3 : " + drone.droneInfo.rpm[2];
                    rpm[3].text = "RPM 4 : " + drone.droneInfo.rpm[3];
                */
                acc[0].text = drone.droneInfo.acc.x.ToString();
                acc[1].text = drone.droneInfo.acc.y.ToString();
                acc[2].text = drone.droneInfo.acc.z.ToString();
                gyro[0].text = drone.droneInfo.gyro.x.ToString();
                gyro[1].text = drone.droneInfo.gyro.y.ToString();
                gyro[2].text = drone.droneInfo.gyro.z.ToString();
                mag[0].text = drone.droneInfo.mag.x.ToString();
                mag[1].text = drone.droneInfo.mag.y.ToString();
                mag[2].text = drone.droneInfo.mag.z.ToString();
            }
         }
        /// <summary>
        /// set Drone panel to initial value
        /// </summary>
        public void setDroneInfo()
        {
            droneTxt = GameObject.Find("Drone name").GetComponent<InputField>();
            sysID = GameObject.Find("SysID").GetComponent<Text>();
            compID = GameObject.Find("CompID").GetComponent<Text>();
            altTxt = GameObject.Find("droneAlt").GetComponent<Text>();
            longTxt = GameObject.Find("droneLong").GetComponent<Text>();
            latTxt = GameObject.Find("droneLat").GetComponent<Text>();
            groundSpeed = GameObject.Find("groundSpeed").GetComponent<Text>();
            rpm[0] = GameObject.Find("RPM 1").GetComponent<Text>();
            rpm[1] = GameObject.Find("RPM 2").GetComponent<Text>();
            rpm[2] = GameObject.Find("RPM 3").GetComponent<Text>();
            rpm[3] = GameObject.Find("RPM 4").GetComponent<Text>();
            acc[0] = GameObject.Find("Acc_x").GetComponent<InputField>();
            acc[1] = GameObject.Find("Acc_y").GetComponent<InputField>();
            acc[2] = GameObject.Find("Acc_z").GetComponent<InputField>();
            gyro[0] = GameObject.Find("Gyro_x").GetComponent<InputField>();
            gyro[1] = GameObject.Find("Gyro_y").GetComponent<InputField>();
            gyro[2] = GameObject.Find("Gyro_z").GetComponent<InputField>();
            mag[0] = GameObject.Find("Mag_x").GetComponent<InputField>();
            mag[1] = GameObject.Find("Mag_y").GetComponent<InputField>();
            mag[2] = GameObject.Find("Mag_z").GetComponent<InputField>();
            droneTxt.text = "";
            sysID.text = "";
            compID.text = "";
            altTxt.text = "";
            longTxt.text = "";
            latTxt.text = "";
            groundSpeed.text = "";
            acc[0].text = "";
            acc[1].text = "";
            acc[2].text = "";
            gyro[0].text = "";
            gyro[1].text = "";
            gyro[2].text = "";
            mag[0].text = "";
            mag[1].text = "";
            mag[2].text = "";

        }
        /// <summary>
        /// get System ID and Component ID by Key
        /// </summary>
        /// <param name="compID"></param>
        /// <param name="sysID"></param>
        public void getID(byte compID, byte sysID)
        {
            id[0] = sysID;
            id[1] = compID;
        }
        public byte[] getID()
        {
            return id;
        }
    }
}
