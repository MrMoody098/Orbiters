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

    float angle;
    int direction = 1;

    void Update()
    {
        HandleRadiusInput();
        HandleDirectionInput();
        Orbit();
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
        if (Input.GetKeyDown(flipDirection))
            direction *= -1;
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
}
