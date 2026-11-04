using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonController : MonoBehaviour
{
    // Este método se llamará cuando el botón sea presionado
    public void LoadScene(string sceneName)
    {
        // Cargar la escena por su nombre (restauramos el tiempo por si venía pausado)
        Time.timeScale = 1;
        SceneManager.LoadScene(sceneName);
    }

    public void ButtonSound()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx("button");
    }

    public void Buttonv2()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx("button2");
    }
}
