using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
[DisallowMultipleComponent]
public sealed class LighterUIAnimator : MonoBehaviour
{
    [SerializeField] private Texture offTexture;
    [SerializeField] private Texture ignitionTexture;
    [SerializeField] private Texture onTexture;
    [SerializeField, Min(0.01f)] private float ignitionDuration = 0.16f;
    [SerializeField] private PlayerLightController lightController;

    private RawImage lighterImage;
    private Coroutine animationRoutine;

    private void Awake()
    {
        lighterImage = GetComponent<RawImage>();
    }

    private void Start()
    {
        if (lightController == null)
        {
            lightController = FindFirstObjectByType<PlayerLightController>();
        }

        if (lightController == null)
        {
            SetTexture(offTexture);
            return;
        }

        lightController.LightStateChanged += AnimateState;
        SetTexture(lightController.IsLightOn ? onTexture : offTexture);
    }

    private void OnDestroy()
    {
        if (lightController != null)
        {
            lightController.LightStateChanged -= AnimateState;
        }
    }

    private void AnimateState(bool isOn)
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (!isOn)
        {
            SetTexture(offTexture);
            return;
        }

        animationRoutine = StartCoroutine(PlayIgnition());
    }

    private IEnumerator PlayIgnition()
    {
        SetTexture(ignitionTexture);
        yield return new WaitForSeconds(ignitionDuration);
        SetTexture(onTexture);
        animationRoutine = null;
    }

    private void SetTexture(Texture texture)
    {
        if (texture != null)
        {
            lighterImage.texture = texture;
        }
    }
}
