using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerMovement3D movement;
    public OrbitalWeapon3D orbitalWeapon;
    public int health;

    [Header("Orbital Weapon Prefab")]
    public GameObject orbitalWeaponPrefab; // assign in inspector or via GameManager

    public void Initialize(
        string horizontal, string vertical,
        KeyCode decRadius, KeyCode incRadius, KeyCode flipDir,
        int maxHealth
    )
    {
        Initialize(horizontal, vertical, decRadius, incRadius, flipDir, maxHealth, false, 1);
    }

    public void Initialize(
        string horizontal, string vertical,
        KeyCode decRadius, KeyCode incRadius, KeyCode flipDir,
        int maxHealth, bool useJoystick, int joystickNumber
    )
    {
        // Movement
        movement = GetComponent<PlayerMovement3D>();
        if (movement == null)
        {
            Debug.LogError($"PlayerController: PlayerMovement3D component not found on {gameObject.name}!");
            return;
        }
        movement.horizontalAxis = horizontal;
        movement.verticalAxis = vertical;
        movement.useJoystickDirect = useJoystick;
        movement.joystickNumber = joystickNumber;

        // Spawn orbital weapon prefab as child
        if (orbitalWeaponPrefab == null)
        {
            Debug.LogError($"PlayerController: orbitalWeaponPrefab is not assigned on {gameObject.name}!");
            return;
        }

        GameObject orb = Instantiate(orbitalWeaponPrefab, transform.position, Quaternion.identity, transform);
        if (orb == null)
        {
            Debug.LogError($"PlayerController: Failed to instantiate orbital weapon prefab on {gameObject.name}!");
            return;
        }

        orbitalWeapon = orb.GetComponent<OrbitalWeapon3D>();
        if (orbitalWeapon == null)
        {
            // Try to add the component if it's missing
            Debug.LogWarning($"PlayerController: OrbitalWeapon3D component not found on instantiated prefab for {gameObject.name}. Attempting to add it...");
            orbitalWeapon = orb.AddComponent<OrbitalWeapon3D>();
            if (orbitalWeapon == null)
            {
                Debug.LogError($"PlayerController: Failed to add OrbitalWeapon3D component to {gameObject.name}!");
                return;
            }
        }

        orbitalWeapon.player = this.transform;
        orbitalWeapon.decreaseRadius = decRadius;
        orbitalWeapon.increaseRadius = incRadius;
        orbitalWeapon.flipDirection = flipDir;

        health = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage! Health: {health}");
        if (health <= 0)
        {
            Debug.Log($"{gameObject.name} is dead!");
            Destroy(gameObject);
        }
    }
}
