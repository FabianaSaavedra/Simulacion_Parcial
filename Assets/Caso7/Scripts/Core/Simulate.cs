using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Motor de la simulación (requisito del parcial: "Clase Simulate.cs").
/// Es el ÚNICO lugar desde donde se llama el Simulate(h) de cada entidad.
///
/// Usa un paso de tiempo fijo h: la simulación avanza igual sin importar los FPS,
/// y speedMultiplier permite acelerarla o pausarla para observar las dinámicas.
///
/// Nota: la clase no puede tener un método llamado "Simulate" (C# no permite que un
/// método se llame igual que su clase), por eso el paso global se llama Step().
/// </summary>
public class Simulate : MonoBehaviour
{
    [Header("Tiempo de simulación")]
    [Tooltip("Paso de simulación h, en segundos simulados.")]
    [Min(0.01f)] public float h = 0.1f;

    [Tooltip("1 = tiempo real, 2 = el doble de rápido, 0 = pausa.")]
    [Range(0f, 5f)] public float speedMultiplier = 1f;

    [Tooltip("Evita que la simulación se congele si el juego va lento.")]
    public int maxStepsPerFrame = 10;

    [Header("Estado de la simulación (solo lectura)")]
    public bool isRunning = true;
    public float simulatedTime;
    public int iterations;
    public string endReason = "";

    float accumulator;
    readonly List<SimEntity> snapshot = new List<SimEntity>();

    void Update()
    {
        if (!isRunning) return;

        accumulator += Time.deltaTime * speedMultiplier;

        int steps = 0;
        while (accumulator >= h && steps < maxStepsPerFrame)
        {
            accumulator -= h;
            Step();
            steps++;
            if (!isRunning) break;
        }
    }

    /// <summary>Un paso global: simula a todas las entidades vivas una vez.</summary>
    void Step()
    {
        // Se recorre una COPIA de la lista: si durante el paso nace un alien
        // o muere un soldado, la lista original cambia sin romper el foreach.
        snapshot.Clear();
        snapshot.AddRange(SimRegistry.All);

        foreach (SimEntity entity in snapshot)
        {
            if (entity != null && entity.IsAlive)
            {
                entity.Simulate(h);
            }
        }

        simulatedTime += h;
        iterations++;

        CheckEndConditions();
    }

    /// <summary>
    /// Condiciones de victoria y derrota.
    /// Se completa en los siguientes pasos, cuando existan las torres y las oleadas.
    /// </summary>
    void CheckEndConditions()
    {
        // Ejemplo de lo que vendrá:
        // if (SimRegistry.Count<LaserTower>() == 0) EndSimulation(false, "Destruyeron todas las torres");
    }

    public void EndSimulation(bool victory, string reason)
    {
        if (!isRunning) return;
        isRunning = false;
        endReason = (victory ? "VICTORIA: " : "DERROTA: ") + reason;
        Debug.Log($"[Simulate] {endReason} (t = {simulatedTime:F1}s, {iterations} iteraciones)");
    }
}
