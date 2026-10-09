using System.Collections.Generic;
using UnityEngine;


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
    public bool victory;
    public float simulatedTime;
    public int iterations;
    public string endReason = "";

    float accumulator;
    readonly List<SimEntity> snapshot = new List<SimEntity>();
    WaveSpawner spawner;
    bool hadTowers;

    void Start()
    {
        spawner = FindFirstObjectByType<WaveSpawner>();
    }

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

    
    void CheckEndConditions()
    {
        int towersAlive = SimRegistry.Count<LaserTower>();

        if (towersAlive > 0)
        {
            hadTowers = true;
        }
        else if (hadTowers)
        {
            EndSimulation(false, "Los aliens destruyeron todas las torres");
            return;
        }

        if (spawner != null && spawner.AllWavesSpawned
            && SimRegistry.Count<Alien>() == 0 && towersAlive > 0)
        {
            EndSimulation(true, $"Sobrevivieron a las {spawner.totalWaves} oleadas con {towersAlive} torre(s) en pie");
        }
    }

    public void EndSimulation(bool won, string reason)
    {
        if (!isRunning) return;
        isRunning = false;
        victory = won;
        endReason = (won ? "VICTORIA: " : "DERROTA: ") + reason;
        Debug.Log($"[Simulate] {endReason} (t = {simulatedTime:F1}s, {iterations} iteraciones)");
    }
}

