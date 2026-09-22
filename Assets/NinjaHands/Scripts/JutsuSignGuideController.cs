using UnityEngine;

public class JutsuSignGuideController : MonoBehaviour
{
    [SerializeField] private JutsuManager jutsuManager;
    [SerializeField] private AudioSource sfxSource;

    private HandPoseGuideVisual _currentlyShown;

    private void OnEnable()
    {
        if (jutsuManager == null) return;
        jutsuManager.OnDetectionStarted.AddListener(HandleDetectionStarted);
        jutsuManager.OnSignCompleted.AddListener(HandleSignCompleted);
        jutsuManager.OnJutsuCompleted.AddListener(HandleJutsuCompleted);
        jutsuManager.OnSequenceReset.AddListener(HandleSequenceReset);
    }

    private void OnDisable()
    {
        if (jutsuManager == null) return;
        jutsuManager.OnDetectionStarted.RemoveListener(HandleDetectionStarted);
        jutsuManager.OnSignCompleted.RemoveListener(HandleSignCompleted);
        jutsuManager.OnJutsuCompleted.RemoveListener(HandleJutsuCompleted);
        jutsuManager.OnSequenceReset.RemoveListener(HandleSequenceReset);
        HideCurrent();
    }

    private void HandleDetectionStarted(string jutsuName)
    {
        ShowStep(0);
    }

    private void HandleSignCompleted(string jutsuName, int stepIndex)
    {
        var jutsu = jutsuManager.CurrentJutsu;
        if (jutsu == null) return;

        if (stepIndex >= 0 && stepIndex < jutsu.signs.Count)
        {
            var step = jutsu.signs[stepIndex];
            PlayRandomCompletionSound(step.completionSounds);
        }

        int nextIndex = stepIndex + 1;
        if (nextIndex < jutsu.signs.Count)
        {
            ShowStep(nextIndex);
        }
        else
        {
            HideCurrent();
        }
    }

    private void PlayRandomCompletionSound(AudioClip[] clips)
    {
        if (sfxSource == null || clips == null || clips.Length == 0) return;

        AudioClip chosen = clips[Random.Range(0, clips.Length)];
        if (chosen != null)
        {
            sfxSource.PlayOneShot(chosen);
        }
    }

    private void HandleJutsuCompleted(string jutsuName)
    {
        HideCurrent();
    }

    private void HandleSequenceReset(string jutsuName)
    {
        HideCurrent();
        if (jutsuManager.IsDetecting)
        {
            ShowStep(0);
        }
    }

    private void ShowStep(int index)
    {
        var jutsu = jutsuManager.CurrentJutsu;
        if (jutsu == null || index < 0 || index >= jutsu.signs.Count) return;

        HandPoseGuideVisual next = jutsu.signs[index].guideVisual;
        if (next == _currentlyShown) return;

        HideCurrent();

        if (next != null)
        {
            next.SetExternalShouldShow(true);
            _currentlyShown = next;
        }
    }

    private void HideCurrent()
    {
        if (_currentlyShown != null)
        {
            _currentlyShown.SetExternalShouldShow(false);
        }
        _currentlyShown = null;
    }
}