using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class AppRoot : MonoBehaviour
{
    public static AppRoot Instance { get; private set; }

    [Header("Servicios")]
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private AudioSettings audioSettings;

    [Header("Arranque")]
    [SerializeField] private string bootSceneName = "Boot";
    [SerializeField] private string firstSceneName = "MainMenu";
    [SerializeField, Min(0f)] private float bootFadeDuration = 1.1f;
    [SerializeField] private int targetFrameRate = 60;

    public SceneLoader SceneLoader => sceneLoader;
    public AudioSettings AudioSettings => audioSettings;

    private void Reset()
    {
        sceneLoader = GetComponent<SceneLoader>();
        audioSettings = GetComponent<AudioSettings>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (GetComponent<LanguageManager>() == null)
        {
            gameObject.AddComponent<LanguageManager>();
        }

        if (targetFrameRate > 0)
        {
            Application.targetFrameRate = targetFrameRate;
        }
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        if (sceneLoader == null)
        {
            Debug.LogError("AppRoot no tiene SceneLoader asignado.");
            return;
        }

        if (SceneManager.GetActiveScene().name == bootSceneName)
        {
            sceneLoader.LoadScene(firstSceneName, bootFadeDuration);
            return;
        }

        sceneLoader.RevealCurrentScene();
    }
}

[DisallowMultipleComponent]
public sealed class LanguageManager : MonoBehaviour
{
    private const string PreferenceKey = "BanquetChain.Language";
    private const string PreferenceInitializedKey = "BanquetChain.Language.Initialized";
    private const string DefaultLanguageVersionKey = "BanquetChain.Language.DefaultVersion";
    private const int DefaultLanguageVersion = 2;
    private static readonly Dictionary<string, string> SpanishToEnglish = new()
    {
        ["Jugar"] = "Play", ["Salir"] = "Quit", ["Volver"] = "Back",
        ["El Banquete Del Pueblo"] = "The People's Banquet",
        ["Ajustes"] = "Settings", ["Créditos"] = "Credits",
        ["Música"] = "Music", ["Efectos"] = "Sound Effects",
        ["Volumen General"] = "Master Volume", ["Continuar"] = "Resume",
        ["Reiniciar"] = "Restart", ["Pausa"] = "Paused",
        ["Menú principal"] = "Main Menu", ["Escribe: "] = "Type: ",
        ["Escribe:"] = "Type:", ["PEDIDO EN CAMINO"] = "ORDER ON THE WAY",
        ["VOLVER AL MENÚ"] = "BACK TO MENU",
        ["EL BANQUETE DEL PUEBLO"] = "THE PEOPLE'S BANQUET",
        ["EL BANQUETE DEL PUEBLO\n\nCreado para Takernal Jam"] = "THE PEOPLE'S BANQUET\n\nCreated for Takernal Jam",
        ["GRACIAS A TODO EL PUEBLO QUE MANTUVO ENCENDIDO EL BANQUETE."] = "THANK YOU TO EVERYONE WHO KEPT THE BANQUET GOING.",
        ["EL BANQUETE ESTÁ COMPLETO\n\nTodo el pueblo cocinó a una sola voz.\nEl ronroneo sagrado vuelve a proteger nuestros hogares y cultivos.\nPara alimentar a un gato gigantesco, hace falta la unión de todo un pueblo."] = "THE BANQUET IS COMPLETE\n\nThe whole village cooked as one.\nThe sacred purr protects our homes and crops once more.\nIt takes an entire village to feed a giant cat.",
        ["AUTORÍA Y PROGRAMACIÓN\nLattive\n\nARTE\nIA & Lattive\n\nAUDIO\nMúsica:\nXtremeFreddy, vía Pixabay — Pixabay Content License.\n\nEfectos de sonido:\n“Keyboard_Tactile_8” por StavSounds, vía Freesound — Creative Commons Zero (CC0 1.0).\n\n“Cat Purr / gato ronroneando” por yetcop, vía Freesound — Creative Commons Zero (CC0 1.0).\n\n“Bubble Pop 06” por Universfield, vía Pixabay — Pixabay Content License."] = "AUTHORSHIP & PROGRAMMING\nLattive\n\nART\nAI & Lattive\n\nAUDIO\nMusic:\nXtremeFreddy, via Pixabay — Pixabay Content License.\n\nSound effects:\n“Keyboard_Tactile_8” by StavSounds, via Freesound — Creative Commons Zero (CC0 1.0).\n\n“Cat Purr / purring cat” by yetcop, via Freesound — Creative Commons Zero (CC0 1.0).\n\n“Bubble Pop 06” by Universfield, via Pixabay — Pixabay Content License.",
        ["WEBGL · SI EL TECLADO NO RESPONDE, HAZ CLIC EN EL JUEGO"] = "WEBGL · IF THE KEYBOARD DOES NOT RESPOND, CLICK THE GAME"
    };

