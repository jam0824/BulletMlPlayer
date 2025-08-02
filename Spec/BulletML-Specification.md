# BulletML 仕様書

## 📋 概要

この文書はBulletMLプレイヤーの実装仕様を詳細に定義します。  
[BulletML公式仕様](https://www.asahi-net.or.jp/~cs8k-cyu/bulletml/) (ABA Games) に準拠した完全実装を目指しています。

**バージョン**: 0.21準拠  
**対応Unity**: 2021.3以上  
**更新日**: 2025年8月

---

## 🎯 基本概念

### BulletMLとは

BulletMLは、シューティングゲームの弾幕パターンをXMLで記述するための言語仕様です。

**主要な特徴:**
- 複雑な弾幕パターンの簡潔な記述
- 再利用可能なコンポーネント設計
- パラメータ化による柔軟性
- ランダム要素とランク（難易度）対応

### 座標系

BulletMLプレイヤーは2つの座標系をサポートします：

| 座標系 | type属性 | 説明 | 0度方向 | 用途 |
|-------|---------|------|---------|------|
| **XY座標系** | `horizontal` | X軸=横、Y軸=縦 | 上（Y+方向） | 水平シューティング |
| **YZ座標系** | `vertical` | Y軸=縦、Z軸=前後 | 上（Y+方向） | 縦シューティング |

---

## 📐 要素仕様

### 1. ルート要素

#### `<bulletml>`
```xml
<bulletml type="vertical|horizontal" xmlns="http://www.asahi-net.or.jp/~cs8k-cyu/bulletml">
  <!-- 弾幕パターン定義 -->
</bulletml>
```

**属性:**
- `type`: 座標系指定（`vertical` | `horizontal`）
- `xmlns`: 名前空間（必須）

### 2. アクション要素

#### `<action>`
```xml
<action label="labelName">
  <!-- アクション内容 -->
</action>
```

実行可能なコマンドの集合を定義します。

**子要素:** `fire`, `fireRef`, `repeat`, `wait`, `vanish`, `changeDirection`, `changeSpeed`, `accel`, `action`, `actionRef`

#### `<actionRef>`
```xml
<actionRef label="labelName">
  <param>value1</param>
  <param>value2</param>
</actionRef>
```

ラベル参照によるアクション呼び出し。

### 3. 発射要素

#### `<fire>`
```xml
<fire label="labelName">
  <direction type="aim|absolute|relative|sequence">expression</direction>
  <speed type="absolute|relative|sequence">expression</speed>
  <bullet>...</bullet>
  <!-- または -->
  <bulletRef label="labelName">
    <param>value</param>
  </bulletRef>
</fire>
```

弾を発射します。

**子要素:**
- `direction`: 発射方向（省略時は`type="aim"`）
- `speed`: 発射速度（省略時は親の速度またはデフォルト値）
- `bullet` | `bulletRef`: 弾の定義

#### `<fireRef>`
```xml
<fireRef label="labelName">
  <param>value1</param>
  <param>value2</param>
</fireRef>
```

ラベル参照による発射パターン呼び出し。

### 4. 弾要素

#### `<bullet>`
```xml
<bullet label="labelName">
  <speed>expression</speed>
  <action>...</action>
  <actionRef label="labelName"/>
</bullet>
```

弾の定義と動作を記述します。

**子要素:**
- `speed`: 弾の初期速度
- `action` | `actionRef`: 弾の動作パターン

#### `<bulletRef>`
```xml
<bulletRef label="labelName">
  <param>value1</param>
  <param>value2</param>
</bulletRef>
```

ラベル参照による弾呼び出し。

### 5. 制御要素

#### `<repeat>`
```xml
<repeat>
  <times>expression</times>
  <action>...</action>
</repeat>
```

アクションの繰り返し実行。

#### `<wait>`
```xml
<wait>expression</wait>
```

指定フレーム数の待機（1フレーム = 1/60秒）。

#### `<vanish>`
```xml
<vanish/>
```

弾を消去します。

### 6. 変更要素

#### `<changeDirection>`
```xml
<changeDirection>
  <direction type="aim|absolute|relative|sequence">expression</direction>
  <term>expression</term>
</changeDirection>
```

弾の方向を段階的に変更します。

#### `<changeSpeed>`
```xml
<changeSpeed>
  <speed type="absolute|relative|sequence">expression</speed>
  <term>expression</term>
</changeSpeed>
```

弾の速度を段階的に変更します。

#### `<accel>`
```xml
<accel>
  <horizontal type="absolute|relative|sequence">expression</horizontal>
  <vertical type="absolute|relative|sequence">expression</vertical>
  <term>expression</term>
</accel>
```

弾に加速度を適用します。

### 7. パラメータ要素

#### `<param>`
```xml
<param>expression</param>
```

参照時のパラメータ値（`$1`, `$2`, ... として参照）。

---

## 🔢 型システム

### 方向指定タイプ

| タイプ | 説明 | 計算式 | 例 |
|-------|------|--------|-----|
| `aim` | プレイヤー狙い | `atan2(target - position) + value` | `<direction type="aim">0</direction>` |
| `absolute` | 絶対角度 | `value` | `<direction type="absolute">90</direction>` |
| `relative` | 相対角度 | `current_direction + value` | `<direction type="relative">45</direction>` |
| `sequence` | 連続角度 | `last_sequence_direction + value` | `<direction type="sequence">10</direction>` |

### 速度指定タイプ

| タイプ | 説明 | 計算式 | 例 |
|-------|------|--------|-----|
| `absolute` | 絶対速度 | `value` | `<speed type="absolute">3.0</speed>` |
| `relative` | 相対速度 | `current_speed + value` | `<speed type="relative">1.5</speed>` |
| `sequence` | 連続速度 | `last_sequence_speed + value` | `<speed type="sequence">0.5</speed>` |

**重要:** `sequence`型は使用場所により動作が異なります：
- **changeSpeed内**: 弾の速度を連続的に変化（累積）
- **fire内**: 直前の弾の速度との相対値

### 加速度指定タイプ

| タイプ | 説明 | 計算式 | 例 |
|-------|------|--------|-----|
| `absolute` | 絶対加速度 | `value` | `<horizontal type="absolute">0.1</horizontal>` |
| `relative` | 相対加速度 | `current_accel + value` | `<vertical type="relative">-0.2</vertical>` |
| `sequence` | 連続加速度 | `last_sequence_accel + value` | `<horizontal type="sequence">0.05</horizontal>` |

---

## 📊 数式評価

### 変数

| 変数 | 説明 | 範囲 | 例 |
|-----|------|------|-----|
| `$rand` | ランダム値 | [0.0, 1.0) | `$rand*360` |
| `$rank` | 難易度値 | [0.0, 1.0] | `3+$rank*2` |
| `$1`, `$2`, ... | パラメータ | 任意 | `$1*2+$2` |

### 演算子

| 演算子 | 説明 | 例 |
|-------|------|-----|
| `+` | 加算 | `10+5` |
| `-` | 減算 | `10-5` |
| `*` | 乗算 | `10*5` |
| `/` | 除算 | `10/5` |
| `%` | 剰余 | `10%3` |
| `()` | 括弧 | `(10+5)*2` |

---

## 🎮 実装仕様

### 弾の生成と管理

#### 弾の生命周期
1. **生成**: `fire` | `fireRef`コマンドで生成
2. **初期化**: 位置、方向、速度の設定
3. **更新**: 毎フレームの位置・速度更新
4. **アクション実行**: 弾に付随するアクションの実行
5. **消滅**: `vanish`コマンドまたは画面外で消滅

#### オブジェクトプーリング
```csharp
[SerializeField] private int m_BulletPoolSize = 1000;
```

パフォーマンス最適化のため、弾オブジェクトはプールで管理されます。

### シーケンス値の管理

BulletMLExecutorは以下のシーケンス値を保持します：

```csharp
private float m_LastSequenceDirection;      // sequence方向
private float m_LastSequenceSpeed;          // sequence速度  
private float m_LastSequenceHorizontalAccel; // sequence水平加速度
private float m_LastSequenceVerticalAccel;   // sequence垂直加速度
private float m_LastChangeSpeedSequence;     // changeSpeed専用sequence
```

### ターゲット追跡

`aim`タイプの方向計算：

```csharp
Vector3 toTarget = m_TargetPosition - bulletPosition;
float aimAngle = CalculateAngleFromVector(toTarget, m_CoordinateSystem);
float finalAngle = aimAngle + expressionValue;
```

---

## 🧪 テスト仕様

### テストカテゴリ

#### 1. 単体テスト
- **BulletMLParser**: XML解析の正確性
- **BulletMLExecutor**: コマンド実行の正確性  
- **ExpressionEvaluator**: 数式評価の正確性
- **BulletMLBullet**: 弾の動作と状態管理

#### 2. 統合テスト
- **XMLファイルテスト**: 実際のBulletMLファイルでの動作確認
- **弾幕パターンテスト**: 複雑なパターンの検証
- **座標系テスト**: XY/YZ座標系での動作確認

#### 3. パフォーマンステスト
- **大量弾管理**: 1000発以上の弾の同時処理
- **メモリ使用量**: オブジェクトプーリングの効果測定
- **フレームレート**: 60FPSの維持確認

### テストデータ

#### 基本パターン
```xml
<!-- プレイヤー狙い弾 -->
<fire><bullet/></fire>

<!-- 円形弾幕 -->
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

#### 複雑パターン
```xml
<!-- ホーミングレーザー -->
<bullet label="hmgLsr">
  <speed>2</speed>
  <action>
    <changeSpeed><speed>0.3</speed><term>30</term></changeSpeed>
    <wait>100</wait>
    <changeSpeed><speed>5</speed><term>100</term></changeSpeed>
  </action>
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

---

## 🔧 実装ノート

### 角度の正規化

```csharp
// 角度を0-360度の範囲に正規化
if (angle < 0f)
{
    angle += 360f;
}
```

ただし、aim計算では負の角度も有効な場合があります。

### フレームレート制御

```csharp
private const float TARGET_FPS = 60f;
private const float DELTA_TIME = 1f / TARGET_FPS;
```

BulletMLの`wait`コマンドは60FPSを前提としています。

### エラーハンドリング

- **XML解析エラー**: パース失敗時の適切なエラー報告
- **参照エラー**: 存在しないラベル参照の検出
- **数式エラー**: 不正な数式の検出と回復

---

## 📚 仕様書改訂履歴

| バージョン | 日付 | 変更内容 |
|-----------|------|----------|
| 1.0.0 | 2024/12 | 初版作成、BulletML 0.21準拠 |
| 1.1.0 | 2025/8 | 仕様書更新、技術詳細拡充 |

---

## 📞 サポート

技術的な質問や実装に関する相談は、プロジェクトのIssueまたはPull Requestでお願いします。

**参考文献:**
- [BulletML公式サイト](https://www.asahi-net.or.jp/~cs8k-cyu/bulletml/) (ABA Games)
- Unity Documentation
- プロジェクトテストスイート