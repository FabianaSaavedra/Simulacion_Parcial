using UnityEngine;


[RequireComponent(typeof(SpriteRenderer))]
public class LaserTower : Agent<TowerState>
{
    [Header("Torre láser: disparo")]
    public float fireRange = 4f;
    public float shotDamage = 2f;
    [Tooltip("Segundos entre disparos.")]
    public float fireCooldown = 0.5f;

    [Header("Energía (recurso limitado)")]
    public float maxEnergy = 20f;
    public float energy;
    public float energyPerShot = 2f;
    [Tooltip("Energía que recupera por segundo cuando no dispara.")]
    public float rechargeRate = 1.5f;
    [Tooltip("Segundos que queda inactiva al sobrecalentarse.")]
    public float overheatDuration = 3f;

    [Header("Colores por estado")]
    public Color idleColor       = new Color(0.3f, 0.8f, 1f);   
    public Color firingColor     = new Color(1f, 1f, 0.6f);     
    public Color overheatedColor = new Color(1f, 0.5f, 0.1f);   
    public Color destroyedColor  = new Color(0.25f, 0.25f, 0.25f); 
    public Color laserColor      = new Color(0.3f, 1f, 1f);

    SpriteRenderer sr;
    LineRenderer laser;
    float fireTimer;
    float laserTimer;
    const float LaserFlashTime = 0.1f;

    protected override void Awake()
    {
        base.Awake();
        speed = 0f;            
        energy = maxEnergy;
        sr = GetComponent<SpriteRenderer>();
        SetupLaser();
        UpdateColor();
    }

    // ---------- 1. DECIDIR ----------
    protected override void DecideState()
    {
        switch (state)
        {
            case TowerState.Idle:
                if (energy >= energyPerShot && AlienInRange())
                    ChangeState(TowerState.Firing);
                break;

            case TowerState.Firing:
                if (energy < energyPerShot)
                    ChangeState(TowerState.Overheated);
                else if (!AlienInRange())
                    ChangeState(TowerState.Idle);
                break;

            case TowerState.Overheated:
                if (timeInState >= overheatDuration)
                    ChangeState(TowerState.Idle);
                break;
        }
    }

    // ---------- 2. ACTUAR ----------
    protected override void Act()
    {
        fireTimer -= h;
        UpdateLaserFlash();

        switch (state)
        {
            case TowerState.Idle:
            case TowerState.Overheated:
                Recharge();
                break;

            case TowerState.Firing:
                TryFire();
                break;
        }
    }

    void Recharge()
    {
        energy = Mathf.Min(maxEnergy, energy + rechargeRate * h);
    }

    void TryFire()
    {
        if (fireTimer > 0f) return;

        Alien target = SimRegistry.FindNearest<Alien>(transform.position, fireRange);
        if (target == null) return;

        target.TakeDamage(shotDamage, this);
        energy -= energyPerShot;
        fireTimer = fireCooldown;
        ShowLaser(target.transform.position);
    }

    bool AlienInRange()
    {
        return SimRegistry.FindNearest<Alien>(transform.position, fireRange) != null;
    }

    /// <summary>La torre no desaparece al morir: queda como ruina gris.</summary>
    protected override void Die()
    {
        ChangeState(TowerState.Destroyed);
        IsAlive = false;
        if (laser != null) laser.enabled = false;
    }

    protected override void OnEnterState(TowerState newState)
    {
        UpdateColor();
    }

    void UpdateColor()
    {
        if (sr == null) return;

        switch (state)
        {
            case TowerState.Idle:       sr.color = idleColor;       break;
            case TowerState.Firing:     sr.color = firingColor;     break;
            case TowerState.Overheated: sr.color = overheatedColor; break;
            case TowerState.Destroyed:  sr.color = destroyedColor;  break;
        }
    }

    // ---------- Rayo láser visible ----------
    void SetupLaser()
    {
        
        GameObject laserObj = new GameObject("Laser");
        laserObj.transform.SetParent(transform, false);
        laser = laserObj.AddComponent<LineRenderer>();

        laser.positionCount = 2;
        laser.useWorldSpace = true;
        laser.startWidth = 0.08f;
        laser.endWidth = 0.08f;
        laser.sortingOrder = 10;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader != null) laser.material = new Material(shader);

        laser.startColor = laserColor;
        laser.endColor = laserColor;
        laser.enabled = false;
    }

    void ShowLaser(Vector3 targetPos)
    {
        if (laser == null) return;
        laser.SetPosition(0, transform.position);
        laser.SetPosition(1, targetPos);
        laser.enabled = true;
        laserTimer = LaserFlashTime;
    }

    void UpdateLaserFlash()
    {
        if (laserTimer <= 0f) return;
        laserTimer -= h;
        if (laserTimer <= 0f && laser != null) laser.enabled = false;
    }

    protected override void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, fireRange);
    }
}
