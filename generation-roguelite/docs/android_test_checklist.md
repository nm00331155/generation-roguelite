# Android 実機テスト準備メモ

## 1. 現在のプロジェクト設定確認
- 画面サイズ: `1080x1920`
- ストレッチ: `canvas_items` / `expand`
- 画面向き: `portrait`

`project.godot` 側で上記は設定済み。

## 2. Godot エディタ側の事前準備
1. エディタ設定 → Export → Android で Android SDK を設定
2. OpenJDK 17 のパスを設定
3. Debug keystore を作成

## 3. エクスポート手順
1. プロジェクト → エクスポート → Android プリセット追加
2. 主な推奨値
   - Orientation: `portrait`
   - Package name: `com.yourname.inheritance`（仮）
   - Min SDK: API 24
   - Target SDK: API 34
3. APK を出力（例: `exported_game.apk`）

## 4. 実機インストール
```bash
adb install -r exported_game.apk
```

### 実行結果（2026-02-27）
- [x] `build/android/generation-roguelite.apk` のエクスポート成功
- [x] `build/android/generation-roguelite-signed.apk` の署名成功
- [x] `adb install -r` によるインストール成功
- [x] `adb shell monkey -p com.example.generationroguelite ...` による起動コマンド送信成功
- [ ] 画面表示（縦持ち・可読性）の目視確認

### このリポジトリでの実行コマンド（PowerShell）
```powershell
# APK が既にある場合
powershell -ExecutionPolicy Bypass -File .\tools\install_android.ps1 -ApkPath build/android/generation-roguelite.apk -LaunchAfterInstall

# Godot CLI が使える場合（エクスポート→インストールを一括）
powershell -ExecutionPolicy Bypass -File .\tools\export_and_install_android.ps1 -GodotExe "C:\path\to\Godot_v4.6-stable_mono_win64.exe" -LaunchAfterInstall
```

## 5. 実機確認チェック
- [ ] タップ/スワイプ入力の反応
- [ ] 上部情報ゾーンが画面内に収まる
- [ ] 中央ゲームゾーンの視認性（キャラ/障害物）
- [ ] 下部UIの押しやすさ（ボタン高さ）
- [ ] 能力値表示の可読性
- [ ] 寿命バー色変化の視認
- [ ] ナビ吹き出し位置/読みやすさ
- [ ] イベントパネルとゲーム画面の同時視認
- [ ] フロートテキストがゲームゾーン内表示
- [ ] ゲーム速度テンポ（1秒=1年）
- [ ] 障害物の反応余裕
- [ ] 下部UI操作と上部アクション入力の干渉なし
- [ ] ノッチ/ステータスバー干渉なし
