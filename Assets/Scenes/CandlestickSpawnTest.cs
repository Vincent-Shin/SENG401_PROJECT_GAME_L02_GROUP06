using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CandlestickSpawnTest : MonoBehaviour
{
    private enum MarketTab
    {
        Etf,
        SlimeCoin
    }

    public float CurrentPrice => currentPrice;
    public float PositionValue => sharesHeld * GetBidPrice();
    public float UnrealizedProfit => sharesHeld > 0f ? (GetBidPrice() - averageBuyPrice) * sharesHeld : 0f;
    public float TotalInvestingProfit => realizedProfit + UnrealizedProfit;
    public float NetWorth => cashBalance + PositionValue;
    public float WinTarget => winTarget;
    public float LossLimit => lossLimit;
    public bool HasReachedWinCondition => TotalInvestingProfit >= winTarget;
    public bool HasReachedLossCondition => TotalInvestingProfit <= -lossLimit;

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
        public EventDirection direction = EventDirection.Bullish;
        [FormerlySerializedAs("warningCandles")] public int warningSeconds = 12;
        [FormerlySerializedAs("activeCandles")] public int activeSeconds = 10;
        public float bullishChanceOverride = 0.5f;
        public float driftBoost = 1.2f;
        public float volatilityBoost = 1.2f;
        public float wickBoost = 1f;
    }

    [Serializable]
    private class MarketRuntimeState
    {
        public float currentPrice;
        public float initialPrice;
        public float sharesHeld;
        public float averageBuyPrice;
        public float realizedProfit;
        public float lastRenderedVolume;
        public float lastCloseDelta;
        public float displayMinPrice;
        public float displayMaxPrice;
        public float manualZoom = 1f;
        public float manualPanPriceOffset;
        public bool hasDisplayRange;
        public bool lastCandleBullish = true;
        public readonly List<CandleSnapshot> candleHistory = new List<CandleSnapshot>();
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
    [SerializeField] private float baseBullishChance = 0.66f;
    [SerializeField] private float baselineUpwardDrift = 0.65f;
    [SerializeField] private float minimumBodyMove = 1.5f;
    [SerializeField] private FloatRange bullishMoveRange = new FloatRange { x = 1.8f, y = 5.2f };
    [SerializeField] private FloatRange bearishMoveRange = new FloatRange { x = -2.0f, y = -0.4f };
    [SerializeField] private FloatRange upperWickRange = new FloatRange { x = 1.5f, y = 4.0f };
    [SerializeField] private FloatRange lowerWickRange = new FloatRange { x = 1.2f, y = 4.0f };
    [SerializeField] private float maximumWickMove = 2.2f;
    [SerializeField] private FloatRange bodyAmplifierRange = new FloatRange { x = 1.2f, y = 3.2f };
    [SerializeField] private FloatRange calmBodyAmplifierRange = new FloatRange { x = 1.0f, y = 2.2f };
    [SerializeField] private FloatRange bodyVarianceRange = new FloatRange { x = 0.7f, y = 1.6f };
    [SerializeField] private float impulseCandleChance = 0.22f;
    [SerializeField] private FloatRange impulseMultiplierRange = new FloatRange { x = 1.2f, y = 2.1f };
    [SerializeField] private FloatRange calmSwingRange = new FloatRange { x = -0.8f, y = 2.6f };
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
    [HideInInspector, SerializeField] private float fixedMinPrice = 0f;
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
    [SerializeField] private string slimeAssetName = "SLIME COIN";
    [SerializeField] private TMP_Text currentPriceText;
    [SerializeField] private TMP_Text changeText;
    [SerializeField] private TMP_Text eventTitleText;
    [SerializeField] private TMP_Text eventBodyText;
    [SerializeField] private TMP_Text eventHintText;
    [SerializeField] private string currencySymbol = "$";
    [SerializeField] private Color bullishColor = new Color32(110, 220, 95, 255);
    [SerializeField] private Color bearishColor = new Color32(255, 77, 77, 255);
    [SerializeField] private Color neutralColor = new Color32(220, 225, 230, 255);
    [SerializeField] private Color warningColor = new Color32(255, 214, 102, 255);
    [SerializeField] private Color slimeBullishColor = new Color32(255, 220, 60, 255);
    [SerializeField] private Color slimeBearishColor = new Color32(255, 120, 40, 255);
    [SerializeField] private Color slimeNeutralColor = new Color32(255, 225, 140, 255);

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
    [SerializeField] private List<EventDefinition> etfEvents = new List<EventDefinition>();
    [SerializeField] private List<EventDefinition> slimeEvents = new List<EventDefinition>();
    [SerializeField] private int firstEventDelaySeconds = 20;
    [FormerlySerializedAs("minCandlesBetweenEvents"), SerializeField] private int minSecondsBetweenEvents = 18;
    [FormerlySerializedAs("maxCandlesBetweenEvents"), SerializeField] private int maxSecondsBetweenEvents = 28;
    [SerializeField] private float eventImpactMultiplier = 3.2f;
    [SerializeField, Range(0f, 1f)] private float etfEventRollChance = 0.75f;

    [Header("Slime Coin")]
    [SerializeField] private float slimeStartingPrice = 90000f;
    [SerializeField] private float slimeBaselineDrift = -0.45f;
    [SerializeField] private FloatRange slimeBullishMoveRange = new FloatRange { x = 0.8f, y = 2.2f };
    [SerializeField] private FloatRange slimeBearishMoveRange = new FloatRange { x = -3.6f, y = -1.4f };
    [SerializeField] private FloatRange slimeCalmSwingRange = new FloatRange { x = -0.35f, y = 0.35f };
    [SerializeField] private FloatRange slimeBodyVarianceRange = new FloatRange { x = 0.92f, y = 1.08f };
    [SerializeField] private float slimeImpulseChance = 0.12f;
    [SerializeField] private FloatRange slimeImpulseMultiplierRange = new FloatRange { x = 1.01f, y = 1.12f };
    [SerializeField] private float slimeMinimumBodyMove = 0.8f;
    [SerializeField] private float slimeMaximumBodyMove = 4.2f;
    [SerializeField] private FloatRange slimeUpperWickRange = new FloatRange { x = 0.2f, y = 0.8f };
    [SerializeField] private FloatRange slimeLowerWickRange = new FloatRange { x = 0.25f, y = 1.1f };
    [SerializeField] private float slimeMaximumWickMove = 1.4f;
    [SerializeField] private float slimeBidAskSpread = 120f;
    [SerializeField] private float slimeBaseVolumeMillions = 4.8f;
    [SerializeField] private float slimeInitialDisplayHalfRange = 9000f;
    [SerializeField] private float slimeGraphAxisStep = 2000f;
    [SerializeField] private float slimeDisplayPaddingPercent = 0.08f;
    [SerializeField] private float slimeMinimumDisplayRange = 2500f;

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

    private readonly MarketRuntimeState etfState = new MarketRuntimeState();
    private readonly MarketRuntimeState slimeState = new MarketRuntimeState();
    private MarketTab activeMarketTab = MarketTab.Etf;
    private bool slimeWinLatched;
    private bool slimeMarketActivated;

    private EventDefinition queuedEvent;
    private EventDefinition activeEvent;
    private int secondsUntilNextEvent;
    private int activeEventSecondsRemaining;
    private MarketTab queuedEventMarket = MarketTab.Etf;
    private MarketTab activeEventMarket = MarketTab.Etf;
    private RectTransform decorationRoot;
    private RectTransform gridLineContainer;
    private RectTransform yAxisContainer;
    private RectTransform xAxisContainer;
    private RectTransform xAxisLine;
    private RectTransform yAxisLine;
    private Button nextTabButton;
    private Button backTabButton;
    private TMP_Text nextTabButtonText;
    private TMP_Text backTabButtonText;
    [SerializeField] private Sprite nextButtonSprite;
    [SerializeField] private Sprite quitButtonSprite;
    [SerializeField] private Sprite confirmButtonSprite;
    private bool pendingExitResolved;
    private bool pendingExitWon;

    private void Awake()
    {
        ApplyGraphModeSettings();
        ResetRuntimeChartReferences();
        AutoBindSceneReferences();
        SanitizeChartHierarchy();
        ConfigureButtons();
        ConfigureNavigationButtons();
        EnsureDefaultEvents();
    }

    private void Start()
    {
        ApplyGraphModeSettings();
        RestartStockSession();
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
        updateIntervalSeconds = 1f;
        firstEventDelaySeconds = Mathf.Max(5, firstEventDelaySeconds);
        minSecondsBetweenEvents = Mathf.Max(1, minSecondsBetweenEvents);
        maxSecondsBetweenEvents = Mathf.Max(minSecondsBetweenEvents, maxSecondsBetweenEvents);
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
        EnsureDefaultEvents();
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
        AdvanceAllMarketsOneCandle();
    }

    public void BuyFromInput()
    {
        float amount = ParseTradeAmount();
        if (amount < minimumTradeAmount)
            return;

        float spend = Mathf.Min(amount, cashBalance);
        if (spend < minimumTradeAmount)
            return;

        float askPrice = GetAskPrice();
        float units = spend / Mathf.Max(0.01f, askPrice);
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

        float bidPrice = GetBidPrice();
        float requestedUnits = amount / Mathf.Max(0.01f, bidPrice);
        float unitsToSell = Mathf.Min(requestedUnits, sharesHeld);
        if (unitsToSell <= 0f)
            return;

        float proceeds = unitsToSell * bidPrice;
        realizedProfit += (bidPrice - averageBuyPrice) * unitsToSell;
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
        cashBalance = startingCash;
        timer = 0f;
        slimeMarketActivated = false;
        queuedEvent = null;
        activeEvent = null;
        queuedEventMarket = MarketTab.Etf;
        activeEventMarket = MarketTab.Etf;
        activeEventSecondsRemaining = 0;
        secondsUntilNextEvent = firstEventDelaySeconds;
        ResetMarketState(etfState, MarketTab.Etf, true);
        ResetMarketState(slimeState, MarketTab.SlimeCoin, false);
        ClearRenderedCandles();
        ClearSegments(priceLineSegments);
        ClearSegments(movingAverageSegments);
    }

    private void AdvanceAllMarketsOneCandle()
    {
        MarketTab selectedTab = activeMarketTab;
        SaveActiveMarketState();
        StepEventState();
        AdvanceMarketOneCandle(etfState, MarketTab.Etf);
        if (slimeMarketActivated)
            AdvanceMarketOneCandle(slimeState, MarketTab.SlimeCoin);
        slimeWinLatched |= GetCombinedInvestingProfit() >= winTarget;
        if (!pendingExitResolved && GetCombinedInvestingProfit() <= -lossLimit)
        {
            pendingExitWon = false;
            pendingExitResolved = true;
        }
        LoadMarketProfile(selectedTab);
        LoadMarketState(selectedTab == MarketTab.Etf ? etfState : slimeState);
        RefreshAllUi();
        RefreshNavigationUi();
    }

    private void ResetMarketState(MarketRuntimeState state, MarketTab marketTab, bool seedHistory)
    {
        LoadMarketProfile(marketTab);

        currentPrice = useFixedPriceBounds ? Mathf.Clamp(GetMarketStartingPrice(marketTab), fixedMinPrice, fixedMaxPrice) : GetMarketStartingPrice(marketTab);
        initialPrice = currentPrice;
        sharesHeld = 0f;
        averageBuyPrice = 0f;
        realizedProfit = 0f;
        candleHistory.Clear();
        lastCloseDelta = 0f;
        hasDisplayRange = false;
        manualZoom = 1f;
        manualPanPriceOffset = 0f;
        lastCandleBullish = true;
        lastRenderedVolume = GetMarketBaseVolume(marketTab);

        if (graphUseSoftDisplayBounds)
        {
            if (marketTab == MarketTab.SlimeCoin)
            {
                displayMinPrice = Mathf.Max(0.01f, currentPrice - slimeInitialDisplayHalfRange);
                displayMaxPrice = currentPrice + slimeInitialDisplayHalfRange;
            }
            else
            {
                displayMinPrice = graphInitialDisplayMin;
                displayMaxPrice = graphInitialDisplayMax;
            }
            hasDisplayRange = true;
        }

        if (seedHistory && respawnOnStart)
        {
            int seedCount = initialSeedCandles > 0 ? initialSeedCandles : GetHistoryCapacity();
            for (int i = 0; i < seedCount; i++)
                AppendRandomCandle();
        }
        else
        {
            candleHistory.Add(new CandleSnapshot(currentPrice, currentPrice, currentPrice, currentPrice));
        }

        SaveMarketState(state);
    }

    private void AdvanceMarketOneCandle(MarketRuntimeState state, MarketTab marketTab)
    {
        LoadMarketProfile(marketTab);
        LoadMarketState(state);
        AppendRandomCandle();
        SaveMarketState(state);
    }

    private void SaveActiveMarketState()
    {
        SaveMarketState(activeMarketTab == MarketTab.Etf ? etfState : slimeState);
    }

    private void SaveMarketState(MarketRuntimeState state)
    {
        state.currentPrice = currentPrice;
        state.initialPrice = initialPrice;
        state.sharesHeld = sharesHeld;
        state.averageBuyPrice = averageBuyPrice;
        state.realizedProfit = realizedProfit;
        state.lastRenderedVolume = lastRenderedVolume;
        state.lastCloseDelta = lastCloseDelta;
        state.displayMinPrice = displayMinPrice;
        state.displayMaxPrice = displayMaxPrice;
        state.manualZoom = manualZoom;
        state.manualPanPriceOffset = manualPanPriceOffset;
        state.hasDisplayRange = hasDisplayRange;
        state.lastCandleBullish = lastCandleBullish;
        state.candleHistory.Clear();
        state.candleHistory.AddRange(candleHistory);
    }

    private void LoadMarketState(MarketRuntimeState state)
    {
        currentPrice = state.currentPrice;
        initialPrice = state.initialPrice;
        sharesHeld = state.sharesHeld;
        averageBuyPrice = state.averageBuyPrice;
        realizedProfit = state.realizedProfit;
        lastRenderedVolume = state.lastRenderedVolume;
        lastCloseDelta = state.lastCloseDelta;
        displayMinPrice = state.displayMinPrice;
        displayMaxPrice = state.displayMaxPrice;
        manualZoom = state.manualZoom;
        manualPanPriceOffset = state.manualPanPriceOffset;
        hasDisplayRange = state.hasDisplayRange;
        lastCandleBullish = state.lastCandleBullish;
        candleHistory.Clear();
        candleHistory.AddRange(state.candleHistory);
    }

    private void SwitchToMarket(MarketTab marketTab)
    {
        SaveActiveMarketState();
        activeMarketTab = marketTab;
        LoadMarketProfile(activeMarketTab);
        LoadMarketState(activeMarketTab == MarketTab.Etf ? etfState : slimeState);
        RefreshAllUi();
        RefreshNavigationUi();
    }

    public void RestartStockSession()
    {
        ResetSimulation();
        activeMarketTab = MarketTab.Etf;
        slimeWinLatched = false;
        pendingExitResolved = false;
        pendingExitWon = false;
        LoadMarketState(etfState);
        RefreshAllUi();
        RefreshNavigationUi();
    }

    private void AppendRandomCandle()
    {
        float open = currentPrice;
        bool hasActiveEvent = activeEvent != null && activeEventMarket == activeMarketTab;
        float bodyAmplifier = hasActiveEvent ? bodyAmplifierRange.RandomValue() : GetCalmBodyAmplifierRange().RandomValue();
        float bullishChance = hasActiveEvent ? activeEvent.bullishChanceOverride : GetBaseBullishChance();
        float directionalBias = hasActiveEvent ? activeEvent.driftBoost * eventImpactMultiplier : GetBaselineDrift();
        float volatility = hasActiveEvent ? activeEvent.volatilityBoost * eventImpactMultiplier : 1f;

        if (activeMarketTab == MarketTab.SlimeCoin && candleHistory.Count < 8)
        {
            bullishChance = 0.28f;
            directionalBias -= 0.85f;
            volatility *= 1.08f;
        }

        if (activeMarketTab == MarketTab.SlimeCoin && open <= 0.5f)
        {
            bullishChance = 0.97f;
            directionalBias += 3.8f;
            volatility *= 1.35f;
        }

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

        FloatRange bullishRange = GetBullishMoveRange();
        FloatRange bearishRange = GetBearishMoveRange();
        float move = bullishMove ? bullishRange.RandomValue() : bearishRange.RandomValue();
        move *= bodyAmplifier;
        move *= GetBodyVarianceRange().RandomValue();

        if (UnityEngine.Random.value < GetImpulseChance())
            move *= GetImpulseMultiplierRange().RandomValue();

        if (!hasActiveEvent)
            move += GetCalmSwingRange().RandomValue();

        move += directionalBias;

        bool usePercentMove = activeMarketTab == MarketTab.SlimeCoin;
        if (usePercentMove)
        {
            move = Mathf.Clamp(move, -GetMaximumBodyMove(), GetMaximumBodyMove());
            if (Mathf.Abs(move) < GetMinimumBodyMove())
                move = Mathf.Sign(move == 0f ? (bullishMove ? 1f : -1f) : move) * GetMinimumBodyMove();
        }

        float close = Mathf.Max(0.01f, usePercentMove ? open * (1f + move / 100f) : open + move);
        float minimumMove = GetMinimumBodyMove();
        float maximumMove = GetMaximumBodyMove();
        if (!usePercentMove && Mathf.Abs(close - open) < minimumMove)
            close = open + Mathf.Sign((close - open) == 0f ? (bullishMove ? 1f : -1f) : (close - open)) * minimumMove;
        if (!usePercentMove && Mathf.Abs(close - open) > maximumMove)
            close = open + Mathf.Sign(close - open) * maximumMove;
        if (activeMarketTab == MarketTab.SlimeCoin && close <= 0.01f)
            close = Mathf.Max(0.01f, open * 0.18f);
        if (useFixedPriceBounds)
            close = Mathf.Clamp(close, fixedMinPrice, fixedMaxPrice);

        float wickBoost = hasActiveEvent ? activeEvent.wickBoost * eventImpactMultiplier : 1f;
        float upperWick = GetUpperWickRange().RandomValue() * volatility * wickBoost;
        float lowerWick = GetLowerWickRange().RandomValue() * volatility * wickBoost;
        if (usePercentMove)
        {
            upperWick = open * Mathf.Min(GetMaximumWickMove(), upperWick) / 100f;
            lowerWick = open * Mathf.Min(GetMaximumWickMove(), lowerWick) / 100f;
        }
        else
        {
            upperWick = Mathf.Min(GetMaximumWickMove(), upperWick);
            lowerWick = Mathf.Min(GetMaximumWickMove(), lowerWick);
        }

        float high = Mathf.Max(open, close) + upperWick;
        float low = Mathf.Max(0f, Mathf.Min(open, close) - lowerWick);

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

        float baseVolume = GetMarketBaseVolume(activeMarketTab) + Mathf.Abs(lastCloseDelta) * 0.12f;
        if (hasActiveEvent)
            baseVolume += activeEvent.volatilityBoost * 0.15f;
        lastRenderedVolume = Mathf.Max(0.4f, baseVolume);
    }

    private void StepEventState()
    {
        if (activeEvent != null)
        {
            activeEventSecondsRemaining--;
            if (activeEventSecondsRemaining <= 0)
            {
                activeEvent = null;
                secondsUntilNextEvent = UnityEngine.Random.Range(minSecondsBetweenEvents, maxSecondsBetweenEvents + 1);
            }

            return;
        }

        if (queuedEvent != null)
        {
            secondsUntilNextEvent--;
            if (secondsUntilNextEvent <= 0)
            {
                activeEvent = queuedEvent;
                activeEventMarket = queuedEventMarket;
                queuedEvent = null;
                activeEventSecondsRemaining = Mathf.Max(1, activeEvent.activeSeconds);
            }

            return;
        }

        secondsUntilNextEvent--;
        if (secondsUntilNextEvent <= 0)
        {
            MarketTab scheduledMarket = GetNextEventMarket();
            List<EventDefinition> availableEvents = GetEventListForMarket(scheduledMarket);
            if (availableEvents.Count == 0)
            {
                secondsUntilNextEvent = UnityEngine.Random.Range(minSecondsBetweenEvents, maxSecondsBetweenEvents + 1);
                return;
            }

            queuedEvent = availableEvents[UnityEngine.Random.Range(0, availableEvents.Count)];
            queuedEventMarket = scheduledMarket;
            secondsUntilNextEvent = Mathf.Max(1, queuedEvent.warningSeconds);
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

        if (activeMarketTab == MarketTab.SlimeCoin && activeEvent == null && queuedEvent == null)
        {
            float slimePadding = Mathf.Max(slimeMinimumDisplayRange * 0.5f, dataRange * slimeDisplayPaddingPercent);
            float recentMin = Mathf.Max(0.01f, minPrice - slimePadding);
            float recentMax = maxPrice + slimePadding;
            float recentRange = Mathf.Max(slimeMinimumDisplayRange, recentMax - recentMin);
            float recentCenter = (recentMin + recentMax) * 0.5f;

            displayMinPrice = Mathf.Max(0.01f, recentCenter - recentRange * 0.5f);
            displayMaxPrice = displayMinPrice + recentRange;
            hasDisplayRange = true;

            displayMin = displayMinPrice;
            displayMax = displayMaxPrice;
            return;
        }

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

            Color candleColor = candle.close >= candle.open ? GetBullishColor() : GetBearishColor();
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
            assetNameText.text = GetActiveAssetName();

        if (currentPriceText != null)
        {
            currentPriceText.text = FormatCurrency(currentPrice);
            currentPriceText.color = lastCloseDelta >= 0f ? GetBullishColor() : GetBearishColor();
        }

        float totalChange = currentPrice - initialPrice;
        float percent = initialPrice > 0.01f ? (totalChange / initialPrice) * 100f : 0f;
        Color changeColor = totalChange >= 0f ? GetBullishColor() : GetBearishColor();

        if (changeText != null)
        {
            string sign = totalChange >= 0f ? "+" : string.Empty;
            changeText.text = sign + FormatCurrency(Mathf.Abs(totalChange)) + " (" + sign + percent.ToString("0.00", CultureInfo.InvariantCulture) + "%) over 3 months";
            changeText.color = changeColor;
        }
    }

    private void RefreshPersonalUi()
    {
        float bidPrice = GetBidPrice();
        float holdingValue = sharesHeld * currentPrice;
        float positionValue = sharesHeld * bidPrice;
        float unrealizedProfit = sharesHeld > 0f ? (currentPrice - averageBuyPrice) * sharesHeld : 0f;
        float totalInvestingProfit = GetCombinedInvestingProfit();
        float marketNetWorth = holdingValue;
        string unitLabel = activeMarketTab == MarketTab.Etf ? "ETF Shares: " : "Slime Coin: ";
        string marketLabel = activeMarketTab == MarketTab.Etf ? "ETF" : "Slime Coin";

        if (cashBalanceText != null)
            cashBalanceText.text = "Cash Balance: " + FormatCurrency(cashBalance);

        if (etfPanelText != null)
        {
            etfPanelText.text =
                unitLabel + sharesHeld.ToString("0.000") + "\n" +
                "Position Value: " + FormatCurrency(positionValue) + "\n" +
                "Avg Buy Price: " + FormatCurrency(averageBuyPrice) + "\n" +
                marketLabel + " Net Worth: " + FormatCurrency(marketNetWorth);
        }

        if (targetText != null)
        {
            string profitColor = totalInvestingProfit >= 0f ? "#6EDC5F" : "#FF4D4D";
            targetText.text =
                "Total P/L: <color=" + profitColor + ">" + FormatSignedCurrency(totalInvestingProfit) + "</color>\n" +
                marketLabel + " Net Worth: " + FormatCurrency(marketNetWorth) + "\n" +
                "<color=#6EDC5F>Win Target: +" + FormatCurrency(winTarget) + "</color>\n" +
                "<color=#FF4D4D>Loss Limit: -" + FormatCurrency(lossLimit) + "</color>";
        }
    }

    private void RefreshStockInfoUi()
    {
        float bid = GetBidPrice();
        float ask = GetAskPrice();
        float percent = initialPrice > 0.01f ? ((currentPrice - initialPrice) / initialPrice) * 100f : 0f;

        if (nameTickerText != null)
            nameTickerText.text = "Ticker: " + GetActiveAssetName();

        if (lastBidText != null)
        {
            lastBidText.text =
                "Last: " + FormatCurrency(currentPrice) + "\n" +
                "Bid: " + FormatCurrency(bid);
        }

        if (askVolumeText != null)
        {
            askVolumeText.text =
                "Ask: " + FormatCurrency(ask) + "\n" +
                "Volume: " + lastRenderedVolume.ToString("0.00") + "M\n" +
                "3-Month Change: " + percent.ToString("+0.00;-0.00") + "%";
        }
    }

    private void RefreshEventUi()
    {
        if (false && activeMarketTab == MarketTab.SlimeCoin)
        {
            if (eventTitleText != null)
            {
                eventTitleText.text = "SLIME COIN";
                eventTitleText.color = GetNeutralColor();
            }

            if (eventBodyText != null)
                eventBodyText.text = "No scheduled event. This chart just enjoys chaos for free.";

            if (eventHintText != null)
                eventHintText.text = slimeWinLatched ? "Profit target locked in. Press ✓ to finish." : "Press Quit to fail, or reach target to unlock ✓.";

            if (eventHintText != null)
            {
                eventHintText.text = slimeWinLatched
                    ? "Profit target locked in. Press \u2713 to finish."
                    : "Press Quit to fail, or reach target to unlock \u2713.";
            }

            return;
        }

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
                eventHintText.text = "Story active for " + FormatDurationSeconds(activeEventSecondsRemaining) + ".";

            return;
        }

        if (queuedEvent != null)
        {
            if (eventTitleText != null)
            {
                eventTitleText.text = queuedEvent.title;
                eventTitleText.color = GetNeutralColor();
            }

            if (eventBodyText != null)
                eventBodyText.text = queuedEvent.warningText;

            if (eventHintText != null)
                eventHintText.text = "Market chatter incoming in " + FormatDurationSeconds(secondsUntilNextEvent) + ". Live for " + FormatDurationSeconds(queuedEvent.activeSeconds) + ".";

            return;
        }

        if (eventTitleText != null)
            eventTitleText.text = string.Empty;

        if (eventBodyText != null)
            eventBodyText.text = string.Empty;

        if (eventHintText != null)
            eventHintText.text = string.Empty;

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

        float axisStep = graphUseSoftDisplayBounds ? GetCurrentGraphAxisStep() : fixedAxisStep;
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
            label.text = FormatAxisPrice(price);
            label.color = axisLabelColor;
            label.fontSize = axisLabelFontSize;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(1f, 0.5f);
            labelRect.anchoredPosition = new Vector2(GetCurrentAxisLabelWidth(), y);
            labelRect.sizeDelta = new Vector2(GetCurrentAxisLabelWidth(), 18f);
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
        ConfigureAmountInputField();

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

    private string GetActiveAssetName()
    {
        return activeMarketTab == MarketTab.Etf ? assetName : slimeAssetName;
    }

    private Color GetNeutralColor()
    {
        return activeMarketTab == MarketTab.Etf ? neutralColor : slimeNeutralColor;
    }

    private Color GetBullishColor()
    {
        return activeMarketTab == MarketTab.Etf ? bullishColor : slimeBullishColor;
    }

    private Color GetBearishColor()
    {
        return activeMarketTab == MarketTab.Etf ? bearishColor : slimeBearishColor;
    }

    private float GetBidAskSpread()
    {
        return activeMarketTab == MarketTab.Etf ? bidAskSpread : slimeBidAskSpread;
    }

    private float GetMarketStartingPrice(MarketTab marketTab)
    {
        return marketTab == MarketTab.Etf ? startingPrice : slimeStartingPrice;
    }

    private float GetMarketBaseVolume(MarketTab marketTab)
    {
        return marketTab == MarketTab.Etf ? baseVolumeMillions : slimeBaseVolumeMillions;
    }

    private float GetBaseBullishChance()
    {
        return activeMarketTab == MarketTab.Etf ? baseBullishChance : 0.46f;
    }

    private float GetBaselineDrift()
    {
        return activeMarketTab == MarketTab.Etf ? baselineUpwardDrift : slimeBaselineDrift;
    }

    private FloatRange GetBullishMoveRange()
    {
        return activeMarketTab == MarketTab.Etf ? bullishMoveRange : slimeBullishMoveRange;
    }

    private FloatRange GetBearishMoveRange()
    {
        return activeMarketTab == MarketTab.Etf ? bearishMoveRange : slimeBearishMoveRange;
    }

    private FloatRange GetCalmSwingRange()
    {
        return activeMarketTab == MarketTab.Etf ? calmSwingRange : slimeCalmSwingRange;
    }

    private FloatRange GetBodyVarianceRange()
    {
        return activeMarketTab == MarketTab.Etf ? bodyVarianceRange : slimeBodyVarianceRange;
    }

    private float GetImpulseChance()
    {
        return activeMarketTab == MarketTab.Etf ? impulseCandleChance : slimeImpulseChance;
    }

    private FloatRange GetImpulseMultiplierRange()
    {
        return activeMarketTab == MarketTab.Etf ? impulseMultiplierRange : slimeImpulseMultiplierRange;
    }

    private FloatRange GetCalmBodyAmplifierRange()
    {
        return activeMarketTab == MarketTab.Etf
            ? calmBodyAmplifierRange
            : new FloatRange { x = 1f, y = 1f };
    }

    private List<EventDefinition> GetEventListForMarket(MarketTab marketTab)
    {
        return marketTab == MarketTab.Etf ? etfEvents : slimeEvents;
    }

    private MarketTab GetNextEventMarket()
    {
        if (!slimeMarketActivated)
            return MarketTab.Etf;

        return UnityEngine.Random.value <= etfEventRollChance ? MarketTab.Etf : MarketTab.SlimeCoin;
    }

    private float GetCombinedInvestingProfit()
    {
        return GetMarketTotalInvestingProfit(etfState, MarketTab.Etf) + GetMarketTotalInvestingProfit(slimeState, MarketTab.SlimeCoin);
    }

    private float GetCombinedNetWorth()
    {
        return cashBalance + GetMarketPositionValue(etfState, MarketTab.Etf) + GetMarketPositionValue(slimeState, MarketTab.SlimeCoin);
    }

    private float GetMarketPositionValue(MarketRuntimeState state, MarketTab marketTab)
    {
        float bid = Mathf.Max(0.01f, state.currentPrice - GetBidAskSpreadFor(marketTab) * 0.5f);
        return state.sharesHeld * bid;
    }

    private float GetMarketTotalInvestingProfit(MarketRuntimeState state, MarketTab marketTab)
    {
        float bid = Mathf.Max(0.01f, state.currentPrice - GetBidAskSpreadFor(marketTab) * 0.5f);
        float unrealized = state.sharesHeld > 0f ? (bid - state.averageBuyPrice) * state.sharesHeld : 0f;
        return state.realizedProfit + unrealized;
    }

    private float GetBidAskSpreadFor(MarketTab marketTab)
    {
        return marketTab == MarketTab.Etf ? bidAskSpread : slimeBidAskSpread;
    }

    private float GetCurrentGraphAxisStep()
    {
        if (activeMarketTab == MarketTab.Etf)
            return graphAxisStep;

        float currentRange = Mathf.Max(slimeMinimumDisplayRange, displayMaxPrice - displayMinPrice);
        return GetNiceAxisStep(currentRange / 5f);
    }

    private float GetCurrentAxisLabelWidth()
    {
        return activeMarketTab == MarketTab.Etf ? axisLabelWidth : Mathf.Max(axisLabelWidth, 68f);
    }

    private float GetMinimumBodyMove()
    {
        return activeMarketTab == MarketTab.Etf ? minimumBodyMove : slimeMinimumBodyMove;
    }

    private float GetMaximumBodyMove()
    {
        return activeMarketTab == MarketTab.Etf ? maximumBodyMove : slimeMaximumBodyMove;
    }

    private FloatRange GetUpperWickRange()
    {
        return activeMarketTab == MarketTab.Etf ? upperWickRange : slimeUpperWickRange;
    }

    private FloatRange GetLowerWickRange()
    {
        return activeMarketTab == MarketTab.Etf ? lowerWickRange : slimeLowerWickRange;
    }

    private float GetMaximumWickMove()
    {
        return activeMarketTab == MarketTab.Etf ? maximumWickMove : slimeMaximumWickMove;
    }

    private void LoadMarketProfile(MarketTab marketTab)
    {
        activeMarketTab = marketTab;
    }

    private void ConfigureNavigationButtons()
    {
        nextTabButton = nextTabButton != null ? nextTabButton : FindButton("Nextbutton");
        backTabButton = backTabButton != null ? backTabButton : FindButton("button left");

        if (backTabButton == null && nextTabButton != null)
            backTabButton = CreateBackButtonFromTemplate(nextTabButton);

        if (nextTabButton != null)
        {
            nextTabButton.onClick.RemoveListener(HandleNextOrActionButton);
            nextTabButton.onClick.AddListener(HandleNextOrActionButton);
            nextTabButtonText = EnsureButtonText(nextTabButton, "Next");
        }

        if (backTabButton != null)
        {
            backTabButton.onClick.RemoveListener(HandleBackButton);
            backTabButton.onClick.AddListener(HandleBackButton);
            backTabButtonText = EnsureButtonText(backTabButton, "Back");
        }
    }

    private Button CreateBackButtonFromTemplate(Button templateButton)
    {
        if (templateButton == null)
            return null;

        GameObject clone = Instantiate(templateButton.gameObject, templateButton.transform.parent);
        clone.name = "button left";

        RectTransform templateRect = templateButton.GetComponent<RectTransform>();
        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        cloneRect.anchorMin = templateRect.anchorMin;
        cloneRect.anchorMax = templateRect.anchorMax;
        cloneRect.pivot = templateRect.pivot;
        cloneRect.sizeDelta = templateRect.sizeDelta;
        cloneRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(-(templateRect.rect.width + 18f), 0f);
        cloneRect.localScale = Vector3.one;
        cloneRect.localRotation = Quaternion.identity;

        Image cloneImage = clone.GetComponent<Image>();
        if (cloneImage != null)
        {
            RectTransform imageRect = cloneImage.rectTransform;
            imageRect.localScale = new Vector3(-1f, 1f, 1f);
        }

        TMP_Text text = clone.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = "Back";
            text.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        }

        return clone.GetComponent<Button>();
    }

    private TMP_Text EnsureButtonText(Button button, string fallbackText)
    {
        if (button == null)
            return null;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            return text;

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(button.transform, false);
        TextMeshProUGUI created = textObject.GetComponent<TextMeshProUGUI>();
        created.text = fallbackText;
        created.alignment = TextAlignmentOptions.Center;
        created.fontSize = 22f;
        created.color = Color.black;
        RectTransform rect = created.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return created;
    }

    public bool TryConsumeExitRequest(out bool won)
    {
        won = pendingExitWon;
        bool resolved = pendingExitResolved;
        pendingExitResolved = false;
        pendingExitWon = false;
        return resolved;
    }

    private void HandleNextOrActionButton()
    {
        if (activeMarketTab == MarketTab.Etf)
        {
            if (!slimeMarketActivated)
                slimeMarketActivated = true;
            SwitchToMarket(MarketTab.SlimeCoin);
            return;
        }

        pendingExitWon = slimeWinLatched;
        pendingExitResolved = true;
    }

    private void HandleBackButton()
    {
        if (activeMarketTab == MarketTab.SlimeCoin)
        {
            SwitchToMarket(MarketTab.Etf);
            return;
        }
    }

    private void RefreshNavigationUi()
    {
        if (backTabButton != null)
            backTabButton.gameObject.SetActive(activeMarketTab == MarketTab.SlimeCoin);

        if (backTabButtonText != null)
        {
            backTabButtonText.text = "Back";
            backTabButtonText.enabled = false;
        }

        if (nextTabButton == null)
            return;

        nextTabButton.gameObject.SetActive(true);
        Image nextImage = nextTabButton.GetComponent<Image>();

        if (activeMarketTab == MarketTab.Etf)
        {
            if (nextTabButtonText != null)
            {
                nextTabButtonText.text = "Next";
                nextTabButtonText.enabled = false;
            }
            ApplyButtonSprite(nextImage, nextButtonSprite, new Color32(255, 255, 255, 255));
            return;
        }

        if (slimeWinLatched)
        {
            if (nextTabButtonText != null)
                nextTabButtonText.text = "✓";
            ApplyButtonSprite(nextImage, confirmButtonSprite, new Color32(92, 200, 92, 255));
        }
        else
        {
            if (nextTabButtonText != null)
            {
                nextTabButtonText.text = "Quit";
                nextTabButtonText.enabled = false;
            }
            ApplyButtonSprite(nextImage, quitButtonSprite, new Color32(190, 100, 100, 255));
        }

        if (slimeWinLatched && nextTabButtonText != null)
        {
            nextTabButtonText.text = "\u2713";
            nextTabButtonText.enabled = false;
        }
    }

    private void ApplyButtonSprite(Image image, Sprite sprite, Color fallbackColor)
    {
        if (image == null)
            return;

        if (sprite != null)
            image.sprite = sprite;

        image.color = fallbackColor;
    }

    private float GetBidPrice()
    {
        return Mathf.Max(0.01f, currentPrice - GetBidAskSpread() * 0.5f);
    }

    private float GetAskPrice()
    {
        return Mathf.Max(GetBidPrice(), currentPrice + GetBidAskSpread() * 0.5f);
    }

    private void ConfigureAmountInputField()
    {
        if (amountInputField == null)
            return;

        amountInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
        amountInputField.lineType = TMP_InputField.LineType.SingleLine;
        amountInputField.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        amountInputField.richText = false;
    }

    private string FormatCurrency(float value)
    {
        return currencySymbol + value.ToString("N2", CultureInfo.InvariantCulture);
    }

    private string FormatAxisPrice(float value)
    {
        return value >= 10000f
            ? value.ToString("N0", CultureInfo.InvariantCulture)
            : Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);
    }

    private float GetNiceAxisStep(float rawStep)
    {
        float safeStep = Mathf.Max(1f, rawStep);
        float magnitude = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(safeStep)));
        float normalized = safeStep / magnitude;

        if (normalized <= 1f)
            return 1f * magnitude;
        if (normalized <= 2f)
            return 2f * magnitude;
        if (normalized <= 5f)
            return 5f * magnitude;
        return 10f * magnitude;
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
            text.alignment = TextAlignmentOptions.Right;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
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
        if (eventTitleText != null && eventTitleText == eventBodyText)
            eventTitleText = FindAlternativeEventTitleText(eventBodyText);
    }

    private TMP_Text FindAlternativeEventTitleText(TMP_Text excluded)
    {
        TMP_Text[] texts = FindObjectsOfType<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text candidate = texts[i];
            if (candidate == null || candidate == excluded)
                continue;

            string candidateName = candidate.name;
            if (candidateName.IndexOf("event", StringComparison.OrdinalIgnoreCase) >= 0 &&
                candidateName.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return candidate;
            }
        }

        return null;
    }

    private void EnsureDefaultEvents()
    {
        bool needsEtfDefaults = etfEvents == null || etfEvents.Count == 0;
        bool needsSlimeDefaults = slimeEvents == null || slimeEvents.Count == 0;

        if (!needsEtfDefaults && !needsSlimeDefaults)
            return;

        if (needsEtfDefaults)
        {
            etfEvents = new List<EventDefinition>
            {
                new EventDefinition
                {
                    title = "Fed Chair Replaced Overnight",
                    warningText = "Fed Chair Replaced Overnight in 12s.",
                    direction = EventDirection.Bearish,
                    warningSeconds = 10,
                    activeSeconds = 10,
                    activeText = "A surprise announcement claims the Fed Chair is stepping down and being replaced immediately. A follow-up post denies it. Then a third source says partially true. Markets react before agreeing on what actually happened.",
                    bullishChanceOverride = 0.34f,
                    driftBoost = -2.9f,
                    volatilityBoost = 2.4f,
                    wickBoost = 1.9f
                },
                new EventDefinition
                {
                    title = "Fed 'Pause'... or Not?",
                    warningText = "Fed 'Pause'... or Not? in 11s.",
                    activeText = "One official signals a possible pause in rate hikes. Another says it is too early to consider. Markets process both statements at the same time.",
                    direction = EventDirection.Volatile,
                    warningSeconds = 10,
                    activeSeconds = 10,
                    bullishChanceOverride = 0.5f,
                    driftBoost = 0.2f,
                    volatilityBoost = 2.8f,
                    wickBoost = 2.1f
                },
                new EventDefinition
                {
                    title = "Unexpected Oil Discovery Under Office Tower",
                    warningText = "Unexpected Oil Discovery Under Office Tower in 13s.",
                    activeText = "Reports claim a large oil reserve was found beneath a well-known skyscraper. Energy stocks react first. Analysts debate whether this is geology or branding.",
                    direction = EventDirection.Bullish,
                    warningSeconds = 10,
                    activeSeconds = 10,
                    bullishChanceOverride = 0.7f,
                    driftBoost = 2.8f,
                    volatilityBoost = 2.2f,
                    wickBoost = 1.7f
                },
                new EventDefinition
                {
                    title = "Hardware CEO: 'AI Will Replace Developers Before 2026'",
                    warningText = "Hardware CEO: 'AI Will Replace Developers Before 2026' in 12s.",
                    activeText = "A major hardware CEO claims AI will replace a large portion of developers within a short timeframe. No clear timeline is provided, but the statement spreads rapidly. Companies begin reassessing cost structures and workforce assumptions.",
                    direction = EventDirection.Bearish,
                    warningSeconds = 10,
                    activeSeconds = 10,
                    bullishChanceOverride = 0.37f,
                    driftBoost = -2.4f,
                    volatilityBoost = 2.3f,
                    wickBoost = 1.8f
                },
                new EventDefinition
                {
                    title = "Companies Begin Finding 'NemoClaw'",
                    warningText = "Finding 'NemoClaw' in 12s.",
                    activeText = "Companies begin deploying NemoClaw agents into internal workflows, from data processing to repetitive task automation. The shift from experimentation to real usage appears faster than expected.",
                    direction = EventDirection.Bullish,
                    warningSeconds = 10,
                    activeSeconds = 10,
                    bullishChanceOverride = 0.76f,
                    driftBoost = 3.0f,
                    volatilityBoost = 2.35f,
                    wickBoost = 1.75f
                }
            };
        }

        if (needsSlimeDefaults)
        {
            slimeEvents = new List<EventDefinition>
            {
                new EventDefinition
                {
                    title = "A Billion-People Country Calls Slime Coin a Scam",
                    warningText = "A Billion-People Country Calls Slime Coin a Scam in 13s.",
                    activeText = "A large government labels Slime Coin as a scam and warns citizens to avoid it. Discussions about monitoring and restrictions begin circulating.",
                    direction = EventDirection.Bearish,
                    warningSeconds = 10,
                    activeSeconds = 10,
                    bullishChanceOverride = 0.28f,
                    driftBoost = -3.4f,
                    volatilityBoost = 2.5f,
                    wickBoost = 2.0f
                },
                new EventDefinition
                {
                    title = "Elon: 'Maybe You Can Buy Tesla With Slime Coin'",
                    warningText = "Elon: 'Maybe You Can Buy Tesla With Slime Coin' in 14s.",
                    activeText = "Elon casually replies to a post suggesting Tesla could accept Slime Coin. The response is vague, but screenshots spread quickly. Traders interpret the message in different ways.",
                    direction = EventDirection.Bullish,
                    warningSeconds = 10,
                    activeSeconds = 10,
                    bullishChanceOverride = 0.74f,
                    driftBoost = 3.6f,
                    volatilityBoost = 2.6f,
                    wickBoost = 2.0f
                },
                new EventDefinition
                {
                    title = "AWS Reports 'Partial Service Degradation'",
                    warningText = "AWS Reports 'Partial Service Degradation' in 10s.",
                    activeText = "Several cloud services report latency and intermittent issues. Some crypto platforms begin responding inconsistently, while status pages remain operational.",
                    direction = EventDirection.Bearish,
                    warningSeconds = 10,
                    activeSeconds = 10,
                    bullishChanceOverride = 0.33f,
                    driftBoost = -3.0f,
                    volatilityBoost = 2.4f,
                    wickBoost = 1.95f
                }
            };
        }
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

            image.color = end.y >= start.y ? GetBullishColor() : GetBearishColor();

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

    private string FormatDurationSeconds(int secondCount)
    {
        return Mathf.Max(0, secondCount) + "s";
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
