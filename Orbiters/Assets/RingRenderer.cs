using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RingRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private OrbitalWeapon3D orbitalWeapon;

    [Header("Ring Settings")]
    [SerializeField] private int segments = 64;
    [SerializeField] private float baseLineWidth = 0.05f;
    [SerializeField] private float pulseAmount = 0.01f;
    [SerializeField] private float pulseSpeed = 5f;
    [SerializeField] private Color minRadiusColor = Color.red;
    [SerializeField] private Color maxRadiusColor = Color.green;

    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = segments;
        line.loop = true;
        line.useWorldSpace = false;
        line.startWidth = baseLineWidth;
        line.endWidth = baseLineWidth;

        // ✅ Ensure proper material for color updates
        if (line.material == null || line.material.shader.name != "Unlit/Color")
        {
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = Color.white; // default, script will control the color
            line.material = mat;
        }
    }

    void LateUpdate()
    {
        if (orbitalWeapon == null) return;

        DrawRing(orbitalWeapon.radius);
        UpdatePulse();
        UpdateColor();
    }

    private void DrawRing(float radius)
    {
        float angleStep = 2f * Mathf.PI / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;

            Vector3 point = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );

            line.SetPosition(i, point);
        }
    }

    private void UpdatePulse()
    {
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        line.startWidth = baseLineWidth + pulse;
        line.endWidth = baseLineWidth + pulse;
    }

    private void UpdateColor()
    {
        float t = Mathf.InverseLerp(
            orbitalWeapon.minRadius,
            orbitalWeapon.maxRadius,
            orbitalWeapon.radius
        );

        Color ringColor = Color.Lerp(minRadiusColor, maxRadiusColor, t);

        // Update both vertex colors and material color to ensure Game view shows it
        line.startColor = ringColor;
        line.endColor = ringColor;
        line.material.color = ringColor;
    }
}
