using UnityEngine;

public class OrbitalWeapon3D : MonoBehaviour
{
    public Transform player;

    [Header("Orbit")]
    public float radius = 1.5f;
    public float minRadius = 0.6f;
    public float maxRadius = 3.5f;

    public float linearSpeed = 6f;
    public float radiusChangeSpeed = 2f;

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
        if (Input.GetKey(KeyCode.Q))
            radius -= radiusChangeSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.E))
            radius += radiusChangeSpeed * Time.deltaTime;

        radius = Mathf.Clamp(radius, minRadius, maxRadius);
    }

    void HandleDirectionInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
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
