using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class WatcherRevealEffect : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Vector3 targetOffset = new(0f, 1.3f, 0f);
    [SerializeField] private bool yawOnly = true;
    [SerializeField, Min(0f)] private float turnSpeed = 360f;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float visibleDuration = 5f;

    [Header("Sound")]
    [SerializeField] private AudioClip watcherSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.75f;
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;

    [Header("Testing")]
    [SerializeField] private bool testWithTKey = true;
    [SerializeField] private bool playOnStart;

    private AudioSource audioSource;
    private AudioClip fallbackSound;
    private Renderer[] renderers;
    private bool isVisible;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = soundVolume;
        audioSource.spatialBlend = spatialBlend;
        fallbackSound = CreateWatcherSound();
        renderers = GetComponentsInChildren<Renderer>(true);
        SetVisible(false);

        Renderer body = GetComponent<Renderer>();
        if (body != null)
        {
            MaterialPropertyBlock block = new();
            body.GetPropertyBlock(block);
            block.SetColor("_BaseColor", new Color(0.025f, 0.025f, 0.03f));
            block.SetColor("_Color", new Color(0.025f, 0.025f, 0.03f));
            body.SetPropertyBlock(block);
        }

        foreach (Renderer item in renderers)
        {
            if (item == body)
                continue;

            MaterialPropertyBlock block = new();
            item.GetPropertyBlock(block);
            block.SetColor("_BaseColor", new Color(0.45f, 0.01f, 0.01f));
            block.SetColor("_Color", new Color(0.45f, 0.01f, 0.01f));
            item.SetPropertyBlock(block);
        }
    }

    private void Start()
    {
        FindPlayer();

        if (playOnStart)
            Play();
    }

    private void Update()
    {
        if (isVisible)
            FacePlayer();

        if (!testWithTKey || isVisible)
            return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            Play();
#else
        if (Input.GetKeyDown(KeyCode.T))
            Play();
#endif
    }

    public void Play()
    {
        if (!isVisible)
            StartCoroutine(ShowSequence());
    }

    public void Hide()
    {
        StopAllCoroutines();
        audioSource.Stop();
        SetVisible(false);
    }

    private IEnumerator ShowSequence()
    {
        FindPlayer();
        SetVisible(true);
        FacePlayer(true);

        audioSource.clip = watcherSound != null ? watcherSound : fallbackSound;
        audioSource.volume = soundVolume;
        audioSource.spatialBlend = spatialBlend;
        audioSource.Play();

        float elapsed = 0f;
        while (elapsed < visibleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        audioSource.Stop();
        SetVisible(false);
    }

    private void FindPlayer()
    {
        if (playerTarget != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTarget = player.transform;
        else if (Camera.main != null)
            playerTarget = Camera.main.transform;
    }

    private void FacePlayer(bool immediate = false)
    {
        if (playerTarget == null)
            return;

        Vector3 direction = playerTarget.position + targetOffset - transform.position;
        if (yawOnly)
            direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = immediate || turnSpeed <= 0f
            ? targetRotation
            : Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.unscaledDeltaTime);
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;
        foreach (Renderer item in renderers)
            item.enabled = visible;
    }

    private static AudioClip CreateWatcherSound()
    {
        const int sampleRate = 22050;
        const float length = 2f;
        int sampleCount = Mathf.RoundToInt(sampleRate * length);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float pulse = 0.65f + Mathf.Sin(2f * Mathf.PI * 0.5f * t) * 0.35f;
            float drone = Mathf.Sin(2f * Mathf.PI * 48f * t) * 0.5f;
            float overtone = Mathf.Sin(2f * Mathf.PI * 73f * t) * 0.2f;
            samples[i] = (drone + overtone) * pulse * 0.35f;
        }

        AudioClip clip = AudioClip.Create("Generated Watcher Drone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
