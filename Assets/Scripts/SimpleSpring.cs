using UnityEngine;
using UnityEngine.Internal;

[System.Serializable]
public class SimpleSpring
{
    [HideInInspector]
    public float currentPosition;   // Current position of the spring
    [HideInInspector]
    public float currentVelocity;   // Current velocity
    [HideInInspector]
    public float targetPosition;    // Desired rest position

    public float smoothTime;
    public float maxSpeed;

    [Tooltip("How fast the spring moves toward the target (Hz)")]
    public float frequency;    // Frequency in Hertz — higher = snappier

    [Tooltip("How much the spring resists overshoot (1 = critically damped)")]
    [Range(0f, 2f)]
    public float dampingRatio; // 1 = no overshoot, <1 = bouncy, >1 = sluggish

    public SimpleSpring(float startPosition = 0f, float frequency = 2f, float dampingRatio = 0.5f)
    {
        this.currentPosition = startPosition;
        this.targetPosition = startPosition;
        this.frequency = frequency;
        this.dampingRatio = dampingRatio;
        this.currentVelocity = 0f;
        this.smoothTime = 1f;
        this.maxSpeed = 1f;
    }

    public void Simulate(float deltaTime)
    {
        // Convert intuitive params to physical constants
        float k = Mathf.Pow(2f * Mathf.PI * frequency, 2f);  // stiffness
        float c = 2f * dampingRatio * Mathf.Sqrt(k);         // damping coefficient

        // Classic semi-implicit Euler integration
        float acceleration = -k * (currentPosition - targetPosition) - c * currentVelocity;
        currentVelocity += acceleration * deltaTime;
        currentPosition += currentVelocity * deltaTime;
    }
    
    public float SmoothDamp(
        float current,
        float target,
        ref float currentVelocity,
        [DefaultValue("Time.deltaTime")] float deltaTime)
    {
        if (dampingRatio > 1f)
        {
            smoothTime = Mathf.Max(0.0001f, smoothTime);
            float num1 = 2f / smoothTime;
            float num2 = num1 * deltaTime;
            float num3 = (float)(1.0 / (1.0 + (double)num2 + 0.47999998927116394 * (double)num2 * (double)num2 +
                                        0.23499999940395355 * (double)num2 * (double)num2 * (double)num2));
            float num4 = current - target;
            float num5 = target;
            float max = maxSpeed * smoothTime;
            float num6 = Mathf.Clamp(num4, -max, max);
            target = current - num6;
            float num7 = (currentVelocity + num1 * num6) * deltaTime;
            currentVelocity = (currentVelocity - num1 * num7) * num3;
            float num8 = target + (num6 + num7) * num3;
            if ((double)num5 - (double)current > 0.0 == (double)num8 > (double)num5)
            {
                num8 = num5;
                currentVelocity = (num8 - num5) / deltaTime;
            }

            return num8;
        }
        else
        {
            float k = 2f * Mathf.PI * frequency;
            float k2 = k * k;
            float c = 2f * dampingRatio * k;
            
            float acceleration = -k2 * (current - target) - c * currentVelocity;
            currentVelocity  += acceleration * deltaTime;
            currentPosition += currentVelocity * deltaTime;
            return currentPosition;
        }
    }
}