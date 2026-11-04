using UnityEngine;
using UnityEngine.SceneManagement;

public class CinematicTrigger : MonoBehaviour
{
    public string cinematicScene = "Cinematic_Transformacion";
    private bool isCinematicPlaying = false;

    private void OnTriggerEnter(Collider other)
    {
        // Verificar si el objeto que entra en el trigger es el jugador
        // Level 1 tiene 2 triggers hacia "cinematic 2": no cargarla dos veces
        if (other.CompareTag("Player") && !isCinematicPlaying && !SceneManager.GetSceneByName(cinematicScene).isLoaded)
        {
            //listo other.GetComponent<PlayerController>().DisablePlayerControl();
            StartCinematic();
        }
    }

    private void StartCinematic()
    {
        
        isCinematicPlaying = true;     
        //Time.timeScale = 0;
        SceneManager.LoadSceneAsync(cinematicScene, LoadSceneMode.Additive);
       
    }
}
