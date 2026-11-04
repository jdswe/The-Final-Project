using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public static GameManager instance;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
    }
    

    public void GameWin()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx("Victory");
        LoadScene("GameWin");
    }


    public void GameOver()
    {
        LoadScene("GameOver");
    }

    public void LoadScene(string name)
    {
        // Evita cargar una escena congelada (pausa / rueda de armas) o con el cursor bloqueado
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(name);
    }
}
