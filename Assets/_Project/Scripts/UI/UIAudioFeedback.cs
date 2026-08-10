using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class UIAudioFeedback : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioClip playClickClip;
    [SerializeField] private AudioClip defaultClickClip;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private string playButtonName = "PlayButton";
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.55f;
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.28f;
    [SerializeField, Min(0f)] private float hoverCooldown = 0.06f;

    private readonly Dictionary<Button, UnityAction> clickListeners = new();
    private readonly List<RaycastResult> raycastResults = new();
    private PointerEventData pointerEventData;
    private Button hoveredButton;
    private Button selectedButton;
    private float nextHoverTime;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.outputAudioMixerGroup = sfxGroup;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RefreshButtons();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        RemoveClickListeners();
    }

    private void Update()
    {
        TrackSelectedButton();
        TrackHoveredButton();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        RemoveClickListeners();
        hoveredButton = null;
        selectedButton = null;
        pointerEventData = null;

        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            Button capturedButton = button;
            UnityAction listener = () => PlayClick(capturedButton);
            button.onClick.AddListener(listener);
            clickListeners.Add(button, listener);
        }
    }

    private void RemoveClickListeners()
    {
        foreach (KeyValuePair<Button, UnityAction> entry in clickListeners)
        {
            if (entry.Key != null)
            {
                entry.Key.onClick.RemoveListener(entry.Value);
            }
        }

        clickListeners.Clear();
    }

    private void TrackSelectedButton()
    {
        Button current = null;
        if (EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject != null)
        {
            current = EventSystem.current.currentSelectedGameObject
                .GetComponentInParent<Button>();
        }

        if (current == selectedButton)
        {
            return;
        }

        selectedButton = current;
        if (current != null && current.isActiveAndEnabled && current.interactable)
        {
            PlayHover();
        }
    }

    private void TrackHoveredButton()
    {
        if (EventSystem.current == null || Mouse.current == null)
        {
            hoveredButton = null;
            return;
        }

        pointerEventData ??= new PointerEventData(EventSystem.current);
        pointerEventData.position = Mouse.current.position.ReadValue();
        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        Button current = null;
        foreach (RaycastResult result in raycastResults)
        {
            current = result.gameObject.GetComponentInParent<Button>();
            if (current != null)
            {
                break;
            }
        }

        if (current == hoveredButton)
        {
            return;
        }

        hoveredButton = current;
        if (current != null && current.isActiveAndEnabled && current.interactable)
        {
            PlayHover();
        }
    }

    private void PlayClick(Button button)
    {
        if (audioSource == null || button == null)
        {
            return;
        }

        bool isPlayButton = string.Equals(
            button.gameObject.name,
            playButtonName,
            System.StringComparison.OrdinalIgnoreCase
        );
        AudioClip clip = isPlayButton ? playClickClip : defaultClickClip;
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, clickVolume);
        }
    }

    private void PlayHover()
    {
        if (audioSource == null
            || hoverClip == null
            || Time.unscaledTime < nextHoverTime)
        {
            return;
        }

        nextHoverTime = Time.unscaledTime + hoverCooldown;
        audioSource.PlayOneShot(hoverClip, hoverVolume);
    }
}
