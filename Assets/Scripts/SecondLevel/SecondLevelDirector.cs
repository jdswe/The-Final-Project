using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla el nivel 2: restablece el estado del juego al entrar, muestra las instrucciones
/// y dibuja las barras de vida del jugador y de la mamá (se crean por código para no depender
/// de prefabs de UI adicionales).
/// </summary>
public class SecondLevelDirector : MonoBehaviour
{
    [Header("Referencias (se buscan solas si están vacías)")]
    public HealthManager playerHealth;
    public HealthManager momHealth;

    [Header("Textos")]
    public string bossName = "MAMÁ";
    [TextArea]
    public string instructions = "¡Mamá está de mal humor!\nClic izquierdo: lanzar patos de goma\nClic derecho: chanclazo (devuelve sus chanclas)\nShift: correr  ·  Espacio: saltar";
    public float instructionsTime = 6f;

    private RectTransform playerFill;
    private RectTransform momFill;
    private GameObject instructionsPanel;

    void Awake()
    {
        // Venimos de una cinemática / pausa: asegurar que el juego corre
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Start()
    {
        if (playerHealth == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerHealth = p.GetComponent<HealthManager>();
        }

        if (momHealth == null)
        {
            MomEnemyController mom = FindFirstObjectByType<MomEnemyController>();
            if (mom != null) momHealth = mom.GetComponent<HealthManager>();
        }

        BuildHud();
        StartCoroutine(HideInstructions());
    }

    void Update()
    {
        if (playerFill != null && playerHealth != null) SetFill(playerFill, playerHealth.HealthPercent);
        if (momFill != null && momHealth != null) SetFill(momFill, momHealth.HealthPercent);
    }

    static void SetFill(RectTransform fill, float percent)
    {
        fill.anchorMax = new Vector2(Mathf.Clamp01(percent), 1f);
    }

    IEnumerator HideInstructions()
    {
        yield return new WaitForSeconds(instructionsTime);
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
    }

    // ---------- Construcción de la UI ----------

    void BuildHud()
    {
        GameObject canvasGO = new GameObject("Level2HUD", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        Font font = LoadFont();

        // Barra de la mamá (arriba, centrada)
        momFill = CreateBar(canvasGO.transform, "MomHealth", new Vector2(0.5f, 1f), new Vector2(0f, -60f),
            new Vector2(900f, 34f), new Color(0.85f, 0.15f, 0.35f), bossName, font);
        if (momHealth == null) momFill.parent.gameObject.SetActive(false);

        // Barra del jugador (abajo a la izquierda)
        playerFill = CreateBar(canvasGO.transform, "PlayerHealth", new Vector2(0f, 0f), new Vector2(260f, 60f),
            new Vector2(420f, 28f), new Color(0.2f, 0.85f, 0.95f), "JETLEE", font);
        if (playerHealth == null) playerFill.parent.gameObject.SetActive(false);

        // Instrucciones
        instructionsPanel = new GameObject("Instructions", typeof(RectTransform), typeof(Image));
        instructionsPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform ip = instructionsPanel.GetComponent<RectTransform>();
        ip.anchorMin = ip.anchorMax = new Vector2(0.5f, 0.5f);
        ip.sizeDelta = new Vector2(900f, 260f);
        ip.anchoredPosition = new Vector2(0f, 120f);
        instructionsPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        Text t = CreateText(instructionsPanel.transform, instructions, font, 36, TextAnchor.MiddleCenter);
        Stretch(t.rectTransform);
    }

    RectTransform CreateBar(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, string label, Font font)
    {
        GameObject bg = new GameObject(name, typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(parent, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = anchor;
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta = size;
        bgRt.anchoredPosition = pos;
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(bg.transform, false);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(4f, 4f);
        fillRt.offsetMax = new Vector2(-4f, -4f);
        fill.GetComponent<Image>().color = color;

        Text t = CreateText(bg.transform, label, font, 24, TextAnchor.MiddleCenter);
        Stretch(t.rectTransform);
        return fillRt;
    }

    static Text CreateText(Transform parent, string content, Font font, int size, TextAnchor anchor)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.text = content;
        t.font = font;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Font LoadFont()
    {
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (f == null)
        {
            try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
        }
        return f;
    }
}
