using Godot;
using System;
using System.Collections.Generic;

namespace GenerationRoguelite.Sample;

public partial class GridSampleManager : Node2D
{
    // ──────────── レイアウト定数 ────────────
    private const int Columns = 3;
    private const int Rows = 3;

    private const float CellSize = 300f;
    private const float CellGap = 12f;
    private const float GridOriginX = 42f;
    private const float GridOriginY = 380f;
    private const float InputMinY = 0f; // 画面全域でスワイプ受付

    // ──────────── 動きの定数（ゆったり化） ────────────
    private const float MoveDuration = 0.28f;
    private const float GuardHoldThreshold = 0.25f;
    private const float SwipeThreshold = 40f;

    // ──────────── バランス定数 ────────────
    private const float BaseLife = 100f;
    private const float HitDamage = 10f;
    private const float GuardDamage = 2f;

    private const int ZoneComboThreshold = 8;
    private const float ZoneDuration = 3f;

    private const float WarningLeadTime = 1.8f;

    // ──────────── ガードフレーム制御 ────────────
    private const int GuardHoldFrame = 5;   // 0-indexed: 長押し中ここで停止
    private const float GuardReleaseFps = 10f; // 離した後の残りフレーム再生速度

    // ──────────── 色定数 ────────────
    private static readonly Color CellBorderDefaultColor = new(0.35f, 0.35f, 0.45f, 0.25f);
    private static readonly Color CellBorderZoneColor = new(1f, 0.85f, 0.2f, 0.4f);
    private static readonly Color CellDefaultColor = new(0.2f, 0.2f, 0.3f, 0.18f);
    private static readonly Color CellPlayerColor = new(0.27f, 0.53f, 1f, 0.2f);

    // ──────────── GameState ────────────
    private enum GameState
    {
        Idle,
        Playing,
        WaitingChoice,
        Dead,
        Cleared,
    }

    private enum ObstacleDirection
    {
        Right,
        Left,
        Top,
        Down,
    }

    private struct ObstacleEntry
    {
        public ObstacleDirection Direction;
        public int Lane;
        public float SpawnTime;
        public float SpeedPerCell;
        public int OrderNumber;
    }

    private struct ObstacleState
    {
        public ObstacleEntry Entry;
        public float ElapsedSinceSpawn;
        public int CurrentCellIndex;
        public bool IsActive;
        public bool IsSpawned;
        public bool IsFinished;
        public bool[] HitApplied;

        public TextureRect? Visual;

        public bool WarningShown;
        public ColorRect[]? WarningRects;
        public Sprite2D? WarningArrow;
        public Label? WarningOrder;
    }

    private struct WaveDef
    {
        public ObstacleEntry[] Obstacles;
    }

    private sealed class SimpleEvent
    {
        public string Text { get; }
        public string OptionA { get; }
        public string OptionB { get; }
        public string ResponseA { get; }
        public string ResponseB { get; }
        public int LifeGainA { get; }
        public int LifeGainB { get; }
        public int ComboGainA { get; }
        public int ComboGainB { get; }

        public SimpleEvent(
            string text, string optionA, string optionB,
            string responseA, string responseB,
            int lifeGainA, int lifeGainB,
            int comboGainA, int comboGainB)
        {
            Text = text; OptionA = optionA; OptionB = optionB;
            ResponseA = responseA; ResponseB = responseB;
            LifeGainA = lifeGainA; LifeGainB = lifeGainB;
            ComboGainA = comboGainA; ComboGainB = comboGainB;
        }
    }

    // ──────────── フィールド ────────────
    private readonly RandomNumberGenerator _rng = new();

    private readonly ColorRect[,] _cellBorders = new ColorRect[Columns, Rows];
    private readonly ColorRect[,] _cellInners = new ColorRect[Columns, Rows];
    private readonly Vector2[,] _cellCenters = new Vector2[Columns, Rows];

    private WaveDef[] _waves = Array.Empty<WaveDef>();
    private readonly List<ObstacleState> _obstacleStates = new();

    private SimpleEvent[] _events = Array.Empty<SimpleEvent>();

    private AnimatedSprite2D _player = null!;
    private Node2D _obstaclesRoot = null!;
    private Node2D _warningRoot = null!;
    private Node2D _effectsRoot = null!;

    private TextureRect _skyBg = null!;
    private TextureRect _midGround = null!;
    private TextureRect _groundLayer = null!;

    private ColorRect _operationArea = null!;

    private CanvasLayer _ui = null!;
    private Label _phaseLabel = null!;
    private Label _ageLabel = null!;
    private Label _waveLabel = null!;
    private Label _comboLabel = null!;
    private Label _scoreLabel = null!;
    private ProgressBar _lifeBar = null!;

    private TextureRect _naviIcon = null!;
    private TextureRect _naviBubble = null!;
    private Label _navigatorLabel = null!;

    private Label _stateLabel = null!;

    private PanelContainer _eventPanel = null!;
    private Label _eventText = null!;
    private Button _choiceA = null!;
    private Button _choiceB = null!;

    private ColorRect _flashOverlay = null!;
    private ColorRect _hitFlashOverlay = null!;
    private ColorRect _deathOverlay = null!;

    private ColorRect _leftAura = null!;
    private ColorRect _rightAura = null!;

    // テクスチャ
    private Texture2D _texSky = null!;
    private Texture2D _texMidGround = null!;
    private Texture2D _texGround = null!;

    private Texture2D _texObstacleRight = null!;
    private Texture2D _texObstacleTop = null!;
    private Texture2D _texObstacleBottom = null!;
    private Texture2D _texWarningIcon = null!;

    private Texture2D _texNaviIcon = null!;
    private Texture2D _texNaviBubble = null!;

    private Texture2D _texEventFrame = null!;
    private Texture2D _texEventButton = null!;
    private Texture2D _texEventButtonHover = null!;

    private Texture2D _texFxHit = null!;
    private Texture2D _texFxJustAvoid = null!;
    private Texture2D _texFxGuard = null!;
    private Texture2D _texFxZone = null!;
    private Texture2D _texFxCombo = null!;

    // プレイヤー状態
    private int _playerCol = 1;
    private int _playerRow = 1;

    private bool _isMoving;
    private bool _isGuarding;
    private bool _guardReleasing; // ガード解除アニメーション再生中

    private bool _spaceHolding;
    private bool _touchHolding;
    private bool _touchEligible;
    private bool _swipeTracking;

    private Vector2 _touchStart;
    private float _holdElapsed;

    private GameState _state = GameState.Idle;

    private int _currentWaveIndex;
    private float _waveElapsed;
    private bool _waveResolved;

    private float _gameTime;

    private float _life = BaseLife;
    private int _combo;
    private int _totalScore;

    private bool _zoneActive;
    private float _zoneRemaining;

    private float _stayTimer;
    private int _lastLeftCol = -1;
    private int _lastLeftRow = -1;
    private float _lastMoveTime = -999f;
    private float _stayTimeAtLastMove;
    private bool _lastMoveHadThreatAtDestination;

    private bool _allWavesCleared;

    private bool[] _directionCalloutDone = new bool[4];

    private Tween? _speechTween;
    private Tween? _shakeTween;

    private Node2D? _swipeHint;
    private Tween? _swipeHintPulseTween;
    private bool _hintShown;

    private TextureRect? _guardFx;
    private TextureRect? _zoneFx;
    private Tween? _zonePulseTween;

    private SimpleEvent? _activeEvent;
    private int _pendingWaveIndex = -1;
    private bool _eventLocked;

    private StyleBoxFlat _lifeBackgroundStyle = null!;
    private StyleBoxFlat _lifeFillStyle = null!;

    // 背景スクロール用
    private float _bgScrollOffset;

    // ──────────── ライフサイクル ────────────
    public override void _Ready()
    {
        _rng.Randomize();

        CacheNodes();
        BuildGridVisuals();
        BuildPlayerFrames();

        LoadTextures();
        ApplyStaticTextures();
        ApplyStyles();

        BuildWaveDefs();
        BuildEvents();

        ResetToIdleState(showIntroMessage: true);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _gameTime += dt;

        // 背景微速スクロール（走ってる感）
        if (_state == GameState.Playing)
        {
            _bgScrollOffset += dt * 30f;
            if (_midGround != null)
            {
                // midgroundを左にスクロールさせるシンプル実装
                float offsetX = -(_bgScrollOffset % 1080f);
                _midGround.Position = new Vector2(offsetX, _midGround.Position.Y);
            }
        }

        if (_state == GameState.Playing)
        {
            _waveElapsed += dt;
            TickStayTimer(dt);
            TickObstacles(dt);
            TickZone(dt);
            TryResolveWaveEnd();
        }

        // イベント中も障害物は動く
        if (_state == GameState.WaitingChoice)
        {
            _waveElapsed += dt;
            TickObstacles(dt);
        }

        TickGuardHold(dt);
        TickGuardFrameControl();
        UpdateAttachedEffectsPosition();
        UpdateHud();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (IsStartInput(@event) && (_state is GameState.Idle or GameState.Dead or GameState.Cleared))
        {
            StartRun();
            return;
        }

        // Playing と WaitingChoice 両方で操作を受け付ける
        if (_state != GameState.Playing && _state != GameState.WaitingChoice)
        {
            return;
        }

        HandleKeyboardInput(@event);
        HandleTouchInput(@event);
    }

