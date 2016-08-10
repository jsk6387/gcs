/*
 * Drone을 포함한 각종 데이터를 처리 및 관리하는 역할을 함.
 * Timer를 이용하여 드론 정보 출력 및 자취 기록을 일정하게 수행함. - Kero Kim -
 */
using GcsProject.Controller;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;
using System;

namespace GcsProject.Model
{
    /// <summary>
    /// 내부 데이터 관리 역할
    /// </summary>
    class GcsModel
    {
        /// <summary>
        /// 드론 및 해당 드론의 연결을 위한 Connector로 구성된 구조체
        /// </summary>
        public struct DroneStruct
        {
            public Drone drone;
            public Connector connector;
        }
        public struct ConnectStruct
        {
            public string ip;
            public int bindPort;
            public int systemId;
            public int componentId;
            public string name;
            public int gcsPort;
        }
        protected List<DroneStruct> droneList; // 드론 정보 리스트
        private Drone printDrone = null; // UI에 운행 정보를 출력하는 드론의 참조
        private GcsController controller = null;
        Timer printTimer = null; // 드론 정보 출력 타이머
        Timer traceTimer = null; // 드론 자취 기록 타이머
        ManualResetEvent printEvent;
        ManualResetEvent traceEvent;
        private string connectFileName = "connect.ini"; // 연결 정보 파일명

