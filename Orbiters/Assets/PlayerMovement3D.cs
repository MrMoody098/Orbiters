using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement3D : MonoBehaviour
{
    public float moveSpeed = 6f;
    [Header("Input Settings")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis = "Vertical";
    [Tooltip("If true, uses direct joystick input instead of input axes. Set joystickNumber to 1 for first controller, 2 for second, etc.")]
    public bool useJoystickDirect = false;
    [Tooltip("Joystick number (1 = first controller, 2 = second controller, etc.)")]
    public int joystickNumber = 1;
    
    [Header("Knockback Detection Settings")]
    [Tooltip("Speed multiplier threshold to detect knockback (currentSpeed > moveSpeed * this value)")]
    public float knockbackSpeedMultiplier = 1.2f;
    [Tooltip("Minimum speed to check for bouncing")]
    public float bounceDetectionMinSpeed = 0.5f;
    [Tooltip("Minimum desired velocity magnitude to consider movement input")]
    public float minDesiredVelocityMagnitude = 0.01f;
    [Tooltip("Dot product threshold for bounce detection (negative = moving opposite to desired direction)")]
    public float bounceDotProductThreshold = -0.3f;
    [Tooltip("Force multiplier applied during knockback/bounce when player tries to move")]
    public float knockbackMovementForceMultiplier = 2f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Get joystick input directly (for controller support)
    float GetJoystickAxis(int joystickNum, int axisNum)
    {
        // Check if joystick is connected
        string[] joysticks = Input.GetJoystickNames();
        if (joysticks.Length < joystickNum)
        {
            return 0f; // Joystick not connected
        }

        // Use the configured axis names (should be "JoystickHorizontal" and "JoystickVertical" for Player 2)
        string axisName = axisNum == 0 ? horizontalAxis : verticalAxis;
        float axisValue = GetAxisSafe(axisName, axisNum == 0);
        
        return axisValue;
    }

    // Get keyboard input only (ignores joystick)
    float GetKeyboardInput(bool isHorizontal)
    {
        float value = 0f;
        
        if (isHorizontal)
        {
            // Horizontal: A/D or Left/Right arrows
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                value -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                value += 1f;
        }
        else
        {
            // Vertical: W/S or Up/Down arrows
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                value += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                value -= 1f;
        }
        
        return value;
    }

    // Safe method to get axis input
    float GetAxisSafe(string axisName, bool isHorizontal)
    {
        try
        {
            return Input.GetAxisRaw(axisName);
        }
        catch (System.ArgumentException)
        {
            // Axis doesn't exist
            if (!string.IsNullOrEmpty(axisName) && (axisName != "Horizontal" && axisName != "Vertical"))
            {
                return Input.GetAxisRaw(isHorizontal ? "Horizontal" : "Vertical");
            }
            return 0f;
        }
    }

    void Start()
    {
        // Check joystick connection if using joystick direct input
        if (useJoystickDirect)
        {
            string[] joysticks = Input.GetJoystickNames();
            if (joysticks.Length >= joystickNumber && !string.IsNullOrEmpty(joysticks[joystickNumber - 1]))
            {
                Debug.Log($"PlayerMovement3D: Joystick {joystickNumber} detected - {joysticks[joystickNumber - 1]}. " +
                    $"Using axes '{horizontalAxis}' and '{verticalAxis}'. " +
                    $"Make sure these axes are configured in Input Manager for Joystick {joystickNumber}.");
            }
            else
            {
                Debug.LogWarning($"PlayerMovement3D: Joystick {joystickNumber} not detected. Make sure your controller is connected. " +
                    $"Connected joysticks: {joysticks.Length}.");
            }
            
            // Test axis values
            try
            {
                float hAxis = Input.GetAxis(horizontalAxis);
                float vAxis = Input.GetAxis(verticalAxis);
                Debug.Log($"PlayerMovement3D: Testing axes - {horizontalAxis}={hAxis:F2}, {verticalAxis}={vAxis:F2}. " +
                    $"If these are always 0 when moving joystick, you need to configure these axes in Input Manager.");
            }
            catch
            {
                Debug.LogWarning($"PlayerMovement3D: Axes '{horizontalAxis}' and/or '{verticalAxis}' not found in Input Manager. " +
                    $"Please create these axes in Edit -> Project Settings -> Input Manager.");
            }
        }
    }

    void FixedUpdate()
    {
        float x, y;

        if (useJoystickDirect)
        {
            // Use direct joystick input only
            x = GetJoystickAxis(joystickNumber, 0); // X axis (horizontal)
            y = GetJoystickAxis(joystickNumber, 1); // Y axis (vertical)
            
            // Debug: Log if we're getting input (only occasionally to avoid spam)
            if (Time.frameCount % 60 == 0 && (Mathf.Abs(x) > 0.01f || Mathf.Abs(y) > 0.01f))
            {
                try
                {
                    float hAxis = Input.GetAxisRaw(horizontalAxis);
                    float vAxis = Input.GetAxisRaw(verticalAxis);
                    Debug.Log($"PlayerMovement3D (Joystick {joystickNumber}): x={x:F2}, y={y:F2}, {horizontalAxis}={hAxis:F2}, {verticalAxis}={vAxis:F2}");
                }
                catch
                {
                    Debug.Log($"PlayerMovement3D (Joystick {joystickNumber}): x={x:F2}, y={y:F2}");
                }
            }
        }
        else
        {
            // Use keyboard input only (ignores joystick)
            x = GetKeyboardInput(true);  // Horizontal
            y = GetKeyboardInput(false); // Vertical
        }

        Vector3 desiredVelocity = new Vector3(x, y, 0f).normalized * moveSpeed;
        
        // Check if player is being knocked back (high velocity from impact)
        float currentSpeed = rb.linearVelocity.magnitude;
        bool isKnockedBack = currentSpeed > moveSpeed * knockbackSpeedMultiplier;
        
        // Check if player is moving away from desired direction (likely bouncing off wall)
        bool isBouncing = false;
        if (currentSpeed > bounceDetectionMinSpeed && desiredVelocity.magnitude > minDesiredVelocityMagnitude)
        {
            float dot = Vector3.Dot(rb.linearVelocity.normalized, desiredVelocity.normalized);
            isBouncing = dot < bounceDotProductThreshold; // Moving opposite to desired direction
        }
        
        if (isKnockedBack || isBouncing)
        {
            // Being knocked back or bouncing - let physics handle it
            // Don't override velocity, just add slight movement influence
            // The drag will naturally slow down the knockback/bounce
            if (desiredVelocity.magnitude > minDesiredVelocityMagnitude)
            {
                // Add slight movement influence while sliding/bouncing
                rb.AddForce(desiredVelocity * knockbackMovementForceMultiplier, ForceMode.Force);
            }
        }
        else
        {
            // Normal movement - set velocity directly for responsive controls
            rb.linearVelocity = desiredVelocity;
        }
    }
}