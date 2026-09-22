using System.Collections;
using UnityEngine;

public class TitleScreenManager : MonoBehaviour
{
    [Tooltip("The title screen's own root object — background image + Start button. Hidden once Start is pressed.")]
    public GameObject titleScreenRoot;

    [Tooltip("Wrapper holding LevelManager + all Level1-5 content. Hidden until Start is pressed.")]
    public GameObject gameplayRoot;

    public LevelManager levelManager;

    [Header("Hand-Tracking Systems (must not run on the title screen)")]
    [Tooltip("Disabled while on the title screen so free-mode jutsu detection doesn't fire from hands positioned there.")]
    [SerializeField] private JutsuManager jutsuManager;
    [Tooltip("Disabled while on the title screen so the chakra charge pose can't charge chakra or trigger the fire VFX.")]
    [SerializeField] private ChakraCharger chakraCharger;

    [Header("Button SFX")]
    [Tooltip("AudioSource on THIS GameObject — must not live under titleScreenRoot or gameplayRoot, or it gets cut off when those deactivate.")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip startButtonSound;

    [Header("Title Music")]
    [Tooltip("Separate AudioSource on THIS GameObject, Loop enabled, Play On Awake OFF.")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip titleMusic;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.6f;
    [SerializeField] private float musicFadeDuration = 1.5f;

    [Header("Timing")]
    [Tooltip("Seconds to wait after Start is pressed before Level 1's targets actually appear. GameplayRoot itself still appears immediately.")]
    [SerializeField] private float gameplayRevealDelay = 1.5f;

    private Coroutine musicFadeRoutine;

    private void Awake()
    {
        if (titleScreenRoot != null)
        {
            titleScreenRoot.SetActive(true);
        }

        if (gameplayRoot != null)
        {
            gameplayRoot.SetActive(false);
        }

        if (jutsuManager != null)
        {
            jutsuManager.enabled = false;
        }

        if (chakraCharger != null)
        {
            chakraCharger.enabled = false;
        }

        if (musicSource != null)
        {
            musicSource.clip = titleMusic;
            musicSource.loop = true;
        }

        FadeMusicIn();
    }

    /// <summary>Hook this to the Start button's press event.</summary>
    public void OnStartPressed()
    {
        if (sfxSource != null && startButtonSound != null)
        {
            sfxSource.PlayOneShot(startButtonSound);
        }

        FadeMusicOut();

        if (gameplayRoot != null)
        {
            gameplayRoot.SetActive(true);
        }

        StartCoroutine(HideTitleScreenNextFrame());
        StartCoroutine(RevealGameplayAfterDelay(gameplayRevealDelay));
    }

    private IEnumerator HideTitleScreenNextFrame()
    {
        yield return null;

        if (titleScreenRoot != null)
        {
            titleScreenRoot.SetActive(false);
        }
    }

    private IEnumerator RevealGameplayAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (jutsuManager != null)
        {
            jutsuManager.enabled = true;
        }

        if (chakraCharger != null)
        {
            chakraCharger.enabled = true;
        }

        if (levelManager != null)
        {
            levelManager.BeginGame();
        }
    }

    /// <summary>Hook this to any "Back to Title" button, in every level.</summary>
    public void ReturnToTitle()
    {
        StartCoroutine(ReturnToTitleNextFrame());
    }

    private IEnumerator ReturnToTitleNextFrame()
    {
        yield return null;

        if (jutsuManager != null)
        {
            jutsuManager.enabled = false;
        }

        if (chakraCharger != null)
        {
            chakraCharger.enabled = false;
        }

        if (levelManager != null)
        {
            levelManager.HideAllSets();
        }

        if (gameplayRoot != null)
        {
            gameplayRoot.SetActive(false);
        }

        if (titleScreenRoot != null)
        {
            titleScreenRoot.SetActive(true);
        }

        FadeMusicIn();
    }

    private void FadeMusicIn()
    {
        if (musicSource == null || titleMusic == null) return;

        if (musicFadeRoutine != null) StopCoroutine(musicFadeRoutine);

        if (!musicSource.isPlaying)
        {
            musicSource.volume = 0f;
            musicSource.Play();
        }

        musicFadeRoutine = StartCoroutine(FadeMusicTo(musicVolume, musicFadeDuration, stopAfterFade: false));
    }

    private void FadeMusicOut()
    {
        if (musicSource == null) return;

        if (musicFadeRoutine != null) StopCoroutine(musicFadeRoutine);

        musicFadeRoutine = StartCoroutine(FadeMusicTo(0f, musicFadeDuration, stopAfterFade: true));
    }

    private IEnumerator FadeMusicTo(float targetVolume, float duration, bool stopAfterFade)
    {
        float startVolume = musicSource.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, t / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;

        if (stopAfterFade && targetVolume <= 0f)
        {
            musicSource.Stop();
        }
    }
}