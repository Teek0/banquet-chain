using TMPro;
using UnityEngine;

public enum TypingTutorialStage
{
    Hidden,
    Intro,
    CorrectLetter,
    ErrorExplained,
    WordCompleted,
    Practice,
    Completed
}

public sealed class TypingTutorial : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private RecipeRunner recipeRunner;
    [SerializeField] private TypingInput typingInput;
    [SerializeField] private RecipeData tutorialRecipe;

    [Header("Presentation")]
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TMP_Text messageLabel;

    private bool isSubscribed;
    private bool isTutorialRecipeActive;

    public TypingTutorialStage Stage { get; private set; }
        = TypingTutorialStage.Hidden;
    public bool IsVisible => panel != null && panel.alpha > 0.01f;
    public string Message => messageLabel != null
        ? messageLabel.text
        : string.Empty;

    private void Awake()
    {
        ResolveReferences();
        Hide(TypingTutorialStage.Hidden);
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void ResolveReferences()
    {
        if (recipeRunner == null)
        {
            recipeRunner = FindFirstObjectByType<RecipeRunner>();
        }

        if (typingInput == null && recipeRunner != null)
        {
            typingInput = recipeRunner.GetComponent<TypingInput>();
        }

        if (panel == null)
        {
            panel = GetComponent<CanvasGroup>();
        }

        if (messageLabel == null)
        {
            messageLabel = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void Subscribe()
    {
        if (isSubscribed || recipeRunner == null || typingInput == null)
        {
            return;
        }

        recipeRunner.RecipeStarted += HandleRecipeStarted;
        recipeRunner.RecipeCompleted += HandleRecipeCompleted;
        recipeRunner.StepStarted += HandleStepStarted;
        typingInput.CorrectCharacterEntered += HandleCorrectCharacter;
        typingInput.IncorrectCharacterEntered += HandleIncorrectCharacter;
        typingInput.WordCompleted += HandleWordCompleted;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (recipeRunner != null)
        {
            recipeRunner.RecipeStarted -= HandleRecipeStarted;
            recipeRunner.RecipeCompleted -= HandleRecipeCompleted;
            recipeRunner.StepStarted -= HandleStepStarted;
        }

        if (typingInput != null)
        {
            typingInput.CorrectCharacterEntered -= HandleCorrectCharacter;
            typingInput.IncorrectCharacterEntered -= HandleIncorrectCharacter;
            typingInput.WordCompleted -= HandleWordCompleted;
        }

        isSubscribed = false;
    }

    private void HandleRecipeStarted(RecipeData recipe)
    {
        isTutorialRecipeActive = recipe != null && recipe == tutorialRecipe;

        if (!isTutorialRecipeActive)
        {
            Hide(TypingTutorialStage.Hidden);
            return;
        }

        Show(
            TypingTutorialStage.Intro,
            "TUTORIAL · ESCRIBE LA PALABRA ENTRE CORCHETES"
        );
    }

    private void HandleStepStarted(RecipeStep _, int index)
    {
        if (!isTutorialRecipeActive)
        {
            return;
        }

        if (index == 0 && Stage == TypingTutorialStage.Intro)
        {
            Show(
                TypingTutorialStage.Intro,
                "TUTORIAL · ESCRIBE LA PALABRA ENTRE CORCHETES"
            );
            return;
        }

        if (index > 0)
        {
            Show(
                TypingTutorialStage.Practice,
                "SIGUE LA PALABRA ACTIVA · CADA UNA MUEVE LA COCINA"
            );
        }
    }

    private void HandleCorrectCharacter(char _, int __)
    {
        if (!isTutorialRecipeActive
            || Stage == TypingTutorialStage.Practice)
        {
            return;
        }

        Show(
            TypingTutorialStage.CorrectLetter,
            "BIEN · LAS LETRAS SUBRAYADAS YA ESTÁN COMPLETAS"
        );
    }

    private void HandleIncorrectCharacter(char _, int __)
    {
        if (!isTutorialRecipeActive)
        {
            return;
        }

        Show(
            TypingTutorialStage.ErrorExplained,
            "ERROR · NO AVANZA · BACKSPACE RETROCEDE LETRAS CORRECTAS"
        );
    }

    private void HandleWordCompleted(string _)
    {
        if (!isTutorialRecipeActive || recipeRunner.CurrentStepIndex != 0)
        {
            return;
        }

        Show(
            TypingTutorialStage.WordCompleted,
            "PALABRA COMPLETA · OBSERVA CÓMO RESPONDE LA COCINA"
        );
    }

    private void HandleRecipeCompleted(RecipeData recipe)
    {
        if (!isTutorialRecipeActive || recipe != tutorialRecipe)
        {
            return;
        }

        isTutorialRecipeActive = false;
        Hide(TypingTutorialStage.Completed);
    }

    private void Show(TypingTutorialStage stage, string message)
    {
        Stage = stage;

        if (messageLabel != null)
        {
            messageLabel.text = message;
        }

        if (panel != null)
        {
            panel.alpha = 1f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }
    }

    private void Hide(TypingTutorialStage stage)
    {
        Stage = stage;

        if (panel != null)
        {
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }
    }
}
