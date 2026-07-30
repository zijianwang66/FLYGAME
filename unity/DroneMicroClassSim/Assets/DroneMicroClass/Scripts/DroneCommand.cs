namespace DroneMicroClass
{
    public struct DroneCommand
    {
        public float pitch;
        public float roll;
        public float yaw;
        public float vertical;
        public bool altitudeHoldEnabled;
        public bool stabilizeEnabled;
        public bool resetRequested;
    }
}
