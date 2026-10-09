using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class SfxPlayer : MonoBehaviour
{
    public static SfxPlayer Instance { get; private set; }

    [Header("Sonidos")]
    public AudioClip laserClip;
    public AudioClip shotClip;

    [Header("Volumen")]
    [Range(0f, 1f)] public float laserVolume = 0.35f;
    [Range(0f, 1f)] public float shotVolume = 0.3f;

    [Header("Evitar saturación")]
    [Tooltip("Segundos mínimos entre dos sonidos del mismo tipo.")]
    public float minInterval = 0.08f;

    AudioSource source;
    float lastLaser = -1f, lastShot = -1f;

    void Awake()
    {
        Instance = this;
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void PlayLaser()
    {
        if (Instance != null) Instance.Play(Instance.laserClip, Instance.laserVolume, ref Instance.lastLaser);
    }

    public static void PlayShot()
    {
        if (Instance != null) Instance.Play(Instance.shotClip, Instance.shotVolume, ref Instance.lastShot);
    }

    void Play(AudioClip clip, float volume, ref float lastTime)
    {
        if (clip == null) return;
        if (Time.time - lastTime < minInterval) return;

        lastTime = Time.time;
        source.PlayOneShot(clip, volume);
    }
}
