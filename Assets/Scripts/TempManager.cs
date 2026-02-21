using UnityEngine;

public class TempManager : MonoBehaviour
{
    private Items item;

    public float currentHeat = 0;
    public bool timerEnabled = false;
    private Renderer rend;
    private Color baseColor;

    private void Awake()
    {
        item = GetComponent<Items>();
        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            baseColor = rend.material.color;
        }

        currentHeat = item.heatTimer;
    }

    void Update()
    {
        if (!timerEnabled)
        {
            currentHeat = item.heatTimer;
            return;
        }

        currentHeat -= Time.deltaTime;

        item.heatTimer = currentHeat;
        ModifyColor();

        if (currentHeat <= 0)
            this.enabled = false;
    }

    private void ModifyColor()
    {
        if (rend == null) return;

        float maxTemp = 200f;
        float heatPercent = Mathf.Clamp01(item.heatTimer / maxTemp);

        Color heatTint = new Color(1f, 0.35f, 0.05f);

        Color finalColor = Color.Lerp(baseColor, heatTint, heatPercent * 0.5f);

        rend.material.color = finalColor;
    }
}
