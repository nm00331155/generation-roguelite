using System;
using System.Collections.Generic;
using System.Linq;
using GenerationRoguelite.Action;
using GenerationRoguelite.Character;
using GenerationRoguelite.Data;
using GenerationRoguelite.Era;
using GenerationRoguelite.Expansion;
using GenerationRoguelite.Events;
using GenerationRoguelite.Meta;
using GenerationRoguelite.Monetization;
using GenerationRoguelite.Navigator;
using GenerationRoguelite.UI;
using Godot;

namespace GenerationRoguelite.Core;

public partial class GameManager : Node2D
{
    private const float PlayerFixedX = 324f;
    private const float ObstacleSpawnX = 1200f;
    private const float FloatTextX = 540f;
    private const float FloatTextY = 440f;
    private const float BaseObstacleLifeDamage = 3f;
    private const float BackgroundScrollSpeed = 160f;
    private const float PlayerGravity = 2200f;
    private const float YouthJumpVelocity = 880f;
    private const float AttackAnimationDuration = 0.2f;
    private const float YouthTapAvoidWindow = 0.36f;
    private const float ElderlySwipeAvoidWindow = 0.28f;
    private const float MidlifeParryWindowSeconds = 0.3f;
    private const float ElderlyCaneDurationSeconds = 3f;
    private const float ElderlyWalkSpeedScale = 0.5f;
    private const float ElderlyEvadeOffset = 34f;
    private const float AttackReachX = 240f;
    private const float CollectibleSpawnMinSeconds = 3f;
    private const float CollectibleSpawnMaxSeconds = 5f;
    private const float CollectibleLifetimeSeconds = 2f;
    private const float CollectiblePickupDistance = 30f;
    private const float PhaseSlowMotionSeconds = 0.5f;
    private const float PhaseSlowMotionScale = 0.45f;
    private const float PhaseBannerDurationSeconds = 1.2f;
    private const float PhaseBannerSlideSeconds = 0.35f;
    private const float PhaseBannerStartX = 1310f;
    private const float PhaseBannerCenterX = 430f;
    private const float EventIntervalMinSeconds = 15f;
    private const float EventIntervalMaxSeconds = 30f;
    private const float EventLimitSeconds = 8f;
    private const float FuneralFadeDuration = 1.2f;
    private const float FuneralDisplayDelaySeconds = 2.4f;
    private const float DropFlashDuration = 0.2f;
    private const float NavigatorBubbleVisibleSeconds = 3f;
    private const float NavigatorBubbleFadeInSeconds = 0.2f;
    private const float NavigatorBubbleFadeOutSeconds = 0.3f;

    private CharacterBody2D _player = null!;
    private Polygon2D _playerVisual = null!;
    private Node2D _obstaclesRoot = null!;
    private Node2D _collectiblesRoot = null!;
    private ParallaxBackground _parallaxBackground = null!;
    private PanelContainer _hud = null!;
    private Control _navigatorArea = null!;
    private FloatTextSpawner _floatTextSpawner = null!;
    private DebugOverlay _debugOverlay = null!;

    private Label _ageLabel = null!;
    private Label _phaseLabel = null!;
    private Label _lifeLabel = null!;
    private ProgressBar? _lifeGauge;
    private Label _generationLabel = null!;
    private Label _statsLabel = null!;
    private Label _scoreLabel = null!;
    private Label _eventLabel = null!;
    private Label _navigatorLabel = null!;
    private Label _hintLabel = null!;
    private Label _metaLabel = null!;
    private Label _collectionLabel = null!;
    private Label _legendLabel = null!;
    private Label _monetizeLabel = null!;
    private Label _onlineLabel = null!;
    private Label _perfLabel = null!;
    private Label _phaseBannerLabel = null!;

    private Polygon2D _background = null!;
    private ColorRect _dropFlashRect = null!;
    private PanelContainer _eventPanel = null!;
    private Label _eventBodyLabel = null!;
    private Button _choiceTapButton = null!;
    private Button _choiceSwipeButton = null!;
    private Button _choiceTimeoutButton = null!;
    private TextureProgressBar _eventTimeBar = null!;

    private Control _funeralOverlay = null!;
    private ColorRect _funeralFadeRect = null!;
    private PanelContainer _funeralPanel = null!;
    private Label _funeralSummaryLabel = null!;
    private Label _funeralDeathCauseLabel = null!;
    private Label _funeralSnapshotLabel = null!;
    private Label _funeralScoreLabel = null!;
    private Label _funeralInterstitialLabel = null!;
    private Button _nextGenerationButton = null!;

