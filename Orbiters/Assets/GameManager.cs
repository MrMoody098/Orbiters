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
    
    [Header("Combat Settings")]
    public float baseDamage = 10f;
    public float baseForce = 6f; // Reduced for more controlled knockback
    public float collisionRadius = 0.5f;
    public float hitCooldown = 0.5f; // Time between hits from same weapon
    public float velocityForceMultiplier = 0.4f; // How much weapon velocity contributes to knockback
    
    // Track last hit times to prevent spam
    private System.Collections.Generic.Dictionary<OrbitalWeapon3D, float> lastHitTimes = 
        new System.Collections.Generic.Dictionary<OrbitalWeapon3D, float>();
    
    // Track last orbital weapon collision time to prevent spam direction changes
    private float lastOrbitalWeaponCollisionTime = 0f;
    public float orbitalWeaponCollisionCooldown = 0.3f; // Cooldown between orbital weapon collisions

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
        SetupPlayer(p1, "Horizontal", "Vertical", KeyCode.Q, KeyCode.E, KeyCode.Space, false, 1, player1MaxHealth);

        // Player 2 - Controller/Joystick (uses custom axes)
        GameObject p2 = Instantiate(playerPrefab, player2Spawn.position, Quaternion.identity);
        p2.name = "Player2";
        SetupPlayer(p2, "JoystickHorizontal", "JoystickVertical", KeyCode.Joystick1Button4, KeyCode.Joystick1Button5, KeyCode.Joystick1Button0, true, 1, player2MaxHealth);
    }

    void SetupPlayer(GameObject playerObj, string horizontal, string vertical, 
        KeyCode decRadius, KeyCode incRadius, KeyCode flipDir, bool useJoystick, int joystickNumber, int maxHealth)
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
        rb.mass = 1f;
        rb.linearDamping = 6f; // Higher drag creates ice-like sliding effect - starts fast, slows down smoothly
        
        // Ensure player has a collider for wall bouncing
        Collider col = playerObj.GetComponent<Collider>();
        if (col == null)
        {
            // Add a sphere collider if none exists
            SphereCollider sphereCol = playerObj.AddComponent<SphereCollider>();
            sphereCol.radius = 0.5f;
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
        pc.Initialize(horizontal, vertical, decRadius, incRadius, flipDir, maxHealth, useJoystick, joystickNumber);
    }

    void Update()
    {
        // Damage check: simple distance collision with force application
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        if (players.Length < 2) return;

        PlayerController player1 = players[0];
        PlayerController player2 = players[1];

        // Check if orbital weapons exist before checking collisions
        if (player1.orbitalWeapon != null && player2 != null && player2.transform != null)
        {
            float distance = Vector3.Distance(player1.orbitalWeapon.transform.position, player2.transform.position);
            if (distance < collisionRadius)
            {
                ApplyHit(player1.orbitalWeapon, player2);
            }
        }
        if (player2.orbitalWeapon != null && player1 != null && player1.transform != null)
        {
            float distance = Vector3.Distance(player2.orbitalWeapon.transform.position, player1.transform.position);
            if (distance < collisionRadius)
            {
                ApplyHit(player2.orbitalWeapon, player1);
            }
        }

        // Check for orbital weapon vs orbital weapon collisions
        if (player1.orbitalWeapon != null && player2.orbitalWeapon != null)
        {
            CheckOrbitalWeaponCollision(player1.orbitalWeapon, player2.orbitalWeapon);
        }
    }

    void CheckOrbitalWeaponCollision(OrbitalWeapon3D weapon1, OrbitalWeapon3D weapon2)
    {
        // Check cooldown to prevent rapid direction changes
        if (Time.time - lastOrbitalWeaponCollisionTime < orbitalWeaponCollisionCooldown)
        {
            return; // Still on cooldown
        }
        
        // Check if orbital weapons are colliding
        float distance = Vector3.Distance(weapon1.transform.position, weapon2.transform.position);
        if (distance < collisionRadius)
        {
            // Update collision time
            lastOrbitalWeaponCollisionTime = Time.time;
            
            // Compare radii
            float radius1 = weapon1.radius;
            float radius2 = weapon2.radius;
            
            // Tolerance for "same size" comparison (to account for floating point precision)
            float radiusTolerance = 0.1f;
            
            if (Mathf.Abs(radius1 - radius2) < radiusTolerance)
            {
                // Same size - both change direction
                weapon1.FlipDirection();
                weapon2.FlipDirection();
            }
            else if (radius1 > radius2)
            {
                // Weapon1 has bigger orbit - it changes direction
                weapon1.FlipDirection();
            }
            else
            {
                // Weapon2 has bigger orbit - it changes direction
                weapon2.FlipDirection();
            }
        }
    }

    void ApplyHit(OrbitalWeapon3D weapon, PlayerController target)
    {
        // Check cooldown to prevent spam hits
        if (lastHitTimes.ContainsKey(weapon))
        {
            if (Time.time - lastHitTimes[weapon] < hitCooldown)
            {
                return; // Still on cooldown
            }
        }
        
        // Update last hit time
        lastHitTimes[weapon] = Time.time;
        
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
