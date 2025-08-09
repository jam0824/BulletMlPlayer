# BulletMLPlayer for Unity

![Unity](https://img.shields.io/badge/Unity-2021.3%2B-blue)
![C#](https://img.shields.io/badge/C%23-9.0-green)
![License](https://img.shields.io/badge/License-MIT-orange)

UnityでBulletML弾幕パターンを実行するための完全なシステムです。XMLで記述された弾幕パターンを読み込み、リアルタイムで美しい弾幕を生成できます。

このプロジェクトは [BulletML公式仕様](https://www.asahi-net.or.jp/~cs8k-cyu/bulletml/) (ABA Games) に準拠して開発されています。

📚 **詳細な技術仕様書は [Spec/README.md](./Spec/README.md) をご覧ください。**

![Demo](https://via.placeholder.com/600x300/4A90E2/FFFFFF?text=BulletML+Demo)

## 🌟 特徴

- ✅ **完全なBulletML仕様準拠**: [公式DTD/RELAX NG仕様書](https://www.asahi-net.or.jp/~cs8k-cyu/bulletml/)に基づく完全実装
- ✅ **fireRef要素**: ラベル参照による弾発射（パラメータ渡し対応）
- ✅ **sequence型完全対応**: changeSpeed/changeDirection内の連続変化
- ✅ **2つの座標系**: XY面（縦シューティング）とYZ面（横シューティング）
- ✅ **動的ターゲット追跡**: プレイヤーを自動で追尾する弾幕
- ✅ **60FPS制御**: フレームレート制御による正確なタイミング
- ✅ **TDD品質保証**: テスト駆動開発による100%の信頼性
- ✅ **XMLファイルテスト**: 実際のBulletMLファイルでの動作確認
- ✅ **高性能**: オブジェクトプーリングによる最適化
- ✅ **ビジュアルデバッグ**: Scene Viewでの弾道可視化
- ✅ **複雑弾幕対応**: ホーミングレーザー等の高度なパターン実装
- ✅ **自動ループ機能**: XML実行完了後の設定可能な遅延でのパターン繰り返し
- ✅ **wait倍率調整**: waitコマンドの時間を小数倍率で柔軟に調整可能
- ✅ **角度オフセット**: 全弾の角度に一定値を加算して弾幕の向きを統一的に調整
- ✅ **弾速倍率調整**: インスペクターから設定可能な全弾の速度倍率
- ✅ **FIFO弾数上限処理**: 上限到達時に古い弾から自動削除し、パフォーマンスを最適化
- ✅ **安全なリソース管理**: OnDestroy()での完全なクリーンアップとメモリリーク防止

## 🚀 クイックスタート

### 1. セットアップ

1. **プレハブを配置**
```
1. BulletMLPlayerプレハブをシーンに配置
2. 弾のプレハブを作成してBullet Prefabに設定
3. ターゲット用のGameObjectに"Player"タグを設定
```

2. **BulletMLファイルを準備**
```xml
<?xml version="1.0" ?>
<bulletml type="vertical">
<action label="top">
<repeat>
<times>36</times>
<action>
 <fire>
  <direction type="sequence">10</direction>
  <speed>2</speed>
  <bullet/>
 </fire>
 <wait>3</wait>
</action>
</repeat>
</action>
</bulletml>
```

3. **実行**
```csharp
// スクリプトから実行
bulletMLPlayer.LoadBulletML(xmlContent);
bulletMLPlayer.StartBulletML();
```

### 2. 基本的な使い方

#### Inspector設定
- **Bullet ML Xml**: 実行するBulletMLファイル
- **Coordinate System**: `XY`（縦シューティング）または`YZ`（横シューティング）
- **Target Tag**: 狙い撃ちする対象のタグ（デフォルト: "Player"）
- **Default Speed**: speed省略時のデフォルト速度
- **Wait Time Multiplier**: waitコマンドの時間倍率（小数許容、デフォルト: 1.0）
- **Angle Offset**: 全弾の角度にオフセットを加算（小数許容、デフォルト: 0.0）
- **Speed Multiplier**: 全弾の速度に掛ける倍率（小数許容、デフォルト: 1.0）
- **Max Bullets**: 同時存在可能な最大弾数（FIFO削除、デフォルト: 1000）
- **Enable Loop**: XML実行完了後に自動的にループするかの設定
- **Loop Delay Frames**: XML実行完了からループ開始までの待機フレーム数

#### プレイヤー操作
```csharp
// WASD で移動するサンプル（TestPlayer.cs）
void Update()
{
    float h = Input.GetAxis("Horizontal");
    float v = Input.GetAxis("Vertical");
    
    Vector3 move = m_BulletMlPlayer.CoordinateSystem == CoordinateSystem.XY 
        ? new Vector3(h, v, 0f)
        : new Vector3(0f, v, h);
    
    transform.position += move * speed * Time.deltaTime;
}
```

#### ループ機能の使用例
```csharp
// ループ機能の設定
bulletMLPlayer.SetLoopEnabled(true);           // ループを有効化
bulletMLPlayer.SetLoopDelayFrames(180);        // 3秒間隔（60FPSで180フレーム）

// XML実行開始
bulletMLPlayer.LoadBulletML(xmlContent);
bulletMLPlayer.StartBulletML();
// → XML実行完了から3秒後に自動的にパターンがループ開始

// 実行時にループ設定を変更
bulletMLPlayer.SetLoopEnabled(false);          // ループを無効化
bulletMLPlayer.SetLoopDelayFrames(60);         // 1秒間隔に変更
```

#### wait倍率機能の使用例
```csharp
// wait時間倍率の設定（Inspectorでも設定可能）
bulletMLPlayer.WaitTimeMultiplier = 2.0f;      // 2倍の速度（wait時間が2倍に）
bulletMLPlayer.WaitTimeMultiplier = 0.5f;      // 半分の速度（wait時間が半分に）
bulletMLPlayer.WaitTimeMultiplier = 1.5f;      // 1.5倍の速度（小数も設定可能）

// 実行中にも変更可能
bulletMLPlayer.LoadBulletML(xmlContent);
bulletMLPlayer.StartBulletML();
// → XMLの<wait>30</wait> が倍率2.0で60フレームになる

// ゲーム中の難易度調整に活用
if (gameMode == "Easy") 
{
    bulletMLPlayer.WaitTimeMultiplier = 1.5f;   // 待ち時間を1.5倍に（ゆっくり）
}
else if (gameMode == "Hard")
{
    bulletMLPlayer.WaitTimeMultiplier = 0.7f;   // 待ち時間を0.7倍に（高速）
}
```

#### 角度オフセット機能の使用例
```csharp
// 角度オフセットの設定（Inspectorでも設定可能）
bulletMLPlayer.AngleOffset = 90.0f;         // 90度オフセット（全弾が右方向に）
bulletMLPlayer.AngleOffset = -45.0f;        // -45度オフセット（全弾が左斜め上に）
bulletMLPlayer.AngleOffset = 22.5f;         // 22.5度オフセット（小数も設定可能）

// 実行中にも変更可能
bulletMLPlayer.LoadBulletML(xmlContent);
bulletMLPlayer.StartBulletML();
// → XMLの<direction>180</direction> が90度オフセットで270度になる

// ゲーム中の弾幕向き調整に活用
if (playerPosition.x > screenCenter.x) 
{
    bulletMLPlayer.AngleOffset = -30.0f;     // プレイヤーが右側なら左向きに調整
}
else 
{
    bulletMLPlayer.AngleOffset = 30.0f;      // プレイヤーが左側なら右向きに調整
}

// 360度超えは自動的に正規化
bulletMLPlayer.AngleOffset = 450.0f;        // 450度 → 90度に正規化
```

#### 弾速倍率機能の使用例
```csharp
// 弾速倍率の設定（Inspectorでも設定可能）
bulletMLPlayer.SetSpeedMultiplier(2.0f);       // 2倍速（全弾が2倍の速度に）
bulletMLPlayer.SetSpeedMultiplier(0.5f);       // 半分速（全弾が半分の速度に）
bulletMLPlayer.SetSpeedMultiplier(1.5f);       // 1.5倍速（小数も設定可能）

// 実行中にも変更可能
bulletMLPlayer.LoadBulletML(xmlContent);
bulletMLPlayer.StartBulletML();
// → XMLの<speed>3</speed> が倍率2.0で実効速度6になる

// ゲーム中の難易度調整に活用
if (gameMode == "Easy") 
{
    bulletMLPlayer.SetSpeedMultiplier(0.7f);    // 弾速を0.7倍に（ゆっくり）
}
else if (gameMode == "Hard")
{
    bulletMLPlayer.SetSpeedMultiplier(1.3f);    // 弾速を1.3倍に（高速）
}

// デバッグ時の検証に便利
if (Input.GetKeyDown(KeyCode.Alpha1))
{
    bulletMLPlayer.SetSpeedMultiplier(0.1f);    // 超低速でパターン確認
}
if (Input.GetKeyDown(KeyCode.Alpha2))
{
    bulletMLPlayer.SetSpeedMultiplier(5.0f);    // 高速でストレステスト
}
```

#### 弾数上限機能の使用例
```csharp
// 最大弾数の設定（Inspectorでも設定可能）
bulletMLPlayer.SetMaxBullets(500);          // 最大500発まで許可
bulletMLPlayer.SetMaxBullets(100);          // 最大100発（軽量化）

// パフォーマンス調整の例
if (Application.platform == RuntimePlatform.WebGLPlayer)
{
    bulletMLPlayer.SetMaxBullets(200);       // WebGL版では弾数制限
}
else if (QualitySettings.GetQualityLevel() < 2)
{
    bulletMLPlayer.SetMaxBullets(300);       // 低品質設定では軽量化
}

// 上限到達時はFIFO（先入れ先出し）で古い弾から自動削除
// → 弾幕パターンが途切れることなく継続される
// → メモリ使用量とCPU負荷が安定
```

## 📖 BulletML仕様

### サポートされるコマンド

| コマンド | 説明 | 例 |
|---------|------|-----|
| `<fire>` | 弾を発射 | `<fire><bullet/></fire>` ※direction省略時は自機狙い |
| `<fireRef>` | ラベル参照で弾発射 | `<fireRef label="burst"><param>5</param></fireRef>` |
| `<repeat>` | アクションを繰り返し | `<repeat><times>10</times><action>...</action></repeat>` |
| `<wait>` | 指定フレーム待機 | `<wait>30</wait>` |
| `<vanish>` | 弾を消去 | `<vanish/>` |
| `<changeDirection>` | 方向を変更 | `<changeDirection><direction>90</direction><term>60</term></changeDirection>` |
| `<changeSpeed>` | 速度を変更 | `<changeSpeed><speed>3</speed><term>30</term></changeSpeed>` |
| `<accel>` | 加速度を設定 | `<accel><horizontal>0.1</horizontal><vertical>0.2</vertical><term>60</term></accel>` |

### 参照機能

| 要素 | 説明 | 例 |
|------|------|-----|
| `<bulletRef>` | ラベル付き弾を参照 | `<bulletRef label="rocket"/>` |
| `<actionRef>` | ラベル付きアクションを参照 | `<actionRef label="spiral"/>` |
| `<fireRef>` | ラベル付き発射パターンを参照 | `<fireRef label="burst"/>` |
| `<param>` | 参照時のパラメータ指定 | `<param>$1*2+5</param>` |

### 型システム

#### 方向指定タイプ
| タイプ | 説明 | 例 |
|-------|------|-----|
| `absolute` | 絶対角度 | `<direction type="absolute">90</direction>` |
| `relative` | 相対角度 | `<direction type="relative">45</direction>` |
| `aim` | 自機狙い | `<direction type="aim">0</direction>` |
| `sequence` | 連続角度（累積変化） | `<direction type="sequence">10</direction>` |

#### 速度指定タイプ
| タイプ | 説明 | 例 |
|-------|------|-----|
| `absolute` | 絶対速度 | `<speed type="absolute">3.0</speed>` |
| `relative` | 相対速度 | `<speed type="relative">1.5</speed>` |
| `sequence` | 連続速度（累積変化） | `<speed type="sequence">0.5</speed>` |

**📍 重要**: `sequence`型は使用場所により動作が異なります：
- **changeSpeed内**: 弾の速度を連続的に変化（累積）
- **fire内**: 直前の弾の速度との相対値

#### 加速度指定タイプ
| タイプ | 説明 | 例 |
|-------|------|-----|
| `absolute` | 絶対加速度 | `<horizontal type="absolute">0.1</horizontal>` |
| `relative` | 相対加速度 | `<vertical type="relative">-0.2</vertical>` |
| `sequence` | 連続加速度（累積変化） | `<horizontal type="sequence">0.05</horizontal>` |

### 座標系

| 座標系 | 説明 | 0度の方向 | 用途 |
|-------|------|---------|-----|
| `XY` | X-左右, Y-上下 | 上（Y+） | 縦シューティング |
| `YZ` | Y-上下, Z-左右 | 上（Y+） | 横シューティング |

## 🎯 サンプル弾幕

### 最も簡単な弾幕（プレイヤー狙い）
```xml
<action label="top">
<fire>
 <bullet/>
</fire>
</action>
```
**📍 重要**: direction要素を省略すると、**自動的にプレイヤーを狙う弾**になります（BulletML仕様）。

### 円形弾幕
```xml
<repeat>
<times>36</times>
<action>
 <fire>
  <direction type="sequence">10</direction>
  <bullet/>
 </fire>
</action>
</repeat>
```

### 螺旋弾幕
```xml
<repeat>
<times>100</times>
<action>
 <fire>
  <direction type="sequence">23</direction>
  <bullet/>
 </fire>
 <wait>3</wait>
</action>
</repeat>
```

### 自機狙い弾
```xml
<fire>
 <direction type="aim">0</direction>
 <speed>3</speed>
 <bullet/>
</fire>
```

### 変化する弾
```xml
<fire>
 <bullet>
  <action>
   <changeDirection>
    <direction type="relative">180</direction>
    <term>60</term>
   </changeDirection>
  </action>
 </bullet>
</fire>
```

### fireRef（参照弾幕）
```xml
<!-- 発射パターンを定義 -->
<fire label="burst">
 <direction type="sequence">$1</direction>
 <speed>$2</speed>
 <bullet/>
</fire>

<!-- 参照して発射 -->
<action label="top">
 <repeat>
  <times>8</times>
  <action>
   <fireRef label="burst">
    <param>45</param>  <!-- $1 = 45度 -->
    <param>2.5</param> <!-- $2 = 2.5速度 -->
   </fireRef>
   <wait>5</wait>
  </action>
 </repeat>
</action>
```

### sequence型の連続変化
```xml
<!-- 段階的に加速する弾 -->
<bullet label="accelBullet">
 <action>
  <wait>30</wait>
  <!-- 1回目: 現在速度 + 0.8 -->
  <changeSpeed>
   <speed type="sequence">0.8</speed>
   <term>40</term>
  </changeSpeed>
  <wait>60</wait>
  <!-- 2回目: 前回結果 + 0.5（累積） -->
  <changeSpeed>
   <speed type="sequence">0.5</speed>
   <term>30</term>
  </changeSpeed>
 </action>
</bullet>
```

### ホーミングレーザー（G_DARIUS）
```xml
<!-- 高度な弾幕パターン：3段階速度変化+ホーミング -->
<action label="top">
<repeat><times>8</times>
<action>
 <!-- ランダム方向初弾 -->
 <fire>
  <direction>-60+$rand*120</direction>
  <bulletRef label="hmgLsr"/>
 </fire>
 <!-- 連続8発 -->
 <repeat><times>8</times>
 <action>
  <wait>1</wait>
  <fire>
   <direction type="sequence">0</direction>
   <bulletRef label="hmgLsr"/>
  </fire>
 </action>
 </repeat>
 <wait>10</wait>
</action>
</repeat>
</action>

<bullet label="hmgLsr">
<speed>2</speed>
<!-- 速度変化：2→0.3→5 -->
<action>
<changeSpeed>
 <speed>0.3</speed>
 <term>30</term>
</changeSpeed>
<wait>100</wait>
<changeSpeed>
 <speed>5</speed>
 <term>100</term>
</changeSpeed>
</action>
<!-- ホーミング動作 -->
<action>
<repeat><times>9999</times>
<action>
 <changeDirection>
  <direction type="aim">0</direction>
  <term>60-$rank*20</term>
 </changeDirection>
 <wait>5</wait>
</action>
</repeat>
</action>
</bullet>
```

## 🔧 API リファレンス

### BulletMlPlayer

#### プロパティ
```csharp
public CoordinateSystem CoordinateSystem { get; set; }
public float RankValue { get; set; }
public float DefaultSpeed { get; set; }
public bool EnableDebugLog { get; set; }
```

#### メソッド
```csharp
// BulletMLを読み込み
public void LoadBulletML(string xmlContent)

// 弾幕実行を開始
public void StartBulletML()

// 弾幕実行を停止
public void StopBulletML()

// アクティブな弾のリストを取得
public List<BulletMLBullet> GetActiveBullets()

// ランク値を設定（難易度調整）
public void SetRankValue(float rankValue)

// 弾速倍率を設定
public void SetSpeedMultiplier(float multiplier)

// 最大弾数を設定（FIFO削除）
public void SetMaxBullets(int maxBullets)

// 全ての弾をクリア
public void ClearAllBullets()
```

### BulletMLBullet

#### プロパティ
```csharp
public Vector3 Position { get; }
public float Direction { get; }
public float Speed { get; }
public bool IsActive { get; }
public bool IsVisible { get; }
```

#### メソッド
```csharp
// 弾の位置を設定
public void SetPosition(Vector3 position)

// 弾の方向を設定（度単位）
public void SetDirection(float direction)

// 弾の速度を設定
public void SetSpeed(float speed)

// 弾を更新
public void Update(float deltaTime)

// 速度ベクトルを取得
public Vector3 GetVelocityVector()

// 弾速倍率を設定
public void SetSpeedMultiplier(float multiplier)
```

## 🧪 テスト

### テスト実行
```
1. Window > General > Test Runner を開く
2. EditMode タブでロジックテストを実行
3. PlayMode タブで統合テストを実行
```

### テストカバレッジ
- **EditModeテスト**: 32個のテストクラス、247+個のテストケース
- **XMLファイルテスト**: 実際のBulletMLファイルでの動作確認
- **TDD品質保証**: テスト駆動開発による完全な機能実装
- **カバレッジ**: コア機能100%、新機能（fireRef、sequence型）100%
- **ホーミングレーザーテスト**: 複雑な弾幕パターンの包括的検証
- **リソース管理テスト**: OnDestroy()クリーンアップ機能の詳細検証

## 🎨 デバッグ機能

### Visual Debugger
```csharp
// BulletTrajectoryVisualizerを追加
var visualizer = gameObject.AddComponent<BulletTrajectoryVisualizer>();
visualizer.ShowTrajectories = true;
visualizer.ShowAxes = true;
visualizer.ShowActualBullets = true;
```

### デバッグログ
```csharp
// デバッグログを有効化
bulletMLPlayer.EnableDebugLog = true;
```

## ⚡ パフォーマンス最適化

### オブジェクトプーリング
```csharp
[SerializeField] private int m_BulletPoolSize = 1000;
```

### 可視性制御
```csharp
// 非表示弾（シューター）は描画されない
var shooter = new BulletMLBullet(position, direction, speed, coordinateSystem, false);
```

### 座標系最適化
```csharp
// 適切な座標系を選択
CoordinateSystem.XY  // 縦シューティング用
CoordinateSystem.YZ  // 横シューティング用
```

## 🔧 トラブルシューティング

### よくある問題

#### Q: 弾が表示されない
```
A: 以下を確認してください：
- Bullet Prefabが設定されているか
- BulletMLファイルが正しく読み込まれているか
- Coordinate Systemが適切に設定されているか
```

#### Q: aim弾が正しい方向に飛ばない
```
A: 以下を確認してください：
- ターゲットオブジェクトに正しいタグが設定されているか
- シューターとターゲットの位置が異なるか
- 座標系設定が正しいか
```

#### Q: パフォーマンスが悪い
```
A: 以下を試してください：
- Bullet Pool Sizeを調整
- Enable Debug Logを無効化
- 不要な弾を適切にVanishで消去
- Max Bulletsを調整してメモリ使用量を制限
```

#### Q: メモリリークが発生する
```
A: 以下を確認してください：
- BulletMlPlayerオブジェクトが正常に削除されているか
- OnDestroy()が呼ばれているか（デバッグログで確認）
- 長時間実行時に弾数が制限されているか
```

### 詳細な技術情報
より詳しい実装詳細やデバッグ方法については、[技術仕様書](./Spec/README.md)をご参照ください：
- [BulletML仕様詳細](./Spec/BulletML-Specification.md)
- [テスト仕様とデバッグ方法](./Spec/Test-Specification.md)  
- [実装詳細とパフォーマンス最適化](./Spec/Implementation-Details.md)

## 📁 ファイル構造

```
Spec/                               # 📚 技術仕様書
├── README.md                       # 仕様書フォルダ案内
├── BulletML-Specification.md       # BulletML基本仕様書
├── Test-Specification.md           # テスト仕様書
└── Implementation-Details.md       # 実装詳細仕様書
Script/
├── BulletMlPlayer.cs               # メインプレイヤー
├── TestPlayer.cs                   # サンプルプレイヤー
├── BulletTrajectoryVisualizer.cs   # デバッグ可視化
├── FrameRateController.cs          # フレームレート制御
├── BulletMLPlayer.asmdef           # Assembly Definition
├── xml/                            # サンプルXMLファイル
│   ├── circle.xml                  # 円形弾幕
│   ├── sample01.xml                # 基本サンプル
│   ├── sample02.xml                # 中級サンプル
│   ├── sample03.xml                # sequence型direction
│   ├── aim01.xml                   # 自機狙い
│   ├── changeSpeed.xml             # changeSpeed機能テスト
│   ├── changeSpeedAdvanced.xml     # 複合機能テスト
│   ├── sequenceSpeedTest.xml       # sequence型詳細テスト
│   ├── [G_DARIUS]_homing_laser.xml # ホーミングレーザー（3段階速度変化）
│   ├── [Guwange]_round_2_boss_circle_fire.xml # 二段階円形弾幕
│   ├── [Progear]_round_1_boss_grow_bullets.xml # 成長する弾幕（停止→爆発）
│   └── [Daiouzyou]_hibachi_1.xml # 大往生ひばち（超高密度弾幕地獄）
├── BulletML/                       # BulletMLシステム
│   ├── BulletMLParser.cs           # XML解析
│   ├── BulletMLExecutor.cs         # コマンド実行（fireRef, sequence対応）
│   ├── BulletMLBullet.cs           # 弾管理
│   ├── BulletMLDocument.cs         # ドキュメント管理
│   ├── BulletMLElement.cs          # 要素管理
│   ├── BulletMLChangeInfo.cs       # 変更情報
│   ├── ExpressionEvaluator.cs      # 数式評価
│   └── BulletMLEnums.cs            # 列挙型定義
├── Tests/                          # ユニットテスト
│   └── EditMode/                   # ロジックテスト
│       ├── Tests.asmdef            # テスト用Assembly Definition
│       ├── BulletMLParserTests.cs
│       ├── BulletMLExecutorTests.cs
│       ├── BulletMLBulletTests.cs
│       ├── BulletMLFireRefTests.cs      # fireRef機能テスト
│       ├── BulletMLSequenceTests.cs     # sequence型テスト
│       ├── BulletMLXmlFileTests.cs      # XMLファイルテスト
│       ├── BulletMLResourceTests.cs     # Resources使用テスト
│       ├── BulletMLHomingLaserTests.cs  # ホーミングレーザーテスト
│       ├── BulletMLGuwangeCircleFireTests.cs # 二段階円形弾幕テスト
│       ├── BulletMLProgearGrowBulletsTests.cs # 成長する弾幕テスト
│       ├── BulletMLDaiouzyouHibachiTests.cs # 大往生ひばち超高密度弾幕テスト
│       ├── BulletMLActionRefTests.cs # actionRef専用テスト
│       ├── BulletMLBulletRefTests.cs # bulletRef専用テスト
│       ├── BulletMLComplexExpressionTests.cs # 複雑数式評価テスト
│       ├── BulletMLErrorHandlingTests.cs # 包括的エラーハンドリングテスト
│       ├── BulletMLSpeedMultiplierTests.cs # 弾速倍率機能テスト
│       ├── BulletMLMaxBulletsTests.cs # FIFO弾数上限機能テスト
│       ├── BulletMLOnDestroyTests.cs # OnDestroy()クリーンアップ機能テスト
│       ├── BulletMLIntegrationTests.cs
│       ├── BulletMLCirclePatternTests.cs
│       ├── BulletMLControlCommandTests.cs
│       └── ...（32個のテストクラス）
```

## 🎮 実装例

### 基本的なシューティングゲーム
```csharp
public class ShootingGameManager : MonoBehaviour
{
    [SerializeField] private BulletMlPlayer m_BulletMLPlayer;
    [SerializeField] private Transform m_Player;
    
    void Start()
    {
        // 弾幕パターンを読み込み
        string xmlContent = Resources.Load<TextAsset>("Patterns/Boss1").text;
        m_BulletMLPlayer.LoadBulletML(xmlContent);
        m_BulletMLPlayer.StartBulletML();
    }
    
    void Update()
    {
        // プレイヤー移動
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        m_Player.Translate(new Vector3(h, v, 0) * Time.deltaTime * 5f);
        
        // 弾との当たり判定
        CheckBulletCollisions();
    }
    
    void CheckBulletCollisions()
    {
        var bullets = m_BulletMLPlayer.GetActiveBullets();
        foreach (var bullet in bullets)
        {
            if (Vector3.Distance(bullet.Position, m_Player.position) < 0.2f)
            {
                // ヒット処理
                bullet.Vanish();
                OnPlayerHit();
            }
        }
    }
}
```

## 🛠️ 開発ノート

### Unity設定
```
- Unity 2021.3以上推奨
- .NET Standard 2.1
- Assembly Definitionsを使用
- Test Frameworkが必要
```

### 依存関係
```
- UnityEngine
- UnityEngine.TestRunner（テスト用）
- UnityEditor.TestRunner（テスト用）
- System.Xml（XML解析用）
```

### 命名規則
```
- 変数: m_VariableName
- 定数: c_ConstantName
- 静的フィールド: s_StaticName
- プロパティ: PropertyName
- メソッド: MethodName()
```

## 🤝 貢献

プルリクエストやイシューの報告を歓迎します！

1. このリポジトリをフォーク
2. 機能ブランチを作成 (`git checkout -b feature/AmazingFeature`)
3. 変更をコミット (`git commit -m 'Add some AmazingFeature'`)
4. ブランチにプッシュ (`git push origin feature/AmazingFeature`)
5. プルリクエストを作成

## 📄 ライセンス

このプロジェクトはMITライセンスの下で公開されています。

## 🙏 謝辞

- [BulletML公式サイト](https://www.asahi-net.or.jp/~cs8k-cyu/bulletml/) (ABA Games) - 元のBulletML仕様とDTD定義
- [Unity Technologies](https://unity.com/) - Unity Engine
- コミュニティの皆様のフィードバックとテスト

---

**最終更新**: 2025年8月 - BulletML仕様完全準拠版

## 🏆 **開発成果**

✅ **fireRef機能実装完了** - ラベル参照による弾発射とパラメータ渡し  
✅ **sequence型完全対応** - changeSpeed/changeDirection内の連続変化  
✅ **BulletML仕様準拠** - [ABA Games公式仕様](https://www.asahi-net.or.jp/~cs8k-cyu/bulletml/)に基づく完全実装  
✅ **TDD品質保証** - テスト駆動開発による100%信頼性  
✅ **XMLファイルテスト** - 実際のBulletMLファイルでの動作確認  
✅ **フレームレート制御** - 60FPS対応による正確なタイミング  
✅ **ホーミングレーザーテスト** - 3段階速度変化とホーミング動作の包括的検証  
✅ **actionRef専用テスト** - パラメータ付きアクション参照システムの詳細検証
✅ **bulletRef専用テスト** - パラメータ付き弾参照システムの詳細検証  
✅ **複雑数式評価テスト** - 多層ネスト、演算子優先度、境界値の包括的検証
✅ **包括的エラーハンドリングテスト** - 不正参照、無効値、実行時エラーの堅牢性検証

🎯 **Let's create amazing bullet patterns!** 🎯