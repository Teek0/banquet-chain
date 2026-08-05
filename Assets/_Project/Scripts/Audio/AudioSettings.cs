using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioSettings : MonoBehaviour
{
    private const string MasterKey = "volume.master";
    private const string MusicKey = "volume.music";
    private const string SfxKey = "volume.sfx";

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterParameter = "MasterVolume";
    [SerializeField] private string musicParameter = "MusicVolume";
    [SerializeField] private string sfxParameter = "SfxVolume";

    public float MasterVolume { get; private set; } = 1f;
    public float MusicVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;

    private void Awake()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
        MusicVolume = PlayerPrefs.GetFloat(MusicKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
    }

    private void Start()
    {
        ApplyAllVolumes();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        ApplyVolume(masterParameter, MasterVolume);
        PlayerPrefs.SetFloat(MasterKey, MasterVolume);
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        ApplyVolume(musicParameter, MusicVolume);
        PlayerPrefs.SetFloat(MusicKey, MusicVolume);
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        ApplyVolume(sfxParameter, SfxVolume);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            PlayerPrefs.Save();
        }
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    private void ApplyAllVolumes()
    {
        ApplyVolume(masterParameter, MasterVolume);
        ApplyVolume(musicParameter, MusicVolume);
        ApplyVolume(sfxParameter, SfxVolume);
    }

    private void ApplyVolume(string parameterName, float linearValue)
    {
        if (audioMixer == null)
        {
            Debug.LogError("AudioSettings no tiene un AudioMixer asignado.");
            return;
        }

        float decibels = LinearToDecibels(linearValue);

        if (!audioMixer.SetFloat(parameterName, decibels))
        {
            Debug.LogError(
                $"AudioMixer no encontró el parámetro expuesto '{parameterName}'."
            );
        }
    }

    private static float LinearToDecibels(float linearValue)
    {
        if (linearValue <= 0.0001f)
        {
            return -80f;
        }

        return Mathf.Log10(linearValue) * 20f;
    }
}
