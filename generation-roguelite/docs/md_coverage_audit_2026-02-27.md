# MD網羅監査レポート（2026-02-27）

## 監査対象
- `docs/full_implementation_guide.md`
- `docs/phase2_5_prompt.md`
- `docs/game_design.md`

## 結論（要約）
- **完全網羅は未達**です。  
- ただし、今回の修正で「実機向け可読性」「デバッグ表示情報不足」「フェーズ別サイズ不整合」の主要部分は改善しました。
- 未達の中心は、**UIシーン接続（MainとScenes/UIの統合不足）** と **画面遷移導線** です。

---

## 今回反映した改善（本監査対応）
1. `Scripts/Core/DebugOverlay.cs` を拡張し、表示項目を以下へ拡張:
   - FPS / RES / TOUCH / STATE / AGE / PHASE / ERA
2. `Scripts/Core/GameManager.cs`:
   - DebugOverlayへ `Age/Phase/Era` を渡すよう更新
   - モバイル可読性補正を追加（小さすぎるボタン・ラベルを補正）
   - 過密HUDラベルの整理（非表示化）
3. `Scripts/Character/PlayerCharacter.cs`:
   - フェーズ別見た目サイズを仕様値へ同期（72x72, 90x126, 100x126, 82x108）
4. `Scripts/Obstacle/ObstacleController.cs`:
   - 代表サイズを 108 に修正、消滅境界を -200 に修正
5. `Scenes/Game/Obstacle.tscn`:
   - 表示矩形を 108x108 相当に修正
6. `Scenes/Game/Character.tscn`:
   - 初期表示矩形を幼少期基準へ修正
7. `Scenes/UI/DebugOverlay.tscn`:
   - 初期テンプレート文言を新表示項目に合わせて更新
8. `Scripts/Data/Constants.cs`:
   - `BaseViewportWidth/Height` を縦持ち基準（1080/1920）へ修正

---

## ステップ網羅判定（全実装指示書基準）

| Step | 判定 | 根拠 |
|---|---|---|
| 0 設定 | 概ね完了 | `project.godot` が 1080x1920, stretch, orientation=1, mobile。`
| 1-3 コア/操作 | 概ね完了 | `GameManager` と `Action` 系で主要ループは実装。 |
| 4-9 イベント〜セーブ | 完了寄り | 既存ハンドオフの通り反映済み、ビルド通過。 |
| 10 全画面UI | **部分未達** | `Scenes/UI/*.tscn` は作成済みだが、`GameManager` は `Main.tscn` 内UIを直接参照しており、専用画面遷移導線が未統合。 |
| 11 スコア調整 | 概ね完了 | スコア更新ロジックは実装あり。 |
| 12-20 チェックリスト | **未完了あり** | 通しプレイ3世代検証・実機目視チェック項目が未完。 |

---

## UI/UX/文字表示で残る主課題
1. **MainとScenes/UIの二重構造**
   - `Scenes/UI/*.tscn` と `UI/*Controller.cs` は存在するが、`GameManager` がそれらを主要導線として利用していない。
2. **画面遷移導線不足**
   - Title/Settings/FamilyTree/Testament/Generation/GameOver の「専用画面切替」が本編から一貫接続されていない。
3. **実機目視の最終確認未完**
   - `docs/android_test_checklist.md` の可読性・干渉・ノッチ項目が未チェック。

---

## 次にやるべき作業（優先順）
1. `Main.tscn` 直書きUIを段階的に `Scenes/UI/*.tscn` インスタンスへ置換。
2. `GameManager` で画面状態（Title/Playing/Settings/Event/Generation/GameOver）の表示切替を統一。
3. 実機でチェックリスト項目を埋め、必要ならフォント/余白を再調整。
4. 3世代の通しプレイを実施し、進行破綻の有無を確認。

---

## ビルド確認
- `dotnet build GenerationRoguelite.csproj` → **成功（警告のみ）**
