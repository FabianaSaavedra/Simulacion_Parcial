using UnityEngine;


public abstract class SimEntity : MonoBehaviour
{
    /// <summary>Si es false, Simulate.cs ya no lo actualiza y los demás no lo "ven".</summary>
    public bool IsAlive { get; protected set; } = true;

    protected virtual void OnEnable()  { SimRegistry.Register(this); }
    protected virtual void OnDisable() { SimRegistry.Unregister(this); }

    
    public abstract void Simulate(float h);
}


public interface IDamageable
{
    bool IsAlive { get; }
    Transform transform { get; }
    void TakeDamage(float amount, SimEntity attacker);
}
