using UnityEngine;
using UnityEngine.SceneManagement;


public class SimulationHUD : MonoBehaviour
{
    [Header("Referencias (se buscan solas si se dejan vacías)")]
    public Simulate sim;
    public WaveSpawner spawner;

    [Header("Opciones")]
    public bool showBars = true;
    public bool showLegend = true;
    [Tooltip("Tamaño general de la interfaz y las letras. 1 = normal, 1.5 = más grande.")]
    [Range(0.8f, 2.5f)] public float textScale = 1.4f;
    [Tooltip("Qué tan oscuro es el fondo de los paneles (0 = transparente, 1 = negro).")]
    [Range(0f, 1f)] public float panelOpacity = 0.8f;

    
    static readonly Color AlienGreen = new Color(0.4f, 1f, 0.3f);
    static readonly Color AlienRed = new Color(1f, 0.25f, 0.25f);
    static readonly Color Orange = new Color(1f, 0.6f, 0.1f);
    static readonly Color SoldierBlue = new Color(0.3f, 0.5f, 1f);
    static readonly Color SoldierYel = new Color(1f, 0.9f, 0.2f);
    static readonly Color SoldierPurp = new Color(0.75f, 0.4f, 1f);
    static readonly Color SoldierGray = new Color(0.8f, 0.8f, 0.8f);
    static readonly Color TowerCyan = new Color(0.3f, 0.8f, 1f);
    static readonly Color TowerIdle = new Color(0.545f, 0.353f, 0.169f); 
    static readonly Color TowerFiring = new Color(1f, 1f, 0.6f);
    static readonly Color TowerOver = new Color(1f, 0.5f, 0.1f);
    static readonly Color TowerDead = new Color(0.25f, 0.25f, 0.25f);

    GUIStyle titleStyle, labelStyle, smallStyle, headerStyle, bannerStyle, bannerSubStyle, buttonStyle;
    float ui;          
    Camera cam;

    
    float VW => Screen.width / ui;
    float VH => Screen.height / ui;

    
    Rect R(float x, float y, float w, float h) => new Rect(x * ui, y * ui, w * ui, h * ui);

    void Start()
    {
        if (sim == null) sim = FindFirstObjectByType<Simulate>();
        if (spawner == null) spawner = FindFirstObjectByType<WaveSpawner>();
        cam = Camera.main;
    }

    void OnGUI()
    {
        if (sim == null) return;

        ui = Screen.height / 720f * textScale;
        BuildStyles();
        UpdateFontSizes();

        if (showBars) DrawWorldBars();
        DrawStatsPanel();
        DrawSpeedControls();
        if (showLegend) DrawLegend();
        DrawEndBanner();
    }

    // ================= PANEL DE ESTADÍSTICAS =================
    void DrawStatsPanel()
    {
        int normal = 0, rapido = 0, tanque = 0;
        int soldiers = 0, reloading = 0;
        int towersAlive = 0, towersTotal = 0, overheated = 0;

        foreach (SimEntity e in SimRegistry.All)
        {
            if (e == null) continue;

            if (e is LaserTower t)
            {
                towersTotal++;
                if (t.IsAlive)
                {
                    towersAlive++;
                    if (t.state == TowerState.Overheated) overheated++;
                }
            }
            else if (!e.IsAlive)
            {
                continue;
            }
            else if (e is Alien a)
            {
                if (a.variant == Alien.Variant.Rapido) rapido++;
                else if (a.variant == Alien.Variant.Tanque) tanque++;
                else normal++;
            }
            else if (e is Soldier s)
            {
                soldiers++;
                if (s.state == SoldierState.Reloading) reloading++;
            }
        }

        int aliens = normal + rapido + tanque;

        float x = 10, y = 10, w = 260, lh = 22;
        Panel(x, y, w, 30 + lh * 6 + 14);

        x += 12; y += 8; w -= 24;
        GUI.Label(R(x, y, w, 26), "Aliens vs Estado", titleStyle); y += 30;
        GUI.Label(R(x, y, w, lh), $"Tiempo: {sim.simulatedTime:F1} s", labelStyle); y += lh;
        GUI.Label(R(x, y, w, lh), $"Velocidad: x{sim.speedMultiplier:0.#}", labelStyle); y += lh;
        GUI.Label(R(x, y, w, lh), WaveText(), labelStyle); y += lh;
        GUI.Label(R(x, y, w, lh), $"<b><color=#7dff66>Aliens: {aliens}</color></b>", labelStyle); y += lh;
        GUI.Label(R(x, y, w, lh), $"<b><color=#8fb0ff>Soldados: {soldiers}</color></b>", labelStyle); y += lh;
        GUI.Label(R(x, y, w, lh), $"<b><color=#c8925a>Torres: {towersAlive}/{towersTotal}</color></b>", labelStyle);
    }

