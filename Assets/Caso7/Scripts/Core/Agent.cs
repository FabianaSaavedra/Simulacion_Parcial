using System.Collections.Generic;
using UnityEngine;


public abstract class Agent<TState> : SimEntity, IDamageable
    where TState : struct, System.Enum
{
    [Header("Atributos base")]
    public float maxHealth = 10f;
    public float health;
    public float speed = 2f;
    public float visionRange = 5f;

    [Header("Estado actual (solo lectura)")]
    public TState state;
    [Tooltip("Segundos simulados que lleva en el estado actual.")]
    public float timeInState;

    protected Vector3 destination;
    protected float h;

    protected virtual void Awake()
    {
        health = maxHealth;
        destination = transform.position;
    }

    // "sealed": ningún agente puede saltarse el orden Decidir -> Actuar.
    public sealed override void Simulate(float h)
    {
        if (!IsAlive) return;

        this.h = h;
        timeInState += h;

        DecideState();
        Act();
    }

    /// <summary>Percibir y decidir transiciones. Usar ChangeState() aquí.</summary>
    protected abstract void DecideState();

    /// <summary>Hacer lo que corresponde al estado actual (moverse, disparar...).</summary>
    protected abstract void Act();

    /// <summary>Cambia de estado y reinicia el contador de tiempo en el estado.</summary>
    protected void ChangeState(TState next)
    {
        if (EqualityComparer<TState>.Default.Equals(state, next)) return;

        OnExitState(state);
        state = next;
        timeInState = 0f;
        OnEnterState(next);
    }

    protected virtual void OnEnterState(TState newState) { }
    protected virtual void OnExitState(TState oldState) { }

    /// <summary>Avanza hacia un punto. Devuelve true si ya llegó.</summary>
    protected bool MoveTowards(Vector3 target)
    {
        target.z = transform.position.z;
        destination = target;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * h);
        return Vector2.Distance(transform.position, target) < 0.05f;
    }

    /// <summary>Distancia 2D a otra entidad.</summary>
    protected float DistanceTo(Component other)
    {
        return Vector2.Distance(transform.position, other.transform.position);
    }

    public virtual void TakeDamage(float amount, SimEntity attacker)
    {
        if (!IsAlive) return;

        health -= amount;
        if (health <= 0f)
        {
            health = 0f;
            Die();
        }
    }

    /// <summary>Muere: deja de simularse y desaparece. Las torres lo sobrescriben.</summary>
    protected virtual void Die()
    {
        IsAlive = false;
        Destroy(gameObject);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, destination);
        }
    }
}
