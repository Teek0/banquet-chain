using System.Collections;
using TMPro;
using UnityEngine;

public sealed class WordBubbleUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TypingInput typingInput;
    [SerializeField] private RecipeRunner recipeRunner;
    [SerializeField] private GameFlow gameFlow;
    [SerializeField] private TMP_Text stateLabel;
    [SerializeField] private TMP_Text wordLabel;
    [SerializeField] private TMP_Text feedbackLabel;
    [SerializeField] private PaperWordRenderer paperWordRenderer;

    [Header("Demo")]
    [SerializeField] private string initialWord = "mantequilla";
    [SerializeField, Min(0)] private int revealedRecipeCount = 1;
    [SerializeField, Min(0f)] private float hintDelaySeconds = 8f;

    [Header("Feedback")]
    [SerializeField] private float completionDuration = 0.8f;
    [SerializeField] private Color activeColor = new(1f, 0.82f, 0.28f);
    [SerializeField] private Color completedColor = new(0.42f, 0.9f, 0.58f);
    [SerializeField] private Color pendingColor = new(0.65f, 0.65f, 0.65f);
    [SerializeField] private Color errorColor = new(1f, 0.38f, 0.32f);
    [SerializeField] private Color successColor = new(0.42f, 0.9f, 0.58f);

    private Coroutine feedbackRoutine;
    private bool revealExpectedWord = true;
    private bool hintCountdownActive;
    private float hintRevealTime;

    private void Awake()
    {
        recipeRunner ??= FindFirstObjectByType<RecipeRunner>();
        typingInput ??= recipeRunner != null
            ? recipeRunner.GetComponent<TypingInput>()
            : FindFirstObjectByType<TypingInput>();

        if (paperWordRenderer == null && wordLabel != null)
        {
            paperWordRenderer = wordLabel.GetComponent<PaperWordRenderer>();
        }

        if (typingInput == null)
        {
            Debug.LogError(
                "WordBubbleUI necesita TypingInput para controlar la escritura.",
                this
            );
        }
    }

    private void OnEnable()
    {
        recipeRunner ??= FindFirstObjectByType<RecipeRunner>();
        gameFlow ??= FindFirstObjectByType<GameFlow>();

        if (typingInput == null)
        {
            return;
        }

        typingInput.CorrectCharacterEntered += HandleCorrectCharacter;
        typingInput.IncorrectCharacterEntered += HandleIncorrectCharacter;
        typingInput.ProgressChanged += HandleProgressChanged;
        typingInput.WordCompleted += HandleWordCompleted;

        if (gameFlow != null)
        {
            gameFlow.RecipeActivated += HandleRecipeActivated;
        }

        if (recipeRunner != null)
        {
            recipeRunner.StepStarted += HandleStepStarted;
        }
    }

    private void Start()
    {
        if (typingInput != null
            && string.IsNullOrWhiteSpace(typingInput.ExpectedWord)
            && !string.IsNullOrWhiteSpace(initialWord))
        {
            SetWord(initialWord);
        }
    }

    private void Update()
    {
        if (hintCountdownActive
            && typingInput != null
            && typingInput.IsInputEnabled
            && !typingInput.IsComplete
            && Time.unscaledTime >= hintRevealTime)
        {
            hintCountdownActive = false;
            revealExpectedWord = true;
            RefreshWord(typingInput.Progress);
        }

        if (paperWordRenderer != null && typingInput != null)
        {
            paperWordRenderer.SetCaretActive(
                typingInput.IsInputEnabled && !typingInput.IsComplete
            );
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

        if (gameFlow != null)
        {
            gameFlow.RecipeActivated -= HandleRecipeActivated;
        }

        if (recipeRunner != null)
        {
            recipeRunner.StepStarted -= HandleStepStarted;
        }

        StopFeedbackRoutine();
        hintCountdownActive = false;
    }

    public void SetWord(string word, bool enableInput = true)
    {
        if (typingInput == null)
        {
            Debug.LogError("WordBubbleUI no tiene TypingInput asignado.", this);
            return;
        }

        StopFeedbackRoutine();
        ClearFeedbackText();
        SetState(">> ESCRIBE", activeColor);
        typingInput.SetExpectedWord(word, enableInput);
        revealExpectedWord = true;
        hintCountdownActive = false;
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
            ClearFeedbackText();
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
        hintCountdownActive = false;
        RefreshWord(typingInput.Progress);
        SetState("OK · LISTO", successColor);
        ShowTemporaryFeedback(
            "Palabra completada",
            successColor,
            completionDuration
        );
    }

    private void HandleRecipeActivated(int recipeIndex, RecipeData _)
    {
        revealExpectedWord = recipeIndex < revealedRecipeCount;
        hintCountdownActive = false;
        RefreshWord(typingInput != null ? typingInput.Progress : 0);
    }

    private void HandleStepStarted(RecipeStep step, int _)
    {
        int recipeIndex = gameFlow != null ? gameFlow.CurrentRecipeIndex : 0;
        revealExpectedWord = ShouldRevealExpectedWord(recipeIndex, step);
        hintCountdownActive = !revealExpectedWord;
        hintRevealTime = Time.unscaledTime + hintDelaySeconds;
        RefreshWord(typingInput != null ? typingInput.Progress : 0);
    }

    private bool ShouldRevealExpectedWord(int recipeIndex, RecipeStep step)
    {
        if (recipeIndex < revealedRecipeCount || step == null)
        {
            return true;
        }

        WorldRecipeBubbleUI[] bubbles = FindObjectsByType<WorldRecipeBubbleUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (WorldRecipeBubbleUI bubble in bubbles)
        {
            if (bubble != null && bubble.CanPresentStep(step))
            {
                return false;
            }
        }

        WorldSpriteRecipeBubbleUI[] spriteBubbles =
            FindObjectsByType<WorldSpriteRecipeBubbleUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (WorldSpriteRecipeBubbleUI bubble in spriteBubbles)
        {
            if (bubble != null && bubble.CanPresentStep(step))
            {
                return false;
            }
        }

        return true;
    }

    private void RefreshWord(int progress)
    {
        if (typingInput == null)
        {
            return;
        }

        string displayedWord = typingInput.ExpectedWord ?? string.Empty;
        int safeProgress = Mathf.Clamp(progress, 0, displayedWord.Length);

        string typed = typingInput.TypedText.ToUpperInvariant();
        string paperWord = revealExpectedWord
            ? typed + displayedWord.Substring(
                Mathf.Min(typed.Length, displayedWord.Length)
            )
            : typed;
        if (paperWordRenderer != null
            && paperWordRenderer.RenderWord(
                paperWord,
                typingInput.CorrectPrefixLength,
                typed.Length
            ))
        {
            paperWordRenderer.SetCaretActive(
                typingInput.IsInputEnabled && !typingInput.IsComplete
            );
            return;
        }

        if (wordLabel == null)
        {
            return;
        }

        string completed = EscapeRichText(displayedWord.Substring(0, safeProgress));
        string typedRemainder = typingInput.HasError
            ? EscapeRichText(typed.Substring(Mathf.Min(safeProgress, typed.Length)))
            : string.Empty;
        string pending = revealExpectedWord
            ? EscapeRichText(displayedWord.Substring(safeProgress))
            : string.Empty;

        string completedHex = ColorUtility.ToHtmlStringRGB(completedColor);
        string pendingHex = ColorUtility.ToHtmlStringRGB(pendingColor);
        string errorHex = ColorUtility.ToHtmlStringRGB(errorColor);

        string typedMarkup = typedRemainder.Length > 0
            ? $"<color=#{errorHex}><b>{typedRemainder}</b></color>"
            : string.Empty;
        string pendingMarkup = pending.Length > 0
            ? $"<color=#{pendingHex}><b>[</b>{pending}<b>]</b></color>"
            : string.Empty;

        wordLabel.text = $"<color=#{completedHex}><b><u>{completed}</u></b></color>"
            + typedMarkup + pendingMarkup;
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
        ClearFeedbackText();
        feedbackRoutine = null;
    }

    private void ClearFeedbackText()
    {
        if (feedbackLabel != null)
        {
            feedbackLabel.text = string.Empty;
        }
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
