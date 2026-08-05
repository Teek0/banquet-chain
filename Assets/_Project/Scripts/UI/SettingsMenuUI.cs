using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsMenuUI : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private AudioSettings audioSettings;
    private bool listenersRegistered;

    private void OnEnable()
    {
        if (!TryResolveDependencies())
        {
            return;
        }

        masterSlider.SetValueWithoutNotify(
            audioSettings.MasterVolume
        );
        musicSlider.SetValueWithoutNotify(
            audioSettings.MusicVolume
        );
        sfxSlider.SetValueWithoutNotify(
            audioSettings.SfxVolume
        );

        masterSlider.onValueChanged.AddListener(
            HandleMasterChanged
        );
        musicSlider.onValueChanged.AddListener(
            HandleMusicChanged
        );
        sfxSlider.onValueChanged.AddListener(
            HandleSfxChanged
        );

        listenersRegistered = true;
    }

    private void OnDisable()
    {
        if (!listenersRegistered)
        {
            return;
        }

        masterSlider.onValueChanged.RemoveListener(
            HandleMasterChanged
        );
        musicSlider.onValueChanged.RemoveListener(
            HandleMusicChanged
        );
        sfxSlider.onValueChanged.RemoveListener(
            HandleSfxChanged
        );

        listenersRegistered = false;
    }

    private bool TryResolveDependencies()
    {
        if (masterSlider == null
            || musicSlider == null
            || sfxSlider == null)
        {
            Debug.LogError(
                "SettingsMenuUI necesita los tres Slider asignados."
            );
            return false;
        }

        if (AppRoot.Instance == null
            || AppRoot.Instance.AudioSettings == null)
        {
            Debug.LogError(
                "SettingsMenuUI necesita AudioSettings desde AppRoot. Inicia desde Boot."
            );
            return false;
        }

        audioSettings = AppRoot.Instance.AudioSettings;
        return true;
    }

    private void HandleMasterChanged(float value)
    {
        audioSettings.SetMasterVolume(value);
    }

    private void HandleMusicChanged(float value)
    {
        audioSettings.SetMusicVolume(value);
    }

    private void HandleSfxChanged(float value)
    {
        audioSettings.SetSfxVolume(value);
    }
}