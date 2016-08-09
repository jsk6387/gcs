public class PositionDouble
{
    public double longitude;
    public double latitude;
    public double altitude;

    public PositionDouble(double longitude, double latitude, double altitude)
    {
        this.latitude = latitude;
        this.longitude = longitude;
        this.altitude = altitude;
    }
    public PositionDouble()
    {
        latitude = 0;
        longitude = 0;
        altitude = 0;
    }
}