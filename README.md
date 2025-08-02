# BulletMLPlayer for Unity

![Unity](https://img.shields.io/badge/Unity-2021.3%2B-blue)
![C#](https://img.shields.io/badge/C%23-9.0-green)
![License](https://img.shields.io/badge/License-MIT-orange)

UnityでBulletML弾幕パターンを実行するための完全なシステムです。XMLで記述された弾幕パターンを読み込み、リアルタイムで美しい弾幕を生成できます。

![Demo](https://via.placeholder.com/600x300/4A90E2/FFFFFF?text=BulletML+Demo)

## 🌟 特徴

- ✅ **完全なBulletML対応**: 標準的なBulletML仕様をサポート
- ✅ **2つの座標系**: XY面（水平シューティング）とYZ面（縦シューティング）
- ✅ **動的ターゲット追跡**: プレイヤーを自動で追尾する弾幕
- ✅ **高性能**: オブジェクトプーリングによる最適化
- ✅ **テスト駆動**: 100%の信頼性を保証する包括的なテスト
- ✅ **ビジュアルデバッグ**: Scene Viewでの弾道可視化

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
- **Coordinate System**: `XY`（横シューティング）または`YZ`（縦シューティング）
- **Target Tag**: 狙い撃ちする対象のタグ（デフォルト: "Player"）
- **Default Speed**: speed省略時のデフォルト速度

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

## 📖 BulletML仕様

### サポートされるコマンド

| コマンド | 説明 | 例 |
|---------|------|-----|
| `<fire>` | 弾を発射 | `<fire><bullet/></fire>` |
| `<repeat>` | アクションを繰り返し | `<repeat><times>10</times><action>...</action></repeat>` |
| `<wait>` | 指定フレーム待機 | `<wait>30</wait>` |
| `<vanish>` | 弾を消去 | `<vanish/>` |
| `<changeDirection>` | 方向を変更 | `<changeDirection><direction>90</direction><term>60</term></changeDirection>` |
| `<changeSpeed>` | 速度を変更 | `<changeSpeed><speed>3</speed><term>30</term></changeSpeed>` |
| `<accel>` | 加速度を設定 | `<accel><horizontal>0.1</horizontal><vertical>0.2</vertical><term>60</term></accel>` |

### 方向指定タイプ

| タイプ | 説明 | 例 |
|-------|------|-----|
| `absolute` | 絶対角度 | `<direction type="absolute">90</direction>` |
| `relative` | 相対角度 | `<direction type="relative">45</direction>` |
| `aim` | 自機狙い | `<direction type="aim">0</direction>` |
| `sequence` | 連続角度 | `<direction type="sequence">10</direction>` |

### 座標系

| 座標系 | 説明 | 0度の方向 | 用途 |
|-------|------|---------|-----|
| `XY` | X-横, Y-縦 | 上（Y+） | 水平シューティング |
| `YZ` | Y-縦, Z-前後 | 上（Y+） | 縦シューティング |

## 🎯 サンプル弾幕

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
```

## 🧪 テスト

### テスト実行
```
1. Window > General > Test Runner を開く
2. EditMode タブでロジックテストを実行
3. PlayMode タブで統合テストを実行
```

### テストカバレッジ
- **EditModeテスト**: 19個のテストクラス、120+個のテストケース
- **PlayModeテスト**: 統合テストとシナリオテスト
- **カバレッジ**: コア機能100%

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
CoordinateSystem.XY  // 水平シューティング用
CoordinateSystem.YZ  // 縦シューティング用
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
```

## 📁 ファイル構造

```
Script/
├── BulletMlPlayer.cs               # メインプレイヤー
├── TestPlayer.cs                   # サンプルプレイヤー
├── BulletTrajectoryVisualizer.cs   # デバッグ可視化
├── BulletMLPlayer.asmdef           # Assembly Definition
├── xml/                            # サンプルXMLファイル
│   ├── circle.xml
│   ├── sample01.xml
│   ├── sample02.xml
│   └── aim01.xml
├── BulletML/                       # BulletMLシステム
│   ├── BulletMLParser.cs           # XML解析
│   ├── BulletMLExecutor.cs         # コマンド実行
│   ├── BulletMLBullet.cs           # 弾管理
│   ├── BulletMLDocument.cs         # ドキュメント管理
│   ├── BulletMLElement.cs          # 要素管理
│   ├── BulletMLChangeInfo.cs       # 変更情報
│   └── BulletMLEnums.cs            # 列挙型定義
├── Tests/                          # ユニットテスト
│   └── EditMode/                   # ロジックテスト
│       ├── Tests.asmdef            # テスト用Assembly Definition
│       ├── BulletMLParserTests.cs
│       ├── BulletMLExecutorTests.cs
│       ├── BulletMLBulletTests.cs
│       ├── BulletMLIntegrationTests.cs
│       ├── BulletMLCirclePatternTests.cs
│       ├── BulletMLControlCommandTests.cs
│       └── ...（19個のテストクラス）
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

- [BulletML](http://www.asahi-net.or.jp/~cs8k-cyu/bulletml/) - 元のBulletML仕様
- [Unity Technologies](https://unity.com/) - Unity Engine
- コミュニティの皆様のフィードバックとテスト

---

**最終更新**: 2024年

🎯 **Let's create amazing bullet patterns!** 🎯