    string WaveText()
    {
        if (spawner == null) return "Oleadas: sin generador";

        int cur = spawner.currentWave, tot = spawner.totalWaves;
        switch (spawner.state)
        {
            case WaveState.Waiting:
                return cur == 0
                    ? $"Primera oleada en {Mathf.CeilToInt(spawner.timer)} s"
                    : $"Oleada {cur}/{tot} · siguiente en {Mathf.CeilToInt(spawner.timer)} s";
            case WaveState.Spawning:
                return $"Oleada {cur}/{tot} · ¡llegando aliens!";
            default:
                return $"Oleada {cur}/{tot} · era la última";
        }
    }

    // ================= CONTROLES DE VELOCIDAD =================
    void DrawSpeedControls()
    {
        float bw = 50, bh = 24, gap = 4;
        float[] speeds = { 0f, 0.5f, 1f, 2f, 4f };
        string[] names = { "Pausa", "x0.5", "x1", "x2", "x4" };

        float x = VW - (bw + gap) * speeds.Length - 6;
        float y = 10;

        for (int i = 0; i < speeds.Length; i++)
        {
            bool selected = Mathf.Approximately(sim.speedMultiplier, speeds[i]);
            GUI.backgroundColor = selected ? new Color(0.4f, 1f, 0.5f) : Color.white;
            if (GUI.Button(R(x + i * (bw + gap), y, bw, bh), names[i], buttonStyle))
                sim.speedMultiplier = speeds[i];
        }
        GUI.backgroundColor = Color.white;

        string legendText = showLegend ? "Ocultar leyenda" : "Ver leyenda";
        if (GUI.Button(R(VW - 146, y + bh + 6, 140, bh), legendText, buttonStyle))
            showLegend = !showLegend;
    }

    // ================= LEYENDA (tres cuadros abajo) =================
    void DrawLegend()
    {
        float w = 215, margin = 10;

        LegendBox(margin, w, "Aliens", new[]
        {
            (AlienGreen, "Avanzando"),
            (AlienRed,   "Atacando torre"),
            (Orange,     "Peleando con soldado")
        });

        LegendBox((VW - w) / 2f, w, "Soldados", new[]
        {
            (SoldierBlue, "Patrullando"),
            (SoldierYel,  "Combatiendo"),
            (SoldierPurp, "Apoyando otra torre"),
            (SoldierGray, "Recargando (gasta energía)")
        });

        LegendBox(VW - w - margin, w, "Torres láser", new[]
        {
            (TowerIdle,   "Inactiva (recarga energía)"),
            (TowerFiring, "Disparando"),
            (TowerOver,   "Sobrecalentada"),
            (TowerDead,   "Destruida")
        });
    }

    
    void LegendBox(float x, float w, string title, (Color color, string text)[] rows)
    {
        float rowH = 19;
        float h = 24 + rows.Length * rowH + 8;
        float y = VH - h - 10;

        Panel(x, y, w, h);

        float cx = x + 10, cy = y + 5;
        GUI.Label(R(cx, cy, w - 20, 22), title, headerStyle);
        cy += 24;

        foreach (var row in rows)
        {
            GUI.color = row.color;
            GUI.DrawTexture(R(cx + 2, cy + 4, 11, 11), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(R(cx + 20, cy, w - 30, rowH), row.text, smallStyle);
            cy += rowH;
        }
    }

    // ================= BARRAS SOBRE LAS ENTIDADES =================
    void DrawWorldBars()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float s = Screen.height / 720f;   

        foreach (SimEntity e in SimRegistry.All)
        {
            if (e == null || !e.IsAlive) continue;

            SpriteRenderer sr = e.GetComponent<SpriteRenderer>();
            Vector3 top = sr != null
                ? new Vector3(e.transform.position.x, sr.bounds.max.y, 0f)
                : e.transform.position;

            if (e is LaserTower t)
            {
                Bar(top, 14 * s, 54 * s, 6 * s, t.health / t.maxHealth, HealthColor(t.health / t.maxHealth));
                Color energyColor = t.state == TowerState.Overheated ? TowerOver : TowerCyan;
                Bar(top, 6 * s, 54 * s, 5 * s, t.energy / t.maxEnergy, energyColor);
            }
            else if (e is Soldier so)
            {
                Bar(top, 12 * s, 30 * s, 4 * s, so.health / so.maxHealth, HealthColor(so.health / so.maxHealth));
                Bar(top, 6 * s, 30 * s, 3 * s, (float)so.ammo / so.maxAmmo, SoldierYel);
            }
            else if (e is Alien a && a.health < a.maxHealth)
            {
                Bar(top, 6 * s, 26 * s, 4 * s, a.health / a.maxHealth, HealthColor(a.health / a.maxHealth));
            }
        }
    }