    public static LanguageManager Instance { get; private set; }
    public static GameLanguage CurrentLanguage => Instance != null ? Instance.currentLanguage : GameLanguage.Spanish;
    public static bool IsEnglish => CurrentLanguage == GameLanguage.English;
    public static event Action<GameLanguage> LanguageChanged;
    [SerializeField] private GameLanguage currentLanguage = GameLanguage.Spanish;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        bool hasCurrentDefault = PlayerPrefs.GetInt(
            DefaultLanguageVersionKey, 0
        ) >= DefaultLanguageVersion;
        bool hasSavedLanguage = hasCurrentDefault
            && PlayerPrefs.GetInt(PreferenceInitializedKey, 0) == 1;
        currentLanguage = hasSavedLanguage
            ? (GameLanguage)PlayerPrefs.GetInt(
                PreferenceKey, (int)GameLanguage.English
            )
            : GameLanguage.English;
        if (!hasCurrentDefault)
        {
            PlayerPrefs.SetInt(PreferenceKey, (int)GameLanguage.English);
            PlayerPrefs.SetInt(DefaultLanguageVersionKey, DefaultLanguageVersion);
            PlayerPrefs.Save();
        }
        GameLocalization.CurrentLanguage = currentLanguage;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start() => LocalizeScene(SceneManager.GetActiveScene());
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (Instance == this) Instance = null;
    }

    public void SetSpanish() => SetLanguage(GameLanguage.Spanish);
    public void SetEnglish() => SetLanguage(GameLanguage.English);
    public void SetLanguage(GameLanguage language)
    {
        if (currentLanguage == language) return;
        currentLanguage = language;
        GameLocalization.CurrentLanguage = currentLanguage;
        PlayerPrefs.SetInt(PreferenceKey, (int)currentLanguage);
        PlayerPrefs.SetInt(PreferenceInitializedKey, 1);
        PlayerPrefs.Save();
        LocalizeScene(SceneManager.GetActiveScene());
        LanguageChanged?.Invoke(currentLanguage);
        GameFlow flow = FindFirstObjectByType<GameFlow>();
        if (flow != null && flow.IsRunning) flow.RestartGame();
    }

    public static string Text(string spanish, string english) => GameLocalization.Text(spanish, english);
    private void HandleSceneLoaded(Scene scene, LoadSceneMode _) => LocalizeScene(scene);

    private void LocalizeScene(Scene scene)
    {
        if (!scene.IsValid()) return;
        foreach (TMP_Text label in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (label == null || label.gameObject.scene != scene || label.GetComponentInParent<LanguageButtonBinding>() != null) continue;
            if (TryTranslateStatic(label.text, out string translated)) label.text = translated;
        }
        BindSceneLanguageButtons(scene);
    }

    private bool TryTranslateStatic(string value, out string translated)
    {
        if (currentLanguage == GameLanguage.English)
        {
            if (SpanishToEnglish.TryGetValue(value, out translated)) return true;

            string compactValue = value.Replace("\n", " ").Trim();
            if (compactValue == "Efectos")
            {
                translated = "Sound Effects";
                return true;
            }

            if (compactValue == "Efectos de sonido")
            {
                translated = "Sound\nEffects";
                return true;
            }

            if (value.StartsWith("EL BANQUETE DEL PUEBLO"))
            {
                translated = value.Replace(
                    "EL BANQUETE DEL PUEBLO", "THE PEOPLE'S BANQUET"
                ).Replace("Creado para", "Created for");
                return true;
            }

            if (value.StartsWith("AUTORÍA Y PROGRAMACIÓN"))
            {
                translated = value
                    .Replace("AUTORÍA Y PROGRAMACIÓN", "AUTHORSHIP & PROGRAMMING")
                    .Replace("\nARTE\n", "\nART\n")
                    .Replace("\nMúsica:", "\nMusic:")
                    .Replace("Efectos de sonido:", "Sound effects:")
                    .Replace("IA &", "AI &")
                    .Replace(" por ", " by ")
                    .Replace(" vía ", " via ")
                    .Replace("gato ronroneando", "purring cat");
                return true;
            }
        }

        if (currentLanguage == GameLanguage.Spanish)
        {
            if (value.Replace("\n", " ").Trim() == "Sound Effects")
            {
                translated = "Efectos\nde sonido";
                return true;
            }

            if (value.StartsWith("THE PEOPLE'S BANQUET"))
            {
                translated = value.Replace(
                    "THE PEOPLE'S BANQUET", "EL BANQUETE DEL PUEBLO"
                ).Replace("Created for", "Creado para");
                return true;
            }

            if (value.StartsWith("AUTHORSHIP & PROGRAMMING"))
            {
                translated = value
                    .Replace("AUTHORSHIP & PROGRAMMING", "AUTORÍA Y PROGRAMACIÓN")
                    .Replace("\nART\n", "\nARTE\n")
                    .Replace("\nMusic:", "\nMúsica:")
                    .Replace("Sound effects:", "Efectos de sonido:")
                    .Replace("AI &", "IA &")
                    .Replace(" by ", " por ")
                    .Replace(" via ", " vía ")
                    .Replace("purring cat", "gato ronroneando");
                return true;
            }

            foreach (KeyValuePair<string, string> pair in SpanishToEnglish)
                if (pair.Value == value) { translated = pair.Key; return true; }
        }
        translated = value;
        return false;
    }

    private static void BindSceneLanguageButtons(Scene scene)
    {
        foreach (Button button in FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        ))
        {
            if (button == null || button.gameObject.scene != scene)
            {
                continue;
            }

            GameLanguage? language = button.gameObject.name switch
            {
                "ESLanguageButton" => GameLanguage.Spanish,
                "ENLanguageButton" => GameLanguage.English,
                "ENGLanguageButton" => GameLanguage.English,
                _ => null
            };

            if (language.HasValue)
            {
                button.GetComponent<LanguageButtonBinding>()?.Configure(
                    language.Value
                );
                if (button.GetComponent<LanguageButtonBinding>() == null)
                {
                    button.gameObject.AddComponent<LanguageButtonBinding>()
                        .Configure(language.Value);
                }
            }
        }
    }
}

public sealed class LanguageButtonBinding : MonoBehaviour
{
    private Button button;
    private GameLanguage language;
    private bool configured;

    public void Configure(GameLanguage selectedLanguage)
    {
        language = selectedLanguage;
        button ??= GetComponent<Button>();
        if (!configured && button != null)
        {
            button.onClick.AddListener(SelectLanguage);
            LanguageManager.LanguageChanged += Refresh;
            configured = true;
        }
        Refresh(LanguageManager.CurrentLanguage);
    }

    private void OnDestroy()
    {
        if (configured && button != null)
        {
            button.onClick.RemoveListener(SelectLanguage);
            LanguageManager.LanguageChanged -= Refresh;
        }
    }

    private void SelectLanguage() => LanguageManager.Instance?.SetLanguage(language);
    private void Refresh(GameLanguage currentLanguage)
    {
        if (button != null) button.interactable = currentLanguage != language;
    }
}
