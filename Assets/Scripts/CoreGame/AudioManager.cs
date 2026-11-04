using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    public Sounds[] musicSound, SfxSound;
    public AudioSource musicSource, sfxSource;
    public AudioMixer mixer;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }

    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (mixer != null)
        {
            float musicVolume = Mathf.Max(PlayerPrefs.GetFloat("musicVolume", 1f), 0.0001f);
            float sfxVolume = Mathf.Max(PlayerPrefs.GetFloat("sfxVolume", 1f), 0.0001f);
            mixer.SetFloat("Music", Mathf.Log10(musicVolume) * 20);
            mixer.SetFloat("sfx", Mathf.Log10(sfxVolume) * 20);
        }

        PlaySceneMusic(SceneManager.GetActiveScene().name);
    }

    // El AudioManager persiste entre escenas: cambia la música cada vez que se carga una escena
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
            PlaySceneMusic(scene.name);
    }

    private void PlaySceneMusic(string currentScene)
    {
        switch (currentScene)
        {
            case "cinematic 1":
                PlayMusic("Cinema1");
                break;
            case "cinematic 2":
                PlayMusic("Cinema2");
                break;
            case "cinematic 3":
                PlayMusic("Cinema3");
                break;
            case "GameWin":
                PlayMusic("victory");
                break;
            case "GameOver":
                PlayMusic("defeat");
                break;
            default:
                PlayMusic("BGMusic");
                break;
        }
    }

    public void PlayMusic(String name)
    {
        Sounds s = Array.Find(musicSound, x => x.name == name);
        if (s == null)
        {
            Debug.Log("musica no encontrado: " + name);
            return;
        }

        if (musicSource.clip == s.clip && musicSource.isPlaying)
            return;

        else
        {
            musicSource.clip = s.clip;
            musicSource.mute = false;
            musicSource.Play();
        }
    }

    public void PlaySfx(string name)
    {
        Sounds s = Array.Find(SfxSound, x => x.name == name);
        if (s == null)
        {
            Debug.Log("sfx no encontrado: " + name);
        }

        else
        {
            sfxSource.clip = s.clip;
            sfxSource.mute = false;
            sfxSource.Play();
        }
    }


}