    /// <summary>Dibuja una barra centrada encima de un punto del mundo (en píxeles reales).</summary>
    void Bar(Vector3 world, float offsetUp, float width, float height, float fill, Color color)
    {
        Vector3 sp = cam.WorldToScreenPoint(world);
        if (sp.z < 0f) return;

        float x = sp.x - width / 2f;
        float y = Screen.height - sp.y - offsetUp - height;

        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(x - 1, y - 1, width + 2, height + 2), Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, width * Mathf.Clamp01(fill), height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    Color HealthColor(float t)
    {
        return Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.3f, 1f, 0.3f), Mathf.Clamp01(t));
    }

    // ================= LETRERO FINAL =================
    void DrawEndBanner()
    {
        if (sim.isRunning || string.IsNullOrEmpty(sim.endReason)) return;

        
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float w = 460, h = 210;
        float x = (VW - w) / 2f, y = (VH - h) / 2f;
        Panel(x, y, w, h);

        bannerStyle.normal.textColor = sim.victory ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);
        GUI.Label(R(x, y + 12, w, 56), sim.victory ? "¡VICTORIA!" : "DERROTA", bannerStyle);

        string reason = sim.endReason.Replace("VICTORIA: ", "").Replace("DERROTA: ", "");
        GUI.Label(R(x + 20, y + 74, w - 40, 50), reason, bannerSubStyle);
        GUI.Label(R(x + 20, y + 122, w - 40, 26), $"Tiempo de juego: {sim.simulatedTime:F1} segundos", bannerSubStyle);

        if (GUI.Button(R(x + (w - 150) / 2f, y + h - 46, 150, 32), "Reiniciar", buttonStyle))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // ================= PANELES Y ESTILOS =================
    /// <summary>Fondo oscuro semitransparente para que el texto se lea sobre cualquier color.</summary>
    void Panel(float x, float y, float w, float h)
    {
        GUI.color = new Color(0.05f, 0.06f, 0.1f, panelOpacity);
        GUI.DrawTexture(R(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, 0.15f);
        GUI.DrawTexture(R(x, y, w, 1.5f), Texture2D.whiteTexture);   
        GUI.color = Color.white;
    }

    void BuildStyles()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, richText = true };
        titleStyle.normal.textColor = Color.white;

        labelStyle = new GUIStyle(GUI.skin.label) { richText = true };
        labelStyle.normal.textColor = new Color(0.95f, 0.95f, 0.97f);

        smallStyle = new GUIStyle(labelStyle);

        headerStyle = new GUIStyle(labelStyle) { fontStyle = FontStyle.Bold };

        bannerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

        bannerSubStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, wordWrap = true };

        buttonStyle = new GUIStyle(GUI.skin.button);
    }

    /// <summary>Las letras se escalan con la pantalla para que se lean igual en cualquier tamaño.</summary>
    void UpdateFontSizes()
    {
        titleStyle.fontSize = Mathf.RoundToInt(17 * ui);
        labelStyle.fontSize = Mathf.RoundToInt(13 * ui);
        smallStyle.fontSize = Mathf.RoundToInt(12 * ui);
        headerStyle.fontSize = Mathf.RoundToInt(13.5f * ui);
        bannerStyle.fontSize = Mathf.RoundToInt(46 * ui);
        bannerSubStyle.fontSize = Mathf.RoundToInt(15 * ui);
        buttonStyle.fontSize = Mathf.RoundToInt(11.5f * ui);
    }
}
