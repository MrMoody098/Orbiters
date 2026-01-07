using UnityEngine;

public class OrbitalWeapon3D : MonoBehaviour
{
    public Transform player;

    [Header("Orbit Settings")]
    public float radius = 1.5f;
    public float minRadius = 0.6f;
    public float maxRadius = 3.5f;

    public float linearSpeed = 6f;
    public float radiusChangeSpeed = 2f;

    [Header("Input Keys / Buttons")]
    public KeyCode increaseRadius = KeyCode.E;
    public KeyCode decreaseRadius = KeyCode.Q;
    public KeyCode flipDirection = KeyCode.Space;

    [Header("Direction Change Settings")]
    public float directionChangeCooldown = 0.5f; // Time between direction flips

    float angle;
    int direction = 1;
    private Vector3 lastPosition;
    private Vector3 currentVelocity;
    private float lastDirectionChangeTime = 0f; // Track when direction was last changed

    void Start()
    {
        if (player != null)
        {
            lastPosition = transform.position;
        }
    }

    void Update()
    {
        HandleRadiusInput();
        HandleDirectionInput();
        Orbit();
        
        // Calculate current velocity for force calculations
        currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
    }

    void HandleRadiusInput()
    {
        if (Input.GetKey(decreaseRadius))
            radius -= radiusChangeSpeed * Time.deltaTime;

        if (Input.GetKey(increaseRadius))
            radius += radiusChangeSpeed * Time.deltaTime;

        radius = Mathf.Clamp(radius, minRadius, maxRadius);
    }

    void HandleDirectionInput()
    {
        // Check if enough time has passed since last direction change
        if (Time.time - lastDirectionChangeTime >= directionChangeCooldown)
        {
            if (Input.GetKeyDown(flipDirection))
            {
                direction *= -1;
                lastDirectionChangeTime = Time.time; // Update the timestamp
            }
        }
    }

    void Orbit()
    {
        float angularSpeed = linearSpeed / radius;
        angle += direction * angularSpeed * Time.deltaTime;

        Vector3 offset = new Vector3(
            Mathf.Cos(angle),
            Mathf.Sin(angle),
            0f
        ) * radius;

        transform.position = player.position + offset;
    }

    // Get the current speed of the orbital weapon
    public float GetCurrentSpeed()
    {
        // Angular speed increases as radius decreases
        // Speed = angular speed * radius = (linearSpeed / radius) * radius = linearSpeed
        // But for force calculation, we want smaller radius = more impact
        // So we use angular speed directly, or speed inversely proportional to radius
        float angularSpeed = linearSpeed / radius;
        return angularSpeed * radius; // This equals linearSpeed, but we can adjust the formula
    }

    // Get the current velocity vector
    public Vector3 GetVelocity()
    {
        return currentVelocity;
    }

    // Get speed-based force multiplier (smaller radius = higher multiplier, but less extreme)
    public float GetForceMultiplier()
    {
        // Smaller radius = higher angular speed = more force
        // But we want it less extreme than before
        
        // Calculate normalized radius (0 = minRadius, 1 = maxRadius)
        float normalizedRadius = (radius - minRadius) / (maxRadius - minRadius);
        
        // Invert so smaller radius gets higher multiplier
        // Use linear instead of quadratic for less extreme difference
        float inverted = 1f - normalizedRadius; // 1.0 at minRadius, 0.0 at maxRadius
        
        // Reduced range: 1.0x to 2.5x instead of 1.0x to 4.0x
        // This makes small ring stronger but not too extreme, and large ring weaker but not too weak
        return 1f + inverted * 1.5f; // Range: 1.0 to 2.5 (linear curve)
    }
    
    // Get the current angular speed
    public float GetAngularSpeed()
    {
        return linearSpeed / radius;
    }
}
