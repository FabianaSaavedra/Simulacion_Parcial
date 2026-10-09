using UnityEngine;

/// <summary>
/// Base de TODO lo que participa en la simulación (aliens, soldados, torres, spawner...).
/// Al activarse se registra solo en el SimRegistry, así Simulate.cs lo encuentra
/// aunque haya nacido en mitad de la simulación (por ejemplo, un alien de una oleada nueva).
/// </summary>
public abstract class SimEntity : MonoBehaviour
{
    /// <summary>Si es false, Simulate.cs ya no lo actualiza y los demás no lo "ven".</summary>
    public bool IsAlive { get; protected set; } = true;

    protected virtual void OnEnable()  { SimRegistry.Register(this); }
    protected virtual void OnDisable() { SimRegistry.Unregister(this); }

    /// <summary>
    /// Un paso de simulación. SOLO lo llama Simulate.cs (requisito del parcial).
    /// </summary>
    /// <param name="h">Tamaño del paso de tiempo simulado, en segundos.</param>
    public abstract void Simulate(float h);
}

/// <summary>
/// Cualquier cosa que puede recibir daño: soldados, aliens y torres.
/// Permite que un alien ataque "lo que tenga enfrente" sin saber si es soldado o torre.
/// </summary>
public interface IDamageable
{
    bool IsAlive { get; }
    Transform transform { get; }
    void TakeDamage(float amount, SimEntity attacker);
}
