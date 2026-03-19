using UnityEngine;

public class TempManager : MonoBehaviour
{
    private Items itemScript;

    public float maxHeat = 360f;
    private float heatPercent = 0f;
    public bool timerEnabled = false;
    private Renderer rend;
    private Material mat;
    private Gradient heatGradient;
    private WorkstationScript WS;

    private float difficultyBounus = 0.1f;

    private void Awake()
    {
        WS = FindFirstObjectByType<WorkstationScript>();
        itemScript = GetComponent<Items>();
        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
            mat.EnableKeyword("_EMISSION");
        }

        CreateGradient();
    }

    void Update()
    {
        if (WS.currentSmithingMode == SmithingMode.Normal)
            difficultyBounus = 0.1f;
        else
            difficultyBounus = 0.4f;

        if (itemScript.heatTimer <= 0)
            return;

        if (timerEnabled)
        {
            float k = 0.05f * difficultyBounus;
            float newHeat = itemScript.heatTimer * Mathf.Exp(-k * Time.deltaTime);

            float heatLost = itemScript.heatTimer - newHeat;
            float maxHeatLossPerSecond = 2.3f; // maximum heat loss per second

            heatLost = Mathf.Clamp(heatLost, 0, maxHeatLossPerSecond * Time.deltaTime);

            itemScript.heatTimer -= heatLost;
            itemScript.heatTimer = Mathf.Clamp(itemScript.heatTimer, 0, maxHeat * 2);
        }

        heatPercent = Mathf.Clamp01(itemScript.heatTimer / 160f);
        Color emissionColor = heatGradient.Evaluate(heatPercent);
        float intensity = Mathf.Lerp(0f, 4f, heatPercent);

        mat.SetColor("_EmissionColor", emissionColor * intensity);
    }


    private void CreateGradient()
    {
        heatGradient = new Gradient();

        heatGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.black, 0f),
                new GradientColorKey(new Color(0.4f, 0f, 0f), 0.25f), // dark red
                new GradientColorKey(Color.red, 0.5f),
                new GradientColorKey(new Color(1f, 0.5f, 0f), 0.75f), // orange
                new GradientColorKey(Color.yellow, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f,0f),
                new GradientAlphaKey(1f,1f)
            }
        );
    }
}
