
namespace UnitySlippyMap.Markers
{ 
     class Position {
        public double longitude;
        public double latitude;
        public double altitude;

        public Position(double longitude, double latitude, double altitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
            this.altitude = altitude;
        }
        public Position()
        {
            latitude = 0;
            longitude = 0;
            altitude = 0;
        }
    }
}
