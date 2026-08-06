using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum GameplayAudioCue
{
    None,
    CorrectLetter,
    Error,
    WordCompleted,
    KitchenAction,
    DishServed,
    CatReaction,
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
    [SerializeField] private AudioClip correctLetterClip;
    [SerializeField] private AudioClip errorClip;
    [SerializeField] private AudioClip wordCompletedClip;
    [SerializeField] private AudioClip kitchenActionClip;
    [SerializeField] private AudioClip dishServedClip;
    [SerializeField] private AudioClip catReactionClip;
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

    public GameplayAudioCue LastCue { get; private set; }
        = GameplayAudioCue.None;
    public int PlayedCueCount { get; private set; }
    public bool HasCompleteFeedbackSet => !ReferenceEquals(correctLetterClip, null)
        && !ReferenceEquals(errorClip, null)
        && !ReferenceEquals(wordCompletedClip, null)
        && !ReferenceEquals(kitchenActionClip, null)
        && !ReferenceEquals(dishServedClip, null)
        && !ReferenceEquals(catReactionClip, null)
        && !ReferenceEquals(finalPurrClip, null)
        && !ReferenceEquals(kitchenAmbienceClip, null);
    public int AvailableClipCount
    {
        get
        {
            int count = 0;
            count += !ReferenceEquals(correctLetterClip, null) ? 1 : 0;
            count += !ReferenceEquals(errorClip, null) ? 1 : 0;
            count += !ReferenceEquals(wordCompletedClip, null) ? 1 : 0;
            count += !ReferenceEquals(kitchenActionClip, null) ? 1 : 0;
            count += !ReferenceEquals(dishServedClip, null) ? 1 : 0;
            count += !ReferenceEquals(catReactionClip, null) ? 1 : 0;
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
        typingInput.WordCompleted += HandleWordCompleted;
        recipeRunner.StepCompleted += HandleStepCompleted;

        if (gameFlow != null)
        {
            gameFlow.GameStarted += HandleGameStarted;
            gameFlow.DishCompleted += HandleDishCompleted;
            gameFlow.BanquetCompleted += HandleBanquetCompleted;
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
            typingInput.WordCompleted -= HandleWordCompleted;
        }

        if (recipeRunner != null)
        {
            recipeRunner.StepCompleted -= HandleStepCompleted;
        }

        if (gameFlow != null)
        {
            gameFlow.GameStarted -= HandleGameStarted;
            gameFlow.DishCompleted -= HandleDishCompleted;
            gameFlow.BanquetCompleted -= HandleBanquetCompleted;
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
        PlayOneShot(
            GameplayAudioCue.CorrectLetter,
            correctLetterClip,
            letterVolume
        );
    }

    private void HandleIncorrectCharacter(char _, int __)
    {
        PlayOneShot(GameplayAudioCue.Error, errorClip, eventVolume);
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
            && (step.ReactionType == KitchenReactionType.Serving
                || string.Equals(
                    step.ExpectedWord?.Trim(),
                    "servir",
                    StringComparison.OrdinalIgnoreCase
                ));
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

    private void HandleBanquetCompleted()
    {
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
        correctLetterClip ??= CreateTone("Fallback Correct", 760f, 0.045f, 0.12f);
        errorClip ??= CreateTone("Fallback Error", 180f, 0.12f, 0.22f);
        wordCompletedClip ??= CreateTone("Fallback Word", 980f, 0.16f, 0.18f);
        kitchenActionClip ??= CreateTone("Fallback Action", 420f, 0.18f, 0.2f);
        dishServedClip ??= CreateTone("Fallback Dish", 620f, 0.28f, 0.24f);
        catReactionClip ??= CreateTone("Fallback Cat", 300f, 0.32f, 0.2f);
        finalPurrClip ??= CreatePurr("Fallback Purr", 1.2f, 0.18f);
        kitchenAmbienceClip ??= CreateAmbience("Fallback Kitchen", 2f, 0.05f);
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
