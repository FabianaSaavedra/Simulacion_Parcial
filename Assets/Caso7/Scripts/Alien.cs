using UnityEngine;


public class Alien : Agent<AlienState>
{
    public enum Variant { Normal, Rapido, Tanque }

    [Header("Alien")]
    public Variant variant = Variant.Normal;
    public float damage = 1f;
    [Tooltip("Distancia a la que puede atacar.")]
    public float attackRange = 0.8f;
    [Tooltip("Segundos entre ataques.")]
    public float attackCooldown = 1f;
    [Tooltip("Si un soldado está más cerca que esto, el alien se desvía a pelear con él.")]
    public float soldierAggroRange = 1.5f;

    [Header("Variantes aleatorias")]
    [Range(0f, 1f)] public float fastChance = 0.2f;
    [Range(0f, 1f)] public float tankChance = 0.15f;

    [Header("Colores por estado")]
    public Color advancingColor = new Color(0.4f, 1f, 0.3f);    
    public Color attackingColor = new Color(1f, 0.25f, 0.25f);  
    public Color fightingColor = new Color(1f, 0.6f, 0.1f);    

    SpriteRenderer sr;
    LaserTower targetTower;
    Soldier targetSoldier;
    float attackTimer;

    protected override void Awake()
    {
        RollVariant();     
        base.Awake();     
        sr = GetComponent<SpriteRenderer>();
        UpdateColor();
    }

    /// <summary>Decide al azar si este alien es Normal, Rápido o Tanque.</summary>
    void RollVariant()
    {
        float r = Random.value;

        if (r < tankChance)
        {
            variant = Variant.Tanque;
            maxHealth *= 2.5f;
            speed *= 0.6f;
            damage *= 1.5f;
            transform.localScale *= 1.3f;
        }
        else if (r < tankChance + fastChance)
        {
            variant = Variant.Rapido;
            speed *= 1.8f;
            maxHealth *= 0.6f;
            transform.localScale *= 0.8f;
        }
        else
        {
            variant = Variant.Normal;
        }
    }

    // ---------- 1. DECIDIR (único lugar con transiciones) ----------
    protected override void DecideState()
    {
        // Percepción
        targetTower = SimRegistry.FindNearest<LaserTower>(transform.position, Mathf.Infinity);
        targetSoldier = SimRegistry.FindNearest<Soldier>(transform.position, soldierAggroRange);

        bool towerInReach = targetTower != null && DistanceTo(targetTower) <= attackRange;

        switch (state)
        {
            case AlienState.Advancing:
                if (targetSoldier != null) ChangeState(AlienState.AttackingSoldier);
                else if (towerInReach) ChangeState(AlienState.AttackingTower);
                break;

            case AlienState.AttackingTower:
                if (targetSoldier != null) ChangeState(AlienState.AttackingSoldier);
                else if (!towerInReach) ChangeState(AlienState.Advancing);
                break;

            case AlienState.AttackingSoldier:
                if (targetSoldier == null) ChangeState(AlienState.Advancing);
                break;
        }
    }

    // ---------- 2. ACTUAR según el estado ----------
    protected override void Act()
    {
        attackTimer -= h;

        switch (state)
        {
            case AlienState.Advancing:
                if (targetTower != null) MoveTowards(targetTower.transform.position);
                break;

            case AlienState.AttackingTower:
                if (targetTower != null && attackTimer <= 0f)
                {
                    targetTower.TakeDamage(damage, this);
                    attackTimer = attackCooldown;
                }
                break;

            case AlienState.AttackingSoldier:
                if (targetSoldier == null) break;

                if (DistanceTo(targetSoldier) > attackRange)
                {
                    MoveTowards(targetSoldier.transform.position);
                }
                else if (attackTimer <= 0f)
                {
                    targetSoldier.TakeDamage(damage, this);
                    attackTimer = attackCooldown;
                }
                break;
        }
    }

    protected override void OnEnterState(AlienState newState)
    {
        UpdateColor();
    }

    void UpdateColor()
    {
        if (sr == null) return;

        switch (state)
        {
            case AlienState.Advancing: sr.color = advancingColor; break;
            case AlienState.AttackingTower: sr.color = attackingColor; break;
            case AlienState.AttackingSoldier: sr.color = fightingColor; break;
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0.6f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, soldierAggroRange);
    }
}