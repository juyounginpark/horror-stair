using DynamicWeatherSystem;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RainyNightSetup : MonoBehaviour
{
    private WeatherStateData rainyNightState;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateWeatherSystem()
    {
        if (FindFirstObjectByType<RainyNightSetup>() != null)
        {
            return;
        }

        GameObject weatherObject = new GameObject("Night Weather");
        weatherObject.AddComponent<RainyNightSetup>();
    }

    private void Awake()
    {
        SetNightLighting();

        CreateModule<FogModule>("Fog Module");
        CreateModule<SkyModule>("Sky Module");

        // WeatherManager searches for its modules in Awake, so it must be added last.
        WeatherManager manager = gameObject.AddComponent<WeatherManager>();
        rainyNightState = CreateRainyNightState();
        manager.SetWeather(rainyNightState, 0f);
    }

    private void OnDestroy()
    {
        if (rainyNightState != null)
        {
            Destroy(rainyNightState);
        }
    }

    private T CreateModule<T>(string objectName) where T : Component
    {
        GameObject moduleObject = new GameObject(objectName);
        moduleObject.transform.SetParent(transform, false);
        return moduleObject.AddComponent<T>();
    }

    private static void SetNightLighting()
    {
        foreach (Light sceneLight in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (sceneLight.type != LightType.Directional)
            {
                continue;
            }

            sceneLight.color = new Color(0.55f, 0.58f, 0.68f);
            sceneLight.intensity = 0.18f;
            sceneLight.useColorTemperature = false;
            sceneLight.shadows = LightShadows.Soft;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.028f, 0.03f, 0.042f);
        RenderSettings.reflectionIntensity = 0.25f;
    }

    private static WeatherStateData CreateRainyNightState()
    {
        WeatherStateData state = ScriptableObject.CreateInstance<WeatherStateData>();
        state.name = "Night Runtime State";
        state.hideFlags = HideFlags.HideAndDontSave;
        state.stateName = "Night";

        state.fogEnabled = true;
        state.fogColor = new Color(0.045f, 0.047f, 0.06f);
        state.fogDensity = 0.008f;

        state.lightColor = new Color(0.55f, 0.58f, 0.68f);
        state.lightIntensity = 0.18f;
        state.ambientColor = new Color(0.028f, 0.03f, 0.042f);

        state.skyboxTint = new Color(0.32f, 0.34f, 0.42f);
        state.skyboxExposure = 0.18f;

        state.rainIntensity = 0f;

        return state;
    }
}
