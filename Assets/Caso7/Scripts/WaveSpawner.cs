using UnityEngine;

/// <summary>Estados del generador de oleadas.</summary>
public enum WaveState
{
    Waiting,   // Cuenta regresiva hasta la próxima oleada
    Spawning,  // Está soltando los aliens de la oleada actual, uno tras otro
    Finished   // Ya lanzó todas las oleadas
}

/// <summary>
/// GENERADOR DE OLEADAS: hace que los aliens "ataquen en grupos".
///
/// Cada oleada sale desde un punto al azar del BORDE del mapa y todos sus aliens
/// aparecen juntos (dentro de groupSpread), así avanzan como grupo.
/// Cada oleada es más grande que la anterior (sizeIncrease).
///
/// También es una SimEntity: su Simulate(h) lo llama Simulate.cs como a todos.
/// </summary>
public class WaveSpawner : SimEntity
{
    [Header("Prefab")]
    public GameObject alienPrefab;

    [Header("Oleadas")]
    public int totalWaves = 5;
    public int firstWaveSize = 4;
    [Tooltip("Cuántos aliens más trae cada oleada.")]
    public int sizeIncrease = 2;
    [Tooltip("Segundos antes de la primera oleada.")]
    public float firstDelay = 3f;
    [Tooltip("Segundos de descanso entre oleadas.")]
    public float timeBetweenWaves = 12f;
    [Tooltip("Segundos entre cada alien de una misma oleada.")]
    public float spawnInterval = 0.4f;

    [Header("Zona de aparición")]
    [Tooltip("Mitad del ancho y alto del mapa: los aliens salen por este borde.")]
    public Vector2 mapHalfSize = new Vector2(13f, 7.5f);
    [Tooltip("Qué tan juntos aparecen los aliens de un mismo grupo.")]
    public float groupSpread = 1.2f;

    [Header("Estado (solo lectura)")]
    public WaveState state = WaveState.Waiting;
    public int currentWave = 0;
    public float timer;
    public int aliensLeftToSpawn;

    /// <summary>True cuando ya salieron todas las oleadas (lo usa Simulate.cs para la victoria).</summary>
    public bool AllWavesSpawned => state == WaveState.Finished;

    Vector3 groupCenter;
    Transform container;

    void Awake()
    {
        timer = firstDelay;
        container = new GameObject("Aliens (oleadas)").transform;
    }

    public override void Simulate(float h)
    {
        switch (state)
        {
            case WaveState.Waiting:
                timer -= h;
                if (timer <= 0f && currentWave < totalWaves) StartWave();
                break;

            case WaveState.Spawning:
                timer -= h;
                if (timer <= 0f)
                {
                    SpawnAlien();
                    aliensLeftToSpawn--;
                    timer = spawnInterval;

                    if (aliensLeftToSpawn <= 0) EndWave();
                }
                break;

            case WaveState.Finished:
                break;
        }
    }

    void StartWave()
    {
        currentWave++;
        aliensLeftToSpawn = firstWaveSize + (currentWave - 1) * sizeIncrease;
        groupCenter = RandomEdgePoint();
        timer = 0f;
        state = WaveState.Spawning;
        Debug.Log($"[Oleadas] Oleada {currentWave}/{totalWaves}: {aliensLeftToSpawn} aliens");
    }

    void EndWave()
    {
        if (currentWave >= totalWaves)
        {
            state = WaveState.Finished;
        }
        else
        {
            state = WaveState.Waiting;
            timer = timeBetweenWaves;
        }
    }

    void SpawnAlien()
    {
        if (alienPrefab == null)
        {
            Debug.LogWarning("[Oleadas] Falta asignar el Alien Prefab en el WaveSpawner.");
            return;
        }

        Vector3 pos = groupCenter + (Vector3)(Random.insideUnitCircle * groupSpread);
        pos.z = 0f;
        Instantiate(alienPrefab, pos, Quaternion.identity, container);
    }

    /// <summary>Un punto al azar sobre el borde del rectángulo del mapa.</summary>
    Vector3 RandomEdgePoint()
    {
        Vector3 c = transform.position;
        float x = Random.Range(-mapHalfSize.x, mapHalfSize.x);
        float y = Random.Range(-mapHalfSize.y, mapHalfSize.y);

        switch (Random.Range(0, 4))
        {
            case 0:  return c + new Vector3(-mapHalfSize.x, y, 0f); // izquierda
            case 1:  return c + new Vector3( mapHalfSize.x, y, 0f); // derecha
            case 2:  return c + new Vector3(x,  mapHalfSize.y, 0f); // arriba
            default: return c + new Vector3(x, -mapHalfSize.y, 0f); // abajo
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position, new Vector3(mapHalfSize.x * 2f, mapHalfSize.y * 2f, 0f));
    }
}
