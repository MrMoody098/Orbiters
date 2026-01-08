using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Wall : MonoBehaviour
{
    [Header("Bounce Settings")]
    [Tooltip("Bounce strength (0 = no bounce, 1 = full bounce)")]
    [Range(0f, 1f)]
    public float bounceFactor = 0.4f; // Small bounce by default
    
    [Tooltip("Minimum velocity required to bounce")]
    public float minBounceVelocity = 0.1f;
    
    [Tooltip("Force multiplier for bounce effect")]
    public float bounceForceMultiplier = 2f;

    void OnCollisionEnter(Collision collision)
    {
        ApplyBounce(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        // Also apply bounce while staying in contact (helps overcome velocity override)
        ApplyBounce(collision);
    }

    void ApplyBounce(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb == null) return;

        // Get the contact point and normal
        if (collision.contactCount > 0)
        {
            ContactPoint contact = collision.contacts[0];
            Vector3 normal = contact.normal;
            
            // Get the incoming velocity (use the player's actual velocity)
            Vector3 incomingVelocity = rb.linearVelocity;
            float speed = incomingVelocity.magnitude;
            
            // Only bounce if velocity is above minimum threshold
            if (speed > minBounceVelocity)
            {
                // Calculate bounce direction (reflect velocity off the wall)
                Vector3 reflectedDirection = Vector3.Reflect(incomingVelocity.normalized, normal);
                
                // Calculate bounce force - use Force instead of Impulse for continuous effect
                // Multiply by mass so it scales properly
                Vector3 bounceForce = reflectedDirection * speed * bounceFactor * rb.mass * bounceForceMultiplier;
                
                // Apply the bounce force - using Force mode so it persists across frames
                rb.AddForce(bounceForce, ForceMode.Force);
            }
        }
    }
}

