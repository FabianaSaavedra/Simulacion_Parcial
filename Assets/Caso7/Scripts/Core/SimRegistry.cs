using System.Collections.Generic;
using UnityEngine;


public static class SimRegistry
{
    static readonly List<SimEntity> entities = new List<SimEntity>();

    /// <summary>Todas las entidades registradas (vivas o recién muertas).</summary>
    public static IReadOnlyList<SimEntity> All => entities;

    public static void Register(SimEntity e)
    {
        if (e != null && !entities.Contains(e)) entities.Add(e);
    }

    public static void Unregister(SimEntity e)
    {
        entities.Remove(e);
    }

    /// <summary>Todas las entidades vivas de un tipo. Ej: SimRegistry.GetAll&lt;Alien&gt;()</summary>
    public static List<T> GetAll<T>() where T : SimEntity
    {
        var result = new List<T>();
        foreach (SimEntity e in entities)
        {
            if (e == null || !e.IsAlive) continue;
            if (e is T t) result.Add(t);
        }
        return result;
    }

    /// <summary>Cuántas entidades vivas hay de un tipo. Útil para la UI y para ganar/perder.</summary>
    public static int Count<T>() where T : SimEntity
    {
        int n = 0;
        foreach (SimEntity e in entities)
        {
            if (e != null && e.IsAlive && e is T) n++;
        }
        return n;
    }

    
    public static T FindNearest<T>(Vector3 from, float range) where T : SimEntity
    {
        T best = null;
        float bestDist = range;

        foreach (SimEntity e in entities)
        {
            if (e == null || !e.IsAlive) continue;
            if (!(e is T candidate)) continue;

            float d = Vector2.Distance(from, candidate.transform.position);
            if (d <= bestDist)
            {
                bestDist = d;
                best = candidate;
            }
        }
        return best;
    }

    /// <summary>Cuántas entidades vivas de un tipo hay dentro de un radio (ej: aliens cerca de una torre).</summary>
    public static int CountInRange<T>(Vector3 from, float range) where T : SimEntity
    {
        int n = 0;
        foreach (SimEntity e in entities)
        {
            if (e == null || !e.IsAlive || !(e is T)) continue;
            if (Vector2.Distance(from, e.transform.position) <= range) n++;
        }
        return n;
    }

    // Limpia la lista al darle Play (por si Unity no recarga el dominio entre ejecuciones).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        entities.Clear();
    }
}
