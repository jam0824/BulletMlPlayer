using UnityEngine;

namespace BulletML
{
    /// <summary>
    /// BulletMLのtype属性の種類
    /// </summary>
    public enum BulletMLType
    {
        none,
        vertical,
        horizontal
    }

    /// <summary>
    /// direction要素のtype属性
    /// </summary>
    public enum DirectionType
    {
        aim,      // 自機を狙う方向
        absolute, // 絶対値（上方向が0で時計回り）
        relative, // この弾の方向が0の相対値
        sequence  // 直前の弾を撃った方向が0の相対値
    }

    /// <summary>
    /// speed要素のtype属性
    /// </summary>
    public enum SpeedType
    {
        absolute, // 絶対値
        relative, // 相対値
        sequence  // 連続変化
    }

    /// <summary>
    /// horizontal/vertical要素のtype属性
    /// </summary>
    public enum AccelType
    {
        absolute, // 絶対値
        relative, // 現在の弾の速度との相対値
        sequence  // 加速度を連続的に変化
    }

    /// <summary>
    /// 座標系の種類
    /// </summary>
    public enum CoordinateSystem
    {
        XY, // XY面（横スクロールシューティング用）：X軸が横、Y軸が縦
        YZ  // YZ面（縦スクロールシューティング用）：Y軸が縦、Z軸が前後
    }

    /// <summary>
    /// BulletML要素の種類
    /// </summary>
    public enum BulletMLElementType
    {
        bulletml,
        bullet,
        action,
        fire,
        changeDirection,
        changeSpeed,
        accel,
        wait,
        vanish,
        repeat,
        direction,
        speed,
        horizontal,
        vertical,
        term,
        times,
        bulletRef,
        actionRef,
        fireRef,
        param
    }
}