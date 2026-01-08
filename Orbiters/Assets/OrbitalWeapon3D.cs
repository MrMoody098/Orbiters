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

    [Header("Collision Settings")]
    public float weaponColliderRadius = 0.5f;
    public float weaponRadiusTolerance = 0.1f; // tolerance for same-size comparison
    
    [Header("Wall Collision Settings")]
    [Tooltip("Cooldown between wall collisions to prevent rapid direction changes")]
    public float wallCollisionCooldown = 0.2f;
    
    [Header("Force Multiplier Settings")]
    [Tooltip("Minimum force multiplier (at max radius)")]
    public float minForceMultiplier = 1f;
    [Tooltip("Maximum force multiplier (at min radius)")]
    public float maxForceMultiplier = 2.5f;
    
    [Header("Audio Settings")]
    [Tooltip("Audio clip to play when hitting a player (will vary pitch/volume based on hit strength)")]
    public AudioClip hitSoundClip;
    [Tooltip("Base volume for hit sounds")]
    [Range(0f, 1f)]
    public float baseHitVolume = 0.7f;
    [Tooltip("Minimum pitch for weak hits")]
    [Range(0.1f, 2f)]
    public float minHitPitch = 0.8f;
    [Tooltip("Maximum pitch for strong hits")]
    [Range(0.1f, 2f)]
    public float maxHitPitch = 1.5f;
    [Tooltip("Force multiplier threshold for minimum pitch (weak hits)")]
    public float minPitchForceMultiplier = 1f;
    [Tooltip("Force multiplier threshold for maximum pitch (strong hits)")]
    public float maxPitchForceMultiplier = 2.5f;

    float angle;
    int direction = 1;
    private Vector3 lastPosition;
    private Vector3 currentVelocity;
    private float lastDirectionChangeTime = 0f; // Track when direction was last changed
    
    // Collision tracking
    private float lastHitTime = 0f; // Per-weapon hit cooldown
    private float lastWeaponCollisionTime = 0f; // Per-weapon weapon collision cooldown
    private float lastWallCollisionTime = 0f; // Per-weapon wall collision cooldown
    private GameManager gameManager; // Reference to GameManager for combat settings
    
    // Reference to owner player to avoid self-collisions
    public PlayerController ownerPlayer;
    
    // Audio source for hit sounds
    private AudioSource audioSource;
    
    // Public method to update weapon collision time (called by other weapons on collision)
    public void SetWeaponCollisionTime(float time)
    {
        lastWeaponCollisionTime = time;
    }

    void Start()
    {
        if (player != null)
        {
            lastPosition = transform.position;
            ownerPlayer = player.GetComponent<PlayerController>();
        }
        
        // Find GameManager (or cache it if you prefer)
        gameManager = FindObjectOfType<GameManager>();
        
        // Ensure collider is set up as trigger
        SetupCollider();
        
        // Setup audio source for hit sounds
        SetupAudioSource();
    }
    
    void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure audio source
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound (no 3D positioning)
        audioSource.volume = baseHitVolume;
    }
    
    void SetupCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
            sphereCol.radius = weaponColliderRadius;
            sphereCol.isTrigger = true;
        }
        else
        {
            col.isTrigger = true; // Make sure it's a trigger
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
                FlipDirection();
            }
        }
    }

    // Public method to flip direction (can be called externally, respects cooldown)
    public void FlipDirection()
    {
        // Check if enough time has passed since last direction change
        if (Time.time - lastDirectionChangeTime >= directionChangeCooldown)
        {
            direction *= -1;
            lastDirectionChangeTime = Time.time; // Update the timestamp
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
    
    // Unity Physics collision callbacks
    void OnTriggerEnter(Collider other)
    {
        // Check collision with wall first (walls should take priority)
        Wall wall = other.GetComponent<Wall>();
        if (wall != null)
        {
            HandleWallCollision(wall);
            return; // Don't check other collisions if we hit a wall
        }
        
        if (gameManager == null) return;
        
        // Check collision with player
        PlayerController hitPlayer = other.GetComponent<PlayerController>();
        if (hitPlayer != null && hitPlayer != ownerPlayer)
        {
            HandlePlayerCollision(hitPlayer);
            return;
        }
        
        // Check collision with another orbital weapon
        OrbitalWeapon3D otherWeapon = other.GetComponent<OrbitalWeapon3D>();
        if (otherWeapon != null && otherWeapon != this)
        {
            HandleWeaponCollision(otherWeapon);
        }
    }
    
    void HandlePlayerCollision(PlayerController target)
    {
        // Don't hit if target is invincible (respawning)
        if (target.IsInvincible())
        {
            return;
        }
        
        // Check cooldown to prevent spam hits
        if (Time.time - lastHitTime < gameManager.hitCooldown)
        {
            return; // Still on cooldown
        }
        
        // Update last hit time
        lastHitTime = Time.time;
        
        // Calculate force multiplier for sound variation
        float forceMultiplier = GetForceMultiplier();
        
        // Play hit sound with variation based on force multiplier
        PlayHitSound(forceMultiplier);
        
        // Apply hit through GameManager (keeps damage/force logic centralized)
        gameManager.ApplyHit(this, target);
    }
    
    void PlayHitSound(float forceMultiplier)
    {
        if (hitSoundClip == null || audioSource == null) return;
        
        // Calculate pitch based on force multiplier
        // Higher force multiplier = higher pitch (harder hit = sharper sound)
        float normalizedForce = Mathf.InverseLerp(minPitchForceMultiplier, maxPitchForceMultiplier, forceMultiplier);
        normalizedForce = Mathf.Clamp01(normalizedForce); // Clamp between 0 and 1
        float pitch = Mathf.Lerp(minHitPitch, maxHitPitch, normalizedForce);
        
        // Calculate volume based on force multiplier (harder hits = louder)
        // Volume increases with force multiplier, but caps at baseHitVolume
        float volumeMultiplier = Mathf.Lerp(0.5f, 1f, normalizedForce);
        float volume = baseHitVolume * volumeMultiplier;
        
        // Set audio source properties and play
        audioSource.pitch = pitch;
        audioSource.volume = volume;
        audioSource.PlayOneShot(hitSoundClip);
    }
    
    void HandleWallCollision(Wall wall)
    {
        // Immediately flip direction when hitting a wall (no cooldown to prevent going through walls)
        direction *= -1;
        lastDirectionChangeTime = Time.time; // Update timestamp to prevent input flip spam
    }
    
    void HandleWeaponCollision(OrbitalWeapon3D otherWeapon)
    {
        // Check cooldown to prevent rapid direction changes
        if (Time.time - lastWeaponCollisionTime < gameManager.orbitalWeaponCollisionCooldown)
        {
            return; // Still on cooldown
        }
        
        // Update collision time for both weapons
        float collisionTime = Time.time;
        lastWeaponCollisionTime = collisionTime;
        otherWeapon.SetWeaponCollisionTime(collisionTime);
        
        // Compare radii
        float radius1 = radius;
        float radius2 = otherWeapon.radius;
        
        // Tolerance for "same size" comparison
        if (Mathf.Abs(radius1 - radius2) < weaponRadiusTolerance)
        {
            // Same size - both change direction
            FlipDirection();
            otherWeapon.FlipDirection();
        }
        else if (radius1 > radius2)
        {
            // This weapon has bigger orbit - it changes direction
            FlipDirection();
        }
        else
        {
            // Other weapon has bigger orbit - it changes direction
            otherWeapon.FlipDirection();
        }
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
        
        // Calculate force multiplier range based on configurable values
        float multiplierRange = maxForceMultiplier - minForceMultiplier;
        return minForceMultiplier + inverted * multiplierRange; // Linear interpolation
    }
    
    // Get the current angular speed
    public float GetAngularSpeed()
    {
        return linearSpeed / radius;
    }
}
