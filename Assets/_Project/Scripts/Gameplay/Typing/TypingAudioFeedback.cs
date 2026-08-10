using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class TypingAudioFeedback : MonoBehaviour
{
    [SerializeField] private TypingInput typingInput;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] typingKeyClips = new AudioClip[0];
    [SerializeField] private AudioClip[] backspaceClips = new AudioClip[0];
    [SerializeField, Range(0f, 1f)] private float volume = 0.18f;
    [SerializeField, Min(0f)] private float minimumKeyInterval = 0.035f;

    private int lastTypingClipIndex = -1;
    private int lastBackspaceClipIndex = -1;
    private float nextTypingSoundTime;

    private void Awake()
    {
        if (typingInput == null)
        {
            typingInput = GetComponent<TypingInput>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void OnEnable()
    {
        if (typingInput == null)
        {
            typingInput = GetComponent<TypingInput>();
        }

        if (typingInput == null)
        {
            return;
        }

        typingInput.CorrectCharacterEntered += HandleCharacterEntered;
        typingInput.IncorrectCharacterEntered += HandleCharacterEntered;
        typingInput.BackspacePerformed += HandleBackspace;
    }

    private void OnDisable()
    {
        if (typingInput == null)
        {
            return;
        }

        typingInput.CorrectCharacterEntered -= HandleCharacterEntered;
        typingInput.IncorrectCharacterEntered -= HandleCharacterEntered;
        typingInput.BackspacePerformed -= HandleBackspace;
    }

    private void HandleCharacterEntered(char character, int position)
    {
        if (Time.unscaledTime < nextTypingSoundTime)
        {
            return;
        }

        nextTypingSoundTime = Time.unscaledTime + minimumKeyInterval;
        PlayRandom(typingKeyClips, ref lastTypingClipIndex);
    }

    private void HandleBackspace()
    {
        PlayRandom(backspaceClips, ref lastBackspaceClipIndex);
    }

    private void PlayRandom(AudioClip[] clips, ref int lastClipIndex)
    {
        if (audioSource == null || clips == null || clips.Length == 0)
        {
            return;
        }

        int clipIndex = Random.Range(0, clips.Length);
        if (clips.Length > 1 && clipIndex == lastClipIndex)
        {
            clipIndex = (clipIndex + Random.Range(1, clips.Length)) % clips.Length;
        }

        AudioClip clip = clips[clipIndex];
        if (clip == null)
        {
            return;
        }

        lastClipIndex = clipIndex;
        audioSource.PlayOneShot(clip, volume);
    }
}
