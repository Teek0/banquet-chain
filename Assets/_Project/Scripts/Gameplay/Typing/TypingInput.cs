using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TypingInput : MonoBehaviour
{
    private string expectedWord = string.Empty;
    private string normalizedWord = string.Empty;
    private int progress;
    private bool completionReported;
    private Keyboard keyboard;

    public event Action<char, int> CorrectCharacterEntered;
    public event Action<char, int> IncorrectCharacterEntered;
    public event Action<int, string> ProgressChanged;
    public event Action<string> WordCompleted;

    public string ExpectedWord => expectedWord;
    public int Progress => progress;
    public bool IsInputEnabled { get; private set; }
    public bool IsComplete => normalizedWord.Length > 0
        && progress >= normalizedWord.Length;

    private void OnEnable()
    {
        keyboard = Keyboard.current;

        if (keyboard != null)
        {
            keyboard.onTextInput += HandleTextInput;
        }
    }

    private void OnDisable()
    {
        if (keyboard != null)
        {
            keyboard.onTextInput -= HandleTextInput;
        }

        keyboard = null;
    }

    private void Update()
    {
        if (Keyboard.current != null
            && Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            ProcessBackspace();
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
        progress = 0;
        completionReported = false;
        IsInputEnabled = enableInput;
        ProgressChanged?.Invoke(progress, GetTypedPrefix());
    }

    public void SetInputEnabled(bool enabled)
    {
        IsInputEnabled = enabled && !IsComplete;
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

        if (enteredCharacter != normalizedWord[progress])
        {
            IncorrectCharacterEntered?.Invoke(character, progress);
            return;
        }

        progress++;
        CorrectCharacterEntered?.Invoke(character, progress);
        ProgressChanged?.Invoke(progress, GetTypedPrefix());

        if (!IsComplete || completionReported)
        {
            return;
        }

        completionReported = true;
        IsInputEnabled = false;
        WordCompleted?.Invoke(expectedWord);
    }

    public void ProcessBackspace()
    {
        if (!IsInputEnabled || progress <= 0)
        {
            return;
        }

        progress--;
        ProgressChanged?.Invoke(progress, GetTypedPrefix());
    }

    private void HandleTextInput(char character)
    {
        ProcessCharacter(character);
    }

    private string GetTypedPrefix()
    {
        return normalizedWord.Substring(0, progress);
    }
}
