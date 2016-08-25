using System.Collections.Generic;


namespace UnitySlippyMap.DroneStruct
{
     public class DroneInfo
    {
        public string name;
        public int componentID;
        public int systemID;
        public double longtitude;
        public double latitude;
        public double altitude;
        public struct Sensor
        {
            public struct Acc { public float x; public float y; public float z; }
            public struct Mag { public float x; public float y; public float z; }
            public struct Gyro { public float x; public float y; public float z; }
            public Acc acc;
            public Mag mag;
            public Gyro gyro;
            public int battery;
            public ushort groundspeed;
            public List<int> rpm;
        }
        public Sensor droneInfo;
    }
}