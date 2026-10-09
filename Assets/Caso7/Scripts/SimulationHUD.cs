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

    
    static readonly Color AlienGreen = new Color(0.4f, 1f, 0.3f);
    static readonly Color AlienRed = new Color(1f, 0.25f, 0.25f);
    static readonly Color Orange = new Color(1f, 0.6f, 0.1f);
    static readonly Color SoldierBlue = new Color(0.3f, 0.5f, 1f);
    static readonly Color SoldierYel = new Color(1f, 0.9f, 0.2f);
    static readonly Color SoldierPurp = new Color(0.75f, 0.4f, 1f);
    static readonly Color SoldierGray = new Color(0.8f, 0.8f, 0.8f);
    static readonly Color TowerCyan = new Color(0.3f, 0.8f, 1f);
    static readonly Color TowerFiring = new Color(1f, 1f, 0.6f);
    static readonly Color TowerOver = new Color(1f, 0.5f, 0.1f);
    static readonly Color TowerDead = new Color(0.25f, 0.25f, 0.25f);

    GUIStyle titleStyle, labelStyle, smallStyle, bannerStyle, bannerSubStyle, headerStyle;
    float ui;          // escala de la interfaz según la resolución
    Camera cam;

    void Start()
    {
        if (sim == null) sim = FindFirstObjectByType<Simulate>();
        if (spawner == null) spawner = FindFirstObjectByType<WaveSpawner>();
        cam = Camera.main;
    }

    void OnGUI()
    {
        if (sim == null) return;
        BuildStyles();

        ui = Screen.height / 720f;

        
        GUI.matrix = Matrix4x4.identity;
        if (showBars) DrawWorldBars();

        
        GUI.matrix = Matrix4x4.Scale(new Vector3(ui, ui, 1f));
        DrawStatsPanel();
        DrawSpeedControls();
        if (showLegend) DrawLegend();
        DrawEndBanner();

        GUI.matrix = Matrix4x4.identity;
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

        Rect box = new Rect(10, 10, 300, 150);
        GUI.Box(box, "");

        float x = 20, y = 14, w = 280, lh = 22;
        GUI.Label(new Rect(x, y, w, 26), "Aliens vs Defensa", titleStyle); y += 28;
        GUI.Label(new Rect(x, y, w, lh), $"Tiempo: {sim.simulatedTime:F1} s    Velocidad: x{sim.speedMultiplier:0.#}", labelStyle); y += lh;
        GUI.Label(new Rect(x, y, w, lh), WaveText(), labelStyle); y += lh;
        GUI.Label(new Rect(x, y, w, lh),
            $"<color=#66ff4d>Aliens: {aliens}</color>  (normal {normal} · rápido {rapido} · tanque {tanque})", smallStyle); y += lh;
        GUI.Label(new Rect(x, y, w, lh),
            $"<color=#4d80ff>Soldados: {soldiers}</color>  (recargando {reloading})", labelStyle); y += lh;
        GUI.Label(new Rect(x, y, w, lh),
            $"<color=#4dccff>Torres: {towersAlive}/{towersTotal}</color>  (sobrecalentadas {overheated})", labelStyle);
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
        float vw = Screen.width / ui;   // ancho de la pantalla virtual
        float bw = 54, bh = 26, gap = 4;
        float[] speeds = { 0f, 0.5f, 1f, 2f, 4f };
        string[] names = { "Pausa", "x0.5", "x1", "x2", "x4" };

        float x = vw - (bw + gap) * speeds.Length - 6;
        float y = 10;

        for (int i = 0; i < speeds.Length; i++)
        {
            bool selected = Mathf.Approximately(sim.speedMultiplier, speeds[i]);
            GUI.backgroundColor = selected ? new Color(0.4f, 1f, 0.5f) : Color.white;
            if (GUI.Button(new Rect(x + i * (bw + gap), y, bw, bh), names[i]))
                sim.speedMultiplier = speeds[i];
        }
        GUI.backgroundColor = Color.white;

        string legendText = showLegend ? "Ocultar leyenda" : "Ver leyenda";
        if (GUI.Button(new Rect(vw - 146, y + bh + 6, 140, bh), legendText))
            showLegend = !showLegend;
    }

    // ================= LEYENDA DE ESTADOS =================
    // Tres cuadros abajo: Aliens a la izquierda, Soldados al centro, Torres a la derecha
    void DrawLegend()
    {
        float vw = Screen.width / ui;   
        float vh = 720f;
        float w = 210;
        float margin = 10;

        LegendBox(margin, vh, w, "Aliens", new[]
        {
            (AlienGreen, "Avanzando"),
            (AlienRed,   "Atacando torre"),
            (Orange,     "Peleando con soldado")
        });

        LegendBox((vw - w) / 2f, vh, w, "Soldados", new[]
        {
            (SoldierBlue, "Patrullando"),
            (SoldierYel,  "Combatiendo"),
            (SoldierPurp, "Apoyando otra torre"),
            (SoldierGray, "Recargando (gasta energía)")
        });

        LegendBox(vw - w - margin, vh, w, "Torres láser", new[]
        {
            (TowerCyan,   "Inactiva (recarga energía)"),
            (TowerFiring, "Disparando"),
            (TowerOver,   "Sobrecalentada"),
            (TowerDead,   "Destruida")
        });
    }

    /// <summary>Un cuadro de leyenda pegado al borde de abajo de la pantalla.</summary>
    void LegendBox(float x, float screenBottom, float w, string title, (Color color, string text)[] rows)
    {
        float h = 20 + rows.Length * 19 + 12;
        float y = screenBottom - h - 10;

        GUI.Box(new Rect(x, y, w, h), "");
        float cx = x + 10, cy = y + 6;

        cy = LegendHeader(cx, cy, title);
        foreach (var row in rows)
            cy = LegendRow(cx, cy, row.color, row.text);
    }

    float LegendHeader(float x, float y, string text)
    {
        GUI.Label(new Rect(x, y, 190, 20), text, headerStyle);
        return y + 20;
    }

    float LegendRow(float x, float y, Color c, string text)
    {
        GUI.color = c;
        GUI.DrawTexture(new Rect(x + 2, y + 4, 12, 12), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 20, y, 175, 20), text, smallStyle);
        return y + 19;
    }

    // ================= BARRAS SOBRE LAS ENTIDADES =================
    void DrawWorldBars()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float s = ui;

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

        float vw = Screen.width / ui, vh = 720f;

        // Oscurecer la pantalla
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0, 0, vw, vh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float w = 520, h = 230;
        Rect box = new Rect((vw - w) / 2f, (vh - h) / 2f, w, h);
        GUI.Box(box, "");

        bannerStyle.normal.textColor = sim.victory ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);
        GUI.Label(new Rect(box.x, box.y + 15, w, 60), sim.victory ? "¡VICTORIA!" : "DERROTA", bannerStyle);

        string reason = sim.endReason.Replace("VICTORIA: ", "").Replace("DERROTA: ", "");
        GUI.Label(new Rect(box.x + 20, box.y + 85, w - 40, 60), reason, bannerSubStyle);
        GUI.Label(new Rect(box.x + 20, box.y + 135, w - 40, 30),
            $"Tiempo de juego: {sim.simulatedTime:F1} segundos", bannerSubStyle);

        if (GUI.Button(new Rect(box.x + (w - 160) / 2f, box.y + h - 50, 160, 34), "Reiniciar"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // ================= ESTILOS =================
    void BuildStyles()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
        titleStyle.normal.textColor = Color.white;

        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
        labelStyle.normal.textColor = Color.white;

        smallStyle = new GUIStyle(labelStyle) { fontSize = 12 };

        headerStyle = new GUIStyle(labelStyle) { fontSize = 13, fontStyle = FontStyle.Bold };

        bannerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 52,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        bannerSubStyle = new GUIStyle(labelStyle)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
    }
}