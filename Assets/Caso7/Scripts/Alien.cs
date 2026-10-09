using UnityEngine;

/// <summary>
/// ALIEN: atacante. Avanza hacia la base para destruirla.
///
/// Versión 1 (paso 3): solo existe la "Base" como punto de destino.
///   Advancing      -> camina hacia la base.
///   AttackingTower -> llegó a la base (todavía no hace daño: las torres llegan en otro paso).
///   AttackingSoldier queda preparado para cuando existan los soldados.
///
/// Al nacer, cada alien puede salir al azar como "Rápido" o "Tanque".
/// </summary>
public class Alien : Agent<AlienState>
{
    public enum Variant { Normal, Rapido, Tanque }

    [Header("Alien")]
    public Variant variant = Variant.Normal;
    public float damage = 1f;
    [Tooltip("Distancia a la que puede atacar.")]
    public float attackRange = 0.8f;

    [Header("Objetivo (temporal: luego serán las torres)")]
    [Tooltip("Si se deja vacío, busca un objeto llamado 'Base' en la escena.")]
    public Transform baseTarget;

    [Header("Variantes aleatorias")]
    [Range(0f, 1f)] public float fastChance = 0.2f;
    [Range(0f, 1f)] public float tankChance = 0.15f;

    [Header("Colores por estado")]
    public Color advancingColor = new Color(0.4f, 1f, 0.3f);    // verde alien
    public Color attackingColor = new Color(1f, 0.25f, 0.25f);  // rojo
    public Color fightingColor  = new Color(1f, 0.6f, 0.1f);    // naranja

    SpriteRenderer sr;

    protected override void Awake()
    {
        RollVariant();     // primero cambia los atributos según la variante...
        base.Awake();      // ...y luego Agent pone health = maxHealth

        sr = GetComponent<SpriteRenderer>();

        if (baseTarget == null)
        {
            GameObject b = GameObject.Find("Base");
            if (b != null) baseTarget = b.transform;
        }

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
        if (baseTarget == null) return;

        float distToBase = Vector2.Distance(transform.position, baseTarget.position);

        switch (state)
        {
            case AlienState.Advancing:
                if (distToBase <= attackRange) ChangeState(AlienState.AttackingTower);
                break;

            case AlienState.AttackingTower:
                if (distToBase > attackRange) ChangeState(AlienState.Advancing);
                break;

            case AlienState.AttackingSoldier:
                // Todavía no hay soldados: vuelve a avanzar.
                ChangeState(AlienState.Advancing);
                break;
        }
    }

    // ---------- 2. ACTUAR según el estado ----------
    protected override void Act()
    {
        switch (state)
        {
            case AlienState.Advancing:
                if (baseTarget != null) MoveTowards(baseTarget.position);
                break;

            case AlienState.AttackingTower:
                // Se queda quieto atacando. El daño real se agrega cuando existan las torres.
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
            case AlienState.Advancing:        sr.color = advancingColor; break;
            case AlienState.AttackingTower:   sr.color = attackingColor; break;
            case AlienState.AttackingSoldier: sr.color = fightingColor;  break;
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
