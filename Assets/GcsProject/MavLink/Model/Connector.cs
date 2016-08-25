/*
 * MAVLink.NET을 활용하여 데이터를 교환하는 역할을 함. - Kero Kim -
 */
using MavLinkNet;
using System;
using System.Net;

namespace GcsProject.Model
{
    public delegate void MavConnected(object drone, object Connector);

    /// <summary>
    /// MAVLink를 이용하여 드론과 데이터 교환을 함
    /// </summary>
    class Connector : IDisposable
    {
        public event MavConnected connectEvent;
        /// <summary>
        /// 현재 연결 상태
        /// </summary>
        public enum ConnectStatus
        {
            Disconnected, // 연결이 끊어짐
            Connected // 연결된 상태
        }
        private MavLinkUdpTransport udp = new MavLinkUdpTransport();
        private Drone drone = null;
        private ConnectStatus connectStatus = ConnectStatus.Disconnected; // 연결 상태 저장
        /// <summary>
        /// 드론 접속을 위한 기본 설정
        /// </summary>
        /// <param name="drone"></param>
        /// <param name="gcsPort">개방할 포트</param>
        public Connector(Drone drone, int gcsPort = 14560)
        {
            this.drone = drone;
            udp.OnPacketReceived += ReceiveProcess; // 패킷 수신 시 발생할 이벤트 등록
            udp.UdpListeningPort = gcsPort; // 프로그램 개방 포트
            udp.UdpTargetPort = drone.bindPort; // 드론 접속 포트
            udp.TargetIpAddress = IPAddress.Parse(drone.ip);
            udp.MavlinkSystemId = drone.id;
            udp.MavlinkComponentId = drone.componentId;
        }
        /// <summary>
        /// 포트 개방 및 Heartbeat 신호 전달 시도
        /// </summary>
        public void Initialize()
        {
            udp.Initialize();
            udp.BeginHeartBeatLoop();
        }
        /// <summary>
        /// 받은 패킷을 처리하는 이벤트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="packet"></param>
        private void ReceiveProcess(object sender, MavLinkPacket packet)
        {
            // 받은 패킷을 처리하는 코드를 이곳에 추가
            // packet.Message에 전달받은 메시지 값이 들어있으므로,
            // 메시지 ID에 맞게 적절하게 타입 캐스팅을 한 후 데이터를 처리하면 됨
            switch (packet.MessageId)
            {
                case 0: // HEARTBEAT
                    {
                        UasHeartbeat msg = (UasHeartbeat)packet.Message;
                        // 드론 데이터 정상 수신 시 연결 상태를 'Connected'로 변경
                        if (connectStatus == ConnectStatus.Disconnected)
                        {
                            drone.type = msg.Type;
                            drone.autopilot = msg.Autopilot;
                            connectStatus = ConnectStatus.Connected;
                            connectEvent(drone, this); // 연결 성공 이벤트 호출
                        }
                        drone.mode = msg.BaseMode;
                        drone.status = msg.SystemStatus;
                        break;
                    }
                case 107: // HIL_SENSOR
                    {
                        UasHilSensor msg = (UasHilSensor)packet.Message;

                        Console.Write(" Xacc : " + msg.Xacc);
                        Console.Write(" Yacc : " + msg.Yacc);
                        Console.Write(" Zacc : " + msg.Zacc);

                        Console.Write(" Xgyro : " + msg.Xgyro);
                        Console.Write(" Ygyro : " + msg.Ygyro);
                        Console.Write(" Zgyro : " + msg.Zgyro);

                        Console.Write(" Xmag : " + msg.Xmag);
                        Console.Write(" Ymag : " + msg.Ymag);
                        Console.Write(" Zmag : " + msg.Zmag);

                        Console.Write("\n");

                        if (drone != null)
                        {
                            drone.sensor.acc.x = msg.Xacc;
                            drone.sensor.acc.y = msg.Yacc;
                            drone.sensor.acc.z = msg.Zacc;
                            drone.sensor.gyro.x = msg.Xgyro;
                            drone.sensor.gyro.y = msg.Ygyro;
                            drone.sensor.gyro.z = msg.Zgyro;
                            drone.sensor.mag.x = msg.Xmag;
                            drone.sensor.mag.y = msg.Ymag;
                            drone.sensor.mag.z = msg.Zmag;
                            drone.sensor.pressure = msg.AbsPressure;
                            drone.sensor.temperature = msg.Temperature;
                        }
                        break;
                    }
                case 113: // HIL_GPS
                    {
                        UasHilGps msg = (UasHilGps)packet.Message;

                        Console.Write(" Lat : " + msg.Lat);
                        Console.Write(" Lon : " + msg.Lon);
                        Console.Write(" Alt : " + msg.Alt);
                        Console.Write(" Ground Speed : " + msg.Vel);

                        Console.Write("\n");

                        if (drone != null)
                        {
                            drone.position.latitude = msg.Lat;
                            drone.position.longitude = msg.Lon;
                            drone.position.altitude = msg.Alt;
                            drone.groundSpeed = msg.Vel;
                        }
                        break;
                    }
            }
        }
        /// <summary>
        /// 메시지를 전달함
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="values"></param>
        public void SendMessage(int messageId, params object[] values)
        {
            // 보낼 메시지를 처리하는 코드를 이곳에 추가
            // 메시지 ID에 맞는 메시지 객체를 생성하고,
            // 전달할 값과 System ID, Component ID 값을 입력한 뒤 udp.SendMessage로 전달
            switch (messageId)
            {
                case 20: // PARAM_REQUEST_READ (target_system, target_component, param_id, param_index)
                    {
                        UasParamRequestRead msg = new UasParamRequestRead();
                        msg.TargetSystem = (byte)values[0];
                        msg.TargetComponent = (byte)values[1];
                        string temp = (string)values[2];
                        msg.ParamId = temp.Replace("\0",string.Empty).ToCharArray(0, ((temp.Length > 16) ? 16 : temp.Length));
                        msg.ParamIndex = (short)values[3];
                        udp.SendMessage(msg);
                        break;
                    }
                case 21: // PARAM_REQUEST_LIST
                    {
                        UasParamRequestList msg = new UasParamRequestList();
                        msg.TargetSystem = (byte)values[0];
                        msg.TargetComponent = (byte)values[1];
                        udp.SendMessage(msg);
                        break;
                    }

                case 23: // PARAM_SET (target_system, target_component, param_id, param_value, param_type)
                    {
                        UasParamSet msg = new UasParamSet();
                        msg.TargetSystem = (byte)values[0];
                        msg.TargetComponent = (byte)values[1];
                        string temp = (string)values[2];
                        msg.ParamId = temp.Replace("\0", string.Empty).ToCharArray(0, ((temp.Length > 16) ? 16 : temp.Length));
                        msg.ParamValue = (float)values[3];
                        msg.ParamType = (MavParamType)values[4];
                        udp.SendMessage(msg);
                        break;
                       
                    }
                case 39: // MISSOIN_ITEM
                    {
                        UasMissionItem msg = new UasMissionItem();
                        msg.TargetSystem = (byte)values[0];
                        msg.TargetComponent = (byte)values[1];
                        msg.Seq = (ushort)values[2];
                        msg.Frame = (MavFrame)values[3];
                        msg.Command = (MavCmd)values[4];
                        msg.Current = (byte)values[5];
                        msg.Autocontinue = (byte)values[6];
                        msg.Param1 = (float)values[7];
                        msg.Param4 = (float)values[10];
                        msg.X = (Int32)values[11];
                        msg.Y = (Int32)values[12];
                        msg.Z = (float)values[13];
                        udp.SendMessage(msg);
                        break;
                    }
                case 73: // MISSION_INT
                    {
                        UasMissionItemInt msg = new UasMissionItemInt();
                        msg.TargetSystem = (byte)values[0];
                        msg.TargetComponent = (byte)values[1];
                        msg.Seq = (ushort)values[2];
                        msg.Frame = (MavFrame)values[3];
                        msg.Command = (MavCmd)values[4];
                        msg.Current = (byte)values[5];
                        msg.Autocontinue = (byte)values[6];
                        msg.Param1 = (float)values[7];
                        msg.Param4 = (float)values[10];
                        msg.X = (Int32)values[11];
                        msg.Y = (Int32)values[12];
                        msg.Z = (float)values[13];
                        udp.SendMessage(msg);
                        break;
                    }
                case 75:    //COMMAND_INT 
                    {
                        UasCommandInt msg = new UasCommandInt();
                        msg.TargetSystem = (byte)values[0];
                        msg.TargetComponent = (byte)values[1];
                        msg.Frame = (MavFrame)values[2];
                        msg.Command = (MavCmd)values[3];
                        msg.Current = (byte)values[4];
                        msg.Autocontinue = (byte)values[5];
                        /*msg.Param1 = (byte)values[6];
                        msg.Param4 = (byte)values[9];
                        msg.X = (Int32)values[10];
                        msg.Y = (Int32)values[11];*/
                        msg.Z = (float)values[12];
                        udp.SendMessage(msg);
                        break;
                    }
                case 76: // COMMAND_LONG (target_System,target_componenet, command, confirmation, param 1 ~ 7)
                    {
                        UasCommandLong msg = new UasCommandLong();
                        msg.TargetSystem = (byte)values[0];
                        msg.TargetComponent = (byte)values[1];
                        msg.Command = (MavCmd)values[2];
                        msg.Confirmation = (byte)values[3];
                        msg.Param1 = (float)values[4];
                        msg.Param2 = (float)values[5];
                        msg.Param4 = (float)values[7];
                        msg.Param5 = (float)values[8];
                        msg.Param6 = (float)values[9];
                        msg.Param7 = (float)values[10];
                        udp.SendMessage(msg);
                        break;
                    }
                case 77:  //COMMAND_ACK
                    {
                        UasCommandAck msg = new UasCommandAck();
                        msg.Command =(MavCmd) values[0];
                        msg.Result = (MavResult)values[1];
                        udp.SendMessage(msg);
                        break;
                    }
                case 91:    //HIL_CONTROLS
                    {
                        UasHilControls msg = new UasHilControls();
                        msg.TimeUsec = (ulong)values[0];
                        msg.RollAilerons = (float)values[1];
                        msg.PitchElevator = (float)values[2];
                        msg.YawRudder = (float)values[3];
                        msg.Throttle = (float)values[4];
                        msg.Aux1 = (float)values[5];
                        msg.Aux2 = (float)values[6];
                        msg.Aux3 = (float)values[7];
                        msg.Aux4 = (float)values[8];
                        msg.Mode = (MavMode)values[9];
                        msg.NavMode = (byte)values[10];
                        udp.SendMessage(msg);
                        break;
                    }
                default:
                    break;
            }
        }
        /// <summary>
        /// 객체 소멸 시 호출
        /// </summary>
        public void Dispose()
        {
            connectStatus = ConnectStatus.Disconnected;
            udp.Dispose();
        }
        /// <summary>
        /// 현재 연결 상태 반환
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            return (connectStatus == ConnectStatus.Disconnected ? false : true);
        }
    }
}
