# オフセット不使用でのBulletML実行方法

## 設定手順

1. BulletMlPlayerを選択
2. Inspectorで以下を設定：
   - Use Shooter Offset: ✓ を外す（false）
   - Player Position: シューターと異なる位置を設定
     例: YZ面の場合 (0, -3, -5)
     例: XY面の場合 (2, -3, 0)

## 動作説明

- Use Shooter Offset = false の場合：
  - シューター位置 = transform.position（BulletMLPlayerの位置）
  - プレイヤー位置 = Player Positionで設定した値
  
- Use Shooter Offset = true の場合：
  - シューター位置 = transform.position + Shooter Offset
  - プレイヤー位置 = Player Positionで設定した値

## 推奨設定

### YZ面（縦スクロール）
- BulletMLPlayer位置: (0, 0, 0)
- Player Position: (0, -3, -5)
- 結果: 弾が下方向に向かって飛ぶ

### XY面（横スクロール）
- BulletMLPlayer位置: (0, 0, 0)  
- Player Position: (2, -3, 0)
- 結果: 弾が右下方向に向かって飛ぶ