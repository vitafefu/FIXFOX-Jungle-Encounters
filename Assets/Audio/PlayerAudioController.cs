using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudioController : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private float jumpSoundCooldown = 0.08f;
    private float lastJumpSoundTime = -999f;
    [SerializeField] private AudioClip crouchSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip waterEnterSound;

    [SerializeField] private AudioClip runSound;
    [SerializeField] private AudioClip swimSound;
    [SerializeField] private AudioClip enemyHitSound;
    [SerializeField] private AudioClip damageSound;

    [SerializeField, Range(0f, 1f)]
    private float enemyHitVolume = 0.7f;

    [SerializeField, Range(0f, 1f)]
    private float damageVolume = 0.8f;

    [Header("Volumes")]

    [SerializeField, Range(0f, 1f)]
    private float jumpVolume = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float crouchVolume = 0.3f;

    [SerializeField, Range(0f, 1f)]
    private float hitVolume = 0.8f;

    [SerializeField, Range(0f, 1f)]
    private float waterEnterVolume = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float runVolume = 0.25f;

    [SerializeField, Range(0f, 1f)]
    private float swimVolume = 0.35f;

    private AudioSource oneShotSource;
    private AudioSource loopSource;

    private bool isRunningLoop;
    private bool isSwimmingLoop;

    private void Awake()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length == 1)
        {
            oneShotSource = sources[0];
            loopSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            oneShotSource = sources[0];
            loopSource = sources[1];
        }

        oneShotSource.playOnAwake = false;
        oneShotSource.spatialBlend = 0f;

        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.spatialBlend = 0f;
    }

    public void PlayJump()
    {
        if (jumpSound == null) return;

        if (Time.time - lastJumpSoundTime < jumpSoundCooldown)
            return;

        lastJumpSoundTime = Time.time;
        oneShotSource.PlayOneShot(jumpSound, jumpVolume);
    }

    public void PlayCrouch()
    {
        if (crouchSound == null) return;

        oneShotSource.PlayOneShot(crouchSound, crouchVolume);
    }

    public void PlayHit()
    {
        if (hitSound == null) return;

        oneShotSource.PlayOneShot(hitSound, hitVolume);
    }

    public void PlayWaterEnter()
    {
        if (waterEnterSound == null) return;

        oneShotSource.PlayOneShot(waterEnterSound, waterEnterVolume);
    }

    public void SetRunning(bool value)
    {
        if (value)
        {
            if (isRunningLoop) return;

            isRunningLoop = true;
            isSwimmingLoop = false;

            PlayRunLoop();
        }
        else
        {
            if (!isRunningLoop) return;

            isRunningLoop = false;

            if (!isSwimmingLoop)
                StopLoop();
        }
    }

    public void SetSwimming(bool value)
    {
        if (value)
        {
            if (isSwimmingLoop) return;

            isSwimmingLoop = true;
            isRunningLoop = false;

            PlaySwimLoop();
        }
        else
        {
            if (!isSwimmingLoop) return;

            isSwimmingLoop = false;

            if (!isRunningLoop)
                StopLoop();
        }
    }

    private void PlayRunLoop()
    {
        if (runSound == null) return;

        loopSource.clip = runSound;
        loopSource.volume = runVolume;
        loopSource.Play();
    }

    private void PlaySwimLoop()
    {
        if (swimSound == null) return;

        loopSource.clip = swimSound;
        loopSource.volume = swimVolume;
        loopSource.Play();
    }

    private void StopLoop()
    {
        loopSource.Stop();
        loopSource.clip = null;
    }
    public void PlayEnemyHit()
    {
        if (enemyHitSound == null) return;

        oneShotSource.PlayOneShot(enemyHitSound, enemyHitVolume);
    }

    public void PlayDamage()
    {
        if (damageSound == null) return;

        oneShotSource.PlayOneShot(damageSound, damageVolume);
    }
}