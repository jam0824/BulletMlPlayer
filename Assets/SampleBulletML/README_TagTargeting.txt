# BulletMLプレイヤータグターゲティング設定手順

## 1. プレイヤーオブジェクトの作成

### 手順A: 既存オブジェクトにタグ設定
1. シーン内の任意のGameObjectを選択
2. Inspectorで「Tag」を「Player」に設定
3. TestPlayerスクリプトをアタッチ（オプション：WASD移動機能）

### 手順B: 新規プレイヤーオブジェクト作成
1. Create → 3D Object → Cube
2. 名前を「Player」に変更
3. Tagを「Player」に設定
4. TestPlayerスクリプトをアタッチ

## 2. BulletMlPlayerの設定

### Inspector設定
- **Target Tag**: "Player" （デフォルト）
- **Fallback Player Position**: タグオブジェクトが見つからない場合の位置
- **Use Shooter Offset**: お好みで（falseを推奨）

### 座標系別推奨設定

#### YZ面（縦スクロール）
- BulletMLPlayer位置: (0, 0, 0)
- Player初期位置: (0, -3, -5)
- TestPlayer Coordinate System: YZ

#### XY面（横スクロール）
- BulletMLPlayer位置: (0, 0, 0)
- Player初期位置: (2, -3, 0)
- TestPlayer Coordinate System: XY

## 3. 動作確認

### デバッグログ有効化
1. BulletMlPlayerの「Enable Debug Log」を✓
2. Console windowでログ確認：
   ```
   ターゲットタグ: 'Player'
   ターゲットオブジェクト: Player
   現在のプレイヤー位置: (0, -3, -5)
   ```

### Scene Viewでの確認
- 青い球: プレイヤー位置（タグオブジェクト見つかった場合）
- 赤い球: フォールバック位置（タグオブジェクト見つからない場合）
- 緑の立方体: プレイヤーオブジェクト
- 黄色の枠: プレイヤー移動範囲

### 操作方法
- **WASD**: プレイヤー移動
- **Play**: BulletMLが自動開始
- **弾がプレイヤーを追跡**することを確認

## 4. トラブルシューティング

### 弾が発射されない
- BulletML XMLが正しく設定されているか確認
- Auto Startが有効か確認

### 弾がプレイヤーを狙わない
- Target Tagが正しく設定されているか確認
- PlayerオブジェクトのTagが「Player」になっているか確認
- デバッグログで「ターゲットオブジェクト: 見つからず」と表示されていないか確認

### 弾が真上に飛ぶ
- プレイヤーとシューターが同じ位置にないか確認
- Use Shooter Offsetを有効にするか、プレイヤーを離れた位置に配置

## 5. カスタマイズ

### 別のタグを使用したい場合
- Target Tagフィールドを変更（例: "Enemy", "Boss"など）

### 複数ターゲットを順次狙いたい場合
- 現在は最初に見つかったオブジェクトのみ対象
- カスタムロジックが必要な場合はUpdateTargetPosition()を拡張