using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerMovement3D movement;
    public OrbitalWeapon3D orbitalWeapon;
    [Header("Health")]
    public int health;
    public int maxHealth;
    
    [Header("Lives")]
    public int lives = 3;
    public int maxLives = 3;

    [Header("Orbital Weapon Prefab")]
    public GameObject orbitalWeaponPrefab; // assign in inspector or via GameManager
    
    [Header("Respawn Settings")]
    [Tooltip("Spawn point to respawn at")]
    public Transform spawnPoint;
    [Tooltip("Invincibility duration after respawn (seconds)")]
    public float respawnInvincibilityDuration = 2f;
    
    private bool isInvincible = false;
    private float invincibilityEndTime = 0f;
    
    [Header("Health Pulse Settings")]
    [Tooltip("Minimum pulse speed when at full health")]
    public float minPulseSpeed = 1f;
    [Tooltip("Maximum pulse speed when at low health")]
    public float maxPulseSpeed = 8f;
    [Tooltip("Health percentage threshold for maximum pulse speed (0 = 0%, 1 = 100%)")]
    [Range(0f, 1f)]
    public float maxPulseHealthThreshold = 0.3f;
    [Tooltip("Intensity of the red pulse (0 = no pulse, 1 = full red)")]
    [Range(0f, 1f)]
    public float pulseIntensity = 0.8f;
    [Tooltip("Emission intensity multiplier when pulsing (makes glow brighter)")]
    [Range(1f, 3f)]
    public float emissionPulseMultiplier = 2f;
    [Tooltip("Base color of the player (will be restored when at full health)")]
    public Color baseColor = Color.white;
    
    private Renderer playerRenderer;
    private Material playerMaterial;
    private Color originalColor;
    private Color originalEmissionColor;
    private float originalEmissionIntensity;
    private bool hasEmission = false;

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
        Initialize(horizontal, vertical, decRadius, incRadius, flipDir, maxHealth, useJoystick, joystickNumber, 3, null);
    }
    
    public void Initialize(
        string horizontal, string vertical,
        KeyCode decRadius, KeyCode incRadius, KeyCode flipDir,
        int maxHealth, bool useJoystick, int joystickNumber, int startingLives, Transform spawnTransform
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
        orbitalWeapon.ownerPlayer = this; // Set owner to avoid self-collisions
        orbitalWeapon.decreaseRadius = decRadius;
        orbitalWeapon.increaseRadius = incRadius;
        orbitalWeapon.flipDirection = flipDir;

        // Find and configure RingRenderer component (might be on a child GameObject)
        RingRenderer ringRenderer = orb.GetComponentInChildren<RingRenderer>();
        if (ringRenderer == null)
        {
            // Try to find it on the root object
            ringRenderer = orb.GetComponent<RingRenderer>();
        }

        // Initialize the RingRenderer with the necessary references
        if (ringRenderer != null)
        {
            ringRenderer.Initialize(this.transform, orbitalWeapon);
        }

        health = maxHealth;
        this.maxHealth = maxHealth;
        
        // Set lives and spawn point
        if (startingLives > 0)
        {
            lives = startingLives;
            maxLives = startingLives;
        }
        if (spawnTransform != null)
        {
            spawnPoint = spawnTransform;
        }
        
        // Setup health pulse visual effect
        SetupHealthPulse();
    }
    
    void SetupHealthPulse()
    {
        // Find the renderer (could be on this object or a child)
        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer == null)
        {
            playerRenderer = GetComponentInChildren<Renderer>();
        }
        
        if (playerRenderer != null)
        {
            // Get or create material instance (so we don't modify the shared material)
            playerMaterial = playerRenderer.material;
            originalColor = playerMaterial.color;
            
            // Use baseColor if specified, otherwise use current material color
            if (baseColor != Color.white || originalColor == Color.white)
            {
                originalColor = baseColor;
                playerMaterial.color = originalColor;
            }
            
            // Check if material has emission
            if (playerMaterial.HasProperty("_EmissionColor"))
            {
                hasEmission = true;
                originalEmissionColor = playerMaterial.GetColor("_EmissionColor");
                // Try to get emission intensity if available
                if (playerMaterial.HasProperty("_EmissionIntensity"))
                {
                    originalEmissionIntensity = playerMaterial.GetFloat("_EmissionIntensity");
                }
                else
                {
                    // Estimate intensity from emission color brightness
                    originalEmissionIntensity = originalEmissionColor.maxColorComponent;
                }
            }
        }
    }
    
    void Update()
    {
        UpdateHealthPulse();
        UpdateInvincibility();
    }
    
    void UpdateInvincibility()
    {
        if (isInvincible && Time.time >= invincibilityEndTime)
        {
            isInvincible = false;
            // Restore normal color/material if needed
            if (playerMaterial != null)
            {
                playerMaterial.color = originalColor;
            }
        }
    }
    
    void UpdateHealthPulse()
    {
        if (playerRenderer == null || playerMaterial == null) return;
        
        // Don't pulse during invincibility (handled separately)
        if (isInvincible) return;
        
        // Calculate health percentage
        float healthPercent = (float)health / maxHealth;
        
        // If at full health, restore original appearance and return
        if (healthPercent >= 1f)
        {
            if (hasEmission)
            {
                playerMaterial.SetColor("_EmissionColor", originalEmissionColor);
                if (playerMaterial.HasProperty("_EmissionIntensity"))
                {
                    playerMaterial.SetFloat("_EmissionIntensity", originalEmissionIntensity);
                }
            }
            playerMaterial.color = originalColor;
            return;
        }
        
        // Calculate pulse speed based on health (lower health = faster pulse)
        // When health is above threshold, pulse slowly
        // When health is below threshold, pulse faster
        float pulseSpeed;
        if (healthPercent > maxPulseHealthThreshold)
        {
            // Above threshold: slow pulse
            float t = (healthPercent - maxPulseHealthThreshold) / (1f - maxPulseHealthThreshold);
            pulseSpeed = Mathf.Lerp(maxPulseSpeed, minPulseSpeed, t);
        }
        else
        {
            // Below threshold: fast pulse (scales with how low health is)
            float t = healthPercent / maxPulseHealthThreshold;
            pulseSpeed = Mathf.Lerp(maxPulseSpeed * 1.5f, maxPulseSpeed, t); // Even faster at very low health
        }
        
        // Calculate pulse value (0 to 1, oscillating)
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0 to 1
        
        // Calculate red tint intensity based on health (lower health = more red)
        float redIntensity = (1f - healthPercent) * pulseIntensity;
        
        // Blend original color with red based on pulse and health
        Color pulseColor = Color.Lerp(originalColor, Color.red, pulse * redIntensity);
        playerMaterial.color = pulseColor;
        
        // If material has emission, pulse the emission color and intensity
        if (hasEmission)
        {
            // Create a bright red emission color
            Color redEmission = Color.red * (originalEmissionIntensity * emissionPulseMultiplier);
            
            // Blend between original emission and bright red based on pulse
            Color currentEmission = Color.Lerp(originalEmissionColor, redEmission, pulse * redIntensity);
            
            playerMaterial.SetColor("_EmissionColor", currentEmission);
            
            // Also pulse the emission intensity for more dramatic effect
            if (playerMaterial.HasProperty("_EmissionIntensity"))
            {
                float currentIntensity = Mathf.Lerp(originalEmissionIntensity, originalEmissionIntensity * emissionPulseMultiplier, pulse * redIntensity);
                playerMaterial.SetFloat("_EmissionIntensity", currentIntensity);
            }
            
            // Enable emission if needed
            playerMaterial.EnableKeyword("_EMISSION");
        }
    }

    // Method to set health (useful for adjusting health at runtime)
    public void SetHealth(int newHealth)
    {
        health = Mathf.Clamp(newHealth, 0, maxHealth);
    }

    // Method to heal the player
    public void Heal(int amount)
    {
        health = Mathf.Clamp(health + amount, 0, maxHealth);
    }

    public void TakeDamage(int amount, float forceMultiplier = 1f)
    {
        // Don't take damage if invincible
        if (isInvincible) return;
        
        health -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage (force multiplier: {forceMultiplier:F2})! Health: {health}");
        if (health <= 0)
        {
            HandleDeath();
        }
    }
    
    void HandleDeath()
    {
        lives--;
        Debug.Log($"{gameObject.name} died! Lives remaining: {lives}");
        
        if (lives > 0)
        {
            // Respawn player
            Respawn();
        }
        else
        {
            // Game over for this player
            Debug.Log($"{gameObject.name} is out of lives! Game Over!");
            Destroy(gameObject);
        }
    }
    
    void Respawn()
    {
        // Reset health
        health = maxHealth;
        
        // Reset position to spawn point
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
        }
        
        // Reset velocity
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Set invincibility
        isInvincible = true;
        invincibilityEndTime = Time.time + respawnInvincibilityDuration;
        
        // Visual feedback for invincibility (optional - make player flash or semi-transparent)
        if (playerMaterial != null)
        {
            // Make player slightly transparent during invincibility
            Color invincibleColor = originalColor;
            invincibleColor.a = 0.5f;
            playerMaterial.color = invincibleColor;
        }
        
        Debug.Log($"{gameObject.name} respawned! Lives remaining: {lives}");
    }
    
    public bool IsInvincible()
    {
        return isInvincible;
    }
}
