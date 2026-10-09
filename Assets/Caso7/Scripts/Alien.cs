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

    [Header("Variantes aleatorias")]
    [Range(0f, 1f)] public float fastChance = 0.2f;
    [Range(0f, 1f)] public float tankChance = 0.15f;

    [Header("Colores por estado")]
    public Color advancingColor = new Color(0.4f, 1f, 0.3f);
    public Color attackingColor = new Color(1f, 0.25f, 0.25f);
    public Color fightingColor = new Color(1f, 0.6f, 0.1f);

    SpriteRenderer sr;
    LaserTower targetTower;
    float attackTimer;

    protected override void Awake()
    {
        RollVariant();
        base.Awake();
        sr = GetComponent<SpriteRenderer>();
        UpdateColor();
    }


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

        targetTower = SimRegistry.FindNearest<LaserTower>(transform.position, Mathf.Infinity);

        bool towerInReach = targetTower != null && DistanceTo(targetTower) <= attackRange;

        switch (state)
        {
            case AlienState.Advancing:
                if (towerInReach) ChangeState(AlienState.AttackingTower);
                break;

            case AlienState.AttackingTower:
                if (!towerInReach) ChangeState(AlienState.Advancing);
                break;

            case AlienState.AttackingSoldier:

                ChangeState(AlienState.Advancing);
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
    }
}

