/*
 * GPS 좌표 값을 저장하는 역할을 함. PositionInt는 MAVLink가 GPS 좌표 값을
 * int 형태로 반환하는 형태를 그대로 따르고 있음.
 * UI에 출력(지도상에 표현)하기 위해서는 PositionDouble 형태로 변환하는 과정이 필요함. - Kero Kim -
 */
namespace GcsProject.Model
{
    /// <summary>
    /// GPS 좌표(위도, 경도, 고도) 값을 MAVLink 기준(int)으로 저장
    /// </summary>
    class PositionInt
    {
        public int longitude;
        public int latitude;
        public int altitude;

        public PositionInt(int longitude, int latitude, int altitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
            this.altitude = altitude;
        }
        public PositionInt()
        {
            latitude = 0;
            longitude = 0;
            altitude = 0;
        }
    }
}
