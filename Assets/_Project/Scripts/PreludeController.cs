using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public sealed class PreludeController : MonoBehaviour
{
    [SerializeField] private Image[] slides = new Image[0];
    [SerializeField] private TMP_Text phraseLabel;
    [SerializeField] private TMP_Text tutorialWordLabel;
    [SerializeField] private TypingInput typingInput;
    [SerializeField] private PaperWordRenderer tutorialWordRenderer;
    [SerializeField] private PaperAlphabetGlyphSet tutorialAlphabet;
    [SerializeField] private CanvasGroup slidesGroup;
    [SerializeField] private CanvasGroup frameGroup;
    [SerializeField] private CanvasGroup tutorialGroup;
    [SerializeField] private TextAsset phrasesJson;
    [SerializeField, Min(0.5f)] private float secondsPerSlide = 5f;
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.6f;
    [SerializeField, Min(0f)] private float finalSceneFadeDuration = 1.2f;
    [SerializeField] private string nextSceneName = "Playground";
    private PreludePhrase[] phrases = new PreludePhrase[0];
    private PreludeWord[] words = new PreludeWord[0];
    private int currentIndex;
    private float elapsed;
    private bool transitioning;

    private void Awake()
    {
        if (typingInput == null)
            typingInput = GetComponent<TypingInput>();
        if (typingInput == null)
        {
            Debug.LogError("PreludeController necesita TypingInput asignado.", this);
            enabled = false;
            return;
        }
        if (tutorialWordLabel == null)
        {
            foreach (TMP_Text candidate in GetComponentsInChildren<TMP_Text>(true))
            {
                bool namedTutorial = candidate.gameObject.name.ToLowerInvariant().Contains("tutorial");
                Transform parent = candidate.transform.parent;
                while (!namedTutorial && parent != null)
                {
                    namedTutorial = parent.name.ToLowerInvariant().Contains("tutorial");
                    parent = parent.parent;
                }
                if (candidate != phraseLabel && namedTutorial)
                { tutorialWordLabel = candidate; break; }
            }
        }
        if (tutorialWordLabel == null)
            Debug.LogError("PreludeController necesita el texto TutorialMinigame asignado.", this);
        if (tutorialWordRenderer == null && tutorialWordLabel != null)
        {
            tutorialWordRenderer = tutorialWordLabel.GetComponentInChildren<PaperWordRenderer>(true);
            if (tutorialWordRenderer != null)
                tutorialWordRenderer.Configure(tutorialAlphabet, null);
        }
        typingInput.ProgressChanged += HandleTypingProgress;
        typingInput.WordCompleted += HandleWordCompleted;
    }

    private void Start()
    {
        LanguageManager.LanguageChanged += HandleLanguageChanged;
        LoadPhrases();
        ShowCurrentSlide();
    }

    private void OnDestroy()
    {
        LanguageManager.LanguageChanged -= HandleLanguageChanged;
        if (tutorialWordRenderer != null)
            tutorialWordRenderer.SetCaretActive(false);
        if (typingInput == null) return;
        typingInput.ProgressChanged -= HandleTypingProgress;
        typingInput.WordCompleted -= HandleWordCompleted;
    }

    private void Update()
    {
        if (tutorialWordRenderer != null && typingInput != null)
            tutorialWordRenderer.SetCaretActive(
                !transitioning
                && typingInput.IsInputEnabled
                && !typingInput.IsComplete
            );
        if (transitioning) return;
        elapsed += Time.unscaledDeltaTime;
        bool advancePressed = Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
        bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if ((advancePressed || clicked) && typingInput.IsComplete) Advance();
        else if (elapsed >= secondsPerSlide && typingInput.IsComplete) Advance();
    }

    public void Advance()
    {
        if (transitioning) return;
        if (currentIndex >= slides.Length - 1) { LoadNextScene(); return; }
        StartCoroutine(TransitionTo(currentIndex + 1));
    }

    private void LoadNextScene()
    {
        if (AppRoot.Instance != null && AppRoot.Instance.SceneLoader != null)
        {
            AppRoot.Instance.SceneLoader.LoadScene(
                nextSceneName,
                finalSceneFadeDuration
            );
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void LoadPhrases()
    {
        if (LanguageManager.IsEnglish)
        {
            phrases = new[]
            {
                new PreludePhrase { line = "Every solstice, the village of Aethel wakes with a sacred mission: gather tributes from land and sea." },
                new PreludePhrase { line = "Offerings arrive from every corner. Bakers and farmers, young and old; every hand matters in this act of faith." },
                new PreludePhrase { line = "Sun, the Ancient Guardian, sleeps there. His silence is peace and his purr is prosperity. We await his awakening with reverence." },
                new PreludePhrase { line = "The fires are lit and the ceremonial stage is ready. Masters of the kitchen, it is time to orchestrate the sacred banquet." }
            };
            words = new[]
            {
                new PreludeWord { line = "GATHER" }, new PreludeWord { line = "OFFERINGS" },
                new PreludeWord { line = "GUARDIAN" }, new PreludeWord { line = "BANQUET" }
            };
            return;
        }

        if (phrasesJson == null) { Debug.LogError("PreludeController necesita el JSON de frases.", this); return; }
        PreludePhraseCollection collection = JsonUtility.FromJson<PreludePhraseCollection>(phrasesJson.text);
        phrases = collection != null && collection.prelude_phrases != null ? collection.prelude_phrases : new PreludePhrase[0];
        words = collection != null && collection.prelude_words != null ? collection.prelude_words : new PreludeWord[0];
    }

    private void HandleLanguageChanged(GameLanguage _)
    {
        if (transitioning) return;
        LoadPhrases();
        ShowCurrentSlide();
    }

    private void ShowCurrentSlide()
    {
        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, slides.Length - 1));
        elapsed = 0f;
        for (int index = 0; index < slides.Length; index++)
            if (slides[index] != null) slides[index].gameObject.SetActive(index == currentIndex);
        if (phraseLabel != null) phraseLabel.text = currentIndex < phrases.Length ? phrases[currentIndex].line : string.Empty;
        if (currentIndex < phrases.Length)
        {
            if (currentIndex < words.Length) typingInput.SetExpectedWord(words[currentIndex].line);
            RefreshTutorialWord();
        }
    }

    private IEnumerator TransitionTo(int nextIndex)
    {
        transitioning = true;
        yield return FadeGroups(1f, 0f);
        currentIndex = nextIndex;
        ShowCurrentSlide();
        yield return FadeGroups(0f, 1f);
        transitioning = false;
    }

    private void HandleTypingProgress(int _, string __) { RefreshTutorialWord(); }
    private void HandleWordCompleted(string _)
    {
        RefreshTutorialWord();
        StartCoroutine(AdvanceAfterCompletion());
    }
    private IEnumerator AdvanceAfterCompletion() { yield return new WaitForSecondsRealtime(0.35f); Advance(); }

    private void RefreshTutorialWord()
    {
        if (tutorialWordLabel == null || typingInput == null) return;
        string expected = typingInput.ExpectedWord.ToUpperInvariant();
        string typed = typingInput.TypedText.ToUpperInvariant();
        int prefix = Mathf.Clamp(typingInput.CorrectPrefixLength, 0, expected.Length);
        string green = ColorUtility.ToHtmlStringRGB(new Color(0.42f, 0.9f, 0.58f));
        string gray = ColorUtility.ToHtmlStringRGB(new Color(0.65f, 0.65f, 0.65f));
        string red = ColorUtility.ToHtmlStringRGB(new Color(1f, 0.3f, 0.25f));
        string typedTail = typed.Length > prefix ? Escape(typed.Substring(prefix)) : string.Empty;
        string pending = prefix < expected.Length ? Escape(expected.Substring(prefix)) : string.Empty;
        string paperWord = typed + expected.Substring(Mathf.Min(typed.Length, expected.Length));
        if (tutorialWordRenderer != null
            && tutorialWordRenderer.RenderWord(paperWord, prefix, typed.Length))
        {
            tutorialWordLabel.text = LanguageManager.Text("Escribe:", "Type:");
            tutorialWordLabel.alignment = TextAlignmentOptions.MidlineLeft;
            tutorialWordLabel.margin = new Vector4(30f, 0f, 0f, 0f);
            return;
        }
        tutorialWordLabel.text = LanguageManager.Text("Escribe: ", "Type: ")
            + $"<color=#{green}>{Escape(expected.Substring(0, prefix))}</color>"
            + (typedTail.Length > 0 ? $"<color=#{red}>{typedTail}</color>" : string.Empty)
            + (pending.Length > 0 ? $"<color=#{gray}>{pending}</color>" : string.Empty);
    }

    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private IEnumerator FadeGroups(float from, float to)
    {
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            SetGroupAlpha(Mathf.Lerp(from, to, fadeElapsed / fadeDuration));
            yield return null;
        }
        SetGroupAlpha(to);
    }

    private void SetGroupAlpha(float alpha)
    {
        if (slidesGroup != null) slidesGroup.alpha = alpha;
        if (frameGroup != null) frameGroup.alpha = alpha;
        if (tutorialGroup != null) tutorialGroup.alpha = alpha;
    }

    [System.Serializable] private sealed class PreludePhraseCollection { public PreludePhrase[] prelude_phrases; public PreludeWord[] prelude_words; }
    [System.Serializable] private sealed class PreludePhrase { public string line; }
    [System.Serializable] private sealed class PreludeWord { public string line; }
}
