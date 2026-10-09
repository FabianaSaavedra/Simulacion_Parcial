using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class EndSounds : MonoBehaviour
{
    [Header("Sonidos")]
    public AudioClip victoriaClip;
    public AudioClip derrotaClip;
    [Range(0f, 1f)] public float volume = 0.8f;

    Simulate sim;
    AudioSource source;
    bool played;

    void Start()
    {
        sim = GetComponent<Simulate>();
        if (sim == null) sim = FindFirstObjectByType<Simulate>();

        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
    }

    void Update()
    {
        if (played || sim == null) return;

        
        if (!sim.isRunning && !string.IsNullOrEmpty(sim.endReason))
        {
            AudioClip clip = sim.victory ? victoriaClip : derrotaClip;
            if (clip != null) source.PlayOneShot(clip, volume);
            played = true;
        }
    }
}