    // ──────────── ノード取得 ────────────
    private void CacheNodes()
    {
        _player = GetNode<AnimatedSprite2D>("Player");
        _player.AnimationFinished += OnPlayerAnimationFinished;

        _obstaclesRoot = GetNode<Node2D>("Obstacles");
        _warningRoot = GetNode<Node2D>("WarningLines");
        _effectsRoot = GetNode<Node2D>("Effects");

        Node2D background = GetNode<Node2D>("Background");
        _skyBg = background.GetNode<TextureRect>("SkyBg");
        _midGround = background.GetNode<TextureRect>("MidGround");

        // GroundLayerは動的に作成（tscnに無い場合）
        _groundLayer = background.GetNodeOrNull<TextureRect>("GroundLayer");
        if (_groundLayer == null)
        {
            _groundLayer = new TextureRect();
            _groundLayer.Name = "GroundLayer";
            background.AddChild(_groundLayer);
        }

        _operationArea = GetNode<ColorRect>("OperationArea");

        _ui = GetNode<CanvasLayer>("UI");
        _phaseLabel = _ui.GetNode<Label>("HUD/PhaseLabel");
        _ageLabel = _ui.GetNode<Label>("HUD/AgeLabel");
        _waveLabel = _ui.GetNode<Label>("HUD/WaveLabel");
        _comboLabel = _ui.GetNode<Label>("HUD/ComboLabel");
        _scoreLabel = _ui.GetNode<Label>("HUD/ScoreLabel");
        _lifeBar = _ui.GetNode<ProgressBar>("HUD/LifeBar");

        _naviIcon = _ui.GetNode<TextureRect>("NavigatorArea/NaviIcon");
        _naviBubble = _ui.GetNode<TextureRect>("NavigatorArea/NaviBubble");
        _navigatorLabel = _ui.GetNode<Label>("NavigatorArea/NaviBubble/NavigatorLabel");

        _stateLabel = _ui.GetNode<Label>("StateLabel");

        _eventPanel = _ui.GetNode<PanelContainer>("EventPanel");

        // EventPanelの子ノード: まずVBox構造を試み、なければ直下を探す
        _eventText = _eventPanel.GetNodeOrNull<Label>("EventVBox/EventText")
                     ?? _eventPanel.GetNode<Label>("EventText");
        _choiceA = _eventPanel.GetNodeOrNull<Button>("EventVBox/ButtonBox/ChoiceA")
                   ?? _eventPanel.GetNode<Button>("ChoiceA");
        _choiceB = _eventPanel.GetNodeOrNull<Button>("EventVBox/ButtonBox/ChoiceB")
                   ?? _eventPanel.GetNode<Button>("ChoiceB");

        _choiceA.Pressed += () => OnEventOptionSelected(chooseA: true);
        _choiceB.Pressed += () => OnEventOptionSelected(chooseA: false);

        _flashOverlay = _ui.GetNode<ColorRect>("FlashOverlay");
        _hitFlashOverlay = _ui.GetNode<ColorRect>("HitFlashOverlay");
        _deathOverlay = _ui.GetNode<ColorRect>("DeathOverlay");

        Node2D grid = GetNode<Node2D>("GridContainer");
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                _cellBorders[col, row] = grid.GetNode<ColorRect>($"Cell_{col}_{row}");
            }
        }
    }

    // ──────────── グリッド構築 ────────────
    private void BuildGridVisuals()
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                float x = GridOriginX + (col * (CellSize + CellGap)) + (CellSize * 0.5f);
                float y = GridOriginY + (row * (CellSize + CellGap)) + (CellSize * 0.5f);
                _cellCenters[col, row] = new Vector2(x, y);

                ColorRect border = _cellBorders[col, row];
                // tscnの位置を上書き（新しいレイアウト用）
                border.OffsetLeft = GridOriginX + (col * (CellSize + CellGap));
                border.OffsetTop = GridOriginY + (row * (CellSize + CellGap));
                border.OffsetRight = border.OffsetLeft + CellSize;
                border.OffsetBottom = border.OffsetTop + CellSize;
                border.Color = CellBorderDefaultColor;

                ColorRect? inner = border.GetNodeOrNull<ColorRect>("Inner");
                if (inner == null)
                {
                    inner = new ColorRect();
                    inner.Name = "Inner";
                    inner.AnchorsPreset = (int)Control.LayoutPreset.TopLeft;
                    inner.AnchorRight = 0f;
                    inner.AnchorBottom = 0f;
                    inner.OffsetLeft = 3f;
                    inner.OffsetTop = 3f;
                    inner.OffsetRight = CellSize - 3f;
                    inner.OffsetBottom = CellSize - 3f;
                    border.AddChild(inner);
                }

                inner.Color = CellDefaultColor;
                _cellInners[col, row] = inner;
            }
        }

        CreateScreenAuras();
    }

    // ──────────── アニメーション構築（FPS調整済み） ────────────
    private void BuildPlayerFrames()
    {
        SpriteFrames frames = new();

        // FPSを全体的に下げてゆったり感を出す
        RegisterAnimation(frames, "idle", GD.Load<Texture2D>("res://Assets/Sprites/female_idle.png"), 5f, true);
        RegisterAnimation(frames, "run", GD.Load<Texture2D>("res://Assets/Sprites/female_run.png"), 6f, true);
        RegisterAnimation(frames, "move", GD.Load<Texture2D>("res://Assets/Sprites/female_move.png"), 12f, false);
        RegisterAnimation(frames, "guard", GD.Load<Texture2D>("res://Assets/Sprites/female_guard.png"), 10f, false); // loop=false!
        RegisterAnimation(frames, "guard_release", GD.Load<Texture2D>("res://Assets/Sprites/female_guard.png"), GuardReleaseFps, false); // フレーム6-8用
        RegisterAnimation(frames, "hurt", GD.Load<Texture2D>("res://Assets/Sprites/female_hurt.png"), 6f, false);
        RegisterAnimation(frames, "death", GD.Load<Texture2D>("res://Assets/Sprites/female_death.png"), 5f, false);

        _player.SpriteFrames = frames;
    }

    private static void RegisterAnimation(SpriteFrames frames, string animationName, Texture2D? sheet, float fps, bool loop)
    {
        if (sheet == null)
        {
            throw new InvalidOperationException($"Texture missing for animation '{animationName}'.");
        }

        frames.AddAnimation(animationName);
        frames.SetAnimationSpeed(animationName, fps);
        frames.SetAnimationLoop(animationName, loop);

        if (animationName == "guard_release")
        {
            // フレーム6,7,8のみ登録
            for (int i = 6; i < 9; i++)
            {
                int col = i % 3;
                int row = i / 3;
                AtlasTexture atlas = new();
                atlas.Atlas = sheet;
                atlas.Region = new Rect2(col * 341, row * 341, 341, 341);
                frames.AddFrame(animationName, atlas);
            }
        }
        else
        {
            for (int i = 0; i < 9; i++)
            {
                int col = i % 3;
                int row = i / 3;
                AtlasTexture atlas = new();
                atlas.Atlas = sheet;
                atlas.Region = new Rect2(col * 341, row * 341, 341, 341);
                frames.AddFrame(animationName, atlas);
            }
        }
    }

    // ──────────── テクスチャ読み込み ────────────
    private void LoadTextures()
    {
        _texSky = LoadRequiredTexture("res://Assets/Backgrounds/sky_bg2.webp");
        _texMidGround = LoadRequiredTexture("res://Assets/Backgrounds/midground_town.webp");

        // 地面テクスチャ: あれば読む、なければmidgroundを流用
        _texGround = GD.Load<Texture2D>("res://Assets/Backgrounds/ground_near.webp")
                     ?? GD.Load<Texture2D>("res://Assets/Backgrounds/ground_mid.webp")
                     ?? _texMidGround;

        _texObstacleRight = LoadRequiredTexture("res://Assets/Objects/obstacle_right.webp");
        _texObstacleTop = LoadRequiredTexture("res://Assets/Objects/obstacle_top.webp");
        _texObstacleBottom = LoadRequiredTexture("res://Assets/Objects/obstacle_bottom.webp");
        _texWarningIcon = LoadRequiredTexture("res://Assets/UI/ui_warning_icon.webp");

        _texNaviIcon = LoadRequiredTexture("res://Assets/Sprites/navigator.png");
        _texNaviBubble = LoadRequiredTexture("res://Assets/UI/navi_bubble_thought.webp");

        _texEventFrame = LoadRequiredTexture("res://Assets/UI/ui_event_frame.webp");
        _texEventButton = LoadRequiredTexture("res://Assets/UI/ui_event_button.webp");
        _texEventButtonHover = LoadRequiredTexture("res://Assets/UI/ui_event_button_hover.webp");

        _texFxHit = LoadRequiredTexture("res://Assets/Effects/fx_hit.webp");
        _texFxJustAvoid = LoadRequiredTexture("res://Assets/Effects/fx_just_avoid.webp");
        _texFxGuard = LoadRequiredTexture("res://Assets/Effects/fx_guard_shield.webp");
        _texFxZone = LoadRequiredTexture("res://Assets/Effects/fx_zone.webp");
        _texFxCombo = LoadRequiredTexture("res://Assets/UI/fx_combo.webp");
    }

    // ──────────── テクスチャ適用（背景修正+画面拡大） ────────────
    private void ApplyStaticTextures()
    {
        // 空: 画面上部〜グリッド上端あたりまで
        _skyBg.Texture = _texSky;
        _skyBg.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _skyBg.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
        _skyBg.OffsetLeft = 0f;
        _skyBg.OffsetTop = 0f;
        _skyBg.OffsetRight = 1080f;
        _skyBg.OffsetBottom = 1000f;

        // 中景: グリッド背後に配置（街並みや森）
        _midGround.Texture = _texMidGround;
        _midGround.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _midGround.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
        _midGround.OffsetLeft = 0f;
        _midGround.OffsetTop = 300f;
        _midGround.OffsetRight = 2160f; // 2倍幅でスクロール用
        _midGround.OffsetBottom = 1100f;

        // 地面: グリッド下部〜操作エリア上端
        _groundLayer.Texture = _texGround;
        _groundLayer.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _groundLayer.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
        _groundLayer.OffsetLeft = 0f;
        _groundLayer.OffsetTop = 1000f;
        _groundLayer.OffsetRight = 1080f;
        _groundLayer.OffsetBottom = 1420f;

        // ナビアイコン
        ConfigureTextureRectFill(_naviIcon);
        _naviIcon.Texture = _texNaviIcon;
        _naviIcon.CustomMinimumSize = new Vector2(80f, 80f);

        // ナビ吹き出し
        ConfigureTextureRectFill(_naviBubble);
        _naviBubble.Texture = _texNaviBubble;
        _naviBubble.Modulate = new Color(1f, 1f, 1f, 0f);
        _navigatorLabel.Modulate = new Color(1f, 1f, 1f, 0f);

        // イベントパネル
        _eventPanel.Visible = false;
        _eventPanel.Modulate = Colors.White;

        // オーバーレイ: mouse_filter=Ignore で入力ブロックを防止
        _flashOverlay.Color = new Color(1f, 1f, 1f, 0f);
        _hitFlashOverlay.Color = new Color(1f, 0f, 0f, 0f);
        _deathOverlay.Color = new Color(0f, 0f, 0f, 0f);
        _flashOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        _hitFlashOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        _deathOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;

        // 操作エリアも入力を通す
        _operationArea.MouseFilter = Control.MouseFilterEnum.Ignore;

        // プレイヤー拡大（画面に対してキャラを大きく）
        _player.Scale = new Vector2(0.72f, 0.72f);
        _player.Position = _cellCenters[1, 1];
    }

    // ──────────── スタイル適用 ────────────
    private void ApplyStyles()
    {
        _lifeBackgroundStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.15f, 0.2f, 0.8f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
        };

        _lifeFillStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.8f, 0.3f, 1f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
        };

        _lifeBar.AddThemeStyleboxOverride("background", _lifeBackgroundStyle);
        _lifeBar.AddThemeStyleboxOverride("fill", _lifeFillStyle);

        StyleBoxTexture panelStyle = new()
        {
            Texture = _texEventFrame,
            TextureMarginLeft = 28, TextureMarginTop = 28,
            TextureMarginRight = 28, TextureMarginBottom = 28,
        };
        _eventPanel.AddThemeStyleboxOverride("panel", panelStyle);

        StyleBoxTexture buttonNormal = new()
        {
            Texture = _texEventButton,
            TextureMarginLeft = 18, TextureMarginTop = 18,
            TextureMarginRight = 18, TextureMarginBottom = 18,
        };

        StyleBoxTexture buttonHover = new()
        {
            Texture = _texEventButtonHover,
            TextureMarginLeft = 18, TextureMarginTop = 18,
            TextureMarginRight = 18, TextureMarginBottom = 18,
        };

        SetupButtonStyle(_choiceA, buttonNormal, buttonHover);
        SetupButtonStyle(_choiceB, buttonNormal, buttonHover);

        // フォントサイズ少し拡大
        _phaseLabel.AddThemeFontSizeOverride("font_size", 32);
        _ageLabel.AddThemeFontSizeOverride("font_size", 32);
        _waveLabel.AddThemeFontSizeOverride("font_size", 30);
        _comboLabel.AddThemeFontSizeOverride("font_size", 30);
        _scoreLabel.AddThemeFontSizeOverride("font_size", 36);

        _navigatorLabel.AddThemeFontSizeOverride("font_size", 24);
        _eventText.AddThemeFontSizeOverride("font_size", 28);
        _choiceA.AddThemeFontSizeOverride("font_size", 30);
        _choiceB.AddThemeFontSizeOverride("font_size", 30);
        _stateLabel.AddThemeFontSizeOverride("font_size", 36);

        _phaseLabel.AddThemeColorOverride("font_color", Colors.White);
        _ageLabel.AddThemeColorOverride("font_color", Colors.White);
        _waveLabel.AddThemeColorOverride("font_color", Colors.White);
        _comboLabel.AddThemeColorOverride("font_color", Colors.White);
        _scoreLabel.AddThemeColorOverride("font_color", Colors.White);
        _navigatorLabel.AddThemeColorOverride("font_color", Colors.White);
        _eventText.AddThemeColorOverride("font_color", Colors.White);
        _stateLabel.AddThemeColorOverride("font_color", Colors.White);

        _navigatorLabel.AutowrapMode = TextServer.AutowrapMode.Word;
        _eventText.AutowrapMode = TextServer.AutowrapMode.Word;
    }

    private static void SetupButtonStyle(Button button, StyleBoxTexture normal, StyleBoxTexture hover)
    {
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.AddThemeStyleboxOverride("focus", hover);
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeColorOverride("font_focus_color", Colors.White);
    }

    // ──────────── Wave定義（SpeedPerCell拡大=ゆっくり） ────────────
    private void BuildWaveDefs()
    {
        _waves = new[]
        {
            new WaveDef
            {
                Obstacles = new[]
                {
                    new ObstacleEntry { Direction = ObstacleDirection.Right, Lane = 0, SpawnTime = 2.5f, SpeedPerCell = 0.6f, OrderNumber = 1 },
                    new ObstacleEntry { Direction = ObstacleDirection.Right, Lane = 1, SpawnTime = 5.0f, SpeedPerCell = 0.6f, OrderNumber = 2 },
                    new ObstacleEntry { Direction = ObstacleDirection.Right, Lane = 2, SpawnTime = 7.5f, SpeedPerCell = 0.6f, OrderNumber = 3 },
                },
            },
            new WaveDef
            {
                Obstacles = new[]
                {
                    new ObstacleEntry { Direction = ObstacleDirection.Right, Lane = 1, SpawnTime = 2.5f, SpeedPerCell = 0.5f, OrderNumber = 1 },
                    new ObstacleEntry { Direction = ObstacleDirection.Top, Lane = 0, SpawnTime = 4.5f, SpeedPerCell = 0.5f, OrderNumber = 2 },
                    new ObstacleEntry { Direction = ObstacleDirection.Right, Lane = 0, SpawnTime = 6.5f, SpeedPerCell = 0.5f, OrderNumber = 3 },
                    new ObstacleEntry { Direction = ObstacleDirection.Top, Lane = 2, SpawnTime = 8.5f, SpeedPerCell = 0.5f, OrderNumber = 4 },
                    new ObstacleEntry { Direction = ObstacleDirection.Right, Lane = 2, SpawnTime = 10.0f, SpeedPerCell = 0.5f, OrderNumber = 5 },
                },
            },
            new WaveDef
            {
                Obstacles = new[]
                {
                    new ObstacleEntry { Direction = ObstacleDirection.Right, Lane = 0, SpawnTime = 2.5f, SpeedPerCell = 0.5f, OrderNumber = 1 },
                    new ObstacleEntry { Direction = ObstacleDirection.Right, Lane = 2, SpawnTime = 3.5f, SpeedPerCell = 0.5f, OrderNumber = 2 },
                    new ObstacleEntry { Direction = ObstacleDirection.Top, Lane = 1, SpawnTime = 5.5f, SpeedPerCell = 0.4f, OrderNumber = 3 },
                    new ObstacleEntry { Direction = ObstacleDirection.Right, Lane = 1, SpawnTime = 7.0f, SpeedPerCell = 0.55f, OrderNumber = 4 },
                    new ObstacleEntry { Direction = ObstacleDirection.Top, Lane = 0, SpawnTime = 8.5f, SpeedPerCell = 0.35f, OrderNumber = 5 },
                    new ObstacleEntry { Direction = ObstacleDirection.Right, Lane = 0, SpawnTime = 10.0f, SpeedPerCell = 0.45f, OrderNumber = 6 },
                    new ObstacleEntry { Direction = ObstacleDirection.Top, Lane = 2, SpawnTime = 11.0f, SpeedPerCell = 0.45f, OrderNumber = 7 },
                    new ObstacleEntry { Direction = ObstacleDirection.Right, Lane = 1, SpawnTime = 12.5f, SpeedPerCell = 0.35f, OrderNumber = 8 },
                },
            },
        };
    }

    private void BuildEvents()
    {
        _events = new[]
        {
            new SimpleEvent(
                "道端で困っている人を見かけた",
                "助ける", "通り過ぎる",
                "優しいね！", "まぁ仕方ないか",
                lifeGainA: 5, lifeGainB: 0,
                comboGainA: 0, comboGainB: 0),
            new SimpleEvent(
                "落とし物を拾った",
                "届ける", "自分のものにする",
                "いいことした！", "うーん...",
                lifeGainA: 0, lifeGainB: 10,
                comboGainA: 2, comboGainB: 0),
            new SimpleEvent(
                "友人から誘いの連絡が来た",
                "遊びに行く", "断って休む",
                "リフレッシュ！", "休息も大事",
                lifeGainA: 3, lifeGainB: 5,
                comboGainA: 0, comboGainB: 0),
        };
    }

    // ──────────── 状態リセット ────────────
    private void ResetToIdleState(bool showIntroMessage)
    {
        _state = GameState.Idle;
        _currentWaveIndex = 0;
        _waveElapsed = 0f;
        _waveResolved = false;

        _life = BaseLife;
        _combo = 0;
        _totalScore = 0;

        _zoneActive = false;
        _zoneRemaining = 0f;

        _isMoving = false;
        _isGuarding = false;
        _guardReleasing = false;
        _spaceHolding = false;
        _touchHolding = false;
        _touchEligible = false;
        _swipeTracking = false;
        _holdElapsed = 0f;

        _stayTimer = 0f;
        _lastLeftCol = -1;
        _lastLeftRow = -1;
        _lastMoveTime = -999f;
        _stayTimeAtLastMove = 0f;
        _lastMoveHadThreatAtDestination = false;

        _directionCalloutDone = new bool[4];
        _bgScrollOffset = 0f;

        _playerCol = 1;
        _playerRow = 1;
        _player.Position = _cellCenters[_playerCol, _playerRow];
        _player.Modulate = Colors.White;
        _player.Play("idle");

        _flashOverlay.Color = new Color(1f, 1f, 1f, 0f);
        _hitFlashOverlay.Color = new Color(1f, 0f, 0f, 0f);
        _deathOverlay.Color = new Color(0f, 0f, 0f, 0f);

        _eventPanel.Visible = false;
        _eventPanel.Modulate = Colors.White;
        _activeEvent = null;
        _pendingWaveIndex = -1;
        _eventLocked = false;

        ClearObstacleVisuals();
        ClearWarningVisuals();
        ClearTransientEffects();

        HideGuardEffect();
        HideZoneEffect();

        _hintShown = false;
        _swipeHintPulseTween?.Kill();
        if (_swipeHint != null)
        {
            _swipeHint.QueueFree();
            _swipeHint = null;
        }
        CreateSwipeHint();

        SetAuraAlpha(0f);
        SetCellBorderZoneColor(enabled: false);

        _allWavesCleared = false;

        UpdateCellColors();

        if (showIntroMessage)
        {
            Speak("画面をスワイプして動こう！", 4f);
        }

        UpdateHud();
    }

    private void StartRun()
    {
        _allWavesCleared = false;
        ResetToIdleState(showIntroMessage: false);
        BeginWave(0);
    }

    private void BeginWave(int waveIndex)
    {
        _state = GameState.Playing;
        _currentWaveIndex = waveIndex;
        _waveElapsed = 0f;
        _waveResolved = false;

        _obstacleStates.Clear();

        ObstacleEntry[] entries = _waves[waveIndex].Obstacles;
        for (int i = 0; i < entries.Length; i++)
        {
            ObstacleState state = new()
            {
                Entry = entries[i],
                ElapsedSinceSpawn = 0f,
                CurrentCellIndex = -1,
                IsActive = false,
                IsSpawned = false,
                IsFinished = false,
                HitApplied = new bool[3],
                Visual = null,
                WarningShown = false,
                WarningRects = null,
                WarningArrow = null,
                WarningOrder = null,
            };
            _obstacleStates.Add(state);
        }

        _stateLabel.Text = "";
        _player.Play(_isGuarding ? "guard" : "run");

        Speak($"Wave {waveIndex + 1} スタート！", 2f);
    }

    // ──────────── 入力判定 ────────────
    private bool IsStartInput(InputEvent @event)
    {
        if (@event is InputEventKey key)
            return key.Pressed && !key.Echo;
        if (@event is InputEventScreenTouch touch)
            return touch.Pressed;
        return false;
    }

    private void HandleKeyboardInput(InputEvent @event)
    {
        if (@event is InputEventKey guardKey && guardKey.Keycode == Key.Space)
        {
            if (guardKey.Pressed && !guardKey.Echo)
            {
                _spaceHolding = true;
                _holdElapsed = 0f;
            }
            else if (!guardKey.Pressed)
            {
                _spaceHolding = false;
                if (!_touchHolding) EndGuard();
            }
            return;
        }

        if (@event is not InputEventKey moveKey || !moveKey.Pressed || moveKey.Echo)
            return;

        bool moved = moveKey.Keycode switch
        {
            Key.Up => TryMove(0, -1),
            Key.Down => TryMove(0, 1),
            Key.Left => TryMove(-1, 0),
            Key.Right => TryMove(1, 0),
            _ => false,
        };

        if (moved) OnFirstSuccessfulMove();
    }

    private void HandleTouchInput(InputEvent @event)
    {
        if (@event is InputEventScreenTouch touch)
        {
            if (touch.Pressed)
            {
                _touchEligible = true;
                _touchHolding = true;
                _swipeTracking = true;
                _touchStart = touch.Position;
                _holdElapsed = 0f;
            }
            else
            {
                _touchHolding = false;
                _touchEligible = false;
                _swipeTracking = false;
                if (!_spaceHolding) EndGuard();
            }
            return;
        }

        if (@event is not InputEventScreenDrag drag || !_swipeTracking || !_touchEligible)
            return;

        Vector2 delta = drag.Position - _touchStart;
        if (delta.Length() < SwipeThreshold)
            return;

        bool moved;
        if (Mathf.Abs(delta.X) > Mathf.Abs(delta.Y))
            moved = TryMove(delta.X > 0f ? 1 : -1, 0);
        else
            moved = TryMove(0, delta.Y > 0f ? 1 : -1);

        _swipeTracking = false;
        _touchHolding = false;
        _holdElapsed = 0f;

        if (moved) OnFirstSuccessfulMove();
    }

    // ──────────── 移動（ヌルっと感の改善） ────────────
    private bool TryMove(int dx, int dy)
    {
        // Playing と WaitingChoice 両方で移動可能
        if ((_state != GameState.Playing && _state != GameState.WaitingChoice) || _isMoving || _isGuarding || _guardReleasing)
            return false;

        int targetCol = _playerCol + dx;
        int targetRow = _playerRow + dy;

        if (targetCol < 0 || targetCol >= Columns || targetRow < 0 || targetRow >= Rows)
            return false;

        int fromCol = _playerCol;
        int fromRow = _playerRow;
        float stayBeforeMove = _stayTimer;

        _lastMoveHadThreatAtDestination = HasIncomingObstacleForCell(targetCol, targetRow, 0.6f);

        _playerCol = targetCol;
        _playerRow = targetRow;
        _stayTimer = 0f;
        _isMoving = true;

        // moveアニメーション開始
        _player.Play("move");

        // ヌルっとした移動: Cubic.InOut で加速→減速
        Tween tween = CreateTween();
        tween.TweenProperty(_player, "position", _cellCenters[_playerCol, _playerRow], MoveDuration)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenCallback(Callable.From(() => OnMoveFinished(fromCol, fromRow, stayBeforeMove)));

        UpdateCellColors();
        return true;
    }

    private void OnMoveFinished(int fromCol, int fromRow, float stayBeforeMove)
    {
        _isMoving = false;

        _lastLeftCol = fromCol;
        _lastLeftRow = fromRow;
        _lastMoveTime = _gameTime;
        _stayTimeAtLastMove = stayBeforeMove;
        _stayTimer = 0f;

        // moveアニメーションが終わるまで待つ（AnimationFinishedで復帰）
        // ただしmoveが既に終わっていればrunに戻す
        if (_player.Animation != "move")
        {
            if (_state == GameState.Playing || _state == GameState.WaitingChoice)
                _player.Play(_isGuarding ? "guard" : "run");
        }
        // else: OnPlayerAnimationFinished で "run" に戻る
    }

    private void OnFirstSuccessfulMove()
    {
        if (_hintShown) return;
        DismissSwipeHint(immediate: false);
        Speak("いい感じ！障害物を避けて進もう！", 2.2f);
    }

    private void TickStayTimer(float dt)
    {
        if (!_isMoving) _stayTimer += dt;
    }

    // ──────────── ガード（フレーム制御付き） ────────────
    private void TickGuardHold(float dt)
    {
        bool holding = _spaceHolding || _touchHolding;

        if ((_state != GameState.Playing && _state != GameState.WaitingChoice) || _isMoving)
        {
            if (!holding) _holdElapsed = 0f;
            return;
        }

        if (holding)
        {
            _holdElapsed += dt;
            if (_holdElapsed >= GuardHoldThreshold)
                StartGuard();
        }
        else
        {
            _holdElapsed = 0f;
            EndGuard();
        }
    }

    private void TickGuardFrameControl()
    {
        // ガード中: フレーム GuardHoldFrame で停止
        if (_isGuarding && _player.Animation == "guard" && _player.Frame >= GuardHoldFrame)
        {
            _player.Frame = GuardHoldFrame;
            _player.Pause();
        }
    }

    private void StartGuard()
    {
        if (_isGuarding || (_state != GameState.Playing && _state != GameState.WaitingChoice))
            return;

        _isGuarding = true;
        _guardReleasing = false;
        _player.Play("guard");
        ShowGuardEffect();
    }

    private void EndGuard()
    {
        if (!_isGuarding) return;

        _isGuarding = false;
        HideGuardEffect();

        // ガード解除: フレーム6-8を再生
        _guardReleasing = true;
        _player.Play("guard_release");
        // guard_release の AnimationFinished で _guardReleasing = false & run に戻る
    }

    // ──────────── 障害物Tick ────────────
    private void TickObstacles(float dt)
    {
        for (int i = 0; i < _obstacleStates.Count; i++)
        {
            ObstacleState state = _obstacleStates[i];
            if (state.IsFinished) { _obstacleStates[i] = state; continue; }

            float warnStart = state.Entry.SpawnTime - WarningLeadTime;
            if (!state.WarningShown && _waveElapsed >= warnStart)
                ShowWarning(ref state);

            if (state.WarningShown && !state.IsSpawned)
                UpdateWarningVisual(ref state);

            if (!state.IsSpawned && _waveElapsed >= state.Entry.SpawnTime)
                SpawnObstacle(ref state);

            if (state.IsActive)
                UpdateObstacleMotion(ref state, dt);

            _obstacleStates[i] = state;
        }
    }

    // ──────────── 警告表示（視認性改善） ────────────
    private void ShowWarning(ref ObstacleState state)
    {
        state.WarningShown = true;

        ColorRect[] warningRects = new ColorRect[3];
        for (int i = 0; i < 3; i++)
        {
            Vector2I cell = GetPathCell(state.Entry.Direction, state.Entry.Lane, i);
            Vector2 center = _cellCenters[cell.X, cell.Y];

            ColorRect rect = new();
            rect.OffsetLeft = center.X - (CellSize * 0.5f);
            rect.OffsetTop = center.Y - (CellSize * 0.5f);
            rect.OffsetRight = center.X + (CellSize * 0.5f);
            rect.OffsetBottom = center.Y + (CellSize * 0.5f);
            rect.Color = new Color(1f, 0.15f, 0.15f, 0.08f);
            _warningRoot.AddChild(rect);
            warningRects[i] = rect;
        }

        // 方向を示す矢印アイコン（大きめ）
        Vector2I entryCell = GetPathCell(state.Entry.Direction, state.Entry.Lane, 0);
        Vector2 entryCenter = _cellCenters[entryCell.X, entryCell.Y];

        // 警告矢印をSprite2Dに変更して回転基準を中心にする
        Sprite2D arrow = new();
        arrow.Texture = _texWarningIcon;
        arrow.Scale = new Vector2(0.6f, 0.6f);
        arrow.Position = GetWarningArrowPosition(state.Entry.Direction, entryCenter);
        arrow.RotationDegrees = GetWarningArrowRotation(state.Entry.Direction);

        // 番号: 大きくして背景付き
        Label order = new();
        order.Text = state.Entry.OrderNumber.ToString();
        order.HorizontalAlignment = HorizontalAlignment.Center;
        order.VerticalAlignment = VerticalAlignment.Center;
        order.Position = GetWarningNumberPosition(state.Entry.Direction, entryCenter);
        order.AddThemeFontSizeOverride("font_size", 52);
        order.AddThemeColorOverride("font_color", Colors.White);
        order.AddThemeColorOverride("font_outline_color", new Color(0.8f, 0.1f, 0.1f, 1f));
        order.AddThemeConstantOverride("outline_size", 6);

        _warningRoot.AddChild(arrow);
        _warningRoot.AddChild(order);

        state.WarningRects = warningRects;
        state.WarningArrow = arrow;
        state.WarningOrder = order;

        int dirIndex = (int)state.Entry.Direction;
        if (!_directionCalloutDone[dirIndex])
        {
            _directionCalloutDone[dirIndex] = true;
            Speak(GetDirectionCallout(state.Entry.Direction), 1.5f);
        }
    }

    private Vector2 GetWarningArrowPosition(ObstacleDirection dir, Vector2 cellCenter)
    {
        float offset = CellSize * 0.5f + 30f;
        return dir switch
        {
            ObstacleDirection.Right => cellCenter + new Vector2(offset, 0f),
            ObstacleDirection.Left => cellCenter + new Vector2(-offset, 0f),
            ObstacleDirection.Top => cellCenter + new Vector2(0f, -offset),
            ObstacleDirection.Down => cellCenter + new Vector2(0f, offset),
            _ => cellCenter,
        };
    }

    private Vector2 GetWarningNumberPosition(ObstacleDirection dir, Vector2 cellCenter)
    {
        float offset = CellSize * 0.5f + 50f;
        return dir switch
        {
            ObstacleDirection.Right => cellCenter + new Vector2(offset, -30f),
            ObstacleDirection.Left => cellCenter + new Vector2(-offset - 20f, -30f),
            ObstacleDirection.Top => cellCenter + new Vector2(-14f, -offset - 20f),
            ObstacleDirection.Down => cellCenter + new Vector2(-14f, offset),
            _ => cellCenter,
        };
    }

    private void UpdateWarningVisual(ref ObstacleState state)
    {
        if (state.WarningRects == null) return;

        float warnStart = state.Entry.SpawnTime - WarningLeadTime;
        float ratio = Mathf.Clamp((_waveElapsed - warnStart) / WarningLeadTime, 0f, 1f);
        float alpha = 0.08f + (0.35f * ratio);

        for (int i = 0; i < state.WarningRects.Length; i++)
            state.WarningRects[i].Color = new Color(1f, 0.15f, 0.15f, alpha);

        // 矢印を点滅
        if (state.WarningArrow != null)
        {
            float blink = (Mathf.Sin(_gameTime * 8f) + 1f) * 0.5f;
            state.WarningArrow.Modulate = new Color(1f, 1f, 1f, 0.5f + blink * 0.5f);
        }
    }

    // ──────────── 障害物スポーン ────────────
    private void SpawnObstacle(ref ObstacleState state)
    {
        state.IsSpawned = true;
        state.IsActive = true;
        state.ElapsedSinceSpawn = 0f;
        state.CurrentCellIndex = -1;
        state.HitApplied = new bool[3];

        RemoveWarningVisual(ref state);

        TextureRect obstacleVisual = new();
        obstacleVisual.Texture = ResolveObstacleTexture(state.Entry.Direction);
        obstacleVisual.Size = new Vector2(110f, 110f); // 少し大きく
        ConfigureTextureRectFill(obstacleVisual);

        if (state.Entry.Direction == ObstacleDirection.Left)
            obstacleVisual.FlipH = true;

        Vector2 start = GetOutsidePoint(state.Entry.Direction, state.Entry.Lane, entering: true);
        obstacleVisual.Position = start - new Vector2(55f, 55f);

        _obstaclesRoot.AddChild(obstacleVisual);
        state.Visual = obstacleVisual;
    }

    // ──────────── 障害物の動き（反射バグ修正） ────────────
    private void UpdateObstacleMotion(ref ObstacleState state, float dt)
    {
        state.ElapsedSinceSpawn += dt;

        float t = state.ElapsedSinceSpawn / Mathf.Max(0.0001f, state.Entry.SpeedPerCell);
        int rawIndex = Mathf.FloorToInt(t);

        int maxCellToResolve = Mathf.Min(rawIndex, 2);
        if (maxCellToResolve > state.CurrentCellIndex)
        {
            for (int cellIndex = state.CurrentCellIndex + 1; cellIndex <= maxCellToResolve; cellIndex++)
                ResolveCellPass(ref state, cellIndex);
            state.CurrentCellIndex = maxCellToResolve;
        }

        if (state.Visual != null)
        {
            Vector2 position = ComputeObstaclePosition(state.Entry, t);
            state.Visual.Position = position - new Vector2(55f, 55f);
        }

        // 3セル通過+退出 → t>=4 で消滅
        if (t >= 4f)
            FinishObstacle(ref state);
    }

    private void ResolveCellPass(ref ObstacleState state, int cellIndex)
    {
        if (cellIndex < 0 || cellIndex >= 3) return;
        if (state.HitApplied[cellIndex]) return;

        Vector2I cell = GetPathCell(state.Entry.Direction, state.Entry.Lane, cellIndex);
        int col = cell.X;
        int row = cell.Y;

        bool playerInCell = _playerCol == col && _playerRow == row;

        if (playerInCell)
        {
            if (_zoneActive)
            {
                state.HitApplied[cellIndex] = true;
                PlayZoneBreakEffect(state.Visual, _cellCenters[col, row]);
                AddSafeScore(col, row, isJust: false, lucky: false, comboGain: 1, baseScore: 100);
                return;
            }
            state.HitApplied[cellIndex] = true;
            ApplyHit();
            return;
        }

        bool justAvoid = IsJustAvoid(col, row, out bool lucky);
        state.HitApplied[cellIndex] = true;

        if (justAvoid)
        {
            AddSafeScore(col, row, isJust: true, lucky: lucky, comboGain: 2, baseScore: 500);
            return;
        }

        AddSafeScore(col, row, isJust: false, lucky: false, comboGain: 1, baseScore: 100);
    }

    private bool IsJustAvoid(int col, int row, out bool lucky)
    {
        lucky = false;
        bool sameCell = _lastLeftCol == col && _lastLeftRow == row;
        if (!sameCell) return false;

        bool movedRecently = (_gameTime - _lastMoveTime) <= 0.5f;
        bool stayedEnough = _stayTimeAtLastMove >= 0.25f;

        if (!movedRecently || !stayedEnough) return false;

        lucky = !_lastMoveHadThreatAtDestination;
        return true;
    }

    private void AddSafeScore(int col, int row, bool isJust, bool lucky, int comboGain, int baseScore)
    {
        ApplyComboGain(comboGain, col, row, isJust);

        int comboMultiplier = Mathf.Max(1, _combo);
        int zoneMultiplier = _zoneActive ? 5 : 1;
        int earned = baseScore * comboMultiplier * zoneMultiplier;
        _totalScore += earned;

        Vector2 popupPos = _cellCenters[col, row] + new Vector2(0f, -64f);
        ShowScorePopup(earned, popupPos, isJust, lucky);

        if (isJust)
        {
            PlayJustAvoidEffects(col, row, lucky);
            Speak(lucky ? "ラッキー！" : PickRandom(new[] { "ナイス！", "かっこいい！", "見事！" }), 1.2f);
        }
        else
        {
            PlayNormalAvoidEffects(col, row);
        }

        UpdateHud();
    }

    private void ApplyComboGain(int gain, int col, int row, bool isJust)
    {
        if (gain <= 0) return;
        _combo += gain;
        PlayComboTierEffects(col, row);
        if (_combo >= ZoneComboThreshold && !_zoneActive)
            ActivateZone();
    }

    private void ApplyHit()
    {
        float damage = _isGuarding ? GuardDamage : HitDamage;
        _life = Mathf.Max(0f, _life - damage);

        _combo = 0;
        if (_zoneActive) DeactivateZone();
        SetAuraAlpha(0f);

        PlayHitEffects();
        _player.Play("hurt");

        Speak(PickRandom(new[] { "痛い！", "気をつけて！", "まだいける！" }), 1.2f);

        if (_life <= 0f) Die();
        UpdateHud();
    }

    private void PlayComboTierEffects(int col, int row)
    {
        Vector2 center = _cellCenters[col, row];

        if (_combo <= 3)
        {
            FlashCell(center, new Color(1f, 1f, 1f, 0.2f), 0.3f, 0f);
            ShowFloatingText("Nice!", center + new Vector2(0f, -88f), Colors.White, 38, 0.9f);
            return;
        }

        if (_combo <= 5)
        {
            FlashCell(center, new Color(0.7f, 0.9f, 1f, 0.25f), 0.35f, 14f);
            ShowFloatingText("Great!", center + new Vector2(0f, -92f), new Color(0.3f, 0.8f, 1f, 1f), 46, 0.9f);
            SetAuraAlpha(0.15f);
            return;
        }

        if (_combo <= 7)
        {
            FlashCell(center, new Color(1f, 0.95f, 0.4f, 0.3f), 0.4f, 18f);
            ShowFloatingText("Amazing!!", center + new Vector2(0f, -96f), new Color(1f, 0.9f, 0.2f, 1f), 52, 1f);
            SetAuraAlpha(0.25f);
            ShakeScreen(3f, 4, 0.06f);

            if (_combo == 6) Speak("すごい！この調子！", 1.2f);
            else if (_combo == 7) Speak("あと1回...！", 1.2f);
        }
    }

    // ──────────── ゾーン ────────────
    private void ActivateZone()
    {
        _zoneActive = true;
        _zoneRemaining = ZoneDuration;

        Speak("ゾーン突入！無敵だ！", 1.8f);
        TriggerTimeStop(0.3f, 0.2f);
        PlayWhiteFlash(0.6f, 0.2f);
        ShowZoneBanner();

        _player.Modulate = new Color(1f, 0.85f, 0.2f, 1f);
        SetCellBorderZoneColor(enabled: true);
        ShowZoneEffect();
        SetAuraAlpha(0.25f);
        UpdateHud();
    }

    private void DeactivateZone()
    {
        _zoneActive = false;
        _zoneRemaining = 0f;
        _combo = 0;
        _player.Modulate = Colors.White;
        SetCellBorderZoneColor(enabled: false);
        HideZoneEffect();
        SetAuraAlpha(0f);
        UpdateHud();
    }

    private void TickZone(float dt)
    {
        if (!_zoneActive) return;
        _zoneRemaining -= dt;
        if (_zoneRemaining <= 0f) DeactivateZone();
    }

    // ──────────── Wave遷移（イベント中も操作可能に修正） ────────────
    private void TryResolveWaveEnd()
    {
        if (_state != GameState.Playing || _waveResolved) return;
        if (_obstacleStates.Count == 0) return;

        for (int i = 0; i < _obstacleStates.Count; i++)
        {
            if (!_obstacleStates[i].IsFinished) return;
        }

        _waveResolved = true;

        if (_currentWaveIndex >= _waves.Length - 1)
        {
            SetClearedState();
            return;
        }

        ShowInterWaveEvent(_currentWaveIndex + 1);
    }

    private void ShowInterWaveEvent(int nextWaveIndex)
    {
        // WaitingChoice でも入力は引き続き受け付ける
        _state = GameState.WaitingChoice;
        _pendingWaveIndex = nextWaveIndex;
        _eventLocked = false;

        _activeEvent = _events[_rng.RandiRange(0, _events.Length - 1)];
        _eventText.Text = _activeEvent.Text;
        _choiceA.Text = _activeEvent.OptionA;
        _choiceB.Text = _activeEvent.OptionB;
        _choiceA.Disabled = false;
        _choiceB.Disabled = false;

        _eventPanel.Visible = true;
        _eventPanel.Modulate = new Color(1f, 1f, 1f, 0f);
        _eventPanel.MouseFilter = Control.MouseFilterEnum.Stop; // パネル自体は入力を受ける

        Tween fadeIn = CreateTween();
        fadeIn.TweenProperty(_eventPanel, "modulate:a", 1f, 0.3f);

        _stateLabel.Text = "イベントを選択して次へ";
    }

    private void OnEventOptionSelected(bool chooseA)
    {
        if (_state != GameState.WaitingChoice || _eventLocked || _activeEvent == null)
            return;

        _eventLocked = true;
        _choiceA.Disabled = true;
        _choiceB.Disabled = true;

        if (chooseA)
            ApplyEventOutcome(_activeEvent.LifeGainA, _activeEvent.ComboGainA, _activeEvent.ResponseA);
        else
            ApplyEventOutcome(_activeEvent.LifeGainB, _activeEvent.ComboGainB, _activeEvent.ResponseB);

        SceneTreeTimer timer = GetTree().CreateTimer(1.2f);
        timer.Timeout += () =>
        {
            _eventPanel.Visible = false;
            _eventPanel.Modulate = Colors.White;
            _eventLocked = false;
            _activeEvent = null;

            if (_state == GameState.Dead) { _pendingWaveIndex = -1; return; }

            int nextWaveIndex = _pendingWaveIndex;
            _pendingWaveIndex = -1;
            BeginWave(nextWaveIndex);
        };
    }

    private void ApplyEventOutcome(int lifeGain, int comboGain, string response)
    {
        _life = Mathf.Clamp(_life + lifeGain, 0f, BaseLife);
        if (comboGain > 0)
            AddSafeScore(_playerCol, _playerRow, isJust: false, lucky: false, comboGain: comboGain, baseScore: 100);
        Speak(response, 2f);
        UpdateHud();
    }

    private void HideEventPanel()
    {
        if (!_eventPanel.Visible) return;
        Tween fadeOut = CreateTween();
        fadeOut.TweenProperty(_eventPanel, "modulate:a", 0f, 0.2f);
        fadeOut.TweenCallback(Callable.From(() =>
        {
            _eventPanel.Visible = false;
            _eventPanel.Modulate = Colors.White;
            _activeEvent = null;
        }));
    }

    // ──────────── クリア / 死亡 ────────────
    private void SetClearedState()
    {
        _state = GameState.Cleared;
        _allWavesCleared = true;
        _player.Play("idle");
        _player.Modulate = Colors.White;
        HideEventPanel();
        HideGuardEffect();
        if (_zoneActive) DeactivateZone();
        _stateLabel.Text = "ALL WAVES CLEAR!";
        _stateLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f, 1f));
        Speak("やった！全Wave突破！", 3f);
    }

    private void Die()
    {
        _state = GameState.Dead;
        HideEventPanel();
        HideGuardEffect();
        if (_zoneActive) DeactivateZone();
        _player.Play("death");
        _stateLabel.Text = "DEAD - Tap to restart";
        _stateLabel.AddThemeColorOverride("font_color", Colors.White);
        Tween deathTween = CreateTween();
        deathTween.TweenProperty(_deathOverlay, "color:a", 0.6f, 2f);
        Speak("お疲れ様...また次の人生で。", 3f);
    }

    // ──────────── アニメーション完了ハンドラ ────────────
    private void OnPlayerAnimationFinished()
    {
        if (_state == GameState.Dead && _player.Animation == "death")
        {
            int frameCount = _player.SpriteFrames.GetFrameCount("death");
            if (frameCount > 0) _player.Frame = frameCount - 1;
            _player.Stop();
            return;
        }

        // ガード解除アニメーション完了
        if (_player.Animation == "guard_release")
        {
            _guardReleasing = false;
            if (_state == GameState.Playing || _state == GameState.WaitingChoice)
                _player.Play("run");
            return;
        }

        // moveアニメーション完了 → runに復帰
        if (_player.Animation == "move")
        {
            if (_state == GameState.Playing || _state == GameState.WaitingChoice)
                _player.Play(_isGuarding ? "guard" : "run");
            return;
        }

        // hurtアニメーション完了 → runに復帰
        if (_player.Animation == "hurt")
        {
            if (_state == GameState.Playing || _state == GameState.WaitingChoice)
                _player.Play(_isGuarding ? "guard" : "run");
        }
    }

    // ──────────── エフェクト系 ────────────
    private void PlayNormalAvoidEffects(int col, int row)
    {
        Vector2 center = _cellCenters[col, row];
        SpawnTransientTexture(_texFxCombo, center + new Vector2(0f, -60f), new Vector2(84f, 84f), 0.35f, 0.7f);
    }

    private void PlayJustAvoidEffects(int col, int row, bool lucky)
    {
        Vector2 center = _cellCenters[col, row];
        TriggerTimeStop(0.2f, 0.05f);
        FlashCell(center, new Color(0.5f, 0.7f, 1f, 0.4f), 0.5f, 0f);
        SpawnTransientTexture(_texFxJustAvoid, center, new Vector2(120f, 120f), 0.4f, 1f);
        _ = lucky;
    }

    private void PlayZoneBreakEffect(TextureRect? obstacleVisual, Vector2 center)
    {
        if (obstacleVisual != null)
            obstacleVisual.Modulate = new Color(1f, 1f, 1f, 0.3f);

        SpawnTransientTexture(_texFxHit, center, new Vector2(100f, 100f), 0.3f, 0.9f);

        for (int i = 0; i < 8; i++)
        {
            ColorRect shard = new();
            shard.Size = new Vector2(8f, 8f);
            shard.Color = new Color(1f, 1f, 1f, 0.95f);
            shard.Position = center - new Vector2(4f, 4f);
            _effectsRoot.AddChild(shard);

            Vector2 dir = new Vector2(_rng.RandfRange(-1f, 1f), _rng.RandfRange(-1f, 1f)).Normalized();
            Vector2 target = shard.Position + (dir * _rng.RandfRange(50f, 100f));

            Tween tween = CreateTween();
            tween.TweenProperty(shard, "position", target, 0.3f);
            tween.Parallel().TweenProperty(shard, "modulate:a", 0f, 0.3f);
            tween.TweenCallback(Callable.From(shard.QueueFree));
        }
    }

    private void PlayHitEffects()
    {
        _hitFlashOverlay.Color = new Color(1f, 0f, 0f, 0f);
        Tween flash = CreateTween();
        flash.TweenProperty(_hitFlashOverlay, "color:a", 0.3f, 0.03f);
        flash.TweenProperty(_hitFlashOverlay, "color:a", 0f, 0.2f);

        ShakeScreen(5f, 3, 0.05f);
        SpawnTransientTexture(_texFxHit, _player.Position, new Vector2(100f, 100f), 0.35f, 1f);

        _lifeFillStyle.BgColor = new Color(1f, 0.2f, 0.2f, 1f);
        _lifeBar.AddThemeStyleboxOverride("fill", _lifeFillStyle);
        SceneTreeTimer timer = GetTree().CreateTimer(0.35f);
        timer.Timeout += UpdateLifeFillColor;
    }

    private void ShowScorePopup(int score, Vector2 position, bool isJust, bool lucky)
    {
        Color color = Colors.White;
        int size = 34;
        string text = $"+{score}";

        if (isJust) { color = new Color(1f, 0.85f, 0.2f, 1f); size = 50; }
        if (lucky) { color = new Color(0.8f, 0.4f, 1f, 1f); size = 50; text = $"LUCKY! +{score}"; }

        ShowFloatingText(text, position, color, size, 1f);
    }

    private void ShowFloatingText(string text, Vector2 position, Color color, int fontSize, float duration)
    {
        Label label = new();
        label.Text = text;
        label.Position = position;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.7f));
        label.AddThemeConstantOverride("outline_size", 4);

        _ui.AddChild(label);

        Tween tween = CreateTween();
        tween.TweenProperty(label, "position", position + new Vector2(0f, -70f), duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.Parallel().TweenProperty(label, "modulate:a", 0f, duration);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    private void FlashCell(Vector2 center, Color color, float duration, float expand)
    {
        ColorRect flash = new();
        flash.Color = color;
        float half = (CellSize * 0.5f) + expand;
        flash.OffsetLeft = center.X - half;
        flash.OffsetTop = center.Y - half;
        flash.OffsetRight = center.X + half;
        flash.OffsetBottom = center.Y + half;
        _effectsRoot.AddChild(flash);

        Tween tween = CreateTween();
        tween.TweenProperty(flash, "modulate:a", 0f, duration);
        tween.TweenCallback(Callable.From(flash.QueueFree));
    }

    private void ShowGuardEffect()
    {
        if (_guardFx != null) return;
        TextureRect fx = new();
        fx.Texture = _texFxGuard;
        fx.Size = new Vector2(150f, 150f);
        fx.Modulate = new Color(1f, 1f, 1f, 0.65f);
        ConfigureTextureRectFill(fx);
        _effectsRoot.AddChild(fx);
        _guardFx = fx;
    }

    private void HideGuardEffect()
    {
        if (_guardFx == null) return;
        _guardFx.QueueFree();
        _guardFx = null;
    }

    private void ShowZoneEffect()
    {
        if (_zoneFx != null) return;
        TextureRect fx = new();
        fx.Texture = _texFxZone;
        fx.Size = new Vector2(180f, 180f);
        ConfigureTextureRectFill(fx);
        _effectsRoot.AddChild(fx);
        _zoneFx = fx;

        _zonePulseTween?.Kill();
        _zonePulseTween = CreateTween();
        _zonePulseTween.SetLoops();
        _zonePulseTween.TweenProperty(fx, "scale", new Vector2(1.1f, 1.1f), 0.5f)
            .From(new Vector2(0.9f, 0.9f));
        _zonePulseTween.TweenProperty(fx, "scale", new Vector2(0.9f, 0.9f), 0.5f);
    }

    private void HideZoneEffect()
    {
        _zonePulseTween?.Kill();
        _zonePulseTween = null;
        if (_zoneFx == null) return;
        _zoneFx.QueueFree();
        _zoneFx = null;
    }

    private void TriggerTimeStop(float timeScale, float duration)
    {
        Engine.TimeScale = timeScale;
        SceneTreeTimer timer = GetTree().CreateTimer(duration, processAlways: true, ignoreTimeScale: true);
        timer.Timeout += () => Engine.TimeScale = 1f;
    }

    private void PlayWhiteFlash(float maxAlpha, float duration)
    {
        _flashOverlay.Color = new Color(1f, 1f, 1f, 0f);
        Tween tween = CreateTween();
        tween.TweenProperty(_flashOverlay, "color:a", maxAlpha, duration * 0.5f);
        tween.TweenProperty(_flashOverlay, "color:a", 0f, duration * 0.5f);
    }

    private void ShowZoneBanner()
    {
        Label banner = new();
        banner.Text = "ZONE!!!";
        banner.Position = new Vector2(340f, 860f);
        banner.Scale = Vector2.Zero;
        banner.AddThemeFontSizeOverride("font_size", 80);
        banner.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f, 1f));
        banner.AddThemeColorOverride("font_outline_color", new Color(0.6f, 0.3f, 0f, 1f));
        banner.AddThemeConstantOverride("outline_size", 6);

        _ui.AddChild(banner);

        Tween tween = CreateTween();
        tween.TweenProperty(banner, "scale", new Vector2(1.2f, 1.2f), 0.25f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(banner, "scale", Vector2.One, 0.2f)
            .SetEase(Tween.EaseType.Out);
        tween.TweenInterval(1.2f);
        tween.TweenProperty(banner, "modulate:a", 0f, 0.5f);
        tween.TweenCallback(Callable.From(banner.QueueFree));
    }

    private void ShakeScreen(float strength, int swings, float segment)
    {
        _shakeTween?.Kill();
        Position = Vector2.Zero;
        _shakeTween = CreateTween();
        for (int i = 0; i < swings; i++)
        {
            Vector2 offset = new(_rng.RandfRange(-strength, strength), _rng.RandfRange(-strength, strength));
            _shakeTween.TweenProperty(this, "position", offset, segment);
        }
        _shakeTween.TweenProperty(this, "position", Vector2.Zero, segment);
    }

    private void SpawnTransientTexture(Texture2D texture, Vector2 center, Vector2 size, float fadeDuration, float alpha)
    {
        TextureRect fx = new();
        fx.Texture = texture;
        fx.Size = size;
        fx.Position = center - (size * 0.5f);
        fx.Modulate = new Color(1f, 1f, 1f, alpha);
        ConfigureTextureRectFill(fx);
        _effectsRoot.AddChild(fx);

        Tween tween = CreateTween();
        tween.TweenProperty(fx, "modulate:a", 0f, fadeDuration);
        tween.TweenCallback(Callable.From(fx.QueueFree));
    }

    // ──────────── 画面装飾 ────────────
    private void CreateScreenAuras()
    {
        _leftAura = new ColorRect();
        _leftAura.Color = new Color(0.2f, 0.4f, 1f, 0f);
        _leftAura.OffsetLeft = 0f; _leftAura.OffsetTop = 0f;
        _leftAura.OffsetRight = 40f; _leftAura.OffsetBottom = 1920f;

        _rightAura = new ColorRect();
        _rightAura.Color = new Color(0.2f, 0.4f, 1f, 0f);
        _rightAura.OffsetLeft = 1040f; _rightAura.OffsetTop = 0f;
        _rightAura.OffsetRight = 1080f; _rightAura.OffsetBottom = 1920f;

        _ui.AddChild(_leftAura);
        _ui.AddChild(_rightAura);
        _leftAura.MoveToFront();
        _rightAura.MoveToFront();
    }

    private void SetAuraAlpha(float alpha)
    {
        _leftAura.Color = new Color(0.2f, 0.4f, 1f, alpha);
        _rightAura.Color = new Color(0.2f, 0.4f, 1f, alpha);
    }

    private void SetCellBorderZoneColor(bool enabled)
    {
        Color borderColor = enabled ? CellBorderZoneColor : CellBorderDefaultColor;
        for (int row = 0; row < Rows; row++)
            for (int col = 0; col < Columns; col++)
                _cellBorders[col, row].Color = borderColor;
    }

    // ──────────── スワイプヒント ────────────
    private void CreateSwipeHint()
    {
        if (_hintShown || _swipeHint != null) return;

        Node2D root = new();
        root.Name = "SwipeHint";
        root.Position = new Vector2(540f, 1600f);
        root.Scale = new Vector2(1f, 1f);

        root.AddChild(CreateHintArrow(new[] { new Vector2(-24f, 18f), new Vector2(24f, 18f), new Vector2(0f, -18f) }, new Vector2(0f, -70f)));
        root.AddChild(CreateHintArrow(new[] { new Vector2(-24f, -18f), new Vector2(24f, -18f), new Vector2(0f, 18f) }, new Vector2(0f, 70f)));
        root.AddChild(CreateHintArrow(new[] { new Vector2(18f, -24f), new Vector2(18f, 24f), new Vector2(-18f, 0f) }, new Vector2(-70f, 0f)));
        root.AddChild(CreateHintArrow(new[] { new Vector2(-18f, -24f), new Vector2(-18f, 24f), new Vector2(18f, 0f) }, new Vector2(70f, 0f)));

        _ui.AddChild(root);
        _swipeHint = root;

        _swipeHintPulseTween?.Kill();
        _swipeHintPulseTween = CreateTween();
        _swipeHintPulseTween.SetLoops();
        _swipeHintPulseTween.TweenProperty(root, "scale", new Vector2(1.15f, 1.15f), 0.7f).From(new Vector2(0.9f, 0.9f));
        _swipeHintPulseTween.TweenProperty(root, "scale", new Vector2(0.9f, 0.9f), 0.7f);
    }

    private void DismissSwipeHint(bool immediate)
    {
        _hintShown = true;
        _swipeHintPulseTween?.Kill();
        if (_swipeHint == null) return;
        if (immediate) { _swipeHint.QueueFree(); _swipeHint = null; return; }

        Tween fade = CreateTween();
        fade.TweenProperty(_swipeHint, "modulate:a", 0f, 0.5f);
        fade.TweenCallback(Callable.From(() => { if (_swipeHint != null) { _swipeHint.QueueFree(); _swipeHint = null; } }));
    }

    private static Polygon2D CreateHintArrow(Vector2[] points, Vector2 position)
    {
        Polygon2D arrow = new();
        arrow.Polygon = points;
        arrow.Position = position;
        arrow.Color = new Color(1f, 1f, 1f, 0.3f);
        return arrow;
    }

    // ──────────── エフェクト位置更新 ────────────
    private void UpdateAttachedEffectsPosition()
    {
        if (_guardFx != null)
            _guardFx.Position = _player.Position + new Vector2(-75f, -75f);
        if (_zoneFx != null)
            _zoneFx.Position = _player.Position + new Vector2(-90f, -90f);
    }

    private void UpdateCellColors()
    {
        for (int row = 0; row < Rows; row++)
            for (int col = 0; col < Columns; col++)
                _cellInners[col, row].Color = CellDefaultColor;
        _cellInners[_playerCol, _playerRow].Color = CellPlayerColor;
    }

    // ──────────── HUD更新 ────────────
    private void UpdateHud()
    {
        _phaseLabel.Text = "青年期";
        _ageLabel.Text = "20歳";
        _waveLabel.Text = $"Wave {Mathf.Clamp(_currentWaveIndex + 1, 1, _waves.Length)}/{_waves.Length}";
        _comboLabel.Text = _zoneActive ? $"ZONE! Combo: {_combo}" : $"Combo: {_combo}";
        _scoreLabel.Text = $"Score: {_totalScore}";

        _lifeBar.MaxValue = BaseLife;
        _lifeBar.Value = _life;
        UpdateLifeFillColor();

        if (_state == GameState.Dead)
        {
            _stateLabel.Text = "DEAD - Tap to restart";
            return;
        }
        if (_state == GameState.Cleared)
        {
            _stateLabel.Text = "ALL WAVES CLEAR!";
            _stateLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f, 1f));
            return;
        }

        _stateLabel.AddThemeColorOverride("font_color", Colors.White);

        if (_state == GameState.Idle)
        {
            _stateLabel.Text = _allWavesCleared ? "ALL WAVES CLEAR!" : "Tap to start";
            return;
        }
        if (_state == GameState.WaitingChoice)
        {
            _stateLabel.Text = "イベントを選択して次へ";
            return;
        }

        _stateLabel.Text = _isGuarding ? "Guarding" : "";
    }

    private void UpdateLifeFillColor()
    {
        Color color;
        if (_life <= 25f) color = new Color(0.9f, 0.2f, 0.2f, 1f);
        else if (_life <= 50f) color = new Color(0.9f, 0.8f, 0.2f, 1f);
        else color = new Color(0.2f, 0.8f, 0.3f, 1f);

        _lifeFillStyle.BgColor = color;
        _lifeBar.AddThemeStyleboxOverride("fill", _lifeFillStyle);
    }

    // ──────────── ナビゲーター ────────────
    private void Speak(string text, float duration = 3f)
    {
        _navigatorLabel.Text = text;
        _speechTween?.Kill();
        _naviBubble.Visible = true;
        _navigatorLabel.Visible = true;
        _speechTween = CreateTween();
        _speechTween.TweenProperty(_naviBubble, "modulate:a", 1f, 0.25f);
        _speechTween.Parallel().TweenProperty(_navigatorLabel, "modulate:a", 1f, 0.25f);
        _speechTween.TweenInterval(duration);
        _speechTween.TweenProperty(_naviBubble, "modulate:a", 0f, 0.4f);
        _speechTween.Parallel().TweenProperty(_navigatorLabel, "modulate:a", 0f, 0.4f);
    }

    private string GetDirectionCallout(ObstacleDirection direction)
    {
        return direction switch
        {
            ObstacleDirection.Right => "右から来るぞ！",
            ObstacleDirection.Left => "左から来るぞ！",
            ObstacleDirection.Top => "上から来るぞ！",
            ObstacleDirection.Down => "下から来るぞ！",
            _ => "来るよ！",
        };
    }

    // ──────────── ジオメトリ（反射バグ修正） ────────────
    private static float GetWarningArrowRotation(ObstacleDirection direction)
    {
        return direction switch
        {
            ObstacleDirection.Right => 180f,
            ObstacleDirection.Left => 0f,
            ObstacleDirection.Top => 90f,
            ObstacleDirection.Down => -90f,
            _ => 0f,
        };
    }

    private static Texture2D LoadRequiredTexture(string path)
    {
        Texture2D texture = GD.Load<Texture2D>(path);
        if (texture == null) throw new InvalidOperationException($"Texture not found: {path}");
        return texture;
    }

    private static void ConfigureTextureRectFill(TextureRect textureRect)
    {
        textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
    }

    private Texture2D ResolveObstacleTexture(ObstacleDirection direction)
    {
        return direction switch
        {
            ObstacleDirection.Right => _texObstacleRight,
            ObstacleDirection.Left => _texObstacleRight,
            ObstacleDirection.Top => _texObstacleTop,
            ObstacleDirection.Down => _texObstacleBottom,
            _ => _texObstacleRight,
        };
    }

    private static Vector2I GetPathCell(ObstacleDirection direction, int lane, int cellIndex)
    {
        return direction switch
        {
            ObstacleDirection.Right => new Vector2I(2 - cellIndex, lane),
            ObstacleDirection.Left => new Vector2I(cellIndex, lane),
            ObstacleDirection.Top => new Vector2I(lane, cellIndex),
            ObstacleDirection.Down => new Vector2I(lane, 2 - cellIndex),
            _ => new Vector2I(1, 1),
        };
    }

    /// <summary>
    /// 障害物の位置を計算（反射バグ修正版）。
    /// t=0..1: 画面外→セル0、t=1..2: セル0→セル1、t=2..3: セル1→セル2、t=3..4: セル2→画面外
    /// t>=4 で退出完了。
    /// </summary>
    private Vector2 ComputeObstaclePosition(ObstacleEntry entry, float t)
    {
        Vector2 outsideStart = GetOutsidePoint(entry.Direction, entry.Lane, entering: true);
        Vector2 outsideEnd = GetOutsidePoint(entry.Direction, entry.Lane, entering: false);

        Vector2I c0i = GetPathCell(entry.Direction, entry.Lane, 0);
        Vector2I c1i = GetPathCell(entry.Direction, entry.Lane, 1);
        Vector2I c2i = GetPathCell(entry.Direction, entry.Lane, 2);
        Vector2 c0 = _cellCenters[c0i.X, c0i.Y];
        Vector2 c1 = _cellCenters[c1i.X, c1i.Y];
        Vector2 c2 = _cellCenters[c2i.X, c2i.Y];

        if (t <= 0f) return outsideStart;
        if (t < 1f) return outsideStart.Lerp(c0, t);
        if (t < 2f) return c0.Lerp(c1, t - 1f);
        if (t < 3f) return c1.Lerp(c2, t - 2f);
        if (t < 4f) return c2.Lerp(outsideEnd, t - 3f);
        return outsideEnd;
    }

    /// <summary>
    /// 画面外の座標（反射バグ修正版）。
    /// entering=true: 侵入側の画面外。entering=false: 退出側の画面外。
    /// 退出側は侵入側の反対方向に飛んでいく。
    /// </summary>
    private Vector2 GetOutsidePoint(ObstacleDirection direction, int lane, bool entering)
    {
        if (entering)
        {
            // 侵入: 最初のセル(cellIndex=0)から進行方向の反対側にオフセット
            Vector2I edgeCell = GetPathCell(direction, lane, 0);
            Vector2 center = _cellCenters[edgeCell.X, edgeCell.Y];
            return direction switch
            {
                ObstacleDirection.Right => center + new Vector2(CellSize + 80f, 0f),   // 右の外から入る
                ObstacleDirection.Left => center + new Vector2(-(CellSize + 80f), 0f), // 左の外から入る
                ObstacleDirection.Top => center + new Vector2(0f, -(CellSize + 80f)),  // 上の外から入る
                ObstacleDirection.Down => center + new Vector2(0f, CellSize + 80f),    // 下の外から入る
                _ => center,
            };
        }
        else
        {
            // 退出: 最後のセル(cellIndex=2)から進行方向にオフセット
            Vector2I edgeCell = GetPathCell(direction, lane, 2);
            Vector2 center = _cellCenters[edgeCell.X, edgeCell.Y];
            return direction switch
            {
                ObstacleDirection.Right => center + new Vector2(-(CellSize + 80f), 0f),  // 右→左に抜ける
                ObstacleDirection.Left => center + new Vector2(CellSize + 80f, 0f),      // 左→右に抜ける
                ObstacleDirection.Top => center + new Vector2(0f, CellSize + 80f),       // 上→下に抜ける
                ObstacleDirection.Down => center + new Vector2(0f, -(CellSize + 80f)),   // 下→上に抜ける
                _ => center,
            };
        }
    }

    private bool HasIncomingObstacleForCell(int col, int row, float secondsAhead)
    {
        float windowEnd = _waveElapsed + Mathf.Max(0.1f, secondsAhead);

        for (int i = 0; i < _obstacleStates.Count; i++)
        {
            ObstacleState state = _obstacleStates[i];
            if (state.IsFinished) continue;

            float speed = Mathf.Max(0.0001f, state.Entry.SpeedPerCell);
            for (int cellIndex = 0; cellIndex < 3; cellIndex++)
            {
                Vector2I pathCell = GetPathCell(state.Entry.Direction, state.Entry.Lane, cellIndex);
                if (pathCell.X != col || pathCell.Y != row) continue;

                float reachTime = state.Entry.SpawnTime + (cellIndex * speed);
                if (reachTime >= _waveElapsed && reachTime <= windowEnd) return true;
            }
        }
        return false;
    }

    // ──────────── クリーンアップ ────────────
    private void RemoveWarningVisual(ref ObstacleState state)
    {
        if (state.WarningRects != null)
        {
            for (int i = 0; i < state.WarningRects.Length; i++)
                state.WarningRects[i].QueueFree();
            state.WarningRects = null;
        }
        state.WarningArrow?.QueueFree();
        state.WarningArrow = null;
        state.WarningOrder?.QueueFree();
        state.WarningOrder = null;
    }

    private void FinishObstacle(ref ObstacleState state)
    {
        state.IsActive = false;
        state.IsFinished = true;
        state.Visual?.QueueFree();
        state.Visual = null;
        RemoveWarningVisual(ref state);
    }

    private void ClearObstacleVisuals()
    {
        foreach (Node child in _obstaclesRoot.GetChildren()) child.QueueFree();
        _obstacleStates.Clear();
    }

    private void ClearWarningVisuals()
    {
        foreach (Node child in _warningRoot.GetChildren()) child.QueueFree();
    }

    private void ClearTransientEffects()
    {
        foreach (Node child in _effectsRoot.GetChildren()) child.QueueFree();
        _guardFx = null;
        _zoneFx = null;
        _zonePulseTween?.Kill();
        _zonePulseTween = null;
    }

    private string PickRandom(string[] options)
    {
        if (options.Length == 0) return string.Empty;
        return options[_rng.RandiRange(0, options.Length - 1)];
    }
}