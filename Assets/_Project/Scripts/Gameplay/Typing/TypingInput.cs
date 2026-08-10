using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TypingInput : MonoBehaviour
{
    private static readonly Key[] WebGlLetterKeys =
    {
        Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G,
        Key.H, Key.I, Key.J, Key.K, Key.L, Key.M, Key.N,
        Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T, Key.U,
        Key.V, Key.W, Key.X, Key.Y, Key.Z
    };

    private string expectedWord = string.Empty;
    private string normalizedWord = string.Empty;
    private string typedBuffer = string.Empty;
    private int progress;
    private bool completionReported;
    private bool resumeInputAfterSuspension;
    private Keyboard keyboard;
    private float nextBackspaceRepeat;
    private bool backspaceWasDown;
    private bool backspaceTextRequested;

    [SerializeField, Min(0f)] private float backspaceRepeatDelay = 0.35f;
    [SerializeField, Min(0.01f)] private float backspaceRepeatInterval = 0.06f;

    public event Action<char, int> CorrectCharacterEntered;
    public event Action<char, int> IncorrectCharacterEntered;
    public event Action BackspacePerformed;
    public event Action<int, string> ProgressChanged;
    public event Action<string> WordCompleted;

    public string ExpectedWord => expectedWord;
    public string TypedText => typedBuffer;
    public int TypedLength => typedBuffer.Length;
    public int CorrectPrefixLength => progress;
    public bool HasError => typedBuffer.Length > progress;
    public int Progress => progress;
    public bool IsInputEnabled { get; private set; }
    public bool IsSuspended { get; private set; }
    public bool IsComplete => normalizedWord.Length > 0
        && typedBuffer == normalizedWord;

    private void OnEnable()
    {
        keyboard = Keyboard.current;

        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = true;
#endif
        }
        else if (keyboard != null)
        {
            keyboard.onTextInput += HandleTextInput;
        }
    }

    private void OnDisable()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer
            && keyboard != null)
        {
            keyboard.onTextInput -= HandleTextInput;
        }

        keyboard = null;
        ResetBackspaceTracking();
    }

    private void Update()
    {
        RefreshKeyboardSubscription();

        if (Keyboard.current == null || !IsInputEnabled)
        {
            ResetBackspaceTracking();
            return;
        }

        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            PollWebGlLetters(Keyboard.current);
        }

        bool backspaceDown = Keyboard.current.backspaceKey.isPressed;
        bool backspaceStarted =
            (backspaceDown && !backspaceWasDown)
            || Keyboard.current.backspaceKey.wasPressedThisFrame
            || (backspaceTextRequested && !backspaceWasDown);

        if (backspaceStarted)
        {
            ProcessBackspace();
            nextBackspaceRepeat = Time.unscaledTime + backspaceRepeatDelay;
        }
        else if (backspaceDown
            && Time.unscaledTime >= nextBackspaceRepeat)
        {
            ProcessBackspace();
            nextBackspaceRepeat = Time.unscaledTime + backspaceRepeatInterval;
        }

        backspaceWasDown = backspaceDown || backspaceTextRequested;
        backspaceTextRequested = false;
    }

    private void RefreshKeyboardSubscription()
    {
        Keyboard currentKeyboard = Keyboard.current;
        if (currentKeyboard == keyboard)
        {
            return;
        }

        if (Application.platform != RuntimePlatform.WebGLPlayer
            && keyboard != null)
        {
            keyboard.onTextInput -= HandleTextInput;
        }

        keyboard = currentKeyboard;
        if (Application.platform != RuntimePlatform.WebGLPlayer
            && keyboard != null)
        {
            keyboard.onTextInput += HandleTextInput;
        }
    }

    private void PollWebGlLetters(Keyboard currentKeyboard)
    {
        for (int index = 0; index < WebGlLetterKeys.Length; index++)
        {
            if (currentKeyboard[WebGlLetterKeys[index]].wasPressedThisFrame)
            {
                ProcessCharacter((char)('a' + index));
            }
        }
    }

    public void SetExpectedWord(string word, bool enableInput = true)
    {
        string candidate = TypingTextNormalizer.NormalizeForComparison(word);

        if (candidate.Length == 0)
        {
            throw new ArgumentException(
                "La palabra esperada debe contener al menos una letra.",
                nameof(word)
            );
        }

        expectedWord = word;
        normalizedWord = candidate;
        typedBuffer = string.Empty;
        progress = 0;
        completionReported = false;
        resumeInputAfterSuspension = enableInput;
        IsInputEnabled = enableInput && !IsSuspended;
        ProgressChanged?.Invoke(progress, typedBuffer);
    }

    public void SetInputEnabled(bool enabled)
    {
        resumeInputAfterSuspension = enabled && !IsComplete;
        IsInputEnabled = resumeInputAfterSuspension && !IsSuspended;
    }

    public void SuspendInput()
    {
        if (IsSuspended)
        {
            return;
        }

        resumeInputAfterSuspension = IsInputEnabled && !IsComplete;
        IsSuspended = true;
        IsInputEnabled = false;
    }

    public void ResumeInput()
    {
        if (!IsSuspended)
        {
            return;
        }

        IsSuspended = false;
        IsInputEnabled = resumeInputAfterSuspension && !IsComplete;
    }

    public void ProcessCharacter(char character)
    {
        if (!IsInputEnabled || IsComplete)
        {
            return;
        }

        string normalizedCharacter = TypingTextNormalizer
            .NormalizeForComparison(character.ToString());

        if (normalizedCharacter.Length != 1)
        {
            return;
        }

        char enteredCharacter = normalizedCharacter[0];

        typedBuffer += enteredCharacter;
        RecalculateProgress();

        bool isCorrectCharacter = typedBuffer.Length <= normalizedWord.Length
            && progress == typedBuffer.Length;

        if (isCorrectCharacter)
        {
            CorrectCharacterEntered?.Invoke(character, progress);
        }
        else
        {
            IncorrectCharacterEntered?.Invoke(
                character,
                typedBuffer.Length - 1
            );
        }

        ProgressChanged?.Invoke(progress, typedBuffer);

        if (!IsComplete || completionReported)
        {
            return;
        }

        completionReported = true;
        resumeInputAfterSuspension = false;
        IsInputEnabled = false;
        WordCompleted?.Invoke(expectedWord);
    }

    public void ProcessBackspace()
    {
        if (!IsInputEnabled || typedBuffer.Length == 0)
        {
            return;
        }

        typedBuffer = typedBuffer.Substring(0, typedBuffer.Length - 1);
        RecalculateProgress();
        BackspacePerformed?.Invoke();
        ProgressChanged?.Invoke(progress, typedBuffer);
    }

    private void HandleTextInput(char character)
    {
        if (character == '\b' || character == '\u007f')
        {
            backspaceTextRequested = true;
            return;
        }

        ProcessCharacter(character);
    }

    private void ResetBackspaceTracking()
    {
        backspaceWasDown = false;
        backspaceTextRequested = false;
        nextBackspaceRepeat = 0f;
    }

    private void RecalculateProgress()
    {
        int maximumPrefix = Mathf.Min(typedBuffer.Length, normalizedWord.Length);
        progress = 0;

        while (progress < maximumPrefix
            && typedBuffer[progress] == normalizedWord[progress])
        {
            progress++;
        }
    }
}