    private PanelContainer _nextGenerationPanel = null!;
    private Label _inheritanceLabel = null!;
    private Label _childTypeLabel = null!;
    private Label _childPreviewLabel = null!;
    private Label _uniqueSkillLabel = null!;
    private Button _biologicalButton = null!;
    private Button _adoptedButton = null!;
    private Button _birthButton = null!;
    private Button _birthAdBonusButton = null!;
    private HBoxContainer _typeButtons = null!;
    private HBoxContainer _actionButtons = null!;

    private Button _willButton = null!;
    private PanelContainer _willPanel = null!;
    private Label _willCandidateLabel = null!;
    private Label _willDetailLabel = null!;
    private Button _willPrevButton = null!;
    private Button _willNextButton = null!;
    private Button _willApplyButton = null!;
    private Button _willCloseButton = null!;

    private CharacterData _character = null!;
    private Equipment? _queuedHeirloom;

    private readonly TimeManager _timeManager = new();
    private readonly PhaseManager _phaseManager = new();
    private readonly EventManager _eventManager = new();
    private readonly PlayerAction _playerAction = new();
    private readonly DDAController _ddaController = new();
    private readonly NavigatorManager _navigatorManager = new();
    private readonly EraManager _eraManager = new();
    private readonly FamilyLawManager _familyLawManager = new();
    private readonly BondManager _bondManager = new();
    private readonly Inventory _inventory = new();
    private readonly FamilyTree _familyTree = new();
    private readonly CollectionManager _collectionManager = new();
    private readonly AchievementManager _achievementManager = new();
    private readonly FounderManager _founderManager = new();
    private readonly RegretGenerator _regretGenerator = new();
    private readonly AdManager _adManager = new();
    private readonly IAPManager _iapManager = new();
    private readonly BattlePassManager _battlePassManager = new();
    private readonly CosmeticManager _cosmeticManager = new();
    private readonly AsyncSocialManager _asyncSocialManager = new();
    private readonly WorldExpansionManager _worldExpansionManager = new();
    private readonly InflationBalancer _inflationBalancer = new();
    private readonly PerformanceMonitor _performanceMonitor = new();
    private readonly SaveManager _saveManager = new();

    private ObstacleSpawner _obstacleSpawner = null!;
    private VoicePlayer _voicePlayer = null!;

    private EventData? _activeEvent;
    private TerrainProfile _currentTerrain = TerrainProfile.Default;
    private readonly RandomNumberGenerator _rng = new();

    private float _eventCooldown = EventIntervalMinSeconds;
    private float _avoidWindowRemaining;
    private bool _isInFuneral;
    private bool _funeralPanelVisible;
    private float _funeralFadeProgress;
    private float _funeralPanelDelay;
    private float _playerGroundY;
    private float _playerVerticalVelocity;
    private float _attackAnimationRemaining;
    private float _parryWindowRemaining;
    private float _elderlyCaneRemaining;
    private float _walkSpeedScale = 1f;
    private float _collectibleSpawnCooldown;
    private float _phaseSlowMotionRemaining;
    private float _phaseBannerElapsed;
    private float _phaseBannerBaseY;
    private bool _phaseBannerActive;
    private float _dropFlashRemaining;
    private LifePhase _lastPhaseForTransition = LifePhase.Childhood;
    private Tween? _navigatorBubbleTween;
    private float _playerHalfWidth = 36f;
    private float _playerHeight = 72f;
    private bool _debugOverlayEnabled = true;
    private Vector2 _lastTouchPosition;

    private StatBonus _appliedEquipmentBonus = StatBonus.Zero;
    private int _appliedEquipmentLifespanModifier;

    private bool _lineageExtinct;
    private bool _nextChildIsBiological = true;
    private bool _forcedAdoption;
    private bool _adoptionUsed;
    private bool _lastResortUsed;
    private int _birthAdBonus;
    private StatBonus _adoptedPreviewBonus = StatBonus.Zero;
    private string _adoptedPreviewName = "なし";
    private StatBonus _adoptedBaseBonus = StatBonus.Zero;
    private StatBonus _pendingAdoptedSkillBonus = StatBonus.Zero;
    private StatBonus _pendingAdoptedBaseBonus = StatBonus.Zero;
    private string _pendingAdoptedSkillName = "なし";
    private float _spouseEventCooldown = 20f;
    private bool _hasSpouse;
    private string _spouseName = "未婚";
    private int _spouseVitality = 10;
    private int _spouseIntelligence = 10;
    private int _spouseCharisma = 10;
    private int _spouseLuck = 10;
    private int _partnerAttemptCount;

