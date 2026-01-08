using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject orbitalWeaponPrefab; // assign the new prefab here

    [Header("Spawn Points")]
    public Transform player1Spawn;
    public Transform player2Spawn;

    [Header("Player Settings")]
    [Tooltip("Starting health for Player 1")]
    public int player1MaxHealth = 100;
    [Tooltip("Starting health for Player 2")]
    public int player2MaxHealth = 100;
    [Tooltip("Starting lives for all players")]
    public int startingLives = 3;
    
    [Header("Combat Settings")]
    public float baseDamage = 10f;
    public float baseForce = 6f; // Reduced for more controlled knockback
    public float hitCooldown = 0.5f; // Time between hits from same weapon
    public float velocityForceMultiplier = 0.4f; // How much weapon velocity contributes to knockback
    
    public float orbitalWeaponCollisionCooldown = 0.3f; // Cooldown between orbital weapon collisions

    [Header("Physics Settings")]
    public float playerMass = 1f;
    public float playerLinearDamping = 6f;
    public float playerColliderRadius = 0.5f;

    void Start()
    {
        // Validate prefabs
        if (playerPrefab == null)
        {
            Debug.LogError("GameManager: playerPrefab is not assigned!");
            return;
        }
        if (orbitalWeaponPrefab == null)
        {
            Debug.LogError("GameManager: orbitalWeaponPrefab is not assigned!");
            return;
        }

        // Player 1
        GameObject p1 = Instantiate(playerPrefab, player1Spawn.position, Quaternion.identity);
        p1.name = "Player1";
        SetupPlayer(p1, "Horizontal", "Vertical", KeyCode.Q, KeyCode.E, KeyCode.Space, false, 1, player1MaxHealth, player1Spawn);

        // Player 2 - Controller/Joystick (uses custom axes)
        GameObject p2 = Instantiate(playerPrefab, player2Spawn.position, Quaternion.identity);
        p2.name = "Player2";
        SetupPlayer(p2, "JoystickHorizontal", "JoystickVertical", KeyCode.Joystick1Button4, KeyCode.Joystick1Button5, KeyCode.Joystick1Button0, true, 1, player2MaxHealth, player2Spawn);
    }

    void SetupPlayer(GameObject playerObj, string horizontal, string vertical, 
        KeyCode decRadius, KeyCode incRadius, KeyCode flipDir, bool useJoystick, int joystickNumber, int maxHealth, Transform spawnPoint)
    {
        // Ensure PlayerMovement3D component exists
        PlayerMovement3D movement = playerObj.GetComponent<PlayerMovement3D>();
        if (movement == null)
        {
            movement = playerObj.AddComponent<PlayerMovement3D>();
        }

        // Ensure Rigidbody exists (required by PlayerMovement3D)
        Rigidbody rb = playerObj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = playerObj.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            rb.useGravity = false; // 2D-like physics
        }
        // Optimize Rigidbody for ice-like knockback (starts fast, slows down gradually)
        rb.mass = playerMass;
        rb.linearDamping = playerLinearDamping; // Higher drag creates ice-like sliding effect - starts fast, slows down smoothly
        
        // Ensure player has a collider for wall bouncing
        Collider col = playerObj.GetComponent<Collider>();
        if (col == null)
        {
            // Add a sphere collider if none exists
            SphereCollider sphereCol = playerObj.AddComponent<SphereCollider>();
            sphereCol.radius = playerColliderRadius;
        }
        else if (!col.enabled)
        {
            col.enabled = true; // Enable collider if it exists but is disabled
        }

        // Add PlayerController
        PlayerController pc = playerObj.GetComponent<PlayerController>();
        if (pc == null)
        {
            pc = playerObj.AddComponent<PlayerController>();
        }
        pc.orbitalWeaponPrefab = orbitalWeaponPrefab;
        pc.Initialize(horizontal, vertical, decRadius, incRadius, flipDir, maxHealth, useJoystick, joystickNumber, startingLives, spawnPoint);
    }

    // Made public so OrbitalWeapon3D can call it
    // Collision detection is now handled by OrbitalWeapon3D using Unity's physics system
    public void ApplyHit(OrbitalWeapon3D weapon, PlayerController target)
    {
        // Cooldown is now handled by OrbitalWeapon3D before calling this method
        
        // Calculate force multiplier based on orbital speed/radius
        // Smaller radius = higher angular speed = more force
        float forceMultiplier = weapon.GetForceMultiplier();
        
        // Calculate damage (scaled by speed/radius)
        float damage = baseDamage * forceMultiplier;
        
        // Apply damage (pass force multiplier for debug info)
        target.TakeDamage(Mathf.RoundToInt(damage), forceMultiplier);
        
        // Apply force to the player's Rigidbody
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            // Calculate direction from orbital weapon to player
            Vector3 direction = (target.transform.position - weapon.transform.position).normalized;
            
            // Get the velocity of the orbital weapon for more realistic force
            Vector3 weaponVelocity = weapon.GetVelocity();
            
            // Base knockback force in the direction away from the weapon
            Vector3 force = direction * baseForce * forceMultiplier;
            
            // Add weapon's velocity to the force for more impactful knockback
            // This makes hits feel more dynamic and powerful
            force += weaponVelocity * velocityForceMultiplier * forceMultiplier;
            
            // Apply the force as an impulse (instant force)
            targetRb.AddForce(force, ForceMode.Impulse);
        }
    }
}
