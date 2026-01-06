using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI Buttons")]
    [SerializeField] private Button splitScreenButton;
    [SerializeField] private Button multiplayerButton;

    void Start()
    {
        // Assign button click events
        splitScreenButton.onClick.AddListener(OnSplitScreenClicked);
        multiplayerButton.onClick.AddListener(OnMultiplayerClicked);
    }

    private void OnSplitScreenClicked()
    {
        Debug.Log("Split Screen selected!");
        // Load the scene or configure split screen
        // Example: SceneManager.LoadScene("SplitScreenScene");
        PlayerPrefs.SetString("GameMode", "SplitScreen");
        SceneManager.LoadScene("GameScene"); // replace with your actual gameplay scene
    }

    private void OnMultiplayerClicked()
    {
        Debug.Log("Multiplayer selected!");
        // Load multiplayer setup or scene
        PlayerPrefs.SetString("GameMode", "Multiplayer");
        SceneManager.LoadScene("GameScene"); // replace with your actual gameplay scene
    }
}
