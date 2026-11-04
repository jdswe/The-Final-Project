using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;
using System;

public class UIControl : MonoBehaviour
{
    bool isPaused = false;
    public GameObject pausePanel;
    [SerializeField] AudioMixer audioMixer;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Toggle muteToggle;

    // Solo bloqueamos el cursor en escenas de juego (con jugador). En GameOver / GameWin debe quedar libre para usar los botones
    bool IsGameplayScene => GameObject.FindWithTag("Player") != null;

    void Start()
    {
        Time.timeScale = 1;
        if (pausePanel != null) pausePanel.SetActive(false);
        ApplyCursorState(false);

        // Cargar mute si está guardado
        if (PlayerPrefs.HasKey("Muted") && muteToggle != null)
        {
            muteToggle.isOn = PlayerPrefs.GetInt("Muted") == 1;
            SetMute();
        }

        // Cargar valores de volumen guardados (valor por defecto 1)
        if (musicSlider != null) musicSlider.value = PlayerPrefs.GetFloat("musicVolume", 1f);
        if (sfxSlider != null) sfxSlider.value = PlayerPrefs.GetFloat("sfxVolume", 1f);

        SetMusicVolume();
        SetSfxVolume();
    }

    void ApplyCursorState(bool paused)
    {
        bool lockCursor = !paused && IsGameplayScene;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && pausePanel != null)
        {
            TogglePause();
        }
    }

    void LateUpdate()
    {
        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        if (pausePanel != null) pausePanel.SetActive(isPaused);

        Time.timeScale = isPaused ? 0 : 1;
        ApplyCursorState(isPaused);
    }

    public void LoadScene(string name)
    {
        Time.timeScale = 1;
        isPaused = false;
        SceneManager.LoadScene(name);
    }

    public void SetMusicVolume()
    {
        if (musicSlider == null || audioMixer == null) return;
        float musicVolume = musicSlider.value;
        audioMixer.SetFloat("Music", MathF.Log10(MathF.Max(musicVolume, 0.0001f)) * 20); // Log10(0) = -infinito
        PlayerPrefs.SetFloat("musicVolume", musicVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume()
    {
        if (sfxSlider == null || audioMixer == null) return;
        float sfxVolume = sfxSlider.value;
        audioMixer.SetFloat("sfx", MathF.Log10(MathF.Max(sfxVolume, 0.0001f)) * 20);
        PlayerPrefs.SetFloat("sfxVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    public void SetMute()
    {
        if (muteToggle == null || audioMixer == null) return;
        if (muteToggle.isOn)
        {
            audioMixer.SetFloat("Music", -80f);
            audioMixer.SetFloat("sfx", -80f);
            PlayerPrefs.SetInt("Muted", 1);
        }
        else
        {
            SetMusicVolume();
            SetSfxVolume();
            PlayerPrefs.SetInt("Muted", 0);
        }
        PlayerPrefs.Save();
    }

    public void PlaySound()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx("button");
    }
}
