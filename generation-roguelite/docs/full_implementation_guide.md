# Generation Roguelite ─ 全実装指示書
> 更新日: 2026-02-27
> Engine: Godot 4.6.1 .NET (C#) ─ 縦持ち 1080×1920

---

## 0. プロジェクト設定（実装済みなら確認のみ）

### 0-1. project.godot

```ini
[display]
window/size/viewport_width=1080
window/size/viewport_height=1920
window/stretch/mode="canvas_items"
window/stretch/aspect="expand"
window/handheld/orientation="portrait"

[rendering]
renderer/rendering_method="mobile"
0-2. フォルダ構成
project_root/
├── Scenes/
│   ├── Main.tscn
│   ├── Game/
│   │   ├── Character.tscn
│   │   ├── Obstacle.tscn
│   │   ├── Navigator.tscn
│   │   └── DropItem.tscn
│   └── UI/
│       ├── HUD.tscn
│       ├── EventPanel.tscn
│       ├── StatPanel.tscn
│       ├── InventoryPanel.tscn
│       ├── GenerationScreen.tscn
│       ├── GameOverScreen.tscn
│       ├── TestamentPanel.tscn
│       ├── FamilyTreeScreen.tscn
│       ├── SettingsScreen.tscn
│       ├── TitleScreen.tscn
│       └── DebugOverlay.tscn
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs
│   │   ├── TimeManager.cs
│   │   ├── ScoreManager.cs
│   │   ├── SaveManager.cs
│   │   ├── PhaseManager.cs
│   │   └── DebugOverlay.cs
│   ├── Character/
│   │   ├── PlayerCharacter.cs
│   │   ├── CharacterStats.cs
│   │   └── CharacterAction.cs
│   ├── Events/
│   │   ├── EventManager.cs
│   │   ├── EventData.cs
│   │   └── EventTemplates.cs
│   ├── Equipment/
│   │   ├── EquipmentManager.cs
│   │   ├── EquipmentData.cs
│   │   ├── DropSystem.cs
│   │   └── Inventory.cs
│   ├── Generation/
│   │   ├── GenerationManager.cs
│   │   ├── FamilyTree.cs
│   │   ├── InheritanceSystem.cs
│   │   ├── PartnerSystem.cs
│   │   └── AdoptionSystem.cs
│   ├── Navigator/
│   │   ├── NavigatorManager.cs
│   │   └── NavigatorDialogue.cs
│   ├── Obstacle/
│   │   ├── ObstacleSpawner.cs
│   │   ├── ObstacleController.cs
│   │   └── DifficultyManager.cs
│   ├── Meta/
│   │   ├── FamilyTeaching.cs
│   │   ├── EraManager.cs
│   │   ├── AchievementManager.cs
│   │   └── DeathCollection.cs
│   ├── UI/
│   │   ├── HUDController.cs
│   │   ├── EventPanelController.cs
│   │   ├── StatPanelController.cs
│   │   ├── InventoryPanelController.cs
│   │   ├── GenerationScreenController.cs
│   │   ├── GameOverScreenController.cs
│   │   ├── TestamentController.cs
│   │   ├── FamilyTreeController.cs
│   │   ├── SettingsScreenController.cs
│   │   ├── TitleScreenController.cs
│   │   └── FloatTextSpawner.cs
│   └── Data/
│       ├── GameData.cs
│       └── Constants.cs
├── Resources/
│   ├── Events/event_templates.json
│   └── Navigator/default_dialogues.json
├── exports/
├── tools/
│   ├── build_sign_deploy.bat
│   └── sign_and_install.bat
└── docs/
Step 1 ─ コアスケルトン
1-1. Constants.cs
public static class Constants
{
    // --- Display ---
    public const int VIEWPORT_WIDTH  = 1080;
    public const int VIEWPORT_HEIGHT = 1920;

    // --- Time ---
    public const float SECONDS_PER_YEAR = 1.0f;

    // --- Character position ---
    public const float CHAR_X = 324f;   // 30% of width
    public const float GROUND_Y = 1720f; // 200px from bottom

    // --- Phases ---
    public const int CHILDHOOD_END = 12;
    public const int YOUTH_END     = 30;
    public const int ADULT_END     = 55;
    // Elder: 56 ～ lifespan

    // --- Obstacle ---
    public const float OBS_BASE_SPEED    = 220f;
    public const float OBS_SPAWN_X       = 1200f;
    public const float OBS_DESPAWN_X     = -200f;
    public const float OBS_SPAWN_INTERVAL = 4.0f;

    // --- Events ---
    public const float EVENT_MIN_INTERVAL = 15f;
    public const float EVENT_MAX_INTERVAL = 30f;
    public const float EVENT_TIMEOUT_MIN  = 7f;
    public const float EVENT_TIMEOUT_MAX  = 12f;

    // --- Equipment ---
    public const int INVENTORY_MAX = 10;

    // --- Generation ---
    public const float INHERITANCE_STAT_RATIO  = 0.5f;
    public const int   INHERITANCE_RANDOM_RANGE = 5;
    public const float WEALTH_INHERITANCE_RATIO = 0.6f;
    public const int   MAX_ADOPTIONS_PER_LINEAGE = 1;

    // --- Era ---
    public const int GENERATIONS_PER_ERA = 3;

    // --- Speed ---
    public static readonly float[] SPEED_OPTIONS = { 1.0f, 1.5f, 2.0f };
}
1-2. GameManager.cs
シングルトン (AutoLoad)。ゲーム状態管理。
状態: Title, Playing, Paused, Event, GenerationTransition, GameOver
StartNewGame() → 初代キャラ生成、TimeManager 開始
OnCharacterDeath() → GenerationManager に委譲
OnGameOver() → GameOverScreen 表示
OnNextGeneration(CharacterStats child) → キャラ差替え、TimeManager リセット
バックグラウンド検知: NotificationWMGoBackRequest で TimeManager 一時停止
1-3. TimeManager.cs
float _yearAccumulator; int CurrentAge; int CurrentYear
_Process(delta): _yearAccumulator += delta * SpeedMultiplier。1.0 超過ごとに CurrentAge++、signal YearPassed(int age) 発火
float LifespanRemaining → HUD の寿命バー連動
Pause() / Resume() / SetSpeed(float multiplier)
寿命に達したら signal LifespanReached → GameManager へ
1-4. PhaseManager.cs
TimeManager.YearPassed を購読
年齢から Phase (Childhood/Youth/Adult/Elder) を判定
Phase 変更時に signal PhaseChanged(Phase newPhase) 発火
Phase 変更演出: 0.3 秒スローモーション (Engine.TimeScale = 0.3f → 戻す)
1-5. Main.tscn 構成
Main (Node2D)
├── ParallaxBackground
│   ├── ParallaxLayer0 (sky)    ── ColorRect 1080×1920
│   ├── ParallaxLayer1 (mid)    ── ColorRect 1080×960
│   └── ParallaxLayer2 (ground) ── ColorRect 1080×480
├── GroundLine (StaticBody2D + CollisionShape2D) Y=1720
├── Character (CharacterBody2D)
│   ├── ColorRect (placeholder)
│   └── CollisionShape2D
├── ObstacleSpawner (Node2D)
├── Navigator (Control)
│   ├── Icon (ColorRect 144×144, purple)
│   └── Bubble (Panel + Label)
├── CanvasLayer (UI)
│   ├── HUD
│   ├── EventPanel (hidden)
│   ├── StatPanel (hidden)
│   ├── InventoryPanel (hidden)
│   ├── GenerationScreen (hidden)
│   ├── GameOverScreen (hidden)
│   ├── TestamentPanel (hidden)
│   ├── FamilyTreeScreen (hidden)
│   ├── SettingsScreen (hidden)
│   ├── TitleScreen (visible at start)
│   ├── FloatTextSpawner
│   └── DebugOverlay
1-6. プレースホルダーの色定義
キャラクター（年代別）:

Phase	Size (W×H)	Color (hex)
Childhood	72×72	#87CEEB
Youth	90×126	#4CAF50
Adult	100×126	#FF9800
Elder	82×108	#9E9E9E
Death	any→赤	#F44336
背景（時代×レイヤー）:

Era	Layer0 (sky)	Layer1 (mid)	Layer2 (ground)
Primitive	#87CEEB	#8B4513	#654321
Ancient	#B0C4DE	#D2B48C	#8B7355
Medieval	#708090	#A0522D	#696969
EarlyModern	#87CEFA	#BC8F8F	#808080
Modern	#4682B4	#778899	#A9A9A9
Future	#1A1A2E	#16213E	#0F3460
障害物:

Type	Color
Enemy	#F44336
Trap	#FF9800
Falling	#FFEB3B
装備レアリティ:

Rarity	Color
Common	#9E9E9E
Uncommon	#4CAF50
Rare	#2196F3
Epic	#9C27B0
Legendary	#FF9800
Step 2 ─ HUD 構築
2-1. HUD レイアウト（1080×1920）
┌────────────────────────────────┐  Y=0
│  TOP BAR (96px)                │
│  [Name Age:XX] [Gen:X] [Score] [⚙]│
├────────────────────────────────┤  Y=96
│  LIFESPAN BAR (horizontal 24px)│
│  [████████████░░░░░░░░░░░░░░░░]│
├────────────────────────────────┤  Y=120
│                                │
│                                │
│      GAME ZONE                 │
│      (1080 × 1464)            │
│                                │
│  [Navi]          [FloatText]   │
│                                │
├────────────────────────────────┤  Y=1584
│  ABILITY ROW (108px)           │
│  [体][知][魅][運][財]          │
├────────────────────────────────┤  Y=1692
│  BOTTOM BAR (228px)            │
│  [EQUIP1][EQUIP2][EQUIP3]      │
│  [EVENT LOG btn] [MENU btn]    │
└────────────────────────────────┘  Y=1920
2-2. HUDController.cs
TopBar: HBoxContainer → Label (名前+年齢), Label (世代), Label (スコア), Button (設定)
LifespanBar: ProgressBar, 横幅 1080px, 高さ 24px
色変化: >80% 緑, 50-80% 黄, 20-50% 橙, <20% 赤点滅
AbilityRow: HBoxContainer に 5つの VBoxContainer (アイコン ColorRect 48×48 + Label 値)
タップで StatPanel 展開
BottomBar: 装備スロット3つ (144×144 ColorRect), メニューボタン, ログボタン
全ノードに Anchor 設定（リサイズ対応）
TimeManager.YearPassed → 年齢ラベル更新
CharacterStats.StatChanged → 能力値・スコア更新
TimeManager.LifespanTick → 寿命バー更新
2-3. FloatTextSpawner.cs
Spawn(string text, Color color, Vector2 position)
Label を生成、フォントサイズ 54px
Tween: 1.5秒で Y-120px 移動 + alpha 1→0
最大同時表示 3、超過分は即消去
呼出元: イベント結果、ステータス変動、ドロップ取得
2-4. DebugOverlay.cs
左上、フォントサイズ 30px、半透明黒背景
表示: FPS, Viewport size, 最終タッチ座標, GameState, CurrentAge, Phase, Era
Settings 画面から ON/OFF 切替（デフォルト ON）
Step 3 ─ キャラクター・入力・障害物
3-1. PlayerCharacter.cs
CharacterBody2D 配置: X=324, Y=GroundY-キャラ高さ
Phase 変更時に ColorRect のサイズ・色を切替
死亡演出: 色→赤、0.5秒 fade out
3-2. CharacterStats.cs
5能力値: Health, Intelligence, Charisma, Luck, Wealth (int, 初期値 10)
int Lifespan (基本寿命 60-80 ランダム、装備・イベントで変動)
signal StatChanged(string statName, int oldVal, int newVal)
ApplyChange(string stat, int delta) → clamp 0-999, signal 発火, FloatText 呼出
3-3. CharacterAction.cs
入力判定はゲームゾーン（Y: 120～1584）内のタッチのみ反応
タップ (duration < 0.3s, distance < 20px):
Childhood: 採集（前方にエリア判定、アイテム回収）
Youth: ジャンプ（velocity.Y = -600, 重力で戻る）
Adult: 防御（0.5秒間 被ダメ半減フラグ）
Elder: 杖バフ（3秒間 周囲の障害物速度 -30%）
スワイプ (distance ≥ 50px, horizontal):
Childhood: なし
Youth: 攻撃（前方 Area2D、障害物に damage）
Adult: 攻撃（より広い Area2D）
Elder: 回避（0.3秒で Y±100 移動）
クールダウン: タップ 0.5秒、スワイプ 1.0秒
3-4. ObstacleSpawner.cs
Timer ノードで OBS_SPAWN_INTERVAL 秒ごとに発火
障害物の種類をランダム選択 (Enemy/Trap/Falling)
生成位置: X=1200, Y=ランダム（地面付近 or 上空）
Falling タイプは上から落下
3-5. ObstacleController.cs
_Process: X -= speed * delta
X < -200 で自動削除 (QueueFree)
Area2D でキャラとの衝突検知
衝突時: CharacterStats.ApplyChange("Health", -damage)
Health ≤ 0 → GameManager.OnCharacterDeath()
3-6. DifficultyManager.cs
直近10回の障害物への対処結果を記録（回避/撃破=成功、被弾=失敗）
成功率 ≥ 85%: interval -= 0.3s (min 1.5s), speed += 15 (max 400)
成功率 ≤ 40%: interval += 0.3s (max 6.0s), speed -= 15 (min 120)
Phase 補正: Childhood ×0.6, Youth ×1.0, Adult ×1.2, Elder ×0.8
Step 4 ─ イベントシステム
4-1. EventData.cs
Copypublic class EventData
{
    public string Id;
    public string Category;     // encounter, discovery, social, disaster, fortune, training
    public string TitleTemplate; // "野生の{animal}に遭遇した"
    public string DescriptionTemplate;
    public List<EventChoice> Choices;
    public string[] RequiredPhases;  // null = any
    public string[] RequiredEras;    // null = any
}
public class EventChoice
{
    public string Label;           // "戦う", "逃げる"
    public Dictionary<string, int> StatChanges; // {"Health": -5, "Wealth": 10}
    public float SuccessRate;      // 0.0-1.0, modified by stats
    public string SuccessStatModifier; // "Luck" → success += Luck * 0.01
    public float DropChance;       // 成功時のドロップ確率
    public string FailDescription;
    public Dictionary<string, int> FailStatChanges;
}
4-2. event_templates.json
30 件 (6カテゴリ × 5)
カテゴリ: encounter（遭遇）, discovery（発見）, social（社交）, disaster（災害）, fortune（幸運）, training（修練）
各イベントに 2-3 選択肢
テンプレート変数: {animal}, {item}, {person}, {place} をランダム置換
4-3. EventManager.cs
TimeManager.YearPassed を購読、内部タイマーで 15-30秒後にイベント発火
Childhood 中はイベント発生しない
発火時: Phase・Era に合ったイベントをフィルタ、直前と同じイベント除外、ランダム選択
EventPanel を表示（ゲームは続行＝障害物は動く）
カウントダウン = テキスト文字数 × 0.1s + 5s (clamp 7-12s)
タイムアウト: 最もリスクの低い選択肢を自動実行
選択実行: 成功判定 → stat 変更 → FloatText → ドロップ判定 → パネル閉じ
4-4. EventPanelController.cs
Panel: 背景半透明黒、幅 960px、中央寄せ
上部: タイトル Label (54px)
中部: 説明 RichTextLabel (42px)
下部: 選択肢ボタン (VBoxContainer, 各ボタン高さ 144px, フォント 48px)
カウントダウン: 右上に残り秒数 Label (60px, 赤で点滅 <3s)
結果表示: 選択後 1.5秒間結果テキスト表示 → 自動閉じ
Step 5 ─ ナビゲーター
5-1. NavigatorManager.cs
ゲームゾーン左下にアイコン (144×144 紫丸)
タップでランダム台詞表示
自動発話トリガー:
Phase 変更時: お祝い/警告メッセージ
寿命 < 30%: 老年の助言
装備ドロップ時: 「何か落ちましたよ！」
長時間イベント未発生時: 雑談
初回プレイ時: チュートリアル的台詞
発話頻度: 最低 20秒間隔
5-2. NavigatorDialogue.cs
default_dialogues.json を読み込み
カテゴリ: greeting, phase_child, phase_youth, phase_adult, phase_elder, low_health, drop, idle, tutorial, gameover
各カテゴリ 5-10 台詞
5-3. 吹き出し表示
Panel: 最大幅 600px, 背景 黒α0.7, 角丸12px
テキスト: 白, 42px, パディング 18px縦 24px横
アニメ: fade-in 0.2s → 表示 3.0s → fade-out 0.3s
表示中に新規発話 → 即座に差替え
Step 6 ─ 世代交代システム
6-1. GenerationManager.cs
GameManager.OnCharacterDeath() から呼ばれる
フロー:
ゲーム一時停止
死亡演出 (キャラ赤→フェード 0.5s)
画面全体赤フラッシュ 0.2s
GenerationScreen 表示
プレイヤーが「次世代へ」タップ
子の有無判定 → 継承 or 養子 or ゲームオーバー
新キャラ生成、ゲーム再開
6-2. GenerationScreenController.cs
背景: 時代の色 + 墓石 ColorRect
表示情報:
故人の名前、享年、死因
ハイライト 3件（最大ステータス、最多イベントカテゴリ、装備レアリティ最高）
世代スコア = 享年 × (全stat合計/5) × (クリアイベント数 × 0.5) × レアリティボーナス
累計スコア
ボタン:
「遺言を残す」→ TestamentPanel
「次世代へ」→ 世代交代処理
（広告プレースホルダー: 2世代以上 & 10分以上でインタースティシャル枠）
6-3. InheritanceSystem.cs
子がいる場合:
各stat = 親stat × 0.5 + Random(-5, +5), clamp(1, 999)
寿命 = Random(60, 80) + 遺伝ボーナス (親が長寿なら +5)
財力 = 親の財力 × 0.6 + 家宝ボーナス
大量遺産 (財力 > 100): 幼少期加速（幼少期の年速 ×1.5）
養子の場合:
3候補を表示、各 stat Random(5, 15)、1つユニークスキル付き
選択後、その候補のステータスで開始
財力継承なし（養子は財力 10 固定スタート）
6-4. PartnerSystem.cs
Adult フェーズ開始時 (31歳) に自動発火
候補数 = 1 + floor(Charisma / 20), max 3
成功判定: Charisma × 2 + Luck > Random(0, 50)
成功 → パートナー獲得、Adult フェーズ中にランダムで子誕生
失敗 → 再試行 3回まで（各5年おき: 31, 36, 41歳）
全失敗 → 子なし確定
6-5. AdoptionSystem.cs
死亡時に子がいない場合に発動
家系全体で 1回のみ 使用可能
条件: Wealth ≥ 20 AND TotalScore ≥ 100
条件未達 → ゲームオーバー
条件達成 → 養子候補 3人を表示、選択
リワード広告枠: 養子条件未達でも「最後の養子」（広告視聴で1回救済）
6-6. ゲームオーバー条件まとめ
子なし + 養子条件未達 + 養子使用済み
30歳以下で死亡 + 子なし + 養子なし
養子画面でタイムアウト（30秒）+ 条件未達
Step 7 ─ 装備・ドロップ
7-1. EquipmentData.cs
Copypublic class EquipmentData
{
    public string Id;
    public string Name;
    public string Category;       // weapon, armor, accessory, ring, amulet, ...
    public Rarity Rarity;         // Common, Uncommon, Rare, Epic, Legendary
    public Dictionary<string, int> StatBonuses; // {"Health": 5, "Luck": 3}
    public float LifespanModifier; // 0.0 = no effect, 0.05 = +5% lifespan
    public bool IsHeirloom;       // Rare以上で家宝化可能
    public string EraRestriction; // null = any era
}
public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }
7-2. DropSystem.cs
イベント成功時 or 障害物撃破時にドロップ判定
基本ドロップ率: イベント成功 50%, 障害物撃破 20%
レアリティ抽選: Common 60%, Uncommon 25%, Rare 10%, Epic 4%, Legendary 1%
Luck 補正: 各レアリティに +Luck × 0.5% を上位シフト
ドロップ演出: ゲームゾーン中央に ColorRect 出現、1秒後に自動回収 + FloatText
7-3. Inventory.cs
最大 10 スロット
装備中: 3 スロット（HUD 下部に表示）
装備効果: 装備中のみ stat ボーナス適用
満杯時にドロップ → 入替画面 (InventoryPanel) を表示
世代交代時: 家宝 (IsHeirloom = true) のみ次世代に継承、他は消滅
家宝は家系で 1つまで、Rare 以上の装備を家宝指定可
7-4. InventoryPanelController.cs
全画面オーバーレイ
グリッド表示: 2列 × 5行 = 10 スロット
各スロット: 144×144 ColorRect（レアリティ色）+ 名前 Label + stat ボーナス表示
タップで詳細ポップアップ（名前、レアリティ、ボーナス、家宝指定ボタン）
「装備する」「外す」「家宝にする」「捨てる」ボタン
Step 8 ─ メタシステム
8-1. FamilyTeaching.cs（家訓）
世代交代画面の「遺言を残す」から設定
家系全体で蓄積、世代を超えて効果持続
初期家訓 10 種:
家訓名	効果	コスト (スコア)
質実剛健	体力成長 +20%	500
博学多才	知力成長 +20%	500
八方美人	魅力成長 +20%	500
天運招福	運成長 +20%	500
蓄財有道	財力成長 +20%	500
長寿の秘訣	基本寿命 +5	800
先祖の加護	全stat初期値 +3	1000
早熟の血	幼少期加速 ×1.5	600
商才	ドロップ率 +10%	700
武芸百般	攻撃ダメージ +30%	700
累積購入可（同じ家訓を複数回は不可）
GameData.FamilyTeachings リストに保存
8-2. TestamentController.cs（遺言画面）
GenerationScreen から遷移
現在のスコアを表示
家訓リスト: 未購入のみ表示、コスト表示
購入 → スコア消費 → GameData.FamilyTeachings に追加
「戻る」→ GenerationScreen
8-3. EraManager.cs
現在の世代数から時代を決定:
1-3世代: Primitive（原始）
4-6世代: Ancient（古代）
7-9世代: Medieval（中世）
10-12世代: EarlyModern（近世）
13-15世代: Modern（現代）
16世代以降: Future（未来）
時代変更時: 背景色テーブル切替、signal EraChanged(string era) 発火
将来的にはイベント内容・障害物種類・BGM も時代連動
8-4. DeathCollection.cs
死因カテゴリ: 寿命、戦闘、災害、病気、事故、不明
各世代の死因を記録
コレクション画面（FamilyTreeScreen 内のタブとして実装）
初回死因は「NEW」バッジ表示
8-5. AchievementManager.cs（実績 ─ 将来拡張用の骨格のみ）
Dictionary<string, bool> Achievements
判定は各システムから呼び出し
初期実績例: 「初代当主」「5世代継続」「Legendary 装備入手」「養子成功」
表示はメニュー画面の1タブとして将来追加
Step 9 ─ セーブ/ロード
9-1. GameData.cs
Copypublic class GameData
{
    // --- Current character ---
    public string CharacterName;
    public int Age;
    public int Lifespan;
    public Dictionary<string, int> Stats;
    public List<EquipmentData> Inventory;
    public List<EquipmentData> EquippedItems;
    public EquipmentData Heirloom;

    // --- Generation ---
    public int GenerationCount;
    public int TotalScore;
    public int CurrentGenerationScore;
    public int AdoptionsUsed; // 0 or 1
    public bool HasPartner;
    public bool HasChild;

    // --- Family ---
    public List<FamilyMember> FamilyTree;
    public List<string> FamilyTeachings;
    public List<string> DeathCollection;

    // --- Meta ---
    public string CurrentEra;
    public float PlayTimeSeconds;

    // --- Settings ---
    public float BgmVolume;
    public float SeVolume;
    public float VoiceVolume;
    public int SpeedIndex;        // 0=1x, 1=1.5x, 2=2x
    public bool DebugOverlayOn;
    public bool AdsRemoved;       // 課金フラグ
}

public class FamilyMember
{
    public string Name;
    public int Generation;
    public int BirthYear;
    public int DeathAge;
    public string DeathCause;
    public string Era;
    public int Score;
    public Dictionary<string, int> PeakStats;
    public bool WasAdopted;
}
Copy
9-2. SaveManager.cs
保存先: user://save_data.json
保存タイミング:
世代交代完了時
イベント完了時
アプリ バックグラウンド移行時
設定変更時
Save(): GameData → JSON → FileAccess で書き出し
Load(): FileAccess → JSON → GameData にデシリアライズ
DeleteSave(): ファイル削除（設定画面のデータ削除）
HasSaveData(): ファイル存在チェック（タイトル画面の「続きから」表示判定）
Step 10 ─ 全画面 UI
10-1. TitleScreenController.cs
全画面 1080×1920
タイトルラベル: 中央上部、フォント 96px
ボタン（VBoxContainer 中央）:
「新しい家系を始める」→ GameManager.StartNewGame()
「続きから」→ SaveManager.Load() → GameManager.ResumeGame()（セーブ無ければ非表示）
「設定」→ SettingsScreen
「実績」→ 将来用（グレーアウト）
バージョン表記: 右下 Label 30px
10-2. SettingsScreenController.cs
背景: 半透明黒
VBoxContainer:
BGM 音量: HSlider (0-100)
SE 音量: HSlider (0-100)
ゲーム速度: OptionButton (1×, 1.5×, 2×)
デバッグ表示: CheckBox
「広告除去（購入）」: Button（課金プレースホルダー、タップで「未実装」トースト）
「データ削除」: Button → 確認ダイアログ → SaveManager.DeleteSave()
「閉じる」: Button
10-3. FamilyTreeController.cs
スクロール可能な縦長画面
各世代を縦に並べる:
ノード: ColorRect 96×96（時代色） + 名前 + 享年 + スコア
ノード間を線 (Line2D or draw_line) で接続
養子は点線
最下部: 現在のキャラ（点滅）
タブ切替: 「家系図」「死因コレクション」
10-4. GameOverScreenController.cs
全画面、暗い背景
表示:
「この家系は途絶えた」Label 72px
家系サマリー: 継続世代数、累計スコア、最長寿命、最高スコア世代
全世代の墓石を横スクロールで表示
ボタン:
「新たな家系を始める」→ GameManager.StartNewGame()
「タイトルへ」→ TitleScreen
（リワード広告枠:「最後の養子」救済）
Step 11 ─ ScoreManager.cs
年間スコア加算: 毎年 +1 × (phase multiplier)
Childhood ×0.5, Youth ×1.0, Adult ×1.5, Elder ×2.0
イベントボーナス: イベント成功 +10～50（難易度による）
障害物ボーナス: 撃破 +5, 回避 +2
世代スコア計算: 享年 × (全stat平均) × (イベントクリア数 × 0.5) × レアリティ最高ボーナス
レアリティボーナス: Common ×1.0, Uncommon ×1.1, Rare ×1.3, Epic ×1.5, Legendary ×2.0
累計スコア: 全世代の世代スコア合計
家訓購入でスコア消費
Step 12 ─ 実装順序チェックリスト
以下の順に実装し、各ステップ完了後にエディタ上で動作確認。

#	タスク	主要ファイル	完了条件
1	Constants + GameManager + TimeManager + PhaseManager	Core/*.cs	エディタ再生で年齢がカウントアップ、Phase 切替を Output に表示
2	Main.tscn + 背景 + キャラ placeholder	Main.tscn, PlayerCharacter.cs	画面に縦長背景とキャラ ColorRect が表示される
3	HUD 全体	HUD.tscn, HUDController.cs	年齢・世代・スコア・寿命バー・能力値がリアルタイム更新
4	FloatText + DebugOverlay	FloatTextSpawner.cs, DebugOverlay.cs	"+5 体力" などのテキストが浮いて消える、デバッグ情報表示
5	入力 (タップ/スワイプ)	CharacterAction.cs	タップでジャンプ、スワイプで攻撃のログ出力
6	障害物 (生成・移動・衝突・DDA)	Obstacle*.cs, DifficultyManager.cs	障害物が右→左に流れ、衝突で HP 減少
7	イベント (JSON 読込・パネル・選択・結果)	Events/.cs, EventPanel	15-30秒ごとにイベント発生、選択で stat 変動
8	ドロップ・装備・インベントリ	Equipment/.cs, Inventory	ドロップ→回収→装備→stat反映→インベントリ画面
9	ナビゲーター	Navigator*.cs	自動発話・タップ発話が吹き出しで表示
10	パートナー・子供	PartnerSystem.cs	31歳で候補表示→成功/失敗→子の有無フラグ
11	世代交代 (死亡→画面→継承→新キャラ)	Generation*.cs, GenerationScreen*	寿命到達→世代交代画面→次世代で再開
12	養子・ゲームオーバー	AdoptionSystem.cs, GameOverScreen*	子なし時養子画面、条件未達でゲームオーバー
13	家訓・遺言	FamilyTeaching.cs, TestamentController.cs	スコア消費で家訓購入→次世代に効果反映
14	時代遷移	EraManager.cs	3世代ごとに背景色が変わる
15	死因コレクション	DeathCollection.cs	死因記録→コレクション画面に表示
16	セーブ/ロード	SaveManager.cs, GameData.cs	世代交代後にアプリ終了→再起動→続きから再開
17	タイトル・設定・家系図画面	TitleScreen*, Settings*, FamilyTree*	全画面遷移が正常動作
18	スコア計算の最終調整	ScoreManager.cs	スコアが意図通りに加算・消費される
19	全体通しプレイテスト	-	3世代以上プレイして破綻なし
20	Android 実機テスト	build_sign_deploy.bat	縦画面・タッチ操作・UI可読性すべて OK