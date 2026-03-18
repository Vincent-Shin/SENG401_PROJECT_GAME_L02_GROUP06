using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CandlestickSpawnTest : MonoBehaviour
{
    private enum EventDirection
    {
        Bullish,
        Bearish,
        Volatile
    }

    [Serializable]
    private struct FloatRange
    {
        public float x;
        public float y;

        public float RandomValue()
        {
            float min = Mathf.Min(x, y);
            float max = Mathf.Max(x, y);
            return UnityEngine.Random.Range(min, max);
        }
    }

    [Serializable]
    private struct CandleSnapshot
    {
        public float open;
        public float high;
        public float low;
        public float close;

        public CandleSnapshot(float open, float high, float low, float close)
        {
            this.open = open;
            this.high = high;
            this.low = low;
            this.close = close;
        }
    }

    [Serializable]
    private class EventDefinition
    {
        public string title;
        [TextArea(2, 4)] public string warningText;
        [TextArea(2, 4)] public string activeText;
        public Sprite eventSprite;
        public EventDirection direction = EventDirection.Bullish;
        public int warningCandles = 3;
        public int activeCandles = 5;
        public float bullishChanceOverride = 0.5f;
        public float driftBoost = 1.2f;
        public float volatilityBoost = 1.2f;
        public float wickBoost = 1f;
    }

    [Header("Graph")]
    [SerializeField] private RectTransform candleViewport;
    [SerializeField] private int graphPointCount = 18;
    [SerializeField] private bool graphFillViewport = true;
    [SerializeField] private float graphHorizontalPadding = 12f;
    [SerializeField] private float lineGraphThickness = 4f;
    [SerializeField] private bool graphUseFixedRange = true;
    [SerializeField] private float graphMinPrice = 80f;
    [SerializeField] private float graphMaxPrice = 140f;
    [SerializeField] private float graphAxisStep = 10f;
    [SerializeField] private bool graphUseSoftDisplayBounds = true;
    [SerializeField] private float graphInitialDisplayMin = 60f;
    [SerializeField] private float graphInitialDisplayMax = 200f;
    [SerializeField] private bool graphEnableMouseZoom = true;
    [SerializeField] private float graphMouseZoomSpeed = 0.15f;
    [SerializeField] private float graphMaxZoom = 4f;
    [HideInInspector, SerializeField] private GameObject candlePrefab;
    [HideInInspector, SerializeField] private int visibleCandleCount = 18;
    [HideInInspector, SerializeField] private int initialSeedCandles;
    [HideInInspector, SerializeField] private float candleWidth = 9f;
    [HideInInspector, SerializeField] private float candleSpacing = 4f;
    [HideInInspector, SerializeField] private bool alignCandlesFromLeft = true;
    [HideInInspector, SerializeField] private bool stretchCandlesToViewport;
    [HideInInspector, SerializeField] private float chartLeftPadding = 12f;
    [HideInInspector, SerializeField] private float chartRightPadding = 12f;
    [SerializeField] private float startingPrice = 100f;
    [SerializeField] private bool respawnOnStart = true;
    [SerializeField] private bool animateContinuously = true;
    [SerializeField] private float updateIntervalSeconds = 1f;

    [Header("ETF Behavior")]
    [SerializeField] private float baseBullishChance = 0.5f;
    [SerializeField] private float baselineUpwardDrift = 0.02f;
    [SerializeField] private float minimumBodyMove = 1.5f;
    [SerializeField] private FloatRange bullishMoveRange = new FloatRange { x = 1.0f, y = 3.2f };
    [SerializeField] private FloatRange bearishMoveRange = new FloatRange { x = -3.0f, y = -1.0f };
    [SerializeField] private FloatRange upperWickRange = new FloatRange { x = 1.5f, y = 4.0f };
    [SerializeField] private FloatRange lowerWickRange = new FloatRange { x = 1.2f, y = 4.0f };
    [SerializeField] private float maximumWickMove = 2.2f;
    [SerializeField] private FloatRange bodyAmplifierRange = new FloatRange { x = 1.2f, y = 3.2f };
    [SerializeField] private FloatRange calmBodyAmplifierRange = new FloatRange { x = 1.0f, y = 2.2f };
    [SerializeField] private FloatRange bodyVarianceRange = new FloatRange { x = 0.7f, y = 1.6f };
    [SerializeField] private float impulseCandleChance = 0.22f;
    [SerializeField] private FloatRange impulseMultiplierRange = new FloatRange { x = 1.2f, y = 2.1f };
    [SerializeField] private FloatRange calmSwingRange = new FloatRange { x = -1.8f, y = 1.8f };
    [SerializeField] private float noEventZigZagBias = 1.0f;
    [SerializeField] private float noEventReversalBoost = 0.9f;

    [Header("Graph Look")]
    [HideInInspector, SerializeField] private float wickWidth = 1f;
    [HideInInspector, SerializeField] private float minimumVisibleWickPixels = 2f;
    [HideInInspector, SerializeField] private float bodyWidthRatio = 0.55f;
    [HideInInspector, SerializeField] private float minimumBodyHeight = 3f;
    [HideInInspector, SerializeField] private float maximumBodyMove = 2.5f;
    [SerializeField] private float pricePaddingPercent = 0.05f;
    [SerializeField] private float chartTopPadding = 24f;
    [SerializeField] private float chartBottomPadding = 22f;
    [HideInInspector, SerializeField] private RectTransform currentPriceLine;
    [SerializeField] private RectTransform priceLineContainer;
    [HideInInspector, SerializeField] private RectTransform movingAverageLineContainer;
    [HideInInspector, SerializeField] private float priceLineThickness = 1.5f;
    [HideInInspector, SerializeField] private float movingAverageThickness = 1f;
    [HideInInspector, SerializeField] private bool showPriceLine;
    [HideInInspector, SerializeField] private bool showMovingAverage = true;
    [HideInInspector, SerializeField] private bool useLineGraphOnly = true;
    [HideInInspector, SerializeField] private int movingAveragePeriods = 3;
    [HideInInspector, SerializeField] private Color priceLineColor = new Color32(87, 170, 87, 255);
    [HideInInspector, SerializeField] private Color movingAverageColor = new Color32(180, 225, 255, 255);
    [HideInInspector, SerializeField] private bool useNeutralWicks;
    [HideInInspector, SerializeField] private Color wickColor = new Color32(120, 120, 120, 255);
    [HideInInspector, SerializeField] private bool useFixedPriceBounds = true;
    [HideInInspector, SerializeField] private float fixedMinPrice = 50f;
    [HideInInspector, SerializeField] private float fixedMaxPrice = 300f;
    [HideInInspector, SerializeField] private float fixedAxisStep = 50f;
    [SerializeField] private Color gridLineColor = new Color32(255, 255, 255, 38);
    [SerializeField] private Color axisLabelColor = new Color32(210, 210, 210, 255);
    [HideInInspector, SerializeField] private Color axisLineColor = new Color32(35, 35, 35, 255);
    [SerializeField] private float gridLineThickness = 1f;
    [HideInInspector, SerializeField] private float axisLineThickness = 2f;
    [HideInInspector, SerializeField] private bool showAxisLines;
    [SerializeField] private float axisLabelWidth = 36f;
    [SerializeField] private float axisLabelFontSize = 14f;
    [HideInInspector, SerializeField] private float displayZoom = 1f;
    [HideInInspector, SerializeField] private bool enableMouseChartControls = true;
    [HideInInspector, SerializeField] private float mouseZoomSpeed = 0.2f;
    [HideInInspector, SerializeField] private float mousePanSpeed = 0.6f;
    [HideInInspector, SerializeField] private float maxManualZoom = 4f;
    [HideInInspector, SerializeField] private bool lockVerticalRange = true;
    [HideInInspector, SerializeField] private float initialVerticalRangeSize = 30f;
    [HideInInspector, SerializeField] private float verticalRangePadding = 2f;
    [HideInInspector, SerializeField] private float verticalRangeTriggerPercent = 0.08f;
    [HideInInspector, SerializeField] private float verticalRangeExpandMultiplier = 1.2f;

    [Header("Header UI")]
    [SerializeField] private TMP_Text assetNameText;
    [SerializeField] private string assetName = "ETF FUND";
    [SerializeField] private TMP_Text currentPriceText;
    [SerializeField] private TMP_Text changeText;
    [SerializeField] private TMP_Text eventTitleText;
    [SerializeField] private TMP_Text eventBodyText;
    [SerializeField] private TMP_Text eventHintText;
    [SerializeField] private Image eventImage;
    [SerializeField] private string currencySymbol = "$";
    [SerializeField] private Color bullishColor = new Color32(110, 220, 95, 255);
    [SerializeField] private Color bearishColor = new Color32(255, 77, 77, 255);
    [SerializeField] private Color neutralColor = new Color32(220, 225, 230, 255);
    [SerializeField] private Color warningColor = new Color32(255, 214, 102, 255);

    [Header("Personal Info UI")]
    [SerializeField] private TMP_Text cashBalanceText;
    [SerializeField] private TMP_Text etfPanelText;
    [SerializeField] private TMP_Text targetText;

    [Header("Stock Info UI")]
    [SerializeField] private TMP_Text nameTickerText;
    [SerializeField] private TMP_Text lastBidText;
    [SerializeField] private TMP_Text askVolumeText;

    [Header("Trade Controls")]
    [SerializeField] private TMP_InputField amountInputField;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button buyButton;

    [Header("Trading Rules")]
    [SerializeField] private float startingCash = 100f;
    [SerializeField] private float winTarget = 35f;
    [SerializeField] private float lossLimit = 25f;
    [SerializeField] private float minimumTradeAmount = 1f;
    [SerializeField] private float bidAskSpread = 0.30f;
    [SerializeField] private float baseVolumeMillions = 1.2f;

    [Header("Events")]
    [SerializeField] private List<EventDefinition> events = new List<EventDefinition>();
    [SerializeField] private int minCandlesBetweenEvents = 4;
    [SerializeField] private int maxCandlesBetweenEvents = 8;
    [SerializeField] private float eventImpactMultiplier = 2f;

    private readonly List<CandleSnapshot> candleHistory = new List<CandleSnapshot>();
    private readonly List<RectTransform> spawnedCandles = new List<RectTransform>();
    private readonly List<RectTransform> priceLineSegments = new List<RectTransform>();
    private readonly List<RectTransform> movingAverageSegments = new List<RectTransform>();
    private readonly List<RectTransform> gridLines = new List<RectTransform>();
    private readonly List<TextMeshProUGUI> axisLabels = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> xAxisLabels = new List<TextMeshProUGUI>();

    private float currentPrice;
    private float initialPrice;
    private float timer;
    private float cashBalance;
    private float sharesHeld;
    private float averageBuyPrice;
    private float realizedProfit;
    private float lastRenderedVolume;
    private float lastCloseDelta;
    private float displayMinPrice;
    private float displayMaxPrice;
    private float manualZoom = 1f;
    private float manualPanPriceOffset;
    private bool hasDisplayRange;
    private bool lastCandleBullish = true;

    private EventDefinition queuedEvent;
    private EventDefinition activeEvent;
    private int candlesUntilNextEvent;
    private int activeEventCandlesRemaining;
    private RectTransform decorationRoot;
    private RectTransform gridLineContainer;
    private RectTransform yAxisContainer;
    private RectTransform xAxisContainer;
    private RectTransform xAxisLine;
    private RectTransform yAxisLine;

    private void Awake()
    {
        ApplyGraphModeSettings();
        ResetRuntimeChartReferences();
        AutoBindSceneReferences();
        SanitizeChartHierarchy();
        ConfigureButtons();
        EnsureDefaultEvents();
    }

    private void Start()
    {
        ApplyGraphModeSettings();
        ResetSimulation();
    }

    private void OnValidate()
    {
        ApplyGraphModeSettings();

        visibleCandleCount = Mathf.Max(8, visibleCandleCount);
        initialSeedCandles = Mathf.Clamp(initialSeedCandles, 0, visibleCandleCount);
        candleWidth = Mathf.Max(2f, candleWidth);
        candleSpacing = Mathf.Max(0f, candleSpacing);
        chartLeftPadding = Mathf.Max(0f, chartLeftPadding);
        chartRightPadding = Mathf.Max(0f, chartRightPadding);
        updateIntervalSeconds = Mathf.Max(0.1f, updateIntervalSeconds);
        movingAveragePeriods = Mathf.Max(2, movingAveragePeriods);
        minimumBodyMove = Mathf.Max(0.05f, minimumBodyMove);
        minimumBodyHeight = Mathf.Max(1f, minimumBodyHeight);
        wickWidth = Mathf.Max(0.5f, wickWidth);
        minimumVisibleWickPixels = Mathf.Max(0f, minimumVisibleWickPixels);
        bodyWidthRatio = Mathf.Clamp(bodyWidthRatio, 0.2f, 1f);
        maximumBodyMove = Mathf.Max(minimumBodyMove, maximumBodyMove);
        maximumWickMove = Mathf.Max(0.1f, maximumWickMove);
        bodyVarianceRange.x = Mathf.Max(0.1f, bodyVarianceRange.x);
        bodyVarianceRange.y = Mathf.Max(0.1f, bodyVarianceRange.y);
        impulseCandleChance = Mathf.Clamp01(impulseCandleChance);
        fixedMinPrice = Mathf.Max(0.01f, fixedMinPrice);
        fixedMaxPrice = Mathf.Max(fixedMinPrice + 1f, fixedMaxPrice);
        fixedAxisStep = Mathf.Max(1f, fixedAxisStep);
        gridLineThickness = Mathf.Max(0.5f, gridLineThickness);
        axisLineThickness = Mathf.Max(1f, axisLineThickness);
        axisLabelWidth = Mathf.Max(16f, axisLabelWidth);
        axisLabelFontSize = Mathf.Max(8f, axisLabelFontSize);
        displayZoom = Mathf.Max(1f, displayZoom);
        mouseZoomSpeed = Mathf.Max(0.01f, mouseZoomSpeed);
        mousePanSpeed = Mathf.Max(0.01f, mousePanSpeed);
        maxManualZoom = Mathf.Max(1f, maxManualZoom);
        initialVerticalRangeSize = Mathf.Max(1f, initialVerticalRangeSize);
        verticalRangePadding = Mathf.Max(0f, verticalRangePadding);
        verticalRangeTriggerPercent = Mathf.Clamp(verticalRangeTriggerPercent, 0.01f, 0.45f);
        verticalRangeExpandMultiplier = Mathf.Max(1.02f, verticalRangeExpandMultiplier);
    }

    private void ApplyGraphModeSettings()
    {
        graphPointCount = Mathf.Max(8, graphPointCount);
        graphHorizontalPadding = Mathf.Max(0f, graphHorizontalPadding);
        graphMinPrice = Mathf.Max(0.01f, graphMinPrice);
        graphMaxPrice = Mathf.Max(graphMinPrice + 1f, graphMaxPrice);
        graphAxisStep = Mathf.Max(1f, graphAxisStep);
        graphInitialDisplayMin = Mathf.Max(0.01f, graphInitialDisplayMin);
        graphInitialDisplayMax = Mathf.Max(graphInitialDisplayMin + 1f, graphInitialDisplayMax);
        graphMouseZoomSpeed = Mathf.Max(0.01f, graphMouseZoomSpeed);
        graphMaxZoom = Mathf.Max(1f, graphMaxZoom);
        lineGraphThickness = Mathf.Max(1f, lineGraphThickness);

        visibleCandleCount = graphPointCount;
        initialSeedCandles = 0;
        stretchCandlesToViewport = graphFillViewport;
        alignCandlesFromLeft = true;
        chartLeftPadding = graphHorizontalPadding;
        chartRightPadding = graphHorizontalPadding;
        useLineGraphOnly = true;
        showPriceLine = false;
        showMovingAverage = false;
        showAxisLines = false;
        enableMouseChartControls = graphEnableMouseZoom;
        displayZoom = 1f;
        manualPanPriceOffset = 0f;
        maxManualZoom = graphMaxZoom;
        mouseZoomSpeed = graphMouseZoomSpeed;
        mousePanSpeed = 0f;
        useFixedPriceBounds = graphUseFixedRange && !graphUseSoftDisplayBounds;
        fixedMinPrice = graphMinPrice;
        fixedMaxPrice = graphMaxPrice;
        fixedAxisStep = graphAxisStep;
        lockVerticalRange = graphUseSoftDisplayBounds;
        initialVerticalRangeSize = graphInitialDisplayMax - graphInitialDisplayMin;
        verticalRangePadding = 4f;
        verticalRangeTriggerPercent = 0.12f;
        verticalRangeExpandMultiplier = 1.15f;
    }

    private void Update()
    {
        HandleMouseChartControls();

        if (!animateContinuously)
            return;

        timer += Time.deltaTime;
        if (timer < updateIntervalSeconds)
            return;

        timer = 0f;
        AdvanceOneCandle();
    }

    public void BuyFromInput()
    {
        float amount = ParseTradeAmount();
        if (amount < minimumTradeAmount)
            return;

        float spend = Mathf.Min(amount, cashBalance);
        if (spend < minimumTradeAmount)
            return;

        float units = spend / Mathf.Max(0.01f, currentPrice);
        float currentCostBasis = sharesHeld * averageBuyPrice;

        sharesHeld += units;
        averageBuyPrice = sharesHeld > 0f ? (currentCostBasis + spend) / sharesHeld : 0f;
        cashBalance -= spend;

        RefreshAllUi();
    }

    public void SellFromInput()
    {
        float amount = ParseTradeAmount();
        if (amount < minimumTradeAmount || sharesHeld <= 0f)
            return;

        float requestedUnits = amount / Mathf.Max(0.01f, currentPrice);
        float unitsToSell = Mathf.Min(requestedUnits, sharesHeld);
        if (unitsToSell <= 0f)
            return;

        float proceeds = unitsToSell * currentPrice;
        realizedProfit += (currentPrice - averageBuyPrice) * unitsToSell;
        sharesHeld -= unitsToSell;
        cashBalance += proceeds;

        if (sharesHeld <= 0.0001f)
        {
            sharesHeld = 0f;
            averageBuyPrice = 0f;
        }

        RefreshAllUi();
    }

    private void ResetSimulation()
    {
        currentPrice = useFixedPriceBounds ? Mathf.Clamp(startingPrice, fixedMinPrice, fixedMaxPrice) : startingPrice;
        initialPrice = currentPrice;
        cashBalance = startingCash;
        sharesHeld = 0f;
        averageBuyPrice = 0f;
        realizedProfit = 0f;
        timer = 0f;
        candleHistory.Clear();
        lastCloseDelta = 0f;
        hasDisplayRange = false;
        manualZoom = 1f;
        manualPanPriceOffset = 0f;
        lastCandleBullish = true;

        if (graphUseSoftDisplayBounds)
        {
            displayMinPrice = graphInitialDisplayMin;
            displayMaxPrice = graphInitialDisplayMax;
            hasDisplayRange = true;
        }

        queuedEvent = null;
        activeEvent = null;
        activeEventCandlesRemaining = 0;
        candlesUntilNextEvent = UnityEngine.Random.Range(minCandlesBetweenEvents, maxCandlesBetweenEvents + 1);

        ClearRenderedCandles();
        ClearSegments(priceLineSegments);
        ClearSegments(movingAverageSegments);

        if (respawnOnStart)
        {
            int seedCount = initialSeedCandles > 0 ? initialSeedCandles : GetHistoryCapacity();
            for (int i = 0; i < seedCount; i++)
                AppendRandomCandle();
        }

        RefreshAllUi();
    }

    private void AdvanceOneCandle()
    {
        AppendRandomCandle();
        RefreshAllUi();
    }

    private void AppendRandomCandle()
    {
        StepEventState();

        float open = currentPrice;
        bool hasActiveEvent = activeEvent != null;
        float bodyAmplifier = hasActiveEvent ? bodyAmplifierRange.RandomValue() : calmBodyAmplifierRange.RandomValue();
        float bullishChance = hasActiveEvent ? activeEvent.bullishChanceOverride : baseBullishChance;
        float directionalBias = hasActiveEvent ? activeEvent.driftBoost * eventImpactMultiplier : baselineUpwardDrift;
        float volatility = hasActiveEvent ? activeEvent.volatilityBoost * eventImpactMultiplier : 1f;

        bool bullishMove = UnityEngine.Random.value < bullishChance;

        if (!hasActiveEvent)
        {
            float reversalChance = Mathf.Clamp01(noEventReversalBoost * 0.35f);
            if (UnityEngine.Random.value < reversalChance)
                bullishMove = !lastCandleBullish;

            float zigZagChance = Mathf.Clamp01(noEventZigZagBias * 0.25f);
            if (UnityEngine.Random.value < zigZagChance)
                bullishMove = !bullishMove;
        }

        float move = bullishMove ? bullishMoveRange.RandomValue() : bearishMoveRange.RandomValue();
        move *= bodyAmplifier;
        move *= bodyVarianceRange.RandomValue();

        if (UnityEngine.Random.value < impulseCandleChance)
            move *= impulseMultiplierRange.RandomValue();

        if (!hasActiveEvent)
            move += calmSwingRange.RandomValue();

        move += directionalBias;

        float close = Mathf.Max(1f, open + move);
        if (Mathf.Abs(close - open) < minimumBodyMove)
            close = open + Mathf.Sign((close - open) == 0f ? (bullishMove ? 1f : -1f) : (close - open)) * minimumBodyMove;
        if (Mathf.Abs(close - open) > maximumBodyMove)
            close = open + Mathf.Sign(close - open) * maximumBodyMove;
        if (useFixedPriceBounds)
            close = Mathf.Clamp(close, fixedMinPrice, fixedMaxPrice);

        float wickBoost = hasActiveEvent ? activeEvent.wickBoost * eventImpactMultiplier : 1f;
        float upperWick = Mathf.Min(maximumWickMove, upperWickRange.RandomValue() * volatility * wickBoost);
        float lowerWick = Mathf.Min(maximumWickMove, lowerWickRange.RandomValue() * volatility * wickBoost);

        float high = Mathf.Max(open, close) + upperWick;
        float low = Mathf.Max(0.01f, Mathf.Min(open, close) - lowerWick);

        if (useFixedPriceBounds)
        {
            high = Mathf.Clamp(high, fixedMinPrice, fixedMaxPrice);
            low = Mathf.Clamp(low, fixedMinPrice, fixedMaxPrice);
            open = Mathf.Clamp(open, fixedMinPrice, fixedMaxPrice);
        }

        lastCloseDelta = close - open;
        lastCandleBullish = close >= open;
        currentPrice = close;

        candleHistory.Add(new CandleSnapshot(open, high, low, close));
        if (candleHistory.Count > GetHistoryCapacity())
            candleHistory.RemoveAt(0);

        float baseVolume = baseVolumeMillions + Mathf.Abs(lastCloseDelta) * 0.12f;
        if (hasActiveEvent)
            baseVolume += activeEvent.volatilityBoost * 0.15f;
        lastRenderedVolume = Mathf.Max(0.4f, baseVolume);
    }

    private void StepEventState()
    {
        if (activeEvent != null)
        {
            activeEventCandlesRemaining--;
            if (activeEventCandlesRemaining <= 0)
            {
                activeEvent = null;
                candlesUntilNextEvent = UnityEngine.Random.Range(minCandlesBetweenEvents, maxCandlesBetweenEvents + 1);
            }

            return;
        }

        if (queuedEvent != null)
        {
            candlesUntilNextEvent--;
            if (candlesUntilNextEvent <= 0)
            {
                activeEvent = queuedEvent;
                queuedEvent = null;
                activeEventCandlesRemaining = Mathf.Max(1, activeEvent.activeCandles);
            }

            return;
        }

        candlesUntilNextEvent--;
        if (candlesUntilNextEvent <= 0)
        {
            if (events.Count == 0)
            {
                candlesUntilNextEvent = UnityEngine.Random.Range(minCandlesBetweenEvents, maxCandlesBetweenEvents + 1);
                return;
            }

            queuedEvent = events[UnityEngine.Random.Range(0, events.Count)];
            candlesUntilNextEvent = Mathf.Max(1, queuedEvent.warningCandles);
        }
    }

    private void RefreshAllUi()
    {
        SyncChartRects();
        RenderFixedAxisAndGrid();
        RenderCandles();
        RefreshHeaderUi();
        RefreshPersonalUi();
        RefreshStockInfoUi();
        RefreshEventUi();
    }

    private void GetDisplayPriceWindow(out float displayMin, out float displayMax)
    {
        float minPrice = float.MaxValue;
        float maxPrice = float.MinValue;

        if (candleHistory.Count == 0)
        {
            minPrice = currentPrice;
            maxPrice = currentPrice;
        }
        else
        {
            for (int i = 0; i < candleHistory.Count; i++)
            {
                CandleSnapshot candle = candleHistory[i];
                minPrice = Mathf.Min(minPrice, candle.low);
                maxPrice = Mathf.Max(maxPrice, candle.high);
            }
        }

        float dataRange = Mathf.Max(0.01f, maxPrice - minPrice);
        float dataMinWithPadding = minPrice - dataRange * pricePaddingPercent;
        float dataMaxWithPadding = maxPrice + dataRange * pricePaddingPercent;

        if (useFixedPriceBounds)
        {
            float zoomBlend = 1f - (1f / displayZoom);
            displayMin = Mathf.Lerp(fixedMinPrice, dataMinWithPadding, zoomBlend);
            displayMax = Mathf.Lerp(fixedMaxPrice, dataMaxWithPadding, zoomBlend);

            displayMin = Mathf.Max(fixedMinPrice, displayMin);
            displayMax = Mathf.Min(fixedMaxPrice, displayMax);

            if (displayMax <= displayMin)
            {
                displayMin = fixedMinPrice;
                displayMax = fixedMaxPrice;
            }
        }
        else
        {
            displayMin = dataMinWithPadding;
            displayMax = dataMaxWithPadding;

            if (lockVerticalRange)
            {
                if (!hasDisplayRange)
                {
                    float center = currentPrice;
                    float halfRange = initialVerticalRangeSize * 0.5f;
                    displayMinPrice = center - halfRange;
                    displayMaxPrice = center + halfRange;
                    hasDisplayRange = true;
                }

                ExpandDisplayRangeIfNeeded(displayMin, displayMax);
                displayMin = displayMinPrice;
                displayMax = displayMaxPrice;
            }
        }

        float effectiveZoom = Mathf.Max(1f, displayZoom * manualZoom);
        if (effectiveZoom > 1f)
        {
            float center = (displayMin + displayMax) * 0.5f;
            float halfRange = Mathf.Max(0.01f, (displayMax - displayMin) * 0.5f / effectiveZoom);
            displayMin = center - halfRange;
            displayMax = center + halfRange;
        }

        if (Mathf.Abs(manualPanPriceOffset) > 0.0001f)
        {
            displayMin += manualPanPriceOffset;
            displayMax += manualPanPriceOffset;
        }

        if (useFixedPriceBounds)
        {
            float currentRange = Mathf.Max(0.01f, displayMax - displayMin);
            if (displayMin < fixedMinPrice)
            {
                displayMin = fixedMinPrice;
                displayMax = fixedMinPrice + currentRange;
            }

            if (displayMax > fixedMaxPrice)
            {
                displayMax = fixedMaxPrice;
                displayMin = fixedMaxPrice - currentRange;
            }
        }
    }

    private void RenderCandles()
    {
        if (candleViewport == null || candleHistory.Count == 0)
            return;

        if (useFixedPriceBounds)
            ClampHistoryToFixedBounds();

        GetDisplayPriceWindow(out float paddedMin, out float paddedMax);
        float paddedRange = Mathf.Max(0.01f, paddedMax - paddedMin);

        float chartHeight = candleViewport.rect.height;
        float viewportWidth = candleViewport.rect.width;
        float usableWidth = Mathf.Max(1f, viewportWidth - chartLeftPadding - chartRightPadding);

        float runtimeCandleWidth = candleWidth;
        float runtimeSpacing = candleSpacing;

        if (stretchCandlesToViewport && candleHistory.Count > 0)
        {
            float maxBodyWidthPerCandle = usableWidth / candleHistory.Count;
            float preferredWidth = Mathf.Max(candleWidth, maxBodyWidthPerCandle * 0.78f);
            runtimeCandleWidth = Mathf.Clamp(preferredWidth, 2f, maxBodyWidthPerCandle);

            if (candleHistory.Count > 1)
                runtimeSpacing = Mathf.Max(0f, (usableWidth - runtimeCandleWidth * candleHistory.Count) / (candleHistory.Count - 1));
            else
                runtimeSpacing = 0f;
        }

        float totalWidth = candleHistory.Count * runtimeCandleWidth + Mathf.Max(0, candleHistory.Count - 1) * runtimeSpacing;
        float startX = alignCandlesFromLeft
            ? (-viewportWidth * 0.5f) + chartLeftPadding + runtimeCandleWidth * 0.5f
            : -totalWidth * 0.5f + runtimeCandleWidth * 0.5f;

        List<Vector2> closePoints = new List<Vector2>(candleHistory.Count);
        List<Vector2> movingAveragePoints = new List<Vector2>(candleHistory.Count);

        if (useLineGraphOnly)
        {
            DeactivateSpawnedCandles();

            for (int i = 0; i < candleHistory.Count; i++)
            {
                CandleSnapshot candle = candleHistory[i];
                float x = startX + i * (runtimeCandleWidth + runtimeSpacing);
                float closeY = NormalizeToHeight(candle.close, paddedMin, paddedRange, chartHeight);

                closePoints.Add(new Vector2(x, closeY));
                movingAveragePoints.Add(new Vector2(x, NormalizeToHeight(GetMovingAverageAt(i), paddedMin, paddedRange, chartHeight)));
            }

            if (currentPriceLine != null)
                currentPriceLine.gameObject.SetActive(false);

            RenderDirectionalLine(priceLineContainer, priceLineSegments, closePoints, lineGraphThickness);

            if (showMovingAverage)
                RenderLine(movingAverageLineContainer, movingAverageSegments, movingAveragePoints, movingAverageColor, movingAverageThickness);
            else
                ClearSegments(movingAverageSegments);

            return;
        }

        if (candlePrefab == null)
            return;

        EnsureCandlePool(candleHistory.Count);

        for (int i = 0; i < spawnedCandles.Count; i++)
            spawnedCandles[i].gameObject.SetActive(i < candleHistory.Count);

        for (int i = 0; i < candleHistory.Count; i++)
        {
            CandleSnapshot candle = candleHistory[i];
            RectTransform candleRoot = spawnedCandles[i];
            RectTransform wick = candleRoot.Find("Wick").GetComponent<RectTransform>();
            RectTransform body = candleRoot.Find("Body").GetComponent<RectTransform>();
            Image wickImage = wick.GetComponent<Image>();
            Image bodyImage = body.GetComponent<Image>();

            float x = startX + i * (runtimeCandleWidth + runtimeSpacing);
            float lowY = NormalizeToHeight(candle.low, paddedMin, paddedRange, chartHeight);
            float highY = NormalizeToHeight(candle.high, paddedMin, paddedRange, chartHeight);
            float openY = NormalizeToHeight(candle.open, paddedMin, paddedRange, chartHeight);
            float closeY = NormalizeToHeight(candle.close, paddedMin, paddedRange, chartHeight);

            float bodyBottom = Mathf.Min(openY, closeY);
            float bodyTop = Mathf.Max(openY, closeY);
            float bodyHeight = Mathf.Max(minimumBodyHeight, bodyTop - bodyBottom);
            float topWickHeight = Mathf.Max(0f, highY - bodyTop);
            float bottomWickHeight = Mathf.Max(0f, bodyBottom - lowY);

            if (candle.high > Mathf.Max(candle.open, candle.close) && topWickHeight < minimumVisibleWickPixels)
                topWickHeight = minimumVisibleWickPixels;

            if (candle.low < Mathf.Min(candle.open, candle.close) && bottomWickHeight < minimumVisibleWickPixels)
                bottomWickHeight = minimumVisibleWickPixels;

            float adjustedLowY = Mathf.Max(chartBottomPadding, bodyBottom - bottomWickHeight);
            float adjustedHighY = Mathf.Min(chartHeight - chartTopPadding, bodyTop + topWickHeight);
            float wickHeight = Mathf.Max(minimumVisibleWickPixels, adjustedHighY - adjustedLowY);

            candleRoot.anchorMin = new Vector2(0.5f, 0.5f);
            candleRoot.anchorMax = new Vector2(0.5f, 0.5f);
            candleRoot.pivot = new Vector2(0.5f, 0.5f);
            candleRoot.anchoredPosition = new Vector2(x, 0f);
            candleRoot.sizeDelta = new Vector2(runtimeCandleWidth, chartHeight);
            candleRoot.localScale = Vector3.one;

            wick.anchorMin = new Vector2(0.5f, 0f);
            wick.anchorMax = new Vector2(0.5f, 0f);
            wick.pivot = new Vector2(0.5f, 0.5f);
            wick.anchoredPosition = new Vector2(0f, adjustedLowY + wickHeight * 0.5f);
            wick.sizeDelta = new Vector2(wickWidth, wickHeight);
            wick.localScale = Vector3.one;

            body.anchorMin = new Vector2(0.5f, 0f);
            body.anchorMax = new Vector2(0.5f, 0f);
            body.pivot = new Vector2(0.5f, 0.5f);
            body.anchoredPosition = new Vector2(0f, bodyBottom + bodyHeight * 0.5f);
            body.sizeDelta = new Vector2(Mathf.Max(2f, runtimeCandleWidth * bodyWidthRatio), bodyHeight);
            body.localScale = Vector3.one;

            Color candleColor = candle.close >= candle.open ? bullishColor : bearishColor;
            if (bodyImage != null) bodyImage.color = candleColor;
            if (wickImage != null) wickImage.color = useNeutralWicks ? wickColor : candleColor;

            closePoints.Add(new Vector2(x, closeY));
            movingAveragePoints.Add(new Vector2(x, NormalizeToHeight(GetMovingAverageAt(i), paddedMin, paddedRange, chartHeight)));
        }

        if (currentPriceLine != null)
        {
            float lineY = NormalizeToHeight(candleHistory[candleHistory.Count - 1].close, paddedMin, paddedRange, chartHeight);
            currentPriceLine.anchoredPosition = new Vector2(0f, lineY - chartHeight * 0.5f);
        }

        if (showPriceLine)
            RenderLine(priceLineContainer, priceLineSegments, closePoints, priceLineColor, priceLineThickness);
        else
            ClearSegments(priceLineSegments);

        if (showMovingAverage)
            RenderLine(movingAverageLineContainer, movingAverageSegments, movingAveragePoints, movingAverageColor, movingAverageThickness);
        else
            ClearSegments(movingAverageSegments);
    }

    private void DeactivateSpawnedCandles()
    {
        for (int i = 0; i < spawnedCandles.Count; i++)
        {
            if (spawnedCandles[i] != null)
                spawnedCandles[i].gameObject.SetActive(false);
        }
    }

    private void ClampHistoryToFixedBounds()
    {
        for (int i = 0; i < candleHistory.Count; i++)
        {
            CandleSnapshot candle = candleHistory[i];
            candle.open = Mathf.Clamp(candle.open, fixedMinPrice, fixedMaxPrice);
            candle.close = Mathf.Clamp(candle.close, fixedMinPrice, fixedMaxPrice);
            candle.high = Mathf.Clamp(candle.high, fixedMinPrice, fixedMaxPrice);
            candle.low = Mathf.Clamp(candle.low, fixedMinPrice, fixedMaxPrice);

            candle.high = Mathf.Max(candle.high, Mathf.Max(candle.open, candle.close));
            candle.low = Mathf.Min(candle.low, Mathf.Min(candle.open, candle.close));

            candleHistory[i] = candle;
        }
    }

    private void RefreshHeaderUi()
    {
        if (assetNameText != null)
            assetNameText.text = assetName;

        if (currentPriceText != null)
        {
            currentPriceText.text = currencySymbol + currentPrice.ToString("0.00");
            currentPriceText.color = lastCloseDelta >= 0f ? bullishColor : bearishColor;
        }

        float totalChange = currentPrice - initialPrice;
        float percent = initialPrice > 0.01f ? (totalChange / initialPrice) * 100f : 0f;
        Color changeColor = totalChange >= 0f ? bullishColor : bearishColor;

        if (changeText != null)
        {
            string sign = totalChange >= 0f ? "+" : string.Empty;
            changeText.text = sign + currencySymbol + totalChange.ToString("0.00") + " (" + sign + percent.ToString("0.00") + "%) over 3 months";
            changeText.color = changeColor;
        }
    }

    private void RefreshPersonalUi()
    {
        float positionValue = sharesHeld * currentPrice;
        float unrealizedProfit = sharesHeld > 0f ? (currentPrice - averageBuyPrice) * sharesHeld : 0f;
        float totalInvestingProfit = realizedProfit + unrealizedProfit;
        float netWorth = cashBalance + positionValue;

        if (cashBalanceText != null)
            cashBalanceText.text = "Cash Balance: " + currencySymbol + cashBalance.ToString("0.00");

        if (etfPanelText != null)
        {
            etfPanelText.text =
                "ETF Shares: " + sharesHeld.ToString("0.000") + "\n" +
                "ETF Position Value: " + currencySymbol + positionValue.ToString("0.00") + "\n" +
                "Avg Buy Price: " + currencySymbol + averageBuyPrice.ToString("0.00") + "\n" +
                "Net Worth: " + currencySymbol + netWorth.ToString("0.00");
        }

        if (targetText != null)
        {
            string profitColor = totalInvestingProfit >= 0f ? "#6EDC5F" : "#FF4D4D";
            targetText.text =
                "Total Investing P/L: <color=" + profitColor + ">" + FormatSignedCurrency(totalInvestingProfit) + "</color>\n" +
                "<color=#6EDC5F>Win Target: +" + currencySymbol + winTarget.ToString("0.00") + "</color>\n" +
                "<color=#FF4D4D>Loss Limit: -" + currencySymbol + lossLimit.ToString("0.00") + "</color>";
        }
    }

    private void RefreshStockInfoUi()
    {
        float bid = currentPrice - bidAskSpread * 0.5f;
        float ask = currentPrice + bidAskSpread * 0.5f;
        float percent = initialPrice > 0.01f ? ((currentPrice - initialPrice) / initialPrice) * 100f : 0f;

        if (nameTickerText != null)
            nameTickerText.text = "Ticker: " + assetName;

        if (lastBidText != null)
        {
            lastBidText.text =
                "Last: " + currencySymbol + currentPrice.ToString("0.00") + "\n" +
                "Bid: " + currencySymbol + bid.ToString("0.00");
        }

        if (askVolumeText != null)
        {
            askVolumeText.text =
                "Ask: " + currencySymbol + ask.ToString("0.00") + "\n" +
                "Volume: " + lastRenderedVolume.ToString("0.00") + "M\n" +
                "3-Month Change: " + percent.ToString("+0.00;-0.00") + "%";
        }
    }

    private void RefreshEventUi()
    {
        if (activeEvent != null)
        {
            if (eventTitleText != null)
            {
                eventTitleText.text = activeEvent.title;
                eventTitleText.color = warningColor;
            }

            if (eventBodyText != null)
                eventBodyText.text = activeEvent.activeText;

            if (eventHintText != null)
                eventHintText.text = "Active: " + Mathf.Max(0, activeEventCandlesRemaining) + " candle(s) left";

            if (eventImage != null)
                eventImage.sprite = activeEvent.eventSprite;

            return;
        }

        if (queuedEvent != null)
        {
            if (eventTitleText != null)
            {
                eventTitleText.text = queuedEvent.title;
                eventTitleText.color = neutralColor;
            }

            if (eventBodyText != null)
                eventBodyText.text = queuedEvent.warningText;

            if (eventHintText != null)
                eventHintText.text = "Starts in " + Mathf.Max(0, candlesUntilNextEvent) + " candle(s), lasts " + Mathf.Max(1, queuedEvent.activeCandles) + " candle(s)";

            if (eventImage != null)
                eventImage.sprite = queuedEvent.eventSprite;

            return;
        }

        if (eventTitleText != null)
        {
            eventTitleText.text = "No major ETF event";
            eventTitleText.color = neutralColor;
        }

        if (eventBodyText != null)
            eventBodyText.text = "The market is acting polite for now. That usually means someone important is about to say something weird.";

        if (eventHintText != null)
            eventHintText.text = "Next event in about " + Mathf.Max(0, candlesUntilNextEvent) + " candle(s)";

        if (eventImage != null)
            eventImage.sprite = null;
    }

    private void RenderFixedAxisAndGrid()
    {
        if ((!useFixedPriceBounds && !graphUseSoftDisplayBounds) || candleViewport == null)
        {
            ClearSegments(gridLines);
            for (int i = 0; i < axisLabels.Count; i++)
            {
                if (axisLabels[i] != null)
                    axisLabels[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < xAxisLabels.Count; i++)
            {
                if (xAxisLabels[i] != null)
                    xAxisLabels[i].gameObject.SetActive(false);
            }

            return;
        }

        EnsureAxisContainers();

        GetDisplayPriceWindow(out float displayMin, out float displayMax);

        float axisStep = graphUseSoftDisplayBounds ? graphAxisStep : fixedAxisStep;
        float axisRange = Mathf.Max(0.01f, displayMax - displayMin);
        int tickCount = Mathf.FloorToInt(axisRange / axisStep) + 1;
        int verticalLineCount = 7;
        int horizontalSegmentsPerLine = Mathf.Max(8, Mathf.FloorToInt(candleViewport.rect.width / 18f));
        int verticalSegmentsPerLine = Mathf.Max(6, Mathf.FloorToInt(candleViewport.rect.height / 18f));
        int requiredDashSegments = tickCount * horizontalSegmentsPerLine + verticalLineCount * verticalSegmentsPerLine;

        EnsureGridLinePool(requiredDashSegments);
        EnsureAxisLabelPool(tickCount);
        EnsureXAxisLabelPool(0);

        float chartHeight = candleViewport.rect.height;
        float chartWidth = candleViewport.rect.width;
        float dashLength = 8f;
        float dashGap = 8f;
        int segmentIndex = 0;
        float chartBottomY = -chartHeight * 0.5f + chartBottomPadding;
        float chartTopY = chartHeight * 0.5f - chartTopPadding;
        float chartLeftX = -chartWidth * 0.5f;
        float chartRightX = chartWidth * 0.5f;

        EnsureAxisLines();
        if (showAxisLines)
        {
            SetupAxisLine(yAxisLine, new Vector2(chartLeftX + 18f, (chartBottomY + chartTopY) * 0.5f), new Vector2(axisLineThickness, chartTopY - chartBottomY), axisLineColor);
            SetupAxisLine(xAxisLine, new Vector2((chartLeftX + chartRightX) * 0.5f, chartBottomY), new Vector2(chartRightX - chartLeftX - 18f, axisLineThickness), axisLineColor);
        }
        else
        {
            if (yAxisLine != null) yAxisLine.gameObject.SetActive(false);
            if (xAxisLine != null) xAxisLine.gameObject.SetActive(false);
        }

        for (int i = 0; i < tickCount; i++)
        {
            float price = displayMin + axisStep * i;
            if (price > displayMax + 0.001f)
                price = displayMax;

            float y = NormalizeToHeight(price, displayMin, axisRange, chartHeight) - chartHeight * 0.5f;

            for (int dash = 0; dash < horizontalSegmentsPerLine; dash++)
            {
                float x = -chartWidth * 0.5f + (dash * (dashLength + dashGap)) + dashLength * 0.5f;
                if (x > chartWidth * 0.5f)
                    break;

                RectTransform line = gridLines[segmentIndex++];
                line.gameObject.SetActive(true);
                line.anchorMin = new Vector2(0.5f, 0.5f);
                line.anchorMax = new Vector2(0.5f, 0.5f);
                line.pivot = new Vector2(0.5f, 0.5f);
                line.anchoredPosition = new Vector2(x, y);
                line.sizeDelta = new Vector2(dashLength, gridLineThickness);
                line.localRotation = Quaternion.identity;
                Image lineImage = line.GetComponent<Image>();
                lineImage.color = gridLineColor;
            }

            TextMeshProUGUI label = axisLabels[i];
            label.gameObject.SetActive(true);
            label.text = Mathf.RoundToInt(price).ToString();
            label.color = axisLabelColor;
            label.fontSize = axisLabelFontSize;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(2f, y);
            labelRect.sizeDelta = new Vector2(axisLabelWidth, 18f);
        }

        for (int i = 0; i < verticalLineCount; i++)
        {
            float x = Mathf.Lerp(-chartWidth * 0.5f, chartWidth * 0.5f, i / (float)(verticalLineCount - 1));
            for (int dash = 0; dash < verticalSegmentsPerLine; dash++)
            {
                float y = -chartHeight * 0.5f + (dash * (dashLength + dashGap)) + dashLength * 0.5f;
                if (y > chartHeight * 0.5f)
                    break;

                RectTransform line = gridLines[segmentIndex++];
                line.gameObject.SetActive(true);
                line.anchorMin = new Vector2(0.5f, 0.5f);
                line.anchorMax = new Vector2(0.5f, 0.5f);
                line.pivot = new Vector2(0.5f, 0.5f);
                line.anchoredPosition = new Vector2(x, y);
                line.sizeDelta = new Vector2(gridLineThickness, dashLength);
                line.localRotation = Quaternion.identity;
                Image lineImage = line.GetComponent<Image>();
                lineImage.color = gridLineColor;
            }
        }

        for (int i = 0; i < xAxisLabels.Count; i++)
            xAxisLabels[i].gameObject.SetActive(false);

        for (int i = segmentIndex; i < gridLines.Count; i++)
            gridLines[i].gameObject.SetActive(false);

        for (int i = tickCount; i < axisLabels.Count; i++)
            axisLabels[i].gameObject.SetActive(false);
    }

    private void ConfigureButtons()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(BuyFromInput);
            buyButton.onClick.AddListener(BuyFromInput);
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveListener(SellFromInput);
            sellButton.onClick.AddListener(SellFromInput);
        }
    }

    private void EnsureAxisContainers()
    {
        if (candleViewport == null)
            return;

        EnsureOverlayContainers();

        if (decorationRoot == null)
        {
            GameObject rootObject = new GameObject("ChartDecorations", typeof(RectTransform));
            RectTransform rect = rootObject.GetComponent<RectTransform>();
            rect.SetParent(candleViewport, false);
            decorationRoot = rect;
        }

        SyncChartRects();

        if (gridLineContainer == null)
        {
            GameObject gridObject = new GameObject("GridLines", typeof(RectTransform));
            RectTransform rect = gridObject.GetComponent<RectTransform>();
            rect.SetParent(decorationRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            gridLineContainer = rect;
        }

        if (yAxisContainer == null)
        {
            GameObject axisObject = new GameObject("YAxis", typeof(RectTransform));
            RectTransform rect = axisObject.GetComponent<RectTransform>();
            rect.SetParent(decorationRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            yAxisContainer = rect;
        }

        if (xAxisContainer == null)
        {
            GameObject axisObject = new GameObject("XAxis", typeof(RectTransform));
            RectTransform rect = axisObject.GetComponent<RectTransform>();
            rect.SetParent(decorationRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            xAxisContainer = rect;
        }
    }

    private void EnsureAxisLines()
    {
        if (decorationRoot == null)
            return;

        if (yAxisLine == null)
            yAxisLine = CreateAxisLine("YAxisLine");

        if (xAxisLine == null)
            xAxisLine = CreateAxisLine("XAxisLine");
    }

    private RectTransform CreateAxisLine(string objectName)
    {
        GameObject lineObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(decorationRoot, false);
        return lineObject.GetComponent<RectTransform>();
    }

    private void SetupAxisLine(RectTransform line, Vector2 position, Vector2 size, Color color)
    {
        if (line == null)
            return;

        line.gameObject.SetActive(true);
        line.anchorMin = new Vector2(0.5f, 0.5f);
        line.anchorMax = new Vector2(0.5f, 0.5f);
        line.pivot = new Vector2(0.5f, 0.5f);
        line.anchoredPosition = position;
        line.sizeDelta = size;
        line.localRotation = Quaternion.identity;

        Image image = line.GetComponent<Image>();
        image.color = color;
    }

    private void EnsureGridLinePool(int count)
    {
        if (gridLineContainer == null)
            return;

        while (gridLines.Count < count)
        {
            GameObject lineObject = new GameObject("GridLine_" + gridLines.Count, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineObject.transform.SetParent(gridLineContainer, false);
            gridLines.Add(lineObject.GetComponent<RectTransform>());
        }
    }

    private void EnsureAxisLabelPool(int count)
    {
        if (yAxisContainer == null)
            return;

        while (axisLabels.Count < count)
        {
            GameObject labelObject = new GameObject("YLabel_" + axisLabels.Count, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(yAxisContainer, false);
            TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Left;
            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            axisLabels.Add(text);
        }
    }

    private void EnsureXAxisLabelPool(int count)
    {
        if (xAxisContainer == null)
            return;

        while (xAxisLabels.Count < count)
        {
            GameObject labelObject = new GameObject("XLabel_" + xAxisLabels.Count, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(xAxisContainer, false);
            TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            xAxisLabels.Add(text);
        }
    }

    private void AutoBindSceneReferences()
    {
        assetNameText = assetNameText != null ? assetNameText : FindText("AssetNameText");
        currentPriceText = currentPriceText != null ? currentPriceText : FindText("CurrentPriceText");
        changeText = changeText != null ? changeText : FindText("ChangeText");
        eventTitleText = eventTitleText != null ? eventTitleText : FindText("EventTitleText");
        eventBodyText = eventBodyText != null ? eventBodyText : FindText("EventBodyText");
        eventHintText = eventHintText != null ? eventHintText : FindText("Eventhinttext");

        cashBalanceText = cashBalanceText != null ? cashBalanceText : FindText("Cashbalancetext");
        etfPanelText = etfPanelText != null ? etfPanelText : FindText("ETF PANEL");
        targetText = targetText != null ? targetText : FindText("Targett text");

        lastBidText = lastBidText != null ? lastBidText : FindText("Last-Bid");
        askVolumeText = askVolumeText != null ? askVolumeText : FindText("ask-volume");
        nameTickerText = nameTickerText != null ? nameTickerText : FindText("nameticker");

        candleViewport = candleViewport != null ? candleViewport : FindRect("CandleViewport");
        currentPriceLine = currentPriceLine != null ? currentPriceLine : FindRect("CurrentPriceLine");
        priceLineContainer = priceLineContainer != null ? priceLineContainer : FindRect("CurrentPriceLine (1)");
        movingAverageLineContainer = movingAverageLineContainer != null ? movingAverageLineContainer : FindRect("MovingAverageLineContainer");

        amountInputField = amountInputField != null ? amountInputField : FindInputField("input");
        buyButton = buyButton != null ? buyButton : FindButton("Buy");
        sellButton = sellButton != null ? sellButton : FindButton("Sell");
        eventImage = eventImage != null ? eventImage : FindImage("Image");
    }

    private void EnsureDefaultEvents()
    {
        if (events != null && events.Count >= 4)
            return;

        events = new List<EventDefinition>
        {
            new EventDefinition
            {
                title = "Trump Fired Fed Chair Governor",
                warningText = "A fresh headline has landed. Traders are arguing whether this is genius policy or a live comedy special.",
                activeText = "Commentators are now speaking in all caps. The market has stopped pretending this is a normal afternoon.",
                direction = EventDirection.Bearish,
                warningCandles = 5,
                activeCandles = 5,
                bullishChanceOverride = 0.32f,
                driftBoost = -2.0f,
                volatilityBoost = 1.7f,
                wickBoost = 1.5f
            },
            new EventDefinition
            {
                title = "Pension Funds Quietly Buy ETFs",
                warningText = "Institutional money is moving slowly, politely, and with enough confidence to make retail investors curious.",
                activeText = "Big money has entered the room carrying spreadsheets and a suspicious amount of conviction.",
                direction = EventDirection.Bullish,
                warningCandles = 4,
                activeCandles = 6,
                bullishChanceOverride = 0.74f,
                driftBoost = 1.8f,
                volatilityBoost = 1.3f,
                wickBoost = 1.1f
            },
            new EventDefinition
            {
                title = "Inflation Report Cooked Again",
                warningText = "Prices are rising faster than expected, and economists are once again pretending this was impossible to see coming.",
                activeText = "Investors are now refreshing inflation charts and regretting every large lunch they bought this week.",
                direction = EventDirection.Bearish,
                warningCandles = 3,
                activeCandles = 5,
                bullishChanceOverride = 0.35f,
                driftBoost = -1.6f,
                volatilityBoost = 1.5f,
                wickBoost = 1.4f
            },
            new EventDefinition
            {
                title = "AI ETF Mania Hits Campus",
                warningText = "Every finance club suddenly has an opinion about AI, synergy, and why broad funds are apparently cool again.",
                activeText = "FOMO has entered the chat wearing a suit and calling itself a long-term strategy.",
                direction = EventDirection.Bullish,
                warningCandles = 4,
                activeCandles = 6,
                bullishChanceOverride = 0.78f,
                driftBoost = 2.1f,
                volatilityBoost = 1.4f,
                wickBoost = 1.2f
            }
        };
    }

    private void EnsureCandlePool(int count)
    {
        while (spawnedCandles.Count < count)
        {
            GameObject instance = Instantiate(candlePrefab, candleViewport);
            instance.name = "Candle_" + spawnedCandles.Count.ToString("00");
            instance.transform.localScale = Vector3.one;
            spawnedCandles.Add(instance.GetComponent<RectTransform>());
        }
    }

    private void ClearRenderedCandles()
    {
        for (int i = 0; i < spawnedCandles.Count; i++)
        {
            if (spawnedCandles[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(spawnedCandles[i].gameObject);
                else
                    DestroyImmediate(spawnedCandles[i].gameObject);
            }
        }

        spawnedCandles.Clear();
    }

    private void RenderLine(RectTransform container, List<RectTransform> pool, List<Vector2> points, Color color, float thickness)
    {
        if (container == null || points == null || points.Count < 2)
        {
            ClearSegments(pool);
            return;
        }

        while (pool.Count < points.Count - 1)
        {
            GameObject segment = new GameObject("LineSegment", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            segment.transform.SetParent(container, false);
            pool.Add(segment.GetComponent<RectTransform>());
        }

        for (int i = 0; i < pool.Count; i++)
            pool[i].gameObject.SetActive(i < points.Count - 1);

        for (int i = 0; i < points.Count - 1; i++)
        {
            RectTransform segment = pool[i];
            Image image = segment.GetComponent<Image>();
            image.color = color;

            Vector2 start = points[i];
            Vector2 end = points[i + 1];
            Vector2 delta = end - start;
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            segment.anchorMin = new Vector2(0.5f, 0.5f);
            segment.anchorMax = new Vector2(0.5f, 0.5f);
            segment.pivot = new Vector2(0f, 0.5f);
            segment.anchoredPosition = start - new Vector2(0f, candleViewport.rect.height * 0.5f);
            segment.sizeDelta = new Vector2(length, thickness);
            segment.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void RenderDirectionalLine(RectTransform container, List<RectTransform> pool, List<Vector2> points, float thickness)
    {
        if (container == null || points == null || points.Count < 2)
        {
            ClearSegments(pool);
            return;
        }

        while (pool.Count < points.Count - 1)
        {
            GameObject segment = new GameObject("DirectionalLineSegment", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            segment.transform.SetParent(container, false);
            pool.Add(segment.GetComponent<RectTransform>());
        }

        for (int i = 0; i < pool.Count; i++)
            pool[i].gameObject.SetActive(i < points.Count - 1);

        for (int i = 0; i < points.Count - 1; i++)
        {
            RectTransform segment = pool[i];
            Image image = segment.GetComponent<Image>();

            Vector2 start = points[i];
            Vector2 end = points[i + 1];
            Vector2 delta = end - start;
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            image.color = end.y >= start.y ? bullishColor : bearishColor;

            segment.anchorMin = new Vector2(0.5f, 0.5f);
            segment.anchorMax = new Vector2(0.5f, 0.5f);
            segment.pivot = new Vector2(0f, 0.5f);
            segment.anchoredPosition = start - new Vector2(0f, candleViewport.rect.height * 0.5f);
            segment.sizeDelta = new Vector2(length, thickness);
            segment.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void ClearSegments(List<RectTransform> pool)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] != null)
                pool[i].gameObject.SetActive(false);
        }
    }

    private float NormalizeToHeight(float value, float minPrice, float range, float height)
    {
        float usableHeight = Mathf.Max(1f, height - chartTopPadding - chartBottomPadding);
        float normalized = chartBottomPadding + ((value - minPrice) / range) * usableHeight;
        return Mathf.Clamp(normalized, chartBottomPadding, chartBottomPadding + usableHeight);
    }

    private float GetMovingAverageAt(int index)
    {
        int start = Mathf.Max(0, index - movingAveragePeriods + 1);
        float total = 0f;
        int count = 0;

        for (int i = start; i <= index; i++)
        {
            total += candleHistory[i].close;
            count++;
        }

        return count > 0 ? total / count : candleHistory[index].close;
    }

    private int GetHistoryCapacity()
    {
        if (candleViewport == null)
            return Mathf.Max(8, visibleCandleCount);

        float usableWidth = Mathf.Max(1f, candleViewport.rect.width - chartLeftPadding - chartRightPadding);
        float slotWidth = Mathf.Max(1f, candleWidth + candleSpacing);
        int widthCapacity = Mathf.Max(1, Mathf.FloorToInt((usableWidth + candleSpacing) / slotWidth));

        if (stretchCandlesToViewport)
            return Mathf.Max(8, visibleCandleCount);

        return Mathf.Max(Mathf.Max(8, visibleCandleCount), widthCapacity);
    }

    private void SyncChartRects()
    {
        if (candleViewport == null)
            return;

        candleViewport.localScale = Vector3.one;
        EnsureOverlayContainers();

        SyncOverlayRect(decorationRoot, candleViewport);
        SyncOverlayRect(gridLineContainer, decorationRoot);
        SyncOverlayRect(yAxisContainer, decorationRoot);
        SyncOverlayRect(xAxisContainer, decorationRoot);
        SyncOverlayRect(priceLineContainer, candleViewport);
        SyncOverlayRect(movingAverageLineContainer, candleViewport);
        SyncOverlayRect(currentPriceLine, candleViewport);
    }

    private void SyncOverlayRect(RectTransform target, RectTransform parent)
    {
        if (target == null || parent == null)
            return;

        if (target.parent != parent)
            target.SetParent(parent, false);

        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.pivot = new Vector2(0.5f, 0.5f);
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;
    }

    private void ResetRuntimeChartReferences()
    {
        decorationRoot = null;
        gridLineContainer = null;
        yAxisContainer = null;
        xAxisContainer = null;
        xAxisLine = null;
        yAxisLine = null;
    }

    private void SanitizeChartHierarchy()
    {
        if (candleViewport == null)
            return;

        candleViewport.localScale = Vector3.one;

        Transform chartParent = candleViewport.parent;
        if (chartParent == null)
            return;

        MoveOverlayIntoViewport(chartParent, ref currentPriceLine, "CurrentPriceLine");
        MoveOverlayIntoViewport(chartParent, ref priceLineContainer, "CurrentPriceLine (1)");
        MoveOverlayIntoViewport(chartParent, ref movingAverageLineContainer, "MovingAverageLineContainer");

        DestroyLegacyChartObject(chartParent, "ChartDecorations");
        DestroyLegacyChartObject(chartParent, "GridLines");
        DestroyLegacyChartObject(chartParent, "YAxis");
        DestroyLegacyChartObject(chartParent, "XAxis");
    }

    private void EnsureOverlayContainers()
    {
        if (candleViewport == null)
            return;

        if (currentPriceLine == null)
            currentPriceLine = CreateOverlayRect("CurrentPriceLine", candleViewport);

        if (priceLineContainer == null)
            priceLineContainer = CreateOverlayRect("PriceLineContainer", candleViewport);

        if (movingAverageLineContainer == null)
            movingAverageLineContainer = CreateOverlayRect("MovingAverageLineContainer", candleViewport);
    }

    private RectTransform CreateOverlayRect(string objectName, Transform parent)
    {
        GameObject overlayObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    private void MoveOverlayIntoViewport(Transform searchRoot, ref RectTransform target, string objectName)
    {
        if (target == null)
        {
            Transform found = FindDeepChild(searchRoot, objectName);
            if (found != null)
                target = found.GetComponent<RectTransform>();
        }

        if (target == null || target == candleViewport)
            return;

        target.SetParent(candleViewport, false);
        target.localScale = Vector3.one;
    }

    private void DestroyLegacyChartObject(Transform searchRoot, string objectName)
    {
        Transform found = FindDeepChild(searchRoot, objectName);
        if (found == null || found == candleViewport || found.IsChildOf(candleViewport))
            return;

        if (Application.isPlaying)
            Destroy(found.gameObject);
        else
            DestroyImmediate(found.gameObject);
    }

    private void ExpandDisplayRangeIfNeeded(float dataMin, float dataMax)
    {
        float currentRange = Mathf.Max(0.01f, displayMaxPrice - displayMinPrice);
        float triggerMargin = Mathf.Max(verticalRangePadding, currentRange * verticalRangeTriggerPercent);

        bool touchesLowerTrigger = dataMin <= displayMinPrice + triggerMargin;
        bool touchesUpperTrigger = dataMax >= displayMaxPrice - triggerMargin;

        if (!touchesLowerTrigger && !touchesUpperTrigger)
            return;

        float requiredMin = Mathf.Min(displayMinPrice, dataMin - verticalRangePadding);
        float requiredMax = Mathf.Max(displayMaxPrice, dataMax + verticalRangePadding);
        float requiredRange = Mathf.Max(initialVerticalRangeSize, requiredMax - requiredMin);
        float expandedRange = Mathf.Max(currentRange * verticalRangeExpandMultiplier, requiredRange);
        float center = (requiredMin + requiredMax) * 0.5f;

        displayMinPrice = center - expandedRange * 0.5f;
        displayMaxPrice = center + expandedRange * 0.5f;
    }

    private void HandleMouseChartControls()
    {
        if (!enableMouseChartControls || candleViewport == null)
            return;

        if (!RectTransformUtility.RectangleContainsScreenPoint(candleViewport, Input.mousePosition))
            return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) <= 0.001f)
            return;

        manualZoom = Mathf.Clamp(manualZoom + scroll * mouseZoomSpeed, 1f, maxManualZoom);
    }

    private float ParseTradeAmount()
    {
        if (amountInputField == null)
            return 0f;

        return float.TryParse(amountInputField.text, out float amount) ? Mathf.Max(0f, amount) : 0f;
    }

    private string FormatSignedCurrency(float value)
    {
        string sign = value >= 0f ? "+" : "-";
        return sign + currencySymbol + Mathf.Abs(value).ToString("0.00");
    }

    private TMP_Text FindText(string name)
    {
        Transform target = FindDeepChild(transform, name);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private RectTransform FindRect(string name)
    {
        Transform target = FindDeepChild(transform, name);
        return target != null ? target.GetComponent<RectTransform>() : null;
    }

    private Button FindButton(string name)
    {
        Transform target = FindDeepChild(transform, name);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private TMP_InputField FindInputField(string name)
    {
        Transform target = FindDeepChild(transform, name);
        return target != null ? target.GetComponent<TMP_InputField>() : null;
    }

    private Image FindImage(string name)
    {
        Transform target = FindDeepChild(transform, name);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