        public GcsModel(GcsController controller)
        {
            this.controller = controller;
            droneList = new List<DroneStruct>();
            printEvent = new ManualResetEvent(false);
            traceEvent = new ManualResetEvent(false);
            printTimer = new Timer(PrintDroneInfo, printEvent, 0, 250); // 250ms 단위로 실행
            traceTimer = new Timer(OnTrace, traceEvent, 0, 2000); // 2000ms 단위로 실행
        }
        /// <summary>
        /// 내부 드론 리스트에 새로운 드론을 추가
        /// </summary>
        /// <param name="drone"></param>
        /// <returns></returns>
        public int AddDrone(Drone drone)
        {
            return AddDrone(drone, new Connector(drone));
        }
        /// <summary>
        /// 내부 드론 리스트에 새로운 드론을 추가. 드론 리스트의 항목 개수 반환. 실패시 -1 반환
        /// </summary>
        /// <param name="drone"></param>
        /// <param name="connector"></param>
        /// <returns></returns>
        public int AddDrone(Drone drone, Connector connector)
        {
            if (drone != null)
            {
                DroneStruct ds = new DroneStruct();
                ds.drone = drone;
                ds.connector = connector;
                droneList.Add(ds);
                // 운행 정보 참조가 등록되어있지 않으면, 등록을 해줌
                if (printDrone == null)
                {
                    Interlocked.Exchange(ref printDrone, drone); // 운행 정보를 출력하기 위해 해당 참조를 등록
                                                                 // Interlocked를 이용해서 비동기로 인한 문제 방지
                }
                return droneList.Count;
            }
            return -1;
        }
        /// <summary>
        /// 드론 리스트를 생성함. 커넥터는 포함하지 않음.
        /// </summary>
        /// <returns></returns>
        public List<Drone> GetDroneList()
        {
            List<Drone> result = new List<Drone>();
            foreach(DroneStruct item in droneList)
            {
                Drone drone = (Drone)item.drone.Clone();
                result.Add(drone);
            }
            return result;
        }
        /// <summary>
        /// 내부 드론 리스트의 드론 객체를 교체함
        /// </summary>
        /// <param name="drone">교체할 드론의 데이터</param>
        /// <param name="index">특정 위치에 추가할 경우 사용하는 위치 지정 인덱스</param>
        /// <returns></returns>
        public bool SetDrone(Drone drone, int index = -1)
        {
            if (drone == null)
            {
                return false;
            }
            int droneIndex = -1;
            if (index == -1)
            {
                for (int i = 0; i < droneList.Count; i++)
                {
                    if (droneList[i].drone.id == drone.id && droneList[i].drone.componentId == drone.componentId)
                    {
                        droneIndex = i;
                        break;
                    }
                }
            }
            else if (index < droneList.Count && index >= 0)
            {
                droneIndex = index;
            }
            if (droneIndex != -1)
            {
                DroneStruct ds = droneList[index];
                ds.drone = drone;
                droneList[index] = ds;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 선택한 드론 객체를 반환함. 실패시 null 반환
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="componentId"></param>
        /// <returns></returns>
        public Drone GetDrone(byte systemId, byte componentId)
        {
            for (int i = 0; i < droneList.Count; i++)
            {
                if (droneList[i].drone.id == systemId && droneList[i].drone.componentId == componentId)
                {
                    return droneList[i].drone;
                }
            }
            return null;
        }
        /// <summary>
        /// 내부 드론 리스트에서 드론을 제거함
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="componentId"></param>
        public void RemoveDrone(byte systemId, byte componentId)
        {
            for (int i = 0; i < droneList.Count; i++)
            {
                if (droneList[i].drone.id == systemId && droneList[i].drone.componentId == componentId)
                {
                    // 제거하는 드론이 운행 정보 참조에 등록된 경우 해제함
                    if (printDrone == droneList[i].drone)
                    {
                        Interlocked.Exchange(ref printDrone, null); // 비동기 처리로 인한 오류 방지
                    }
                    droneList[i].connector.Dispose();
                    droneList.RemoveAt(i);
                    break;
                }
            }
        }
        /// <summary>
        /// 선택한 드론의 커넥터 개방 여부를 설정
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="componentId"></param>
        /// <param name="isOpen">커넥터 개방 여부</param>
        public void ConnectorSwitch(byte systemId, byte componentId, bool isOpen)
        {
            for (int i = 0; i < droneList.Count; i++)
            {
                if (droneList[i].drone.id == systemId && droneList[i].drone.componentId == componentId)
                {
                    Connector connector = droneList[i].connector;
                    if (connector != null)
                    {
                        if (isOpen)
                        {
                            connector.Initialize();
                        }
                        else
                        {
                            connector.Dispose();
                        }
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// 선택한 드론의 운행 정보 설정
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="componentId"></param>
        /// <param name="plan"></param>
        public void SetPlan(byte systemId, byte componentId, List<PositionInt> plan)
        {
            for (int i = 0; i < droneList.Count; i++)
            {
                if (droneList[i].drone.id == systemId && droneList[i].drone.componentId == componentId)
                {
                    droneList[i].drone.plan = plan;
                    break;
                }
            }
        }
        /// <summary>
        /// UI에 드론 운행 정보와 드론 Marker를 출력하도록 요청
        /// </summary>
        /// <param name="obj"></param>
        public void PrintDroneInfo(object obj)
        {
            foreach (var item in droneList)
            {
                controller.PrintDrone(item.drone.id, item.drone.componentId, item.drone.position);
            }
            if (printDrone != null)
            {
                controller.PrintDroneInfo(printDrone);
            }
        }
        /// <summary>
        /// UI의 운행 정보를 출력할 드론을 선택
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="componentId"></param>
        public void ChangePrintDrone(byte systemId, byte componentId)
        {
            for (int i = 0; i < droneList.Count; i++)
            {
                if (droneList[i].drone.id == systemId && droneList[i].drone.componentId == componentId)
                {
                    Interlocked.Exchange(ref printDrone, droneList[i].drone); // 비동기 오류 방지
                    controller.PrintPlanList(printDrone);
                    break;
                }
            }
        }
        /// <summary>
        /// 각 드론에게 자취 저장을 요청하고, 저장된 자취값을 UI에 Marker 형태로 출력
        /// </summary>
        /// <param name="obj"></param>
        public void OnTrace(object obj)
        {
            foreach (DroneStruct item in droneList)
            {
                Drone result = item.drone.SetTrace();
                controller.PrintTrace(result.id, result.componentId, result.position);
            }
        }
        /// <summary>
        /// 연결 정보 파일을 읽음
        /// </summary>
        /// <returns></returns>
        public bool LoadConnectFile()
        {
            string[] sections = GetSectionNames();
            List<ConnectStruct> list = new List<ConnectStruct>();
            foreach (var item in sections)
            {
                ConnectStruct data = new ConnectStruct();
                try
                {
                    data.ip = ReadIniFile(item, "ip");
                    data.bindPort = int.Parse(ReadIniFile(item, "drone_port"));
                    data.systemId = int.Parse(ReadIniFile(item, "system_id"));
                    data.componentId = int.Parse(ReadIniFile(item, "component_id"));
                    data.name = ReadIniFile(item, "name");
                    data.gcsPort = int.Parse(ReadIniFile(item, "gcs_port"));
                    list.Add(data);
                }
                catch (Exception e)
                {
                    return false;
                }
                controller.PrintConnectList(list);
            }
            return true;
        }
        /// <summary>
        /// ini 파일에서 필요한 값을 읽음
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string ReadIniFile(string section, string key, string defaultValue = "")
        {
            const int MAX_SIZE = 255;
            StringBuilder temp = new StringBuilder(MAX_SIZE);
            GetPrivateProfileString(section, key, defaultValue, temp, MAX_SIZE, connectFileName);
            return temp.ToString();
        }
        /// <summary>
        /// ini 파일의 Section 리스트를 얻음
        /// </summary>
        /// <returns></returns>
        public string[] GetSectionNames()
        {
            for (int maxsize = 500; true; maxsize *= 2)
            {
                byte[] bytes = new byte[maxsize];
                int size = GetPrivateProfileString(0, "", "", bytes, maxsize, connectFileName);

                if (size < maxsize - 2)
                {
                    string Selected = Encoding.ASCII.GetString(bytes, 0, size - (size > 0 ? 1 : 0));
                    return Selected.Split(new char[] { '\0' });
                }
            }
        }
        /// <summary>
        /// 연결 정보 파일을 작성
        /// </summary>
        /// <param name="data"></param>
        /// <param name="section">기본값을 사용할 경우 현재 일시를 기준으로 생성</param>
        /// <returns></returns>
        public bool WriteConnectFile(ConnectStruct data, string section = "__currentDate")
        {
            if (section.Equals("__currentDate")) // 현재 일시를 기준으로 섹션명을 생성
            {
                section = DateTime.Now.ToString("yyyyMMddHHmmss");
            }
            try
            {
                WritePrivateProfileString(section, "ip", data.ip, connectFileName);
                WritePrivateProfileString(section, "drone_port", data.bindPort.ToString(), connectFileName);
                WritePrivateProfileString(section, "system_id", data.systemId.ToString(), connectFileName);
                WritePrivateProfileString(section, "component_id", data.componentId.ToString(), connectFileName);
                WritePrivateProfileString(section, "name", data.name, connectFileName);
                WritePrivateProfileString(section, "gcs_port", data.gcsPort.ToString(), connectFileName);
                return true;
            }
            catch(Exception e)
            {
                return false;
            }
        }
        // ini 파일 입출력을 위한 API
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, 
                                                             string key, 
                                                             string val, 
                                                             string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, 
                                                          string key, 
                                                          string def, 
                                                          StringBuilder retVal, 
                                                          int size, 
                                                          string filePath);
        [DllImport ("kernel32")]
        static extern int GetPrivateProfileString (int Section,
                                                   string Key, 
                                                   string Value,
                                                   [MarshalAs (UnmanagedType.LPArray)] byte[] Result, 
                                                   int Size, 
                                                   string FileName);
    }
}
