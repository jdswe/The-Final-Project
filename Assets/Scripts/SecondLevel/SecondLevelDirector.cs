using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [System.Serializable]
    public class ControlHint
    {
        public Sprite icon;          // sprite de "control sprites.png"
        public string label;
    }

    [Header("Estilo (mismo de la UI del juego)")]
    public TMP_FontAsset font;       // fuente de la UI (ImportsFonts)
    public Color barBackColor = new Color(0f, 0f, 0f, 0.75f);
    public Color playerBarColor = new Color(0.2f, 0.85f, 0.95f);
    public Color bossBarColor = new Color(0.95f, 0.35f, 0.55f);

    [Header("Textos")]
    public string bossName = "MAMÁ";
    public string playerName = "JETLEE";
    public string title = "¡Mamá está de mal humor!";
    public ControlHint[] controls;   // teclas que se muestran al iniciar el nivel
    public float instructionsTime = 7f;

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
        scaler.matchWidthOrHeight = 0.5f;

        // Barra de la mamá (arriba, centrada)
        momFill = CreateBar(canvasGO.transform, "MomHealth", new Vector2(0.5f, 1f), new Vector2(0f, -70f),
            new Vector2(900f, 30f), bossBarColor, bossName);
        if (momHealth == null) momFill.parent.gameObject.SetActive(false);

        // Barra del jugador (abajo a la izquierda)
        playerFill = CreateBar(canvasGO.transform, "PlayerHealth", new Vector2(0f, 0f), new Vector2(270f, 70f),
            new Vector2(420f, 24f), playerBarColor, playerName);
        if (playerHealth == null) playerFill.parent.gameObject.SetActive(false);

        BuildControls(canvasGO.transform);
    }

    void BuildControls(Transform parent)
    {
        instructionsPanel = new GameObject("Controls", typeof(RectTransform), typeof(Image));
        instructionsPanel.transform.SetParent(parent, false);
        RectTransform ip = instructionsPanel.GetComponent<RectTransform>();
        ip.anchorMin = ip.anchorMax = new Vector2(0.5f, 0.5f);
        ip.anchoredPosition = new Vector2(0f, 60f);
        Image back = instructionsPanel.GetComponent<Image>();
        back.color = new Color(0.05f, 0.04f, 0.15f, 0.6f);
        back.raycastTarget = false;

        int count = controls != null ? controls.Length : 0;
        float cell = 230f, iconH = 110f;
        ip.sizeDelta = new Vector2(Mathf.Max(600f, count * cell + 60f), 300f);

        TextMeshProUGUI header = CreateText(instructionsPanel.transform, title, 44);
        header.rectTransform.anchorMin = header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        header.rectTransform.anchoredPosition = new Vector2(0f, -45f);
        header.rectTransform.sizeDelta = new Vector2(ip.sizeDelta.x, 60f);

        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) * 0.5f) * cell;
            ControlHint hint = controls[i];

            if (hint.icon != null)
            {
                GameObject iconGO = new GameObject("Icon " + hint.label, typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(instructionsPanel.transform, false);
                Image img = iconGO.GetComponent<Image>();
                img.sprite = hint.icon;
                img.preserveAspect = true;
                img.raycastTarget = false;
                RectTransform rt = img.rectTransform;
                rt.anchoredPosition = new Vector2(x, -10f);
                rt.sizeDelta = new Vector2(cell - 30f, iconH);
            }

            TextMeshProUGUI label = CreateText(instructionsPanel.transform, hint.label, 28);
            label.rectTransform.anchoredPosition = new Vector2(x, -100f);
            label.rectTransform.sizeDelta = new Vector2(cell, 60f);
        }
    }

    RectTransform CreateBar(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, string label)
    {
        GameObject bg = new GameObject(name, typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(parent, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = anchor;
        bgRt.sizeDelta = size;
        bgRt.anchoredPosition = pos;
        Image bgImg = bg.GetComponent<Image>();
        bgImg.color = barBackColor;
        bgImg.raycastTarget = false;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(bg.transform, false);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(4f, 4f);
        fillRt.offsetMax = new Vector2(-4f, -4f);
        Image fillImg = fill.GetComponent<Image>();
        fillImg.color = color;
        fillImg.raycastTarget = false;

        // Nombre encima de la barra
        TextMeshProUGUI t = CreateText(bg.transform, label, 30);
        t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        t.rectTransform.anchoredPosition = new Vector2(0f, 24f);
        t.rectTransform.sizeDelta = new Vector2(size.x, 40f);
        return fillRt;
    }

    TextMeshProUGUI CreateText(Transform parent, string content, float size)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = content;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }
}
