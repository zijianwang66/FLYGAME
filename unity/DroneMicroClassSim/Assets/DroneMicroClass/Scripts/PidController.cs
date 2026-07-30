using UnityEngine;

namespace DroneMicroClass
{
    public sealed class PidController
    {
        private float integral;
        private float previousError;
        private bool hasPreviousError;

        public PidGains Gains { get; set; }

        public PidController(PidGains gains)
        {
            Gains = gains;
        }

        public void Reset()
        {
            integral = 0f;
            previousError = 0f;
            hasPreviousError = false;
        }

        public float Update(float error, float deltaTime)
        {
            if (deltaTime <= Mathf.Epsilon)
            {
                return 0f;
            }

            integral += error * deltaTime;
            float derivative = hasPreviousError ? (error - previousError) / deltaTime : 0f;
            previousError = error;
            hasPreviousError = true;

            float output = Gains.proportional * error
                + Gains.integral * integral
                + Gains.derivative * derivative;

            if (Gains.outputLimit > 0f)
            {
                output = Mathf.Clamp(output, -Gains.outputLimit, Gains.outputLimit);
            }

            return output;
        }
    }
}
