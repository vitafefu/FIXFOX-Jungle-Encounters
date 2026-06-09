using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LevelMusic : MonoBehaviour
{
    [SerializeField] private AudioClip levelMusic;
    [SerializeField, Range(0f, 1f)] private float volume = 0.4f;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.clip = levelMusic;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = volume;
    }

    private void Start()
    {
        if (levelMusic != null)
            source.Play();
    }
}