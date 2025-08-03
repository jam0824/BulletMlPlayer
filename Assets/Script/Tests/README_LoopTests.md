# BulletMLPlayer ループ機能テスト

## 概要

BulletMLPlayerのループ機能に対する包括的なテストスイートです。EditModeテストとPlayModeテストの両方を提供しています。

## テストファイル

### EditModeテスト
- **ファイル**: `EditMode/BulletMLPlayerLoopTests.cs`
- **説明**: ループ機能の基本的な動作をEditModeでテストします
- **特徴**: 高速実行、モック環境での詳細テスト

### PlayModeテスト
- **ファイル**: `PlayMode/BulletMLPlayerLoopPlayModeTests.cs`
- **説明**: 実際のUnityランタイム環境でのループ機能をテストします
- **特徴**: 実際のフレームサイクルを使用した統合テスト

## テスト実行方法

### Unity Test Runnerを使用

1. Unityエディタで `Window > General > Test Runner` を開く
2. `EditMode` タブまたは `PlayMode` タブを選択
3. テストツリーから実行したいテストを選択
4. `Run Selected` または `Run All` をクリック

### コマンドラインでの実行

```bash
# EditModeテストの実行
Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults results_editmode.xml

# PlayModeテストの実行
Unity -batchmode -quit -projectPath . -runTests -testPlatform PlayMode -testResults results_playmode.xml
```

## テストケース概要

### 基本機能テスト

- **ループ有効/無効設定**: `SetLoopEnabled()` の動作
- **遅延フレーム設定**: `SetLoopDelayFrames()` の動作
- **設定値取得**: ゲッター関数の動作

### 統合テスト

- **XML実行完了検知**: アクティブ弾数0でのループ開始検知
- **ループタイミング**: 設定した遅延フレーム後のループ実行
- **状態リセット**: `StartTopAction()` や `ClearAllBullets()` での状態リセット

### エッジケーステスト

- **ゼロ遅延**: `SetLoopDelayFrames(0)` での即座ループ
- **大きな遅延値**: 1000フレームなどの長期間遅延
- **実行時設定変更**: 実行中のループ設定変更

### パフォーマンステスト

- **連続ループ**: 複数回の連続ループ実行
- **メモリリーク検知**: 長時間実行でのメモリ使用量チェック
- **安定性テスト**: 長期間実行での動作安定性

### 複雑なパターンテスト

- **複雑なXML**: repeatや複数fireを含むパターンでのループ
- **手動再開**: ループ待機中の手動 `StartTopAction()` 呼び出し

## 期待される結果

すべてのテストが成功することで、以下が保証されます：

1. **基本機能の正確性**: ループ設定が正しく動作する
2. **タイミングの精度**: 設定した遅延フレーム後に正確にループする
3. **状態管理の正確性**: ループ状態が適切に管理される
4. **パフォーマンス**: メモリリークや不安定性がない
5. **互換性**: 様々なBulletMLパターンでループが動作する

## トラブルシューティング

### テストが失敗する場合

1. **フレームレート関連**: PlayModeテストでタイミングが合わない場合、フレームレートの設定を確認
2. **メモリ不足**: 大量ループテストでメモリ不足が発生する場合、テスト環境を確認
3. **Unity環境**: Test Runnerが正しく設定されているか確認

### デバッグ用ログ

テスト中にデバッグログを有効にするには：

```csharp
m_Player.SetDebugLogEnabled(true); // BulletMlPlayerでデバッグログを有効化
```

## カスタムテストの追加

新しいテストケースを追加する場合：

1. 適切なテストファイル（EditMode/PlayMode）を選択
2. 既存のテストパターンに従ってテストメソッドを作成
3. `[Test]` または `[UnityTest]` 属性を適用
4. Arrange-Act-Assert パターンに従う

### サンプルテスト

```csharp
[Test]
public void CustomLoopTest_YourCondition_ExpectedBehavior()
{
    // Arrange
    m_Player.SetLoopEnabled(true);
    m_Player.SetLoopDelayFrames(10);
    
    // Act
    // テスト対象の操作
    
    // Assert
    Assert.AreEqual(expectedValue, actualValue, "説明");
}
```