    private int _lastGenerationRawScore;
    private int _lastGenerationFinalScore;
    private int _currentInheritanceSeed;
    private string _currentCharacterName = string.Empty;

    private readonly GenerationHistory _generationHistory = new();
    private readonly List<string> _generationEventLog = [];
    private readonly List<CollectibleInstance> _collectibles = [];
    private readonly List<Equipment> _willCandidates = [];

    private int _willCandidateIndex;
    private string _willStatusText = string.Empty;

    private int _nextGeneration = 1;
    private int _generationScore;
    private int _totalScore;

    private readonly Queue<string> _recentEvents = new();

    private string _lifePath = "未選択";
    private bool _lifePathSelected;

    private string _currentDeathCause = "老衰";
    private bool _regretShown;
    private string _legendText = "";

    private string _lastDropText = "ドロップなし";
    private string _lastAssistText = "";

    private int _pendingInheritanceBonusWealth;
    private bool _retryTokenAvailable;
    private int _shopSlotBonus;

    private string _perfSummary = "perf: --";
    private string _saveStatus = "save: pending";

    public override void _Ready()
    {
        _rng.Randomize();

        _player = GetNode<CharacterBody2D>("World/Player");
        _playerVisual = GetNode<Polygon2D>("World/Player/PlayerVisual");
        _obstaclesRoot = GetNode<Node2D>("World/Obstacles");
        _collectiblesRoot = GetNode<Node2D>("World/Collectibles");
        _parallaxBackground = GetNode<ParallaxBackground>("World/ParallaxBackground");
        _background = GetNode<Polygon2D>("World/ParallaxBackground/SkyLayer/Background");
        _dropFlashRect = GetNode<ColorRect>("UI/DropFlash");
        _hud = GetNode<PanelContainer>("UI/HUD");
        _eventPanel = GetNode<PanelContainer>("UI/EventPanel");
        _eventBodyLabel = GetNode<Label>("UI/EventPanel/Margin/VBox/EventBodyLabel");
        _choiceTapButton = GetNode<Button>("UI/EventPanel/Margin/VBox/ChoiceTapButton");
        _choiceSwipeButton = GetNode<Button>("UI/EventPanel/Margin/VBox/ChoiceSwipeButton");
        _choiceTimeoutButton = GetNode<Button>("UI/EventPanel/Margin/VBox/ChoiceTimeoutButton");
        _eventTimeBar = GetNode<TextureProgressBar>("UI/EventPanel/Margin/VBox/TimeBar");

        _funeralOverlay = GetNode<Control>("UI/FuneralOverlay");
        _funeralFadeRect = GetNode<ColorRect>("UI/FuneralOverlay/FadeRect");
        _funeralPanel = GetNode<PanelContainer>("UI/FuneralOverlay/FuneralPanel");
        _funeralSummaryLabel = GetNode<Label>("UI/FuneralOverlay/FuneralPanel/Margin/VBox/SummaryLabel");
        _funeralDeathCauseLabel = GetNode<Label>("UI/FuneralOverlay/FuneralPanel/Margin/VBox/DeathCauseLabel");
        _funeralSnapshotLabel = GetNode<Label>("UI/FuneralOverlay/FuneralPanel/Margin/VBox/SnapshotLabel");
        _funeralScoreLabel = GetNode<Label>("UI/FuneralOverlay/FuneralPanel/Margin/VBox/GenerationScoreLabel");
        _funeralInterstitialLabel = GetNode<Label>("UI/FuneralOverlay/FuneralPanel/Margin/VBox/InterstitialLabel");
        _nextGenerationButton = GetNode<Button>("UI/FuneralOverlay/FuneralPanel/Margin/VBox/NextGenerationButton");

        _nextGenerationPanel = GetNode<PanelContainer>("UI/FuneralOverlay/NextGenerationPanel");
        _inheritanceLabel = GetNode<Label>("UI/FuneralOverlay/NextGenerationPanel/Margin/VBox/InheritanceLabel");
        _childTypeLabel = GetNode<Label>("UI/FuneralOverlay/NextGenerationPanel/Margin/VBox/ChildTypeLabel");
        _childPreviewLabel = GetNode<Label>("UI/FuneralOverlay/NextGenerationPanel/Margin/VBox/ChildPreviewLabel");
        _uniqueSkillLabel = GetNode<Label>("UI/FuneralOverlay/NextGenerationPanel/Margin/VBox/UniqueSkillLabel");
        _biologicalButton = GetNode<Button>("UI/FuneralOverlay/NextGenerationPanel/Margin/VBox/TypeButtons/BiologicalButton");
        _adoptedButton = GetNode<Button>("UI/FuneralOverlay/NextGenerationPanel/Margin/VBox/TypeButtons/AdoptedButton");
        _birthButton = GetNode<Button>("UI/FuneralOverlay/NextGenerationPanel/Margin/VBox/ActionButtons/BirthButton");
        _birthAdBonusButton = GetNode<Button>("UI/FuneralOverlay/NextGenerationPanel/Margin/VBox/ActionButtons/BirthAdBonusButton");
        _typeButtons = GetNode<HBoxContainer>("UI/FuneralOverlay/NextGenerationPanel/Margin/VBox/TypeButtons");
        _actionButtons = GetNode<HBoxContainer>("UI/FuneralOverlay/NextGenerationPanel/Margin/VBox/ActionButtons");

        _willButton = GetNode<Button>("UI/WillButton");
        _willPanel = GetNode<PanelContainer>("UI/WillPanel");
        _willCandidateLabel = GetNode<Label>("UI/WillPanel/Margin/VBox/CandidateLabel");
        _willDetailLabel = GetNode<Label>("UI/WillPanel/Margin/VBox/DetailLabel");
        _willPrevButton = GetNode<Button>("UI/WillPanel/Margin/VBox/CandidateButtons/PrevButton");
        _willNextButton = GetNode<Button>("UI/WillPanel/Margin/VBox/CandidateButtons/NextButton");
        _willApplyButton = GetNode<Button>("UI/WillPanel/Margin/VBox/ApplyButton");
        _willCloseButton = GetNode<Button>("UI/WillPanel/Margin/VBox/CloseButton");

        _choiceTapButton.Pressed += OnChoiceTapPressed;
        _choiceSwipeButton.Pressed += OnChoiceSwipePressed;
        _choiceTimeoutButton.Pressed += OnChoiceTimeoutPressed;
        _nextGenerationButton.Pressed += OnNextGenerationButtonPressed;
        _biologicalButton.Pressed += OnBiologicalButtonPressed;
        _adoptedButton.Pressed += OnAdoptedButtonPressed;
        _birthButton.Pressed += OnBirthButtonPressed;
        _birthAdBonusButton.Pressed += OnBirthAdBonusButtonPressed;
        _willButton.Pressed += OnWillButtonPressed;
        _willPrevButton.Pressed += OnWillPrevButtonPressed;
        _willNextButton.Pressed += OnWillNextButtonPressed;
        _willApplyButton.Pressed += OnWillApplyButtonPressed;
        _willCloseButton.Pressed += OnWillCloseButtonPressed;

        _ageLabel = GetNode<Label>("UI/HUD/Margin/VBox/AgeLabel");
        _phaseLabel = GetNode<Label>("UI/HUD/Margin/VBox/PhaseLabel");
        _lifeLabel = GetNode<Label>("UI/HUD/Margin/VBox/LifeLabel");
        _lifeGauge = GetNodeOrNull<ProgressBar>("UI/HUD/Margin/VBox/LifeGauge");
        _generationLabel = GetNode<Label>("UI/HUD/Margin/VBox/GenerationLabel");
        _statsLabel = GetNode<Label>("UI/HUD/Margin/VBox/StatsLabel");
        _scoreLabel = GetNode<Label>("UI/HUD/Margin/VBox/ScoreLabel");
        _eventLabel = GetNode<Label>("UI/HUD/Margin/VBox/EventLabel");
        _navigatorArea = GetNode<Control>("UI/NavigatorArea");
        _navigatorLabel = GetNode<Label>("UI/NavigatorArea/Bubble/Margin/BubbleLabel");
        _hintLabel = GetNode<Label>("UI/HUD/Margin/VBox/HintLabel");
        _metaLabel = GetNode<Label>("UI/HUD/Margin/VBox/MetaLabel");
        _collectionLabel = GetNode<Label>("UI/HUD/Margin/VBox/CollectionLabel");
        _legendLabel = GetNode<Label>("UI/HUD/Margin/VBox/LegendLabel");
        _monetizeLabel = GetNode<Label>("UI/HUD/Margin/VBox/MonetizeLabel");
        _onlineLabel = GetNode<Label>("UI/HUD/Margin/VBox/OnlineLabel");
        _perfLabel = GetNode<Label>("UI/HUD/Margin/VBox/PerfLabel");
        _phaseBannerLabel = GetNode<Label>("UI/PhaseBannerLabel");
        _debugOverlay = GetNode<DebugOverlay>("UI/DebugOverlay");

        ApplyMobileUiPolish();

        var uiLayer = GetNode<CanvasLayer>("UI");
        _floatTextSpawner = new FloatTextSpawner
        {
            Name = "FloatTextSpawner",
            TargetLayer = uiLayer,
        };
        uiLayer.AddChild(_floatTextSpawner);

        _eventTimeBar.MaxValue = EventLimitSeconds;
        _eventTimeBar.Value = EventLimitSeconds;
        _eventPanel.Visible = false;
        _navigatorArea.Visible = false;
        _navigatorArea.Modulate = new Color(1f, 1f, 1f, 0f);
        _dropFlashRect.Visible = false;
        _dropFlashRect.Color = new Color(1f, 1f, 1f, 0f);
        _funeralOverlay.Visible = false;
        _funeralPanel.Visible = false;
        _nextGenerationPanel.Visible = false;
        _nextGenerationButton.Visible = false;
        _willButton.Visible = false;
        _willPanel.Visible = false;
        _phaseBannerLabel.Visible = false;
        _phaseBannerLabel.Position = new Vector2(PhaseBannerStartX, _phaseBannerLabel.Position.Y);
        _phaseBannerBaseY = _phaseBannerLabel.Position.Y;
        _phaseBannerActive = false;
        _phaseBannerElapsed = 0f;
        _dropFlashRemaining = 0f;

        _player.Position = new Vector2(PlayerFixedX, _player.Position.Y);
        ApplyPlayerVisualForPhase(_phaseManager.CurrentPhase);

        _playerGroundY = _player.Position.Y;
        _playerVerticalVelocity = 0f;
        _attackAnimationRemaining = 0f;
        _parryWindowRemaining = 0f;
        _elderlyCaneRemaining = 0f;
        _walkSpeedScale = 1f;
        _collectibleSpawnCooldown = _rng.RandfRange(CollectibleSpawnMinSeconds, CollectibleSpawnMaxSeconds);

        _voicePlayer = new VoicePlayer(GetNode<AudioStreamPlayer>("UI/VoicePlayer"));

        _obstacleSpawner = new ObstacleSpawner(_obstaclesRoot, ObstacleSpawnX, _player.Position.Y);
        _phaseManager.PhaseChanged += OnPhaseChanged;

        LoadPersistentState();
        _adManager.AdsRemoved = _iapManager.AdsRemoved;
        _battlePassManager.SetPremiumEnabled(_iapManager.PremiumPassOwned);
        ApplyPurchasedNavigatorProfiles();
        ApplyTheme();

        _debugOverlay.SetOverlayEnabled(_debugOverlayEnabled);
        _debugOverlay.UpdateMetrics(
            (float)Engine.GetFramesPerSecond(),
            GetViewportRect().Size,
            "Loading",
            _character.Age,
            PhaseToText(_phaseManager.CurrentPhase),
            BuildEraLabel());

        _founderManager.UpdateUnlocks(0, 0);

        StartGeneration(null);
        UpdateHud();
    }

