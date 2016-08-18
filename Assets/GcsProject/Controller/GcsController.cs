/*
 * GCSController는 UnitySlippyMap을 MVC 모델의 View로 보고,
 * MAVLink로 받아들인 데이터를 가진 드론 데이터, Model의 값을
 * UI로 전달하기 위해 각종 변환 작업을 수행하고,
 * UI에서 Model로 데이터를 전달하는 과정의 중간 역할을 함. - Kero Kim -
 */
using GcsProject.Model;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnitySlippyMap.Map;
using MavLinkNet;
namespace GcsProject.Controller
{
    /// <summary>
    /// UI와 Model간의 데이터 교환을 위한 Controller
    /// </summary>
    class GcsController : MonoBehaviour
    {
        private UIManager manager; // UI로 데이터를 전달하기 위한 참조
        private MapBehaviour map;
        /// <summary>
        /// 연결 시도 반환 값
        /// </summary>
        public enum ConnectError
        {
            Success, // 연결 설정 값이 정상적으로 입력됨
            ConflictName, // 내부에 중복된 드론 이름이 있음
            ConflictId, // 내부에 System ID와 Component ID가 중복된 드론이 있음
            Error, // 그 외의 알 수 없는 오류 발생
            ConflictGcsPort, // GCS 포트 충돌
            SocketError // 소켓 오류
        }
        private GcsModel model = null;
        /// <summary>
        /// System ID와 Component ID를 동시에 관리하기 위한 구조체
        /// </summary>
        private struct IDStruct
        {
            public byte systemId;
            public byte componentId;
        }
        private Dictionary<int, IDStruct> indexTable; // 드론을 식별하기 위해 UI의 Key 값을 
                                                      // Model의 System ID, Component ID와 매핑할 수 있도록 하는 인덱스 테이블
        private static int key = 0; // Key 값
        void Awake()
        {
            model = new GcsModel(this);
            indexTable = new Dictionary<int, IDStruct>();
            manager = GameObject.Find("GameObject").GetComponent<UIManager>();
            map = GameObject.Find("Test").GetComponent<MapBehaviour>();
        }
        /// <summary>
        /// 드론에 연결을 시도함
        /// </summary>
        /// <param name="ip">연결할 IP</param>
        /// <param name="port">연결할 Port (Bind Port)</param>
        /// <param name="systemId">드론의 System ID</param>
        /// <param name="componentId">드론의 Component ID</param>
        /// <param name="name">드론 이름</param>
        /// <returns></returns>
        public ConnectError Connect(string ip, int port, int systemId, int componentId, string name, int gcsPort = 14560)
        {
            foreach (Drone item in model.GetDroneList())
            {
                if (item.name.Equals(name)) // 이름 중복 여부 확인
                {
                    return ConnectError.ConflictName;
                }
                else if (item.id == systemId && item.componentId == componentId) // 드론의 System ID와 Component ID 중복 여부 확인
                {
                    return ConnectError.ConflictId;
                }
            }
            try
            {
                Drone drone = new Drone(Convert.ToByte(systemId), Convert.ToByte(componentId), name, ip, port);
                Connector connector = new Connector(drone, gcsPort);
                connector.Initialize();
                connector.connectEvent += Connected; // 연결 성공 시 발생할 이벤트 등록
            }
            catch(SocketException e)
            {
                if (e.SocketErrorCode == SocketError.AddressAlreadyInUse) // 포트 충돌
                {
                    return ConnectError.ConflictGcsPort;
                }
                else // 그 밖의 소켓 오류 발생
                {
                    return ConnectError.SocketError;
                }
            }
            catch (Exception e)
            {
                return ConnectError.Error;
            }
            return ConnectError.Success;
        }
        /// <summary>
        /// 연결 성공시 발생하는 이벤트
        /// </summary>
        /// <param name="drone"></param>
        /// <param name="connector"></param>
        private void Connected(object drone, object connector)
        {
            Drone newDrone = (Drone)drone;
            Connector newConnector = (Connector)connector;
            model.AddDrone(newDrone, newConnector);
            IDStruct keyValue = new IDStruct();
            keyValue.systemId = newDrone.id;
            keyValue.componentId = newDrone.componentId;
            indexTable.Add(key, keyValue);
            // UI에 연결 성공 신호를 보냄
            UIManager.UIMessage msg = new UIManager.UIMessage();
            msg.id = UIManager.UIMessageType.ConnectedComplete;
            object[] param = new object[1];
            param[0] = key++;
            msg.parameters = param;
            manager.Push(msg);
        }
        /// <summary>
        /// 드론의 연결을 끊고 드론 정보를 삭제함
        /// </summary>
        /// <param name="key"></param>
        public void RemoveDrone(int key)
        {
            model.RemoveDrone(Convert.ToByte(indexTable[key].systemId), Convert.ToByte(indexTable[key].componentId));
        }
        /// <summary>
        /// 드론의 운행 계획을 설정함
        /// </summary>
        /// <param name="key"></param>
        /// <param name="plan"></param>
        public void SetPlan(int key, List<PositionDouble> plan)
        {
            List<PositionInt> newPlan = new List<PositionInt>();
            foreach (PositionDouble item in plan)
            {
                newPlan.Add(PositionDoubleToInt(item));
            }
            model.SetPlan(Convert.ToByte(indexTable[key].systemId), Convert.ToByte(indexTable[key].componentId), newPlan);
        }
        /// <summary>
        /// 운행 정보를 출력할 드론을 변경함
        /// </summary>
        /// <param name="key"></param>
        public void GetDroneInfo(int key)
        {
            model.ChangePrintDrone(Convert.ToByte(indexTable[key].systemId), Convert.ToByte(indexTable[key].componentId));
        }
        /// <summary>
        /// UI에 드론 Marker 출력을 요청함
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="componentId"></param>
        /// <param name="position"></param>
        public void PrintDrone(byte systemId, byte componentId, PositionInt position)
        {
            PositionDouble newPos = PositionIntToDouble(position);
            double[] pos = { newPos.longitude, newPos.latitude };
            foreach (var item in indexTable)
            {
                if (item.Value.systemId == systemId && item.Value.componentId == componentId)
                {
                    UIManager.UIMessage msg = new UIManager.UIMessage();
                    msg.id = UIManager.UIMessageType.DrawDroneMarker;
                    object[] param = new object[2];
                    param[0] = item.Key;
                    param[1] = pos;
                    msg.parameters = param;
                    manager.Push(msg);
                    break;
                }
            }
        }
        /// <summary>
        /// UI에 드론의 자취를 출력함
        /// </summary>
        /// <param name="systemId"></param>
        /// <param name="componentId"></param>
        /// <param name="position"></param>
        public void PrintTrace(byte systemId, byte componentId, PositionInt position)
        {
            PositionDouble newPos = PositionIntToDouble(position);
            double[] pos = { newPos.longitude, newPos.latitude };
            foreach (var item in indexTable)
            {
                if (item.Value.systemId == systemId && item.Value.componentId == componentId)
                {
                    UIManager.UIMessage msg = new UIManager.UIMessage();
                    msg.id = UIManager.UIMessageType.DrawTraceMarker;
                    object[] param = new object[2];
                    param[0] = item.Key;
                    param[1] = pos;
                    msg.parameters = param;
                    manager.Push(msg);
                    break;
                }
            }
        }
        /// <summary>
        /// UI에 드론의 운행 정보를 출력함
        /// </summary>
        /// <param name="drone"></param>
        public void PrintDroneInfo(Drone drone)
        {
            UnitySlippyMap.DroneStruct.DroneInfo droneInfo = new UnitySlippyMap.DroneStruct.DroneInfo();
            PositionDouble pos = PositionIntToDouble(drone.position);
            droneInfo.altitude = pos.altitude;
            droneInfo.latitude = pos.latitude;
            droneInfo.longtitude = pos.longitude;
            droneInfo.droneInfo.acc.x = drone.sensor.acc.x;
            droneInfo.droneInfo.acc.x = drone.sensor.acc.x;
            droneInfo.droneInfo.acc.x = drone.sensor.acc.x;
            droneInfo.droneInfo.battery = drone.battery;
            droneInfo.droneInfo.groundspeed = drone.groundSpeed;
            droneInfo.droneInfo.gyro.x = drone.sensor.gyro.x;
            droneInfo.droneInfo.gyro.x = drone.sensor.gyro.x;
            droneInfo.droneInfo.gyro.x = drone.sensor.gyro.x;
            droneInfo.droneInfo.mag.x = drone.sensor.mag.x;
            droneInfo.droneInfo.mag.x = drone.sensor.mag.x;
            droneInfo.droneInfo.mag.x = drone.sensor.mag.x;
            droneInfo.droneInfo.rpm = drone.motorSpeed;
            droneInfo.systemID = drone.id;
            droneInfo.componentID = drone.componentId;
            droneInfo.name = drone.name;
            // UI로 운행 정보 전달
            UIManager.UIMessage msg = new UIManager.UIMessage();
            msg.id = UIManager.UIMessageType.PrintDroneInfo;
            object[] param = new object[1];
            param[0] = droneInfo;
            msg.parameters = param;
            manager.Push(msg);
        }
        /// <summary>
        /// UI에 운행 계획 리스트 및 Marker를 출력
        /// </summary>
        /// <param name="drone"></param>
        public void PrintPlanList(Drone drone)
        {
            foreach (var item in drone.plan)
            {
                UIManager.UIMessage msg = new UIManager.UIMessage();
                object[] param = new object[1];
                PositionDouble pos = PositionIntToDouble(item);
                // UI로 운행 계획 리스트 전달
                msg.id = UIManager.UIMessageType.PrintPlanList;
                param[0] = pos;
                msg.parameters = param;
                manager.Push(msg);
                // UI로 운행 계획 Marker 출력 요청
                msg.id = UIManager.UIMessageType.DrawPlanMarker;
                msg.parameters = param;
                manager.Push(msg);
            }
            map.setListCnt(0); // 운행 계획 리스트 출력 후 카운트를 초기화 해 주어야 함
        }
        /// <summary>
        /// GPS 좌표값을 int에서 double 형태로 변환함
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private PositionDouble PositionIntToDouble(PositionInt pos)
        {
            // MAVLink Message Specification에 따른 변환 작업
            double longitude = pos.longitude / 1E7;
            double latitude = pos.latitude / 1E7;
            double altitude = pos.altitude / 1000;
            return new PositionDouble(longitude, latitude, altitude);
        }
        /// <summary>
        /// GPS 좌표값을 double에서 int 형태로 변환함
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private PositionInt PositionDoubleToInt(PositionDouble pos)
        {
            // MAVLink Message Specification에 따른 변환 작업
            int longitude = (int)(pos.longitude * 1E7);
            int latitude = (int)(pos.latitude * 1E7);
            int altitude = (int)(pos.altitude * 1000);
            return new PositionInt(longitude, latitude, altitude);
        }
        /// <summary>
        /// 연결 정보 리스트 출력을 요청
        /// </summary>
        /// <returns></returns>
        public bool GetConnectList()
        {
            return model.LoadConnectFile();
        }
        /// <summary>
        /// 연결 정보 리스트를 저장
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool AddConnectList(ConnectList.ConnectStruct data)
        {
            GcsModel.ConnectStruct temp = new GcsModel.ConnectStruct();
            temp.ip = data.ip;
            temp.bindPort = data.bindPort;
            temp.name = data.name;
            temp.systemId = data.systemId;
            temp.componentId = data.componentId;
            temp.gcsPort = data.gcsPort;
            return model.WriteConnectFile(temp);
    }
        /// <summary>
        /// UI에 연결 정보 리스트를 출력
        /// </summary>
        /// <param name="data"></param>
        public void PrintConnectList(List<GcsModel.ConnectStruct> data)
        {
            List<ConnectList.ConnectStruct> newCs = new List<ConnectList.ConnectStruct>();
            foreach (var item in data)
            {
                ConnectList.ConnectStruct temp = new ConnectList.ConnectStruct();
                temp.ip = item.ip;
                temp.bindPort = item.bindPort;
                temp.name = item.name;
                temp.systemId = item.systemId;
                temp.componentId = item.componentId;
                temp.gcsPort = item.gcsPort;
                newCs.Add(temp);
            }
            UIManager.UIMessage msg = new UIManager.UIMessage();
            msg.id = UIManager.UIMessageType.PrintConnectList;
            object[] param = new object[1];
            param[0] = newCs;
            msg.parameters = param;
            manager.Push(msg);
        }
        /// <summary>
        /// key 값으로 드론 ID 요청
        /// </summary>
        /// <param name="key"></param>
        public void GetDroneID(int key)
        {
            byte[] id = new byte[2];
            id[0] = indexTable[key].systemId;
            id[1] = indexTable[key].componentId;

            UIManager.UIMessage msg = new UIManager.UIMessage();
            msg.id = UIManager.UIMessageType.SendID;
            object[] param = new object[2];
            param[0] = id[0];
            param[1] = id[1];
            msg.parameters = param;
            manager.Push(msg);
        }
        /// <summary>
        /// trace Marker 정보 출력 요청
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Drone GetTraceInfo(int key)
        {
            //PositionDouble newPos = PositionIntToDouble(model.GetDrone(indexTable[key].systemId, indexTable[key].componentId).position);
            return model.GetDrone(indexTable[key].systemId, indexTable[key].componentId);
        }

