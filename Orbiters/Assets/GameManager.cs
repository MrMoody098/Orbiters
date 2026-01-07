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
    public int maxHealth = 100;

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
        SetupPlayer(p1, "Horizontal", "Vertical", KeyCode.Q, KeyCode.E, KeyCode.Space, false, 1);

        // Player 2 - Controller/Joystick (uses custom axes)
        GameObject p2 = Instantiate(playerPrefab, player2Spawn.position, Quaternion.identity);
        p2.name = "Player2";
        SetupPlayer(p2, "JoystickHorizontal", "JoystickVertical", KeyCode.Joystick1Button4, KeyCode.Joystick1Button5, KeyCode.Joystick1Button0, true, 1);
    }

    void SetupPlayer(GameObject playerObj, string horizontal, string vertical, 
        KeyCode decRadius, KeyCode incRadius, KeyCode flipDir, bool useJoystick, int joystickNumber)
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
        // Damage check: simple distance collision
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        if (players.Length < 2) return;

        PlayerController player1 = players[0];
        PlayerController player2 = players[1];

        // Check if orbital weapons exist before checking collisions
        if (player1.orbitalWeapon != null && player2 != null && player2.transform != null)
        {
            if (Vector3.Distance(player1.orbitalWeapon.transform.position, player2.transform.position) < 0.5f)
            {
                player2.TakeDamage(10);
            }
        }
        if (player2.orbitalWeapon != null && player1 != null && player1.transform != null)
        {
            if (Vector3.Distance(player2.orbitalWeapon.transform.position, player1.transform.position) < 0.5f)
            {
                player1.TakeDamage(10);
            }
        }
    }
}
