using System.Collections;
using TMPro;
using UnityEngine;

public sealed class WordBubbleUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TypingInput typingInput;
    [SerializeField] private TMP_Text stateLabel;
    [SerializeField] private TMP_Text wordLabel;
    [SerializeField] private TMP_Text feedbackLabel;

    [Header("Demo")]
    [SerializeField] private string initialWord = "mantequilla";

    [Header("Feedback")]
    [SerializeField] private float errorDuration = 0.35f;
    [SerializeField] private float completionDuration = 0.8f;
    [SerializeField] private Color activeColor = new(1f, 0.82f, 0.28f);
    [SerializeField] private Color completedColor = new(0.42f, 0.9f, 0.58f);
    [SerializeField] private Color pendingColor = Color.white;
    [SerializeField] private Color errorColor = new(1f, 0.38f, 0.32f);
    [SerializeField] private Color successColor = new(0.42f, 0.9f, 0.58f);

    private Coroutine feedbackRoutine;

    private void Awake()
    {
        if (typingInput == null
            || stateLabel == null
            || wordLabel == null
            || feedbackLabel == null)
        {
            Debug.LogError(
                "WordBubbleUI necesita TypingInput y sus tres textos asignados.",
                this
            );
        }
    }

    private void OnEnable()
    {
        if (typingInput == null)
        {
            return;
        }

        typingInput.CorrectCharacterEntered += HandleCorrectCharacter;
        typingInput.IncorrectCharacterEntered += HandleIncorrectCharacter;
        typingInput.ProgressChanged += HandleProgressChanged;
        typingInput.WordCompleted += HandleWordCompleted;
    }

    private void Start()
    {
        if (typingInput != null && !string.IsNullOrWhiteSpace(initialWord))
        {
            SetWord(initialWord);
        }
    }

    private void OnDisable()
    {
        if (typingInput != null)
        {
            typingInput.CorrectCharacterEntered -= HandleCorrectCharacter;
            typingInput.IncorrectCharacterEntered -= HandleIncorrectCharacter;
            typingInput.ProgressChanged -= HandleProgressChanged;
            typingInput.WordCompleted -= HandleWordCompleted;
        }

        StopFeedbackRoutine();
    }

    public void SetWord(string word, bool enableInput = true)
    {
        if (typingInput == null)
        {
            Debug.LogError("WordBubbleUI no tiene TypingInput asignado.", this);
            return;
        }

        StopFeedbackRoutine();
        feedbackLabel.text = string.Empty;
        SetState(">> ESCRIBE", activeColor);
        typingInput.SetExpectedWord(word, enableInput);
        RefreshWord(typingInput.Progress);
    }

    private void HandleCorrectCharacter(char _, int progress)
    {
        if (typingInput != null && !typingInput.HasError)
        {
            SetState(">> ESCRIBE", activeColor);
        }

        RefreshWord(progress);
    }

    private void HandleIncorrectCharacter(char _, int __)
    {
        ShowPersistentError();
    }

    private void HandleProgressChanged(int progress, string _)
    {
        if (typingInput != null && typingInput.HasError)
        {
            ShowPersistentError();
        }
        else if (typingInput != null && !typingInput.IsComplete)
        {
            StopFeedbackRoutine();
            feedbackLabel.text = string.Empty;
            SetState(">> ESCRIBE", activeColor);
        }

        RefreshWord(progress);
    }

    private void ShowPersistentError()
    {
        if (typingInput == null || feedbackLabel == null)
        {
            return;
        }

        StopFeedbackRoutine();
        SetState("! CORRIGE", errorColor);
        feedbackLabel.text = $"ESCRITO: {typingInput.TypedText.ToUpperInvariant()}"
            + " · BACKSPACE PARA CORREGIR";
        feedbackLabel.color = errorColor;
    }

    private void HandleWordCompleted(string _)
    {
        RefreshWord(typingInput.Progress);
        SetState("OK · LISTO", successColor);
        ShowTemporaryFeedback(
            "Palabra completada",
            successColor,
            completionDuration
        );
    }

    private void RefreshWord(int progress)
    {
        if (typingInput == null || wordLabel == null)
        {
            return;
        }

        string displayedWord = typingInput.ExpectedWord ?? string.Empty;
        int safeProgress = Mathf.Clamp(progress, 0, displayedWord.Length);
        string completed = EscapeRichText(
            displayedWord.Substring(0, safeProgress)
        );
        string pending = EscapeRichText(
            displayedWord.Substring(safeProgress)
        );

        string completedHex = ColorUtility.ToHtmlStringRGB(completedColor);
        string pendingHex = ColorUtility.ToHtmlStringRGB(pendingColor);

        string pendingMarkup = pending.Length > 0
            ? $"<color=#{pendingHex}><b>[</b>{pending}<b>]</b></color>"
            : string.Empty;

        wordLabel.text = $"<color=#{completedHex}><b><u>{completed}</u></b></color>"
            + pendingMarkup;
    }

    private void SetState(string message, Color color)
    {
        if (stateLabel == null)
        {
            return;
        }

        stateLabel.text = message;
        stateLabel.color = color;
    }

    private void ShowTemporaryFeedback(
        string message,
        Color color,
        float duration
    )
    {
        if (feedbackLabel == null)
        {
            return;
        }

        StopFeedbackRoutine();
        feedbackLabel.text = message;
        feedbackLabel.color = color;
        feedbackRoutine = StartCoroutine(ClearFeedbackAfter(duration));
    }

    private IEnumerator ClearFeedbackAfter(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        feedbackLabel.text = string.Empty;
        feedbackRoutine = null;
    }

    private void StopFeedbackRoutine()
    {
        if (feedbackRoutine == null)
        {
            return;
        }

        StopCoroutine(feedbackRoutine);
        feedbackRoutine = null;
    }

    private static string EscapeRichText(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