        public void CmdTakeOff(int key)
        {/*
             byte systemId = indexTable[key].systemId;
             byte componentId = indexTable[key].componentId;
             object[] param = new object[13];
            param[0] = MavCmd.ComponentArmDisarm;
            param[1] = MavResult.Accepted;
            model.GetDroneConnecter(systemId, componentId).SendMessage(77, param);
            param[0] = systemId;
             param[1] = componentId;
             param[2] = MavCmd.NavTakeoff;
             param[3] = (byte)0;
             param[4] = (byte)1;      // minimum pitch
             param[5] = null;
             param[6] = (byte)30;
             param[7] = (byte)1;         // yaw angle
             param[8] = (byte)model.GetDrone(systemId, componentId).position.latitude;
             param[9] = (byte)model.GetDrone(systemId, componentId).position.longitude;
             param[10] =(byte)(model.GetDrone(systemId, componentId).position.altitude+50*1000);

            model.GetDroneConnecter(systemId, componentId).SendMessage(76, param);
            /*
             byte systemId = indexTable[key].systemId;
             byte componentId = indexTable[key].componentId;
             object[] param = new object[13];
             param[0] = systemId;
             param[1] = componentId;
             param[2] = MavFrame.Global;
             param[3] = MavCmd.NavTakeoff;
             param[4] = (byte)1;     //current
             param[5] = (byte)0;     // autocontinue
             param[6] = (byte)1;      // minimum pitch
             param[7] = null;
             param[8] = (byte)30;
             param[9] = (byte)1;         // yaw angle
             param[10] = model.GetDrone(systemId, componentId).position.latitude;
             param[11] = model.GetDrone(systemId, componentId).position.longitude;
             param[12] =(float)model.GetDrone(systemId, componentId).position.altitude+50*1000f;
             model.GetDroneConnecter(systemId, componentId).SendMessage(75, param);
             */
           

            byte systemId = indexTable[key].systemId;
            byte componentId = indexTable[key].componentId;
            object[] param = new object[14];
            /*
            param[0] = (ulong)0;
            param[1] = 0f;
            param[2] = 0f;
            param[3] = 0f;
            param[4] = 0f;
            param[5] = (float)0;
            param[6] = 0f;
            param[7] = 0f;
            param[8] = 0f;
            param[9] = MavMode.StabilizeArmed;
            param[10] = (byte)0;
            model.GetDroneConnecter(systemId, componentId).SendMessage(91, param);
            
            
            param[0] = systemId;
            param[1] = componentId;
            param[2] = (ushort)0;
            param[3] = MavFrame.Global;
            param[4] = MavCmd.NavTakeoff;
            param[5] = (byte)1;     //current
            param[6] = (byte)1;     // autocontinue
            param[7] = (float)1;      // minimum pitch
            param[8] = null;
            param[9] = (byte)30;
            param[10] = (float)1;         // yaw angle
            param[11] = model.GetDrone(systemId, componentId).position.latitude;
            param[12] = model.GetDrone(systemId, componentId).position.longitude;
            param[13] = (float)model.GetDrone(systemId, componentId).position.altitude + 50 * 1000f;
            model.GetDroneConnecter(systemId, componentId).SendMessage(39, param);
            

            param[0] = systemId;
            param[1] = componentId;
            param[2] = MavCmd.MissionStart;
            param[3] = (byte)0;     //confirmation
            param[4] = 0f;
            param[5] = 0f;
            param[6] = 0f;
            param[7] = 0f;
            param[8] = 0f;
            param[9] = 0f;
            param[10] = 0f;
            model.GetDroneConnecter(systemId, componentId).SendMessage(76, param);
            */

            param[0] = systemId;
            param[1] = componentId;
            model.GetDroneConnecter(systemId, componentId).SendMessage(21, param);
            
            param[0] = systemId;
            param[1] = componentId;
            param[2] = "FS_THR_ENABLE";
            param[3] = (float)0;
            param[4] = MavParamType.Int32;
            model.GetDroneConnecter(systemId, componentId).SendMessage(23, param);
            /*
            param[2] = "ARMING_CHECK";
            param[3] = (float)0;
            param[4] = MavParamType.Int32;
            model.GetDroneConnecter(systemId, componentId).SendMessage(23, param);

            param[0] = systemId;
            param[1] = componentId;
            param[2] = (ushort)0;
            param[3] = MavFrame.Global;
            param[4] = MavCmd.NavTakeoff;
            param[5] = (byte)1;     //current
            param[6] = (byte)1;     // autocontinue
            param[7] = (float)1;      // minimum pitch
            param[8] = null;
            param[9] = (byte)30;
            param[10] = (float)1;         // yaw angle
            param[11] = model.GetDrone(systemId, componentId).position.latitude;
            param[12] = model.GetDrone(systemId, componentId).position.longitude;
            param[13] = (float)model.GetDrone(systemId, componentId).position.altitude + 50 * 1000f;
            model.GetDroneConnecter(systemId, componentId).SendMessage(39, param);


            param[0] = systemId;
            param[1] = componentId;
            param[2] = MavCmd.MissionStart;
            param[3] = (byte)0;     //confirmation
            param[4] = 0f;
            param[5] = 0f;
            param[6] = 0f;
            param[7] = 0f;
            param[8] = 0f;
            param[9] = 0f;
            param[10] = 0f;
            model.GetDroneConnecter(systemId, componentId).SendMessage(76, param);
            */
        }

    }
}
