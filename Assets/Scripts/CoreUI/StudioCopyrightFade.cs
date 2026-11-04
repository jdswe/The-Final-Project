using UnityEngine;
using TMPro;
using System.Collections;

public class StudioCopyrightFade : MonoBehaviour
{
    public CanvasGroup panelCanvasGroup;
    public TextMeshProUGUI studioText;
    public TextMeshProUGUI copyRightText;

    public float fadeDuration = 2f;
    private float targetAlpha = 1f; // Totalmente visible

    void Start()
    {
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0;
        if (studioText != null) studioText.alpha = 0;
        if (copyRightText != null) copyRightText.alpha = 0; // Agregar el fade a este texto también

        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(0, targetAlpha, elapsed / fadeDuration);

            // Aplicamos el alpha a todos los elementos
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = newAlpha;
            if (studioText != null) studioText.alpha = newAlpha;
            if (copyRightText != null) copyRightText.alpha = newAlpha;

            yield return null;
        }

        // Asegurar que queda en el targetAlpha exacto
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = targetAlpha;
        if (studioText != null) studioText.alpha = targetAlpha;
        if (copyRightText != null) copyRightText.alpha = targetAlpha;
    }
}
