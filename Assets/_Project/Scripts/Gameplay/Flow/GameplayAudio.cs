using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum GameplayAudioCue
{
    None,
    CorrectLetter,
    Backspace,
    Error,
    WordCompleted,
    KitchenAction,
    DishServed,
    CatReaction,
    BubblePop,
    FinalCelebration,
    FinalPurr
}

public sealed class GameplayAudio : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private TypingInput typingInput;
    [SerializeField] private RecipeRunner recipeRunner;
    [SerializeField] private GameFlow gameFlow;

    [Header("Sources and routing")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource purrSource;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup musicGroup;

    [Header("Replaceable clips")]
    [SerializeField] private AudioClip[] typingKeyClips =
        System.Array.Empty<AudioClip>();
    [SerializeField] private AudioClip[] backspaceClips =
        System.Array.Empty<AudioClip>();
    [SerializeField] private AudioClip errorClip;
    [SerializeField] private AudioClip wordCompletedClip;
    [SerializeField] private AudioClip kitchenActionClip;
    [SerializeField] private AudioClip dishServedClip;
    [SerializeField] private AudioClip catReactionClip;
    [SerializeField] private AudioClip bubblePopClip;
    [SerializeField] private AudioClip finalCelebrationClip;
    [SerializeField] private AudioClip finalPurrClip;
    [SerializeField] private AudioClip kitchenAmbienceClip;

    [Header("Balance")]
    [SerializeField, Range(0f, 1f)] private float letterVolume = 0.18f;
    [SerializeField, Range(0f, 1f)] private float eventVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.24f;
    [SerializeField, Min(0f)] private float minimumLetterInterval = 0.035f;

    private readonly List<AudioClip> generatedClips = new();
    private bool isSubscribed;
    private float lastLetterTime = float.NegativeInfinity;
    private int lastTypingKeyIndex = -1;
    private int lastBackspaceIndex = -1;

    public GameplayAudioCue LastCue { get; private set; }
        = GameplayAudioCue.None;
    public int PlayedCueCount { get; private set; }
    public bool HasCompleteFeedbackSet => HasAnyClip(typingKeyClips)
        && HasAnyClip(backspaceClips)
        && !ReferenceEquals(errorClip, null)
        && !ReferenceEquals(wordCompletedClip, null)
        && !ReferenceEquals(kitchenActionClip, null)
        && !ReferenceEquals(dishServedClip, null)
        && !ReferenceEquals(catReactionClip, null)
        && !ReferenceEquals(bubblePopClip, null)
        && !ReferenceEquals(finalCelebrationClip, null)
        && !ReferenceEquals(finalPurrClip, null)
        && !ReferenceEquals(kitchenAmbienceClip, null);
    public int AvailableClipCount
    {
        get
        {
            int count = 0;
            count += HasAnyClip(typingKeyClips) ? 1 : 0;
            count += HasAnyClip(backspaceClips) ? 1 : 0;
            count += !ReferenceEquals(errorClip, null) ? 1 : 0;
            count += !ReferenceEquals(wordCompletedClip, null) ? 1 : 0;
            count += !ReferenceEquals(kitchenActionClip, null) ? 1 : 0;
            count += !ReferenceEquals(dishServedClip, null) ? 1 : 0;
            count += !ReferenceEquals(catReactionClip, null) ? 1 : 0;
            count += !ReferenceEquals(bubblePopClip, null) ? 1 : 0;
            count += !ReferenceEquals(finalCelebrationClip, null) ? 1 : 0;
            count += !ReferenceEquals(finalPurrClip, null) ? 1 : 0;
            count += !ReferenceEquals(kitchenAmbienceClip, null) ? 1 : 0;
            return count;
        }
    }

    private void Awake()
    {
        Prepare();
    }

    private void OnEnable()
    {
        Prepare();
        Subscribe();
        StartAmbience();
    }

    public void Prepare()
    {
        ResolveReferences();
        CreateFallbackClips();
        ConfigureSources();
    }

    private void OnDisable()
    {
        Unsubscribe();
        StopFinalPurr();
    }

    private void OnDestroy()
    {
        foreach (AudioClip clip in generatedClips)
        {
            if (clip != null)
            {
                Destroy(clip);
            }
        }

        generatedClips.Clear();
    }

    private void ResolveReferences()
    {
        if (typingInput == null)
        {
            typingInput = FindFirstObjectByType<TypingInput>();
        }

        if (recipeRunner == null)
        {
            recipeRunner = FindFirstObjectByType<RecipeRunner>();
        }

        if (gameFlow == null)
        {
            gameFlow = FindFirstObjectByType<GameFlow>();
        }

        AudioSource[] sources = GetComponents<AudioSource>();

        if (sfxSource == null && sources.Length > 0)
        {
            sfxSource = sources[0];
        }

        if (musicSource == null && sources.Length > 1)
        {
            musicSource = sources[1];
        }

        if (purrSource == null && sources.Length > 2)
        {
            purrSource = sources[2];
        }
    }

    private void ConfigureSources()
    {
        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.outputAudioMixerGroup = sfxGroup;
        }

        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = ambienceVolume;
            musicSource.outputAudioMixerGroup = musicGroup;
        }

        if (purrSource != null)
        {
            purrSource.playOnAwake = false;
            purrSource.loop = true;
            purrSource.volume = eventVolume;
            purrSource.outputAudioMixerGroup = sfxGroup;
        }
    }

    private void Subscribe()
    {
        if (isSubscribed || typingInput == null || recipeRunner == null)
        {
            return;
        }

        typingInput.CorrectCharacterEntered += HandleCorrectCharacter;
        typingInput.IncorrectCharacterEntered += HandleIncorrectCharacter;
        typingInput.BackspacePerformed += HandleBackspace;
        typingInput.WordCompleted += HandleWordCompleted;
        recipeRunner.StepCompleted += HandleStepCompleted;
        WorldSpriteRecipeBubbleUI.BubbleShown += HandleBubbleShown;

        if (gameFlow != null)
        {
            gameFlow.GameStarted += HandleGameStarted;
            gameFlow.DishCompleted += HandleDishCompleted;
            gameFlow.StateChanged += HandleGameFlowStateChanged;
        }

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (typingInput != null)
        {
            typingInput.CorrectCharacterEntered -= HandleCorrectCharacter;
            typingInput.IncorrectCharacterEntered -= HandleIncorrectCharacter;
            typingInput.BackspacePerformed -= HandleBackspace;
            typingInput.WordCompleted -= HandleWordCompleted;
        }

        if (recipeRunner != null)
        {
            recipeRunner.StepCompleted -= HandleStepCompleted;
        }

        WorldSpriteRecipeBubbleUI.BubbleShown -= HandleBubbleShown;

        if (gameFlow != null)
        {
            gameFlow.GameStarted -= HandleGameStarted;
            gameFlow.DishCompleted -= HandleDishCompleted;
            gameFlow.StateChanged -= HandleGameFlowStateChanged;
        }

        isSubscribed = false;
    }

    private void HandleCorrectCharacter(char _, int __)
    {
        float now = Time.unscaledTime;

        if (now - lastLetterTime < minimumLetterInterval)
        {
            return;
        }

        lastLetterTime = now;
        PlayRandomOneShot(
            GameplayAudioCue.CorrectLetter,
            typingKeyClips,
            letterVolume,
            ref lastTypingKeyIndex
        );
    }

    private void HandleIncorrectCharacter(char _, int __)
    {
        PlayRandomOneShot(
            GameplayAudioCue.CorrectLetter,
            typingKeyClips,
            letterVolume,
            ref lastTypingKeyIndex
        );
        PlayOneShot(GameplayAudioCue.Error, errorClip, eventVolume);
    }

    private void HandleBackspace()
    {
        PlayRandomOneShot(
            GameplayAudioCue.Backspace,
            backspaceClips,
            letterVolume,
            ref lastBackspaceIndex
        );
    }

    private void HandleWordCompleted(string _)
    {
        PlayOneShot(
            GameplayAudioCue.WordCompleted,
            wordCompletedClip,
            eventVolume
        );
    }

    private void HandleStepCompleted(RecipeStep step, int _)
    {
        bool isServing = step != null
            && step.ReactionType == KitchenReactionType.Serving;
        PlayOneShot(
            isServing
                ? GameplayAudioCue.DishServed
                : GameplayAudioCue.KitchenAction,
            isServing ? dishServedClip : kitchenActionClip,
            eventVolume
        );
    }

    private void HandleGameStarted()
    {
        StopFinalPurr();
        StartAmbience();
    }

    private void HandleDishCompleted(int _, RecipeData __)
    {
        PlayOneShot(
            GameplayAudioCue.CatReaction,
            catReactionClip,
            eventVolume
        );
    }

    private void HandleBubbleShown()
    {
        PlayOneShot(
            GameplayAudioCue.BubblePop,
            bubblePopClip,
            eventVolume
        );
    }

    private void HandleGameFlowStateChanged(GameFlowState state)
    {
        if (state == GameFlowState.FinalCelebration)
        {
            PlayOneShot(
                GameplayAudioCue.FinalCelebration,
                finalCelebrationClip,
                eventVolume
            );
            return;
        }

        if (state != GameFlowState.FinalSleeping)
        {
            return;
        }

        LastCue = GameplayAudioCue.FinalPurr;
        PlayedCueCount++;

        if (purrSource == null || finalPurrClip == null)
        {
            return;
        }

        purrSource.clip = finalPurrClip;
        purrSource.Play();
    }

    private void PlayOneShot(
        GameplayAudioCue cue,
        AudioClip clip,
        float volume
    )
    {
        LastCue = cue;
        PlayedCueCount++;

        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }

    private void PlayRandomOneShot(
        GameplayAudioCue cue,
        AudioClip[] clips,
        float volume,
        ref int lastIndex
    )
    {
        AudioClip clip = SelectRandomClip(clips, ref lastIndex);
        PlayOneShot(cue, clip, volume);
    }

    private static AudioClip SelectRandomClip(
        AudioClip[] clips,
        ref int lastIndex
    )
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        int validCount = 0;
        foreach (AudioClip clip in clips)
        {
            if (clip != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        int target = UnityEngine.Random.Range(0, validCount);
        int selectedIndex = -1;
        for (int index = 0; index < clips.Length; index++)
        {
            if (clips[index] == null)
            {
                continue;
            }

            if (target-- == 0)
            {
                selectedIndex = index;
                break;
            }
        }

        if (validCount > 1 && selectedIndex == lastIndex)
        {
            do
            {
                selectedIndex = (selectedIndex + 1) % clips.Length;
            }
            while (clips[selectedIndex] == null);
        }

        lastIndex = selectedIndex;
        return clips[selectedIndex];
    }

    private void StartAmbience()
    {
        if (musicSource == null
            || kitchenAmbienceClip == null
            || musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = kitchenAmbienceClip;
        musicSource.Play();
    }

    private void StopFinalPurr()
    {
        if (purrSource != null)
        {
            purrSource.Stop();
            purrSource.clip = null;
        }
    }

    private void CreateFallbackClips()
    {
        if (!HasAnyClip(typingKeyClips))
        {
            typingKeyClips = new[]
            {
                CreateTone("Fallback Correct", 760f, 0.045f, 0.12f)
            };
        }

        if (!HasAnyClip(backspaceClips))
        {
            backspaceClips = new[]
            {
                CreateTone("Fallback Backspace", 520f, 0.055f, 0.1f)
            };
        }

        errorClip ??= CreateTone("Fallback Error", 180f, 0.12f, 0.22f);
        wordCompletedClip ??= CreateTone("Fallback Word", 980f, 0.16f, 0.18f);
        kitchenActionClip ??= CreateTone("Fallback Action", 420f, 0.18f, 0.2f);
        dishServedClip ??= CreateTone("Fallback Dish", 620f, 0.28f, 0.24f);
        catReactionClip ??= CreateTone("Fallback Cat", 300f, 0.32f, 0.2f);
        bubblePopClip ??= CreateTone("Fallback Bubble", 720f, 0.1f, 0.18f);
        finalCelebrationClip ??= CreateTone(
            "Fallback Final Celebration",
            880f,
            0.45f,
            0.2f
        );
        finalPurrClip ??= CreatePurr("Fallback Purr", 1.2f, 0.18f);
        kitchenAmbienceClip ??= CreateAmbience("Fallback Kitchen", 2f, 0.05f);
    }

    private static bool HasAnyClip(AudioClip[] clips)
    {
        if (clips == null)
        {
            return false;
        }

        foreach (AudioClip clip in clips)
        {
            if (clip != null)
            {
                return true;
            }
        }

        return false;
    }

    private AudioClip CreateTone(
        string clipName,
        float frequency,
        float duration,
        float amplitude
    )
    {
        return CreateGeneratedClip(
            clipName,
            duration,
            time => Mathf.Sin(time * frequency * Mathf.PI * 2f)
                * amplitude
                * Mathf.Clamp01(1f - time / duration)
        );
    }

    private AudioClip CreatePurr(string clipName, float duration, float amplitude)
    {
        return CreateGeneratedClip(
            clipName,
            duration,
            time => (
                Mathf.Sin(time * 52f * Mathf.PI * 2f)
                + Mathf.Sin(time * 78f * Mathf.PI * 2f) * 0.45f
            ) * amplitude * 0.55f
        );
    }

    private AudioClip CreateAmbience(
        string clipName,
        float duration,
        float amplitude
    )
    {
        return CreateGeneratedClip(
            clipName,
            duration,
            time => (
                Mathf.Sin(time * 110f * Mathf.PI * 2f)
                + Mathf.Sin(time * 165f * Mathf.PI * 2f) * 0.4f
            ) * amplitude
        );
    }

    private AudioClip CreateGeneratedClip(
        string clipName,
        float duration,
        Func<float, float> sample
    )
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * sampleRate));
        float[] samples = new float[sampleCount];

        for (int index = 0; index < sampleCount; index++)
        {
            samples[index] = sample((float)index / sampleRate);
        }

        AudioClip clip = AudioClip.Create(
            clipName,
            sampleCount,
            1,
            sampleRate,
            false
        );
        clip.SetData(samples, 0);
        generatedClips.Add(clip);
        return clip;
    }
}
