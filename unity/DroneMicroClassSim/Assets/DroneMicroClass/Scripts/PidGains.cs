using System;
using UnityEngine;

namespace DroneMicroClass
{
    [Serializable]
    public struct PidGains
    {
        [Min(0f)] public float proportional;
        [Min(0f)] public float integral;
        [Min(0f)] public float derivative;
        [Min(0f)] public float outputLimit;

        public PidGains(float proportional, float integral, float derivative, float outputLimit)
        {
            this.proportional = proportional;
            this.integral = integral;
            this.derivative = derivative;
            this.outputLimit = outputLimit;
        }
    }
}
