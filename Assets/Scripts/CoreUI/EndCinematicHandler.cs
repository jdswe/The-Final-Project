using UnityEngine;
using UnityEngine.SceneManagement;

public class EndCinematicHandler : MonoBehaviour
{

    public string cinematicScene = "Cinematic_Transformacion";
    
    public void LoadGameScene(string name)
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(name);
    }


    public void EndCinematic()
    {
        // Solo descargamos si la escena aditiva sigue cargada (evita error si la senal se dispara dos veces)
        if (SceneManager.GetSceneByName(cinematicScene).isLoaded)
            SceneManager.UnloadSceneAsync(cinematicScene);
        

    }
}