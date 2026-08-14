using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(PlayerController))]
[DisallowMultipleComponent]
public sealed class StaminaVignette : MonoBehaviour
{
    [Header("Low Stamina")]
    [SerializeField, Range(0.01f, 1f)] private float effectStartRatio = 0.5f;
    [SerializeField, Range(0f, 1f)] private float exhaustedIntensity = 0.58f;
    [SerializeField, Range(0f, 1f)] private float exhaustedSmoothness = 0.65f;

    [Header("Breathing Pulse")]
    [SerializeField, Min(0.01f)] private float breathingSpeed = 0.8f;
    [SerializeField, Range(0f, 0.25f)] private float breathingAmplitude = 0.07f;
    [SerializeField, Range(0f, 0.15f)] private float randomAmplitude = 0.025f;
    [SerializeField, Range(0f, 0.25f)] private float smoothnessPulseAmplitude = 0.06f;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.09f;

    private PlayerController player;
    private Vignette vignette;
    private float restingIntensity;
    private float restingSmoothness;
    private float intensityVelocity;
    private float smoothnessVelocity;
    private float noiseSeed;

    private void Start()
    {
        player = GetComponent<PlayerController>();
        noiseSeed = UnityEngine.Random.Range(0f, 100f);

        Volume targetVolume = FindHighestPriorityGlobalVolume();
        if (targetVolume == null)
        {
            Debug.LogWarning("Stamina Vignette requires an active Global Volume.", this);
            enabled = false;
            return;
        }

        VolumeProfile runtimeProfile = targetVolume.profile;
        if (!runtimeProfile.TryGet(out vignette))
        {
            vignette = runtimeProfile.Add<Vignette>(true);
        }

        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.smoothness.overrideState = true;
        restingIntensity = vignette.intensity.value;
        restingSmoothness = vignette.smoothness.value;
    }

    private void Update()
    {
        if (vignette == null)
        {
            return;
        }

        float lowStaminaAmount = Mathf.InverseLerp(
            effectStartRatio,
            0f,
            player.StaminaRatio);

        float time = Time.unscaledTime;
        float breathing = Mathf.Sin(time * breathingSpeed * Mathf.PI * 2f);
        float randomPulse = (Mathf.PerlinNoise(noiseSeed, time * 1.7f) - 0.5f) * 2f;

        float baseLowIntensity = Mathf.Lerp(
            restingIntensity,
            exhaustedIntensity,
            lowStaminaAmount);

        float pulse = (breathing * breathingAmplitude + randomPulse * randomAmplitude)
            * lowStaminaAmount;

        float targetIntensity = Mathf.Clamp01(baseLowIntensity + pulse);
        vignette.intensity.value = Mathf.SmoothDamp(
            vignette.intensity.value,
            targetIntensity,
            ref intensityVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);

        float baseLowSmoothness = Mathf.Lerp(
            restingSmoothness,
            exhaustedSmoothness,
            lowStaminaAmount);
        float smoothnessPulse = breathing * smoothnessPulseAmplitude * lowStaminaAmount;
        float targetSmoothness = Mathf.Clamp01(baseLowSmoothness + smoothnessPulse);

        vignette.smoothness.value = Mathf.SmoothDamp(
            vignette.smoothness.value,
            targetSmoothness,
            ref smoothnessVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);
    }

    private void OnDisable()
    {
        if (vignette != null)
        {
            vignette.intensity.value = restingIntensity;
            vignette.smoothness.value = restingSmoothness;
        }
    }

    private static Volume FindHighestPriorityGlobalVolume()
    {
        Volume selectedVolume = null;
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);

        foreach (Volume volume in volumes)
        {
            if (!volume.isGlobal || !volume.enabled || !volume.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (selectedVolume == null || volume.priority > selectedVolume.priority)
            {
                selectedVolume = volume;
            }
        }

        return selectedVolume;
    }
}
