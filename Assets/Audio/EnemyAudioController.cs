using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemyAudioController : MonoBehaviour
{
    [Header("Idle / Growl")]
    [SerializeField] private AudioClip idleSound;
    [SerializeField, Range(0f, 1f)] private float idleVolume = 0.25f;
    [SerializeField] private float idleIntervalMin = 3f;
    [SerializeField] private float idleIntervalMax = 7f;

    [Header("Damage / Death")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField, Range(0f, 1f)] private float damageVolume = 0.6f;

    [SerializeField] private AudioClip deathSound;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 0.8f;

    [Header("Distance")]
    [SerializeField] private bool useDistanceCheck = true;
    [SerializeField] private float audibleDistance = 10f;
    [SerializeField] private Transform listener;

    private AudioSource source;
    private Enemy enemy;
    private float idleTimer;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        enemy = GetComponent<Enemy>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;

        if (listener == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                listener = player.transform;
        }

        ResetIdleTimer();
    }

    private void OnEnable()
    {
        if (enemy != null)
        {
            enemy.OnDamaged += HandleDamaged;
            enemy.OnDied += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (enemy != null)
        {
            enemy.OnDamaged -= HandleDamaged;
            enemy.OnDied -= HandleDied;
        }
    }

    private void Update()
    {
        if (enemy != null && enemy.IsDead)
            return;

        UpdateIdleSound();
    }

    private void UpdateIdleSound()
    {
        if (idleSound == null)
            return;

        if (useDistanceCheck && !IsListenerCloseEnough())
            return;

        idleTimer -= Time.deltaTime;

        if (idleTimer > 0f)
            return;

        source.PlayOneShot(idleSound, idleVolume);
        ResetIdleTimer();
    }

    private bool IsListenerCloseEnough()
    {
        if (listener == null)
            return true;

        float distance = Vector2.Distance(transform.position, listener.position);
        return distance <= audibleDistance;
    }

    private void ResetIdleTimer()
    {
        idleTimer = Random.Range(idleIntervalMin, idleIntervalMax);
    }

    private void HandleDamaged(DamageData damageData)
    {
        if (damageSound == null)
            return;

        if (useDistanceCheck && !IsListenerCloseEnough())
            return;

        source.PlayOneShot(damageSound, damageVolume);
    }

    private void HandleDied()
    {
        if (deathSound == null)
            return;

        AudioSource.PlayClipAtPoint(deathSound, transform.position, deathVolume);
    }
}