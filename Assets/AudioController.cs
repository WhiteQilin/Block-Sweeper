using UnityEngine;
using UnityEngine.SceneManagement;

// TITLE: PERSISTENT AUDIO CONTROLLER (SINGLETON PATTERN)
// ROLE: Manages background music (BGM) and general sound effects (SFX) playback across all scenes.

public class AudioController : MonoBehaviour
{
    // Singleton pattern ensures only one instance exists.
    public static AudioController Instance;

    [Header("BGM Settings")]
    [Tooltip("Drag the main background music clip here.")]
    public AudioClip gameBGM;

    [Tooltip("Drag the Main Menu background music clip here.")]
    public AudioClip menuBGM;
    public float bgmVolume = 0.5f;

    // --- NEW: SFX SETTINGS ---
    [Header("SFX Settings")]
    [Tooltip("Drag the generic button click sound clip here (e.g., 'clack').")]
    public AudioClip genericButtonClickSFX;
    [Tooltip("Drag the generic hover sound clip here (e.g., 'whoosh').")]
    public AudioClip genericButtonHoverSFX;
    public float sfxVolume = 0.8f;

    [Header("Gameplay SFX")]
    [Tooltip("Sound when a non-mine tile is successfully revealed.")]
    public AudioClip tileRevealSFX;
    [Tooltip("Sound when a flag is placed or removed.")]
    public AudioClip flagToggleSFX;
    [Tooltip("Sound when a mine explodes (Loss).")]
    public AudioClip mineExplodeSFX;
    [Tooltip("Sound played when the game is won.")]
    public AudioClip gameWinSFX;
    // --- END NEW SFX SLOTS ---

    private AudioSource audioSource;
    private AudioSource sfxSource;
    private AudioSource hoverSfxSource;

    private string gameSceneName = "GameScene";
    private string menuSceneName = "SampleScene";

    private void Awake()
    {
        // --- Singleton Implementation ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize AudioSource component for BGM
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = bgmVolume;

        // Initialize AudioSource component for SFX (clicks/explosions)
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;

        // Initialize AudioSource component for Hover SFX
        hoverSfxSource = gameObject.AddComponent<AudioSource>();
        hoverSfxSource.loop = false;
        hoverSfxSource.volume = sfxVolume * 0.7f;

        // Add a listener for scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == menuSceneName)
        {
            StartMenuMusic();
        }
        else if (scene.name == gameSceneName)
        {
            StartGameMusic();
        }
    }

    // --- Public BGM Control Methods (remain the same) ---

    public void StartMenuMusic()
    {
        if (menuBGM == null)
        {
            UnityEngine.Debug.LogError("Menu BGM AudioClip is not assigned in the Inspector!");
            return;
        }

        if (audioSource.clip != menuBGM || !audioSource.isPlaying)
        {
            audioSource.clip = menuBGM;
            audioSource.Play();
        }
    }

    public void StartGameMusic()
    {
        if (gameBGM == null)
        {
            UnityEngine.Debug.LogError("Game BGM AudioClip is not assigned in the Inspector!");
            return;
        }

        if (audioSource.clip != gameBGM || !audioSource.isPlaying)
        {
            audioSource.clip = gameBGM;
            audioSource.Play();
        }
    }

    public void StopMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    // --- Public SFX Control Methods ---

    public void PlaySFX()
    {
        if (genericButtonClickSFX == null) return;
        sfxSource.PlayOneShot(genericButtonClickSFX);
    }

    public void PlayHoverSFX(AudioClip clipToPlay = null)
    {
        AudioClip actualClip = clipToPlay != null ? clipToPlay : genericButtonHoverSFX;

        if (actualClip == null) return;
        if (hoverSfxSource.isPlaying) return;

        hoverSfxSource.PlayOneShot(actualClip);
    }

    // NEW: Plays sound when a tile is revealed (pop/click)
    public void PlayTileRevealSFX()
    {
        if (tileRevealSFX == null) return;
        sfxSource.PlayOneShot(tileRevealSFX);
    }

    // NEW: Plays sound when a flag is placed/removed
    public void PlayFlagToggleSFX()
    {
        if (flagToggleSFX == null) return;
        sfxSource.PlayOneShot(flagToggleSFX);
    }

    // NEW: Plays explosion sound
    public void PlayMineExplodeSFX()
    {
        if (mineExplodeSFX == null) return;
        sfxSource.PlayOneShot(mineExplodeSFX);
    }

    // NEW: Plays win sound
    public void PlayGameWinSFX()
    {
        if (gameWinSFX == null) return;
        sfxSource.PlayOneShot(gameWinSFX);
    }


    public void FadeOutMusic(float duration = 0.5f)
    {
        StartCoroutine(FadeMusicOutCoroutine(duration));
    }

    private System.Collections.IEnumerator FadeMusicOutCoroutine(float duration)
    {
        float startVolume = audioSource.volume;
        float startTime = Time.time;

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            audioSource.volume = Mathf.Lerp(startVolume, 0, t);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }
}