    public override void _ExitTree()
    {
        Engine.TimeScale = 1f;
        _choiceTapButton.Pressed -= OnChoiceTapPressed;
        _choiceSwipeButton.Pressed -= OnChoiceSwipePressed;
        _choiceTimeoutButton.Pressed -= OnChoiceTimeoutPressed;
        _nextGenerationButton.Pressed -= OnNextGenerationButtonPressed;
        _biologicalButton.Pressed -= OnBiologicalButtonPressed;
        _adoptedButton.Pressed -= OnAdoptedButtonPressed;
        _birthButton.Pressed -= OnBirthButtonPressed;
        _birthAdBonusButton.Pressed -= OnBirthAdBonusButtonPressed;
        _willButton.Pressed -= OnWillButtonPressed;
        _willPrevButton.Pressed -= OnWillPrevButtonPressed;
        _willNextButton.Pressed -= OnWillNextButtonPressed;
        _willApplyButton.Pressed -= OnWillApplyButtonPressed;
        _willCloseButton.Pressed -= OnWillCloseButtonPressed;
        _navigatorBubbleTween?.Kill();
        SavePersistentState();
        _phaseManager.PhaseChanged -= OnPhaseChanged;
        _eventManager.Dispose();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut)
        {
            _timeManager.SetPaused(true);
            SavePersistentState();
        }
        else if (what == NotificationApplicationFocusIn)
        {
            _timeManager.SetPaused(false);
        }
    }

}
