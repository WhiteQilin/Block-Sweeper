using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuController : UnityEngine.MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("Drag the 'Panel_MainMenu' object here")]
    public UnityEngine.GameObject mainMenuPanel;

    [Tooltip("Drag the 'Panel_Difficulty' object here")]
    public UnityEngine.GameObject difficultyPanel;

    // --- NEW: RECT TRANSFORMS FOR SLIDING ---
    private RectTransform mainMenuRect;
    private RectTransform difficultyRect;
    // --- END NEW ---

    [Header("Difficulty Visuals")]
    [Tooltip("Drag the 'Image_Face' (Center Face) here")]
    public UnityEngine.UI.Image faceImage;

    [Tooltip("Drag the 'Difficulty_Label' (Top Text Slab) here")]
    public UnityEngine.UI.Image labelImage;

    [Header("Face Sprites")]
    public UnityEngine.Sprite easyFace;
    public UnityEngine.Sprite normalFace;
    public UnityEngine.Sprite hardFace;

    [Header("Label Sprites")]
    public UnityEngine.Sprite easyLabel;
    public UnityEngine.Sprite normalLabel;
    public UnityEngine.Sprite hardLabel;

    [Header("Difficulty Animation")]
    public float scaleUpMagnitude = 1.1f;
    public float scaleDuration = 0.1f;

    [Header("Panel Transition Settings")]
    public float transitionDuration = 0.3f;
    public float offScreenXOffset = 1500f;
    public float offScreenYOffset = 1500f;

    // State tracking
    private int currentDifficulty = 0;
    private Coroutine faceScaleCoroutine;
    private Coroutine labelScaleCoroutine;
    private Coroutine panelTransitionCoroutine;

    private Vector3 centerScreenPosition = Vector3.zero;

    // --- NEW: BASELINE SCALE TRACKERS ---
    // These store the scale the objects had when the scene started (e.g., 0.4, 0.4, 0.4)
    private Vector3 initialFaceScale;
    private Vector3 initialLabelScale;
    // --- END NEW ---

    private void Awake()
    {
        // Get the RectTransforms once
        if (mainMenuPanel) mainMenuRect = mainMenuPanel.GetComponent<RectTransform>();
        if (difficultyPanel) difficultyRect = difficultyPanel.GetComponent<RectTransform>();

        // Ensure both panels exist
        if (mainMenuRect == null || difficultyRect == null)
        {
            UnityEngine.Debug.LogError("MainMenuPanel or DifficultyPanel RectTransform not found! Check assignments in Inspector.");
        }
    }

    private void Start()
    {
        // Capture the initial, non-animated scale from the Inspector
        if (faceImage != null) initialFaceScale = faceImage.rectTransform.localScale;
        if (labelImage != null) initialLabelScale = labelImage.rectTransform.localScale;

        currentDifficulty = UnityEngine.PlayerPrefs.GetInt("Difficulty", 0);

        // --- INITIAL SETUP FOR TRANSITION ---
        if (mainMenuRect != null && difficultyRect != null)
        {
            // Set Difficulty panel off-screen to the right
            difficultyRect.anchoredPosition = new Vector2(offScreenXOffset, 0);
            // Set Main Menu panel at center
            mainMenuRect.anchoredPosition = centerScreenPosition;
        }

        // Show main menu on start
        mainMenuPanel.SetActive(true);
        difficultyPanel.SetActive(true);

        UpdateDifficultyVisuals();

        // --- START MENU MUSIC ON START ---
        if (AudioController.Instance != null)
        {
            AudioController.Instance.StartMenuMusic();
        }
    }

    // --- NAVIGATION ---

    public void ShowDifficultySelect()
    {
        if (panelTransitionCoroutine != null) StopCoroutine(panelTransitionCoroutine);

        PlayButtonSound();

        panelTransitionCoroutine = StartCoroutine(SlidePanels(mainMenuRect, difficultyRect, new Vector2(-offScreenXOffset, 0), centerScreenPosition));
    }

    public void ShowMainMenu()
    {
        if (panelTransitionCoroutine != null) StopCoroutine(panelTransitionCoroutine);

        PlayButtonSound();

        if (AudioController.Instance != null)
        {
            AudioController.Instance.StartMenuMusic();
        }

        panelTransitionCoroutine = StartCoroutine(SlidePanels(difficultyRect, mainMenuRect, new Vector2(offScreenXOffset, 0), centerScreenPosition));
    }

    // --- SLIDING COROUTINE (UNCHANGED) ---
    IEnumerator SlidePanels(RectTransform panelToMoveOut, RectTransform panelToMoveIn, Vector2 targetOutPos, Vector2 targetInPos)
    {
        float elapsed = 0f;
        Vector2 startOutPos = panelToMoveOut.anchoredPosition;
        Vector2 startInPos = panelToMoveIn.anchoredPosition;

        panelToMoveOut.gameObject.SetActive(true);
        panelToMoveIn.gameObject.SetActive(true);

        while (elapsed < transitionDuration)
        {
            float t = elapsed / transitionDuration;

            float easedT = Mathf.SmoothStep(0f, 1f, t);

            panelToMoveOut.anchoredPosition = Vector2.Lerp(startOutPos, targetOutPos, easedT);
            panelToMoveIn.anchoredPosition = Vector2.Lerp(startInPos, targetInPos, easedT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        panelToMoveOut.anchoredPosition = targetOutPos;
        panelToMoveIn.anchoredPosition = targetInPos;

        panelToMoveOut.gameObject.SetActive(false);

        panelTransitionCoroutine = null;
    }
    // --- END SLIDING COROUTINE ---


    // --- DIFFICULTY LOGIC (MODIFIED FOR SAFE ANIMATION START) ---

    public void OnArrowClick(int direction)
    {
        PlayButtonSound();

        currentDifficulty += direction;

        if (currentDifficulty < 0) currentDifficulty = 2;
        if (currentDifficulty > 2) currentDifficulty = 0;

        UpdateDifficultyVisuals();

        // 1. FACE ANIMATION START (ROBUST)
        if (faceScaleCoroutine != null)
        {
            StopCoroutine(faceScaleCoroutine);
            // GUARANTEE RESET: Snap back to the scale captured in Start()
            faceImage.rectTransform.localScale = initialFaceScale;
        }
        faceScaleCoroutine = StartCoroutine(AnimateScale(faceImage.rectTransform, initialFaceScale, false));

        // 2. LABEL ANIMATION START (ROBUST)
        if (labelScaleCoroutine != null)
        {
            StopCoroutine(labelScaleCoroutine);
            // GUARANTEE RESET: Snap back to the scale captured in Start()
            labelImage.rectTransform.localScale = initialLabelScale;
        }
        labelScaleCoroutine = StartCoroutine(AnimateScale(labelImage.rectTransform, initialLabelScale, true));
    }

    private void UpdateDifficultyVisuals()
    {
        switch (currentDifficulty)
        {
            case 0: // Easy
                faceImage.sprite = easyFace;
                labelImage.sprite = easyLabel;
                UnityEngine.PlayerPrefs.SetInt("Difficulty", 0);
                break;
            case 1: // Normal
                faceImage.sprite = normalFace;
                labelImage.sprite = normalLabel;
                UnityEngine.PlayerPrefs.SetInt("Difficulty", 1);
                break;
            case 2: // Hard
                faceImage.sprite = hardFace;
                labelImage.sprite = hardLabel;
                UnityEngine.PlayerPrefs.SetInt("Difficulty", 2);
                break;
        }
        UnityEngine.PlayerPrefs.Save();
    }

    // --- MODIFIED SCALE ANIMATION COROUTINE ---
    // Takes the original scale as an argument for guaranteed return.
    IEnumerator AnimateScale(RectTransform target, Vector3 startScale, bool isLabel)
    {
        // startScale is now the clean, non-animated scale (initialFaceScale or initialLabelScale)
        Vector3 initialScale = startScale;
        Vector3 targetScale = initialScale * scaleUpMagnitude;

        // --- Scale UP ---
        float elapsed = 0f;
        while (elapsed < scaleDuration)
        {
            float t = elapsed / scaleDuration;
            // Lerp from current position (which might be slightly off) to targetScale
            target.localScale = Vector3.Lerp(target.localScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localScale = targetScale; // Snap to max scale

        // --- Scale DOWN (Return to clean start scale) ---
        elapsed = 0f;
        while (elapsed < scaleDuration)
        {
            float t = elapsed / scaleDuration;
            // Lerp from max scale back to the clean initial scale
            target.localScale = Vector3.Lerp(targetScale, initialScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localScale = initialScale; // GUARANTEE: Snap back to the clean starting scale

        // Clean up the Coroutine tracker
        if (isLabel)
        {
            labelScaleCoroutine = null;
        }
        else
        {
            faceScaleCoroutine = null;
        }
    }
    // --- END MODIFIED ---

    // --- NEW HELPER FUNCTION TO PLAY SFX ---
    private void PlayButtonSound()
    {
        if (AudioController.Instance != null)
        {
            AudioController.Instance.PlaySFX();
        }
    }
    // --- END NEW HELPER FUNCTION ---

    // --- BUTTON EVENTS ---

    public void OnPlayClicked()
    {
        PlayButtonSound(); // Play sound when clicking Play button
        ShowDifficultySelect();
    }

    public void OnStartGameClicked()
    {
        PlayButtonSound(); // Play sound when clicking Start Game button
        if (panelTransitionCoroutine != null) StopCoroutine(panelTransitionCoroutine);
        panelTransitionCoroutine = StartCoroutine(AnimateAndLoadScene("GameScene"));
    }

    IEnumerator AnimateAndLoadScene(string sceneName)
    {
        // 1. Ensure the panel is active and get its current position
        difficultyPanel.SetActive(true);
        Vector2 startPos = difficultyRect.anchoredPosition;

        // 2. Calculate the target position based on the screen height.
        float safeYOffset = Screen.height + difficultyRect.rect.height;
        Vector2 targetPos = new Vector2(startPos.x, startPos.y + safeYOffset);

        float elapsed = 0f;
        float animationTime = transitionDuration * 1.5f;

        // 3. Slide the Difficulty Panel rapidly UP and off-screen
        while (elapsed < animationTime)
        {
            float t = elapsed / animationTime;
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            difficultyRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, easedT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 4. Snap to final position and load the scene
        difficultyRect.anchoredPosition = targetPos;

        SceneManager.LoadScene(sceneName);
    }

    public void OnHomeClicked()
    {
        PlayButtonSound(); // Play sound when clicking Home button
        ShowMainMenu();
    }

    public void OnQuitClicked()
    {
        PlayButtonSound(); // Play sound when clicking Quit button
        UnityEngine.Application.Quit();
        UnityEngine.Debug.Log("Game Quit!");
    }
}