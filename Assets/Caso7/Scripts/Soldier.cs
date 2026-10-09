using UnityEngine;


[RequireComponent(typeof(SpriteRenderer))]
public class Soldier : Agent<SoldierState>
{
    [Header("Soldado: disparo")]
    public float shootRange = 3f;
    public float shotDamage = 1.5f;
    [Tooltip("Segundos entre disparos.")]
    public float fireCooldown = 0.6f;

    [Header("Munición (recurso limitado)")]
    public int maxAmmo = 10;
    public int ammo;
    [Tooltip("Energía que le quita a la torre para llenar el cargador completo.")]
    public float energyPerFullReload = 6f;
    [Tooltip("Segundos que tarda en llenar el cargador completo.")]
    public float reloadTime = 2f;
    [Tooltip("Qué tan cerca de la torre debe estar para recargar.")]
    public float reloadDistance = 0.9f;

    [Header("Patrulla y apoyo")]
    public float patrolRadius = 3f;
    [Tooltip("Distancia a la que considera que llegó a la torre que fue a apoyar.")]
    public float arriveDistance = 1.5f;

    [Header("Colores por estado")]
    public Color patrollingColor    = new Color(0.3f, 0.5f, 1f);   // azul
    public Color fightingColor      = new Color(1f, 0.9f, 0.2f);   // amarillo
    public Color repositioningColor = new Color(0.75f, 0.4f, 1f);  
    public Color reloadingColor     = new Color(0.8f, 0.8f, 0.8f); 
    public Color tracerColor        = new Color(1f, 0.95f, 0.4f);

    SpriteRenderer sr;
    LineRenderer tracer;
    float fireTimer;
    float tracerTimer;
    float reloadProgress;          
    const float TracerTime = 0.08f;

    LaserTower homeTower;          
    LaserTower supportTower;       
    LaserTower reloadTower;        
    Alien targetAlien;

    Vector3 patrolPoint;
    bool hasPatrolPoint;

    protected override void Awake()
    {
        base.Awake();
        ammo = maxAmmo;
        sr = GetComponent<SpriteRenderer>();
        SetupTracer();
        UpdateColor();
    }

    // ---------- 1. DECIDIR (único lugar con transiciones) ----------
    protected override void DecideState()
    {
        
        targetAlien = SimRegistry.FindNearest<Alien>(transform.position, visionRange);

        if (homeTower == null || !homeTower.IsAlive)
            homeTower = SimRegistry.FindNearest<LaserTower>(transform.position, Mathf.Infinity);

        
        if (state != SoldierState.Reloading && ammo <= 0)
        {
            ChangeState(SoldierState.Reloading);
            return;
        }

        switch (state)
        {
            case SoldierState.Patrolling:
                if (targetAlien != null)
                {
                    ChangeState(SoldierState.Fighting);
                }
                else
                {
                    LaserTower threatened = FindThreatenedTower();
                    if (threatened != null && DistanceTo(threatened) > patrolRadius)
                    {
                        supportTower = threatened;
                        ChangeState(SoldierState.Repositioning);
                    }
                }
                break;

            case SoldierState.Fighting:
                if (targetAlien == null) ChangeState(SoldierState.Patrolling);
                break;

            case SoldierState.Repositioning:
                if (targetAlien != null)
                {
                    ChangeState(SoldierState.Fighting);
                }
                else if (supportTower == null || !supportTower.IsAlive)
                {
                    ChangeState(SoldierState.Patrolling);
                }
                else if (DistanceTo(supportTower) <= arriveDistance)
                {
                    homeTower = supportTower;   // ahora patrulla esta torre
                    ChangeState(SoldierState.Patrolling);
                }
                break;

            case SoldierState.Reloading:
                if (ammo >= maxAmmo) ChangeState(SoldierState.Patrolling);
                break;
        }
    }

    // ---------- 2. ACTUAR según el estado ----------
    protected override void Act()
    {
        fireTimer -= h;
        UpdateTracer();

        switch (state)
        {
            case SoldierState.Patrolling:
                Patrol();
                break;

            case SoldierState.Fighting:
                Fight();
                break;

            case SoldierState.Repositioning:
                if (supportTower != null) MoveTowards(supportTower.transform.position);
                break;

            case SoldierState.Reloading:
                Reload();
                break;
        }
    }

    void Patrol()
    {
        Vector3 center = homeTower != null ? homeTower.transform.position : transform.position;

        if (!hasPatrolPoint)
        {
            patrolPoint = center + (Vector3)(Random.insideUnitCircle * patrolRadius);
            hasPatrolPoint = true;
        }

        if (MoveTowards(patrolPoint)) hasPatrolPoint = false;
    }

    void Fight()
    {
        if (targetAlien == null) return;

        // Si está lejos, se acerca hasta tenerlo a tiro
        if (DistanceTo(targetAlien) > shootRange)
        {
            MoveTowards(targetAlien.transform.position);
            return;
        }

        if (fireTimer <= 0f && ammo > 0)
        {
            targetAlien.TakeDamage(shotDamage, this);
            ammo--;
            fireTimer = fireCooldown;
            ShowTracer(targetAlien.transform.position);
            SfxPlayer.PlayShot();
        }
    }

    void Reload()
    {
        if (reloadTower == null || !reloadTower.IsAlive)
            reloadTower = SimRegistry.FindNearest<LaserTower>(transform.position, Mathf.Infinity);

        if (reloadTower == null) return;   

        // Primero ir hasta la torre
        if (DistanceTo(reloadTower) > reloadDistance)
        {
            MoveTowards(reloadTower.transform.position);
            return;
        }

        
        float energyWanted = energyPerFullReload / reloadTime * h;
        float taken = Mathf.Min(energyWanted, reloadTower.energy);
        reloadTower.energy -= taken;

        reloadProgress += taken / energyPerFullReload * maxAmmo;
        while (reloadProgress >= 1f && ammo < maxAmmo)
        {
            ammo++;
            reloadProgress -= 1f;
        }
    }

    /// <summary>La torre viva con más aliens dentro de su alcance (o null si ninguna está en peligro).</summary>
    LaserTower FindThreatenedTower()
    {
        LaserTower best = null;
        int bestCount = 0;

        foreach (LaserTower tower in SimRegistry.GetAll<LaserTower>())
        {
            int aliensNear = SimRegistry.CountInRange<Alien>(tower.transform.position, tower.fireRange);
            if (aliensNear > bestCount)
            {
                bestCount = aliensNear;
                best = tower;
            }
        }
        return best;
    }

    protected override void OnEnterState(SoldierState newState)
    {
        if (newState == SoldierState.Patrolling) hasPatrolPoint = false;
        if (newState == SoldierState.Reloading)
        {
            reloadTower = null;
            reloadProgress = 0f;
        }
        UpdateColor();
    }

    void UpdateColor()
    {
        if (sr == null) return;

        switch (state)
        {
            case SoldierState.Patrolling:    sr.color = patrollingColor;    break;
            case SoldierState.Fighting:      sr.color = fightingColor;      break;
            case SoldierState.Repositioning: sr.color = repositioningColor; break;
            case SoldierState.Reloading:     sr.color = reloadingColor;     break;
        }
    }

    // ---------- Disparo visible ----------
    void SetupTracer()
    {
        
        GameObject obj = new GameObject("Tracer");
        obj.transform.SetParent(transform, false);
        tracer = obj.AddComponent<LineRenderer>();

        tracer.positionCount = 2;
        tracer.useWorldSpace = true;
        tracer.startWidth = 0.04f;
        tracer.endWidth = 0.04f;
        tracer.sortingOrder = 10;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader != null) tracer.material = new Material(shader);

        tracer.startColor = tracerColor;
        tracer.endColor = tracerColor;
        tracer.enabled = false;
    }

    void ShowTracer(Vector3 targetPos)
    {
        if (tracer == null) return;
        tracer.SetPosition(0, transform.position);
        tracer.SetPosition(1, targetPos);
        tracer.enabled = true;
        tracerTimer = TracerTime;
    }

    void UpdateTracer()
    {
        if (tracerTimer <= 0f) return;
        tracerTimer -= h;
        if (tracerTimer <= 0f && tracer != null) tracer.enabled = false;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shootRange);
    }
}
