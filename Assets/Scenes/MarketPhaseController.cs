using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketPhaseController : MonoBehaviour
{
    public static MarketPhaseController Instance { get; private set; }

    [Header("Cycle")]
    [SerializeField] private float riseDurationSeconds = 120f;
    [SerializeField] private float fallDurationSeconds = 120f;
    [SerializeField] private float minMarketMultiplier = 0.7f;
    [SerializeField] private float maxMarketMultiplier = 1f;

    [Header("HUD")]
    [SerializeField] private TMP_Text hudPercentText;
    [SerializeField] private TMP_Text hudStatusText;
    [SerializeField] private Image hudFillImage;

    [Header("Pause Menu")]
    [SerializeField] private TMP_Text pausePercentText;
    [SerializeField] private TMP_Text pauseStatusText;
    [SerializeField] private TMP_Text pauseDescriptionText;

    private float cycleTimer;

    public float CurrentPercent { get; private set; }
    public float CurrentMultiplier { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RefreshUI();
    }

    private void Update()
    {
        float riseDuration = Mathf.Max(0.01f, riseDurationSeconds);
        float fallDuration = Mathf.Max(0.01f, fallDurationSeconds);
        float totalDuration = riseDuration + fallDuration;

        cycleTimer += Time.deltaTime;
        float phaseTime = cycleTimer % totalDuration;

        if (phaseTime <= riseDuration)
        {
            CurrentPercent = (phaseTime / riseDuration) * 100f;
        }
        else
        {
            float fallTime = phaseTime - riseDuration;
            CurrentPercent = 100f - (fallTime / fallDuration) * 100f;
        }

        CurrentMultiplier = Mathf.Lerp(minMarketMultiplier, maxMarketMultiplier, CurrentPercent / 100f);
        RefreshUI();
    }

    public string GetMarketStatus()
    {
        if (CurrentPercent < 30f)
            return "Market Down";

        if (CurrentPercent < 80f)
            return "Highly Competitive";

        return "Market Going Up";
    }

    public string GetMarketDescription()
    {
        if (CurrentPercent < 30f)
            return "Market is down. Build your resume now because apply odds are still low.";

        if (CurrentPercent < 80f)
            return "Market is competitive. Strengthen your resume before applying broadly.";

        return "Market is going up. Apply odds are stronger, but resume still matters.";
    }

    public float GetCurrentMarketPercent01()
    {
        return CurrentPercent / 100f;
    }

    private void RefreshUI()
    {
        string percentLabel = Mathf.RoundToInt(CurrentPercent) + "%";
        string status = GetMarketStatus();
        string description = GetMarketDescription();

        if (hudPercentText != null)
            hudPercentText.text = percentLabel;

        if (hudStatusText != null)
            hudStatusText.text = status;

        if (hudFillImage != null)
            hudFillImage.fillAmount = GetCurrentMarketPercent01();

        if (pausePercentText != null)
            pausePercentText.text = percentLabel;

        if (pauseStatusText != null)
            pauseStatusText.text = status;

        if (pauseDescriptionText != null)
            pauseDescriptionText.text = description;
    }
}
