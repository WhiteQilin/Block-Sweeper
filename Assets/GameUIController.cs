using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameUIController : MonoBehaviour
{
    [Header("HUD References")]
    public TextMeshProUGUI mineCountText;
    public TextMeshProUGUI timerText;

    [Header("Game Mode Toggle")]
    public bool IsFlagMode { get; private set; } = false;
    public Button revealButton;
    public Button flagButton;

    [Header("Visual Feedback")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.7f, 0.7f, 0.7f);

    [Header("Game Over Screens")]
    public GameObject gameOverPanel;
    public GameObject gameWinPanel;

    [Header("Juice / Effects")]
    public Transform cameraTransform;

    // --- NEW: TWO SEPARATE SHAKE TARGET SLOTS ---
    [Tooltip("Drag the Background_Grass object here.")]
    public GameObject grassShakeObject;
    private RectTransform grassShakeRect;

    [Tooltip("Drag the Background_Ground object here.")]
    public GameObject groundShakeObject;
    private RectTransform groundShakeRect;
    // --- END NEW SLOTS ---

    public UnityEngine.UI.Image damageOverlay;

    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.3f;
    public float uiShakeMultiplier = 150f;

    [Header("Settings")]
    public string menuSceneName = "SampleScene";
    // This value is now a fallback, overwritten in Start()
    public float timeLimit = 120f;

    private float timer;
    private bool gameActive = true;

    private void Start()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (gameWinPanel) gameWinPanel.SetActive(false);

        if (damageOverlay)
        {
            damageOverlay.gameObject.SetActive(true);
            Color c = damageOverlay.color;
            c.a = 0f;
            damageOverlay.color = c;
        }

        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        // --- NEW: INITIALIZE BOTH RECT TRANSFORMS ---
        if (grassShakeObject != null) grassShakeRect = grassShakeObject.GetComponent<RectTransform>();
        if (groundShakeObject != null) groundShakeRect = groundShakeObject.GetComponent<RectTransform>();
        // --- END NEW INITIALIZATION ---

        if (uiShakeMultiplier <= 0.1f)
        {
            uiShakeMultiplier = 150f;
            UnityEngine.Debug.Log("Fixed UI Shake Multiplier (it was 0).");
        }

        // --- MODIFIED: Set Time Limit based on Difficulty ---
        int difficultyIndex = PlayerPrefs.GetInt("Difficulty", 0);

        switch (difficultyIndex)
        {
            case 0: // Easy (10x10, 10 Mines)
                timeLimit = 90f;   // 1 minute 30 seconds
                break;
            case 1: // Normal (20x20, 50 Mines)
                timeLimit = 180f;  // 3 minutes (180 seconds)
                break;
            case 2: // Hard (30x30, 180 Mines)
                timeLimit = 420f;  // 7 minutes (420 seconds) - Provides a fair chance on the dense board
                break;
            default:
                timeLimit = 120f;  // Default to 2 minutes
                break;
        }
        // --- END MODIFIED ---

        timer = timeLimit;
        SetRevealMode();
    }

    private void Update()
    {
        if (gameActive)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                timer = 0;
                UnityEngine.Debug.Log("Time's Up!");
                TriggerGameOver();
            }

            // --- MODIFIED: Convert seconds to Minutes:Seconds format ---
            int minutes = Mathf.FloorToInt(timer / 60f);
            int seconds = Mathf.FloorToInt(timer % 60f);

            // Use D2 format for leading zeros (e.g., 01:09)
            timerText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
            // --- END MODIFIED ---
        }
    }

    // --- MODE TOGGLE FUNCTIONS ---

    public void SetRevealMode()
    {
        IsFlagMode = false;
        UpdateModeVisuals();
    }

    public void SetFlagMode()
    {
        IsFlagMode = true;
        UpdateModeVisuals();
    }

    private void UpdateModeVisuals()
    {
        if (revealButton != null && flagButton != null)
        {
            if (IsFlagMode)
            {
                flagButton.image.color = activeColor;
                revealButton.image.color = inactiveColor;
            }
            else
            {
                flagButton.image.color = inactiveColor;
                revealButton.image.color = activeColor;
            }
        }
    }

    // --- PUBLIC GAME STATE FUNCTIONS ---

    public void UpdateMineCount(int count)
    {
        mineCountText.text = count.ToString("D3");
    }

    public void TriggerGameOver()
    {
        if (!gameActive) return;
        gameActive = false;

        // --- NEW: STOP MUSIC ON LOSS ---
        if (AudioController.Instance != null)
        {
            AudioController.Instance.FadeOutMusic();
        }
        // --- END NEW ---

        StartCoroutine(GameOverSequence());
    }

    public void TriggerWin()
    {
        if (!gameActive) return;
        gameActive = false;

        // --- NEW: STOP MUSIC ON WIN ---
        if (AudioController.Instance != null)
        {
            AudioController.Instance.FadeOutMusic();
        }
        // --- END NEW ---

        gameWinPanel.SetActive(true);
    }

    IEnumerator GameOverSequence()
    {
        Vector3 originalCamPos = cameraTransform.localPosition;
        Vector2 originalGrassPos = Vector2.zero;
        Vector2 originalGroundPos = Vector2.zero;

        // Store original UI positions
        if (grassShakeRect) originalGrassPos = grassShakeRect.anchoredPosition;
        if (groundShakeRect) originalGroundPos = groundShakeRect.anchoredPosition;

        float elapsed = 0.0f;

        // Flash RED
        if (damageOverlay)
        {
            Color c = damageOverlay.color;
            c.a = 0.6f;
            damageOverlay.color = c;
        }

        UnityEngine.Debug.Log("STARTING DUAL SHAKE!");

        while (elapsed < shakeDuration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * shakeMagnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * shakeMagnitude;
            Vector2 shakeOffset = new Vector2(x * uiShakeMultiplier, y * uiShakeMultiplier);

            // 1. Shake Camera (Affects Tiles)
            cameraTransform.localPosition = new Vector3(originalCamPos.x + x, originalCamPos.y + y, originalCamPos.z);

            // 2. Shake Background Targets (Affects Grass and Ground)
            if (grassShakeRect) grassShakeRect.anchoredPosition = originalGrassPos + shakeOffset;
            if (groundShakeRect) groundShakeRect.anchoredPosition = originalGroundPos + shakeOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3. Reset Positions
        cameraTransform.localPosition = originalCamPos;
        if (grassShakeRect) grassShakeRect.anchoredPosition = originalGrassPos;
        if (groundShakeRect) groundShakeRect.anchoredPosition = originalGroundPos;

        // 4. Fade Out Red
        if (damageOverlay)
        {
            damageOverlay.CrossFadeAlpha(0f, 0.2f, false);
        }

        gameOverPanel.SetActive(true);
    }

    // --- BUTTON NAVIGATION FUNCTIONS ---

    public void OnHomeClicked()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    public void OnQuitToMenuClicked()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    public void OnRestartClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}