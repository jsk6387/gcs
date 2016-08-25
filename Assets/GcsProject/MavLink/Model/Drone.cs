/*
 * MAVLink를 통해 받아들인 데이터를 효율적으로 관리하기 위한 클래스.
 * 대부분 MAVLink의 Message Specification을 따르는 형태로 작성되어서,
 * UI에 출력할 때 일부 값은 변환 작업이 필요할 수 있음. - Kero Kim -
 */
using MavLinkNet;
using System;
using System.Collections.Generic;

namespace GcsProject.Model
{
    /// <summary>
    /// 드론 데이터
    /// </summary>
    class Drone : ICloneable
    {
        public string name; // 사용자 정의 이름
        public byte id; // 장비 id 값
        public byte componentId; // 컴포넌트 id 값
        public struct SensorValue // 센서값
        {
            public struct Gyro { public float x; public float y; public float z; } // 자이로 센서
            public struct Acc { public float x; public float y; public float z; } // 가속도 센서
            public struct Mag { public float x; public float y; public float z; } // 지자기 센서

            public Gyro gyro;
            public Acc acc;
            public Mag mag;
            public float pressure; // 압력 센서
            public float temperature; // 온도 센서
        }
        public SensorValue sensor;
        public PositionInt position; // 위치
        public List<int> motorSpeed; // 모터 회전 속도
        public List<PositionInt> plan; // 운행 계획
        public int battery; // 배터리 잔량
        public Dictionary<DateTime, Drone> trace; // 로그(타임스탬프, 드론)
        public ushort groundSpeed; // 이동 속도
        public string ip; // IP 주소
        public int bindPort; // 포트
        public MavType type; // 장비 종류 (quadrotor, helicopter 등)
        public MavAutopilot autopilot; // 장비 모델 (px4 등)
        public MavModeFlag mode; // 운행 모드
        public MavState status; // 장비 상태
        
        public Drone(byte id, byte componentId, string name, string ip = "127.0.0.1", int bindPort = 4000)
        {
            this.id = id;
            this.componentId = componentId;
            this.name = name;
            position = new PositionInt();
            motorSpeed = new List<int>();
            plan = new List<PositionInt>();
            battery = 0;
            trace = new Dictionary<DateTime, Drone>();
            groundSpeed = 0;
            this.ip = ip;
            this.bindPort = bindPort;
        }
        public object Clone()
        {
            // Drone 객체가 복제될 때, Deep Copy가 이루어질 수 있게 작성한 코드
            Drone drone = new Drone(id, componentId, name, ip, bindPort);
            drone.sensor.gyro.x = sensor.gyro.x;
            drone.sensor.gyro.y = sensor.gyro.y;
            drone.sensor.gyro.z = sensor.gyro.z;
            drone.sensor.acc.x = sensor.acc.x;
            drone.sensor.acc.y = sensor.acc.y;
            drone.sensor.acc.z = sensor.acc.z;
            drone.sensor.mag.x = sensor.mag.x;
            drone.sensor.mag.y = sensor.mag.y;
            drone.sensor.mag.z = sensor.mag.z;
            drone.sensor.pressure = sensor.pressure;
            drone.sensor.temperature = sensor.temperature;
            drone.position.altitude = position.altitude;
            drone.position.latitude = position.latitude;
            drone.position.longitude = position.longitude;
            foreach (int item in motorSpeed)
            {
                drone.motorSpeed.Add(item);
            }
            foreach (PositionInt item in plan)
            {
                drone.plan.Add(item);
            }
            drone.battery = battery;
            foreach (KeyValuePair<DateTime, Drone> item in trace)
            {
                drone.trace.Add(item.Key, item.Value);
            }
            drone.groundSpeed = groundSpeed;
            drone.ip = ip;
            drone.bindPort = bindPort;
            drone.type = type;
            drone.autopilot = autopilot;
            drone.mode = mode;
            drone.status = status;

            return drone;
        }
        /// <summary>
        /// 현재 드론 상태(자취)를 기록하고, 기록한 값을 반환
        /// </summary>
        /// <returns></returns>
        public Drone SetTrace()
        {
            Drone newDrone = (Drone)Clone();
            trace.Add(DateTime.Now, newDrone);
            return newDrone;
        }
    }
}
