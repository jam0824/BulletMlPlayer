using System.Collections.Generic;
using UnityEngine;

namespace BulletML
{
    /// <summary>
    /// BulletMLコマンドを実行するクラス
    /// </summary>
    public class BulletMLExecutor
    {
        [SerializeField] private BulletMLDocument m_Document;
        [SerializeField] private ExpressionEvaluator m_ExpressionEvaluator;
        [SerializeField] private Vector3 m_TargetPosition;
        [SerializeField] private CoordinateSystem m_CoordinateSystem;
        [SerializeField] private float m_LastSequenceDirection;
        [SerializeField] private float m_LastSequenceSpeed;
        [SerializeField] private float m_LastSequenceHorizontalAccel;
        [SerializeField] private float m_LastSequenceVerticalAccel;
        [SerializeField] private float m_DefaultSpeed = 1f; // デフォルト速度
        [SerializeField] private float m_WaitTimeMultiplier = 1.0f; // wait時間の倍率
        [SerializeField] private float m_AngleOffset = 0.0f; // 全弾の角度にオフセットを加算
        
        // changeSpeed内専用のsequence値
        [SerializeField] private float m_LastChangeSpeedSequence = 0f;
        [SerializeField] private bool m_ChangeSpeedSequenceInitialized = false;
        
        /// <summary>
        /// 新しい弾が生成された時のコールバック
        /// </summary>
        public System.Action<BulletMLBullet> OnBulletCreated;

        public BulletMLDocument Document => m_Document;
        public Vector3 TargetPosition => m_TargetPosition;
        public CoordinateSystem CoordinateSystem => m_CoordinateSystem;
        public float WaitTimeMultiplier 
        { 
            get => m_WaitTimeMultiplier; 
            set => m_WaitTimeMultiplier = value; 
        }
        public float AngleOffset 
        { 
            get => m_AngleOffset; 
            set => m_AngleOffset = value; 
        }

        public BulletMLExecutor()
        {
            m_ExpressionEvaluator = new ExpressionEvaluator();
            m_TargetPosition = Vector3.zero;
            m_CoordinateSystem = CoordinateSystem.XY;
            m_LastSequenceDirection = 0f;
            m_LastSequenceSpeed = 1f;
            m_LastSequenceHorizontalAccel = 0f;
            m_LastSequenceVerticalAccel = 0f;
        }

        /// <summary>
        /// ドキュメントを設定する
        /// </summary>
        public void SetDocument(BulletMLDocument _document)
        {
            m_Document = _document;
            
            // ドキュメントのタイプに基づいて座標系を設定
            if (_document != null)
            {
                switch (_document.Type)
                {
                    case BulletMLType.vertical:
                        m_CoordinateSystem = CoordinateSystem.XY;
                        break;
                    case BulletMLType.horizontal:
                        m_CoordinateSystem = CoordinateSystem.YZ;
                        break;
                    default:
                        m_CoordinateSystem = CoordinateSystem.XY;
                        break;
                }
            }
        }

        /// <summary>
        /// ターゲット位置を設定する
        /// </summary>
        public void SetTargetPosition(Vector3 _targetPosition)
        {
            m_TargetPosition = _targetPosition;
        }

        /// <summary>
        /// 座標系を設定する
        /// </summary>
        public void SetCoordinateSystem(CoordinateSystem _coordinateSystem)
        {
            m_CoordinateSystem = _coordinateSystem;
        }

        /// <summary>
        /// ランク値（難易度）を設定する
        /// </summary>
        public void SetRankValue(float _rankValue)
        {
            m_ExpressionEvaluator.SetRankValue(_rankValue);
        }

        /// <summary>
        /// デフォルト速度を設定する
        /// </summary>
        public void SetDefaultSpeed(float _defaultSpeed)
        {
            m_DefaultSpeed = _defaultSpeed;
        }

        /// <summary>
        /// デフォルト速度を取得する
        /// </summary>
        public float GetDefaultSpeed()
        {
            return m_DefaultSpeed;
        }

        /// <summary>
        /// sequence値をリセットする（テスト用）
        /// </summary>
        public void ResetSequenceValues()
        {
            m_LastSequenceDirection = 0f;
            m_LastSequenceSpeed = 1f;
            m_LastSequenceHorizontalAccel = 0f;
            m_LastSequenceVerticalAccel = 0f;
            m_LastChangeSpeedSequence = 0f;
            m_ChangeSpeedSequenceInitialized = false;
        }

        /// <summary>
        /// fireコマンドを実行する
        /// </summary>
        public List<BulletMLBullet> ExecuteFireCommand(BulletMLElement _fireElement, BulletMLBullet _sourceBullet, Dictionary<int, float> _overrideParameters = null)
        {
            var newBullets = new List<BulletMLBullet>();

            if (_fireElement.ElementType != BulletMLElementType.fire)
            {
                Debug.LogError("Element is not a fire command");
                return newBullets;
            }

            // デフォルト値
            float direction = _sourceBullet.Direction;
            float speed = _sourceBullet.Speed;
            Vector3 position = _sourceBullet.Position;

            // direction要素を処理
            var directionElement = _fireElement.GetChild(BulletMLElementType.direction);
            bool isSequenceType = false;
            if (directionElement != null)
            {
                isSequenceType = directionElement.GetDirectionType() == DirectionType.sequence;
                direction = CalculateDirection(directionElement, _sourceBullet, false, _overrideParameters);
            }
            else
            {
                // direction要素が省略された場合のデフォルト処理
                // BulletML仕様：デフォルトは自機狙い（aim）
                Vector3 toTarget = m_TargetPosition - _sourceBullet.Position;
                
                // ゼロベクトルの場合（同じ位置）はデフォルト方向を使用
                if (toTarget.magnitude < 0.001f)
                {
                    Debug.LogWarning("fire direction省略: シューターとターゲットが同じ位置です。デフォルト方向を使用します。");
                    direction = NormalizeAngle(0f + m_AngleOffset); // デフォルト方向（上方向）
                }
                else
                {
                    direction = NormalizeAngle(CalculateAngleFromVector(toTarget, m_CoordinateSystem) + m_AngleOffset);
                }
            }

            // speed要素を処理
            var speedElement = _fireElement.GetChild(BulletMLElementType.speed);
            if (speedElement != null)
            {
                speed = CalculateSpeed(speedElement, _sourceBullet, false, _overrideParameters);
            }
            else
            {
                // speed要素が省略された場合のデフォルト処理
                // 親弾（シューター）の速度が0の場合は、デフォルト速度を使用
                if (_sourceBullet.Speed <= 0f)
                {
                    speed = m_DefaultSpeed; // デフォルト速度
                }
                else
                {
                    speed = _sourceBullet.Speed; // 親弾の速度を継承
                }
            }

            // bullet要素またはbulletRef要素を処理
            var bulletElement = _fireElement.GetChild(BulletMLElementType.bullet);
            var bulletRefElement = _fireElement.GetChild(BulletMLElementType.bulletRef);

            BulletMLElement actualBulletElement = null;

            if (bulletElement != null)
            {
                actualBulletElement = bulletElement;
            }
            else if (bulletRefElement != null)
            {
                // bulletRefを解決
                string label = bulletRefElement.GetAttribute("label");
                if (!string.IsNullOrEmpty(label) && m_Document != null)
                {
                    actualBulletElement = m_Document.GetLabeledBullet(label);
                    
                    // パラメータを設定
                    var paramElements = bulletRefElement.GetChildren(BulletMLElementType.param);
                    var parameters = new Dictionary<int, float>();
                    for (int i = 0; i < paramElements.Count; i++)
                    {
                        float paramValue = EvaluateExpression(paramElements[i].Value);
                        parameters[i + 1] = paramValue;
                    }
                    m_ExpressionEvaluator.SetParameters(parameters);
                }
            }

            // 弾を作成
            var newBullet = new BulletMLBullet(position, direction, speed, m_CoordinateSystem);
            
            // bulletの内容を適用
            if (actualBulletElement != null)
            {
                ApplyBulletElementInternal(actualBulletElement, newBullet, _overrideParameters);
            }

            // シーケンス値を更新
            // sequence typeの場合はCalculateDirection内で既に更新済み
            if (!isSequenceType)
            {
                m_LastSequenceDirection = direction;
            }
            m_LastSequenceSpeed = speed;

            newBullets.Add(newBullet);
            return newBullets;
        }

        /// <summary>
        /// 角度を-360°～360°の範囲に正規化する
        /// クルクル回る問題を防ぐため、360°を超えた時点で正規化
        /// </summary>
        private float NormalizeAngle(float angle)
        {
            // 360度を超えた場合は360度を引く
            while (angle > 360f)
                angle -= 360f;
            
            // -360度未満の場合は360度を足す
            while (angle < -360f)
                angle += 360f;
            
            return angle;
        }

        /// <summary>
        /// 方向を計算する
        /// </summary>
        private float CalculateDirection(BulletMLElement _directionElement, BulletMLBullet _sourceBullet, bool _isInChangeDirection = false, Dictionary<int, float> _overrideParameters = null)
        {
            // パラメータを使用してExpression評価
            float value;
            if (_overrideParameters != null && _overrideParameters.Count > 0)
            {
                // overrideParametersがある場合はそれを使用
                var originalParameters = m_ExpressionEvaluator.GetParameters();
                m_ExpressionEvaluator.SetParameters(_overrideParameters);
                value = EvaluateExpression(_directionElement.Value);
                m_ExpressionEvaluator.SetParameters(originalParameters);
            }
            else
            {
                // 弾のactionRunnerのパラメータを使用してExpression評価
                value = EvaluateExpressionWithBulletParameters(_directionElement.Value, _sourceBullet);
            }
            var directionType = _directionElement.GetDirectionType();

            switch (directionType)
            {
                case DirectionType.aim:
                    // 自機を狙う方向を計算
                    Vector3 toTarget = m_TargetPosition - _sourceBullet.Position;
                    
                    // ゼロベクトルの場合（同じ位置）はデフォルト方向を使用
                    if (toTarget.magnitude < 0.001f)
                    {
                        Debug.LogWarning("aim direction: シューターとターゲットが同じ位置です。デフォルト方向を使用します。");
                        float defaultAngle = value + m_AngleOffset;
                        return NormalizeAngle(defaultAngle);
                    }
                    
                    float aimAngle = CalculateAngleFromVector(toTarget, m_CoordinateSystem);
                    float finalAngle = aimAngle + value + m_AngleOffset;
                    return NormalizeAngle(finalAngle);

                case DirectionType.absolute:
                    return NormalizeAngle(value + m_AngleOffset);

                case DirectionType.relative:
                    return NormalizeAngle(_sourceBullet.Direction + value + m_AngleOffset);

                case DirectionType.sequence:
                    if (_isInChangeDirection)
                    {
                        // changeDirection要素内では方向を連続的に変化させる
                        float oldSequenceDirection = m_LastSequenceDirection;
                        float newDirection = m_LastSequenceDirection + value;
                        float normalizedDirection = NormalizeAngle(newDirection);
                        m_LastSequenceDirection = normalizedDirection;
                        
                        return NormalizeAngle(normalizedDirection + m_AngleOffset);
                    }
                    else
                    {
                        // fire要素内では累積的に方向を変化させる
                        float oldSequenceDirection = m_LastSequenceDirection;
                        float newDirection = m_LastSequenceDirection + value;
                        float normalizedDirection = NormalizeAngle(newDirection);
                        m_LastSequenceDirection = normalizedDirection;
                        
                        return NormalizeAngle(normalizedDirection + m_AngleOffset);
                    }

                default:
                    return NormalizeAngle(value + m_AngleOffset);
            }
        }

        /// <summary>
        /// 速度を計算する
        /// </summary>
        private float CalculateSpeed(BulletMLElement _speedElement, BulletMLBullet _sourceBullet, bool _isInChangeSpeed = false, Dictionary<int, float> _overrideParameters = null)
        {
            // パラメータを使用してExpression評価
            float value;
            if (_overrideParameters != null && _overrideParameters.Count > 0)
            {
                // overrideParametersがある場合はそれを使用
                var originalParameters = m_ExpressionEvaluator.GetParameters();
                m_ExpressionEvaluator.SetParameters(_overrideParameters);
                value = EvaluateExpression(_speedElement.Value);
                m_ExpressionEvaluator.SetParameters(originalParameters);
            }
            else
            {
                // 弾のactionRunnerのパラメータを使用してExpression評価
                value = EvaluateExpressionWithBulletParameters(_speedElement.Value, _sourceBullet);
            }
            var speedType = _speedElement.GetSpeedType();

            switch (speedType)
            {
                case SpeedType.absolute:
                    return value;

                case SpeedType.relative:
                    return _sourceBullet.Speed + value;

                case SpeedType.sequence:
                    if (_isInChangeSpeed)
                    {
                        // changeSpeed要素内では弾の速度を連続的に変化させる
                        // 初回実行時のみ弾の現在速度で初期化
                        if (!m_ChangeSpeedSequenceInitialized)
                        {
                            m_LastChangeSpeedSequence = _sourceBullet.Speed;
                            m_ChangeSpeedSequenceInitialized = true;
                        }
                        
                        // changeSpeed専用のシーケンス値を累積更新
                        m_LastChangeSpeedSequence += value;
                        return m_LastChangeSpeedSequence;
                    }
                    else
                    {
                        // それ以外の要素内では直前の弾の速度との相対値
                        return m_LastSequenceSpeed + value;
                    }

                default:
                    return value;
            }
        }

        /// <summary>
        /// ベクトルから角度を計算する
        /// </summary>
        private float CalculateAngleFromVector(Vector3 _vector, CoordinateSystem _coordinateSystem)
        {
            float angle = 0f;
            
            switch (_coordinateSystem)
            {
                case CoordinateSystem.XY:
                    angle = Mathf.Atan2(_vector.x, _vector.y) * Mathf.Rad2Deg;
                    break;

                case CoordinateSystem.YZ:
                    angle = Mathf.Atan2(_vector.z, _vector.y) * Mathf.Rad2Deg;
                    break;

                default:
                    angle = 0f;
                    break;
            }
            
            // 角度を0-360度の範囲に正規化
            if (angle < 0f)
            {
                angle += 360f;
            }
            
            return angle;
        }

        /// <summary>
        /// bullet要素を弾に適用する（テスト用public版）
        /// </summary>
        public void ApplyBulletElement(BulletMLElement _bulletElement, BulletMLBullet _bullet)
        {
            ApplyBulletElementInternal(_bulletElement, _bullet, null);
        }

        /// <summary>
        /// bullet要素を弾に適用する（内部実装）
        /// </summary>
        private void ApplyBulletElementInternal(BulletMLElement _bulletElement, BulletMLBullet _bullet, Dictionary<int, float> _overrideParameters = null)
        {
            // 1. 最初にaction要素とactionRef要素を追加（パラメータを持つactionRunnerを先にpush）
            var actionElements = _bulletElement.GetChildren(BulletMLElementType.action);
            foreach (var actionElement in actionElements)
            {
                // overrideParametersがあれば使用、なければ現在のExpressionEvaluatorのパラメータを使用
                var parametersToUse = _overrideParameters ?? m_ExpressionEvaluator.GetParameters();
                var actionRunner = new BulletMLActionRunner(actionElement, parametersToUse);
                _bullet.PushAction(actionRunner);
            }

            var actionRefElements = _bulletElement.GetChildren(BulletMLElementType.actionRef);
            foreach (var actionRefElement in actionRefElements)
            {
                string label = actionRefElement.GetAttribute("label");
                if (!string.IsNullOrEmpty(label) && m_Document != null)
                {
                    var referencedAction = m_Document.GetLabeledAction(label);
                    if (referencedAction != null)
                    {
                        // パラメータを設定
                        var paramElements = actionRefElement.GetChildren(BulletMLElementType.param);
                        var parameters = new Dictionary<int, float>();
                        for (int i = 0; i < paramElements.Count; i++)
                        {
                            float paramValue = EvaluateExpression(paramElements[i].Value);
                            parameters[i + 1] = paramValue;
                        }

                        var actionRunner = new BulletMLActionRunner(referencedAction, parameters);
                        _bullet.PushAction(actionRunner);
                    }
                }
            }

                            // 2. actionRunner追加後にdirection要素を適用
                var directionElement = _bulletElement.GetChild(BulletMLElementType.direction);
                if (directionElement != null)
                {
                    float direction = CalculateDirection(directionElement, _bullet, false, _overrideParameters);
                    _bullet.SetDirection(direction);
                }

                // 3. actionRunner追加後にspeed要素を適用
                var speedElement = _bulletElement.GetChild(BulletMLElementType.speed);
                if (speedElement != null)
                {
                    float speed = CalculateSpeed(speedElement, _bullet, false, _overrideParameters);
                    _bullet.SetSpeed(speed);
                }
        }

        /// <summary>
        /// 現在のアクションを実行する
        /// </summary>
        /// <returns>アクションが継続中の場合true、完了した場合false</returns>
        public bool ExecuteCurrentAction(BulletMLBullet _bullet)
        {
            var currentAction = _bullet.GetCurrentAction();
            if (currentAction == null)
            {
                // Debug.Log($"[ExecuteCurrentAction] 弾にアクションなし: 位置={_bullet.Position}");
                return false;
            }
            
            // Debug.Log($"[ExecuteCurrentAction] アクション実行中: CurrentIndex={currentAction.CurrentIndex}, IsFinished={currentAction.IsFinished}, ActionStackSize={_bullet.ActionStack.Count}");
            
            // wait中の場合
            if (currentAction.WaitFrames > 0)
            {
                currentAction.DecrementWaitFrames();
                return true;
            }

            // アクションが完了している場合
            if (currentAction.IsFinished)
            {
                // Debug.Log($"[ExecuteCurrentAction] アクション完了してpop: 残りActionStackSize={_bullet.ActionStack.Count - 1}");
                _bullet.PopAction();
                return _bullet.GetCurrentAction() != null;
            }

            // 現在のコマンドを取得
            var actionElement = currentAction.ActionElement;
            if (actionElement == null || actionElement.Children == null)
            {
                // アクション要素が無効な場合は完了扱い
                currentAction.Finish();
                _bullet.PopAction();
                return _bullet.GetCurrentAction() != null;
            }
            
            if (currentAction.CurrentIndex >= actionElement.Children.Count)
            {
                // アクション完了
                // Debug.Log($"[ExecuteCurrentAction] アクション範囲外で完了: CurrentIndex={currentAction.CurrentIndex}, ChildrenCount={actionElement.Children.Count}");
                currentAction.Finish();
                _bullet.PopAction();
                return _bullet.GetCurrentAction() != null;
            }

            var currentCommand = actionElement.Children[currentAction.CurrentIndex];
            // Debug.Log($"[ExecuteCurrentAction] コマンド実行: {currentCommand.ElementType} (Index={currentAction.CurrentIndex})");

            bool commandResult = ExecuteCommand(currentCommand, _bullet, currentAction);
            
            // コマンド実行後にインデックスを増加（waitの場合はWaitFrames設定後に増加）
            currentAction.IncrementIndex();
            
            // コマンド実行後にアクションが完了状態になった場合、即座に処理
            if (currentAction.IsFinished)
            {
                // Debug.Log($"[ExecuteCurrentAction] コマンド実行後に完了してpop: 残りActionStackSize={_bullet.ActionStack.Count - 1}");
                _bullet.PopAction();
                return _bullet.GetCurrentAction() != null;
            }
            
            return commandResult;
        }

        /// <summary>
        /// コマンドを実行する
        /// </summary>
        private bool ExecuteCommand(BulletMLElement _command, BulletMLBullet _bullet, BulletMLActionRunner _actionRunner)
        {
            
            switch (_command.ElementType)
            {
                case BulletMLElementType.wait:
                    return ExecuteWaitCommand(_command, _bullet, _actionRunner);

                case BulletMLElementType.vanish:
                    return ExecuteVanishCommand(_command, _bullet);

                case BulletMLElementType.repeat:
                    return ExecuteRepeatCommand(_command, _bullet, _actionRunner);

                case BulletMLElementType.fire:
                    return ExecuteFireCommandWithBullet(_command, _bullet);

                case BulletMLElementType.fireRef:
                    return ExecuteFireRefCommand(_command, _bullet, _actionRunner);

                case BulletMLElementType.changeDirection:
                    return ExecuteChangeDirectionCommand(_command, _bullet, _actionRunner);

                case BulletMLElementType.changeSpeed:
                    return ExecuteChangeSpeedCommand(_command, _bullet, _actionRunner);

                case BulletMLElementType.accel:
                    return ExecuteAccelCommand(_command, _bullet, _actionRunner);

                case BulletMLElementType.action:
                    return ExecuteActionCommand(_command, _bullet, _actionRunner);

                case BulletMLElementType.actionRef:
                    return ExecuteActionRefCommand(_command, _bullet, _actionRunner);

                default:
                    Debug.LogWarning($"Unhandled command type: {_command.ElementType}");
                    return true;
            }
        }

        /// <summary>
        /// waitコマンドを実行する
        /// </summary>
        private bool ExecuteWaitCommand(BulletMLElement _waitElement, BulletMLBullet _bullet, BulletMLActionRunner _actionRunner)
        {
            // パラメータを設定
            m_ExpressionEvaluator.SetParameters(_actionRunner.Parameters);
            
            // XML値を評価してwait倍率を適用
            float rawWaitValue = EvaluateExpression(_waitElement.Value);
            float adjustedWaitValue = rawWaitValue * m_WaitTimeMultiplier;
            int waitFrames = Mathf.RoundToInt(adjustedWaitValue);
            _actionRunner.SetWaitFrames(waitFrames);
            
            return true;
        }

        /// <summary>
        /// vanishコマンドを実行する
        /// </summary>
        private bool ExecuteVanishCommand(BulletMLElement _vanishElement, BulletMLBullet _bullet)
        {
            _bullet.Vanish();
            return false; // 弾が消えたのでアクション終了
        }

        /// <summary>
        /// repeatコマンドを実行する
        /// </summary>
        private bool ExecuteRepeatCommand(BulletMLElement _repeatElement, BulletMLBullet _bullet, BulletMLActionRunner _actionRunner)
        {
            var timesElement = _repeatElement.GetChild(BulletMLElementType.times);
            var actionElement = _repeatElement.GetChild(BulletMLElementType.action);

            if (timesElement == null || actionElement == null)
            {
                Debug.LogError("[ExecuteRepeatCommand] Invalid repeat command structure");
                return true;
            }

            // パラメータを設定
            m_ExpressionEvaluator.SetParameters(_actionRunner.Parameters);
            
            int repeatCount = Mathf.RoundToInt(EvaluateExpression(timesElement.Value));
            // Debug.Log($"[ExecuteRepeatCommand] repeat回数: {repeatCount}");
            
            if (repeatCount <= 0)
            {
                // repeat回数が0の場合、現在のアクションを完了させる
                _actionRunner.Finish();
                return true; // repeatしない
            }

            // repeatCountの回数だけアクションを積む
            for (int i = 0; i < repeatCount; i++)
            {
                var repeatActionRunner = new BulletMLActionRunner(actionElement, _actionRunner.Parameters);
                _bullet.PushAction(repeatActionRunner);
                // Debug.Log($"[ExecuteRepeatCommand] actionRunner {i+1}/{repeatCount} をpush, 現在のスタックサイズ: {_bullet.ActionStack.Count}");
            }
            
            return true;
        }

        /// <summary>
        /// fireコマンドを実行する（弾からの発射）
        /// </summary>
        private bool ExecuteFireCommandWithBullet(BulletMLElement _fireElement, BulletMLBullet _bullet)
        {
            var newBullets = ExecuteFireCommand(_fireElement, _bullet);
            
            // 新しい弾をコールバック経由で通知
            foreach (var newBullet in newBullets)
            {
                OnBulletCreated?.Invoke(newBullet);
            }
            
            return true;
        }

        /// <summary>
        /// actionコマンドを実行する
        /// </summary>
        private bool ExecuteActionCommand(BulletMLElement _actionElement, BulletMLBullet _bullet, BulletMLActionRunner _actionRunner)
        {
            var newActionRunner = new BulletMLActionRunner(_actionElement, _actionRunner.Parameters);
            _bullet.PushAction(newActionRunner);
            return true;
        }

        /// <summary>
        /// actionRefコマンドを実行する
        /// </summary>
        private bool ExecuteActionRefCommand(BulletMLElement _actionRefElement, BulletMLBullet _bullet, BulletMLActionRunner _actionRunner)
        {
            string label = _actionRefElement.GetAttribute("label");
            if (string.IsNullOrEmpty(label) || m_Document == null)
            {
                Debug.LogError("Invalid actionRef command");
                return true;
            }

            var referencedAction = m_Document.GetLabeledAction(label);
            if (referencedAction == null)
            {
                Debug.LogError($"Referenced action not found: {label}");
                return true;
            }

            // パラメータを設定
            var paramElements = _actionRefElement.GetChildren(BulletMLElementType.param);
            var parameters = new Dictionary<int, float>();
            for (int i = 0; i < paramElements.Count; i++)
            {
                m_ExpressionEvaluator.SetParameters(_actionRunner.Parameters);
                float paramValue = EvaluateExpression(paramElements[i].Value);
                parameters[i + 1] = paramValue;
            }

            var newActionRunner = new BulletMLActionRunner(referencedAction, parameters);
            _bullet.PushAction(newActionRunner);
            return true;
        }

        /// <summary>
        /// fireRefコマンドを実行する
        /// </summary>
        private bool ExecuteFireRefCommand(BulletMLElement _fireRefElement, BulletMLBullet _bullet, BulletMLActionRunner _actionRunner)
        {
            string label = _fireRefElement.GetAttribute("label");
            // Debug.Log($"[ExecuteFireRefCommand] fireRef '{label}' を実行開始");
            
            if (string.IsNullOrEmpty(label) || m_Document == null)
            {
                Debug.LogError("Invalid fireRef command");
                return true;
            }

            var referencedFire = m_Document.GetLabeledFire(label);
            if (referencedFire == null)
            {
                Debug.LogError($"Referenced fire not found: {label}");
                return true;
            }

            // パラメータを設定
            var paramElements = _fireRefElement.GetChildren(BulletMLElementType.param);
            var parameters = new Dictionary<int, float>();
            for (int i = 0; i < paramElements.Count; i++)
            {
                m_ExpressionEvaluator.SetParameters(_actionRunner.Parameters);
                float paramValue = EvaluateExpression(paramElements[i].Value);
                parameters[i + 1] = paramValue;
            }

            // 元のパラメータを保存
            var originalParameters = m_ExpressionEvaluator.GetParameters();
            
            // 新しいパラメータを設定
            m_ExpressionEvaluator.SetParameters(parameters);

            // fire要素を実行（fireRefのパラメータを引き継ぎ）
            var newBullets = ExecuteFireCommand(referencedFire, _bullet, parameters);
            // Debug.Log($"[ExecuteFireRefCommand] fireRef '{label}' で{newBullets.Count}個の弾を生成");
            
            // 新しい弾をコールバック経由で通知
            foreach (var newBullet in newBullets)
            {
                OnBulletCreated?.Invoke(newBullet);
            }

            // 元のパラメータを復元
            m_ExpressionEvaluator.SetParameters(originalParameters);
            
            return true;
        }

        /// <summary>
        /// changeDirectionコマンドを実行する
        /// </summary>
        private bool ExecuteChangeDirectionCommand(BulletMLElement _changeDirectionElement, BulletMLBullet _bullet, BulletMLActionRunner _actionRunner)
        {
            var directionElement = _changeDirectionElement.GetChild(BulletMLElementType.direction);
            var termElement = _changeDirectionElement.GetChild(BulletMLElementType.term);

            if (directionElement == null || termElement == null)
            {
                Debug.LogError("Invalid changeDirection command structure");
                return true;
            }

            // パラメータを設定
            m_ExpressionEvaluator.SetParameters(_actionRunner.Parameters);

            // ターゲット方向を計算（changeDirection要素内であることを明示）
            float targetDirection = CalculateDirection(directionElement, _bullet, true);
            int duration = Mathf.RoundToInt(EvaluateExpression(termElement.Value));

            // 方向変更を開始
            var directionChange = _bullet.DirectionChange;
            directionChange.ChangeType = BulletMLChangeType.Direction;
            directionChange.StartValue = _bullet.Direction;
            directionChange.TargetValue = targetDirection;
            directionChange.Duration = duration;
            directionChange.CurrentFrame = 0;
            directionChange.IsActive = true;

            return true;
        }

        /// <summary>
        /// changeSpeedコマンドを実行する
        /// </summary>
        private bool ExecuteChangeSpeedCommand(BulletMLElement _changeSpeedElement, BulletMLBullet _bullet, BulletMLActionRunner _actionRunner)
        {
            var speedElement = _changeSpeedElement.GetChild(BulletMLElementType.speed);
            var termElement = _changeSpeedElement.GetChild(BulletMLElementType.term);

            if (speedElement == null || termElement == null)
            {
                Debug.LogError("Invalid changeSpeed command structure");
                return true;
            }

            // パラメータを設定
            m_ExpressionEvaluator.SetParameters(_actionRunner.Parameters);

            // ターゲット速度を計算（changeSpeed要素内であることを明示）
            float targetSpeed = CalculateSpeed(speedElement, _bullet, true);
            int duration = Mathf.RoundToInt(EvaluateExpression(termElement.Value));

            // 速度変更を開始
            var speedChange = _bullet.SpeedChange;
            speedChange.ChangeType = BulletMLChangeType.Speed;
            speedChange.StartValue = _bullet.Speed;
            speedChange.TargetValue = targetSpeed;
            speedChange.Duration = duration;
            speedChange.CurrentFrame = 0;
            speedChange.IsActive = true;

            return true;
        }

        /// <summary>
        /// accelコマンドを実行する
        /// </summary>
        private bool ExecuteAccelCommand(BulletMLElement _accelElement, BulletMLBullet _bullet, BulletMLActionRunner _actionRunner)
        {
            var horizontalElement = _accelElement.GetChild(BulletMLElementType.horizontal);
            var verticalElement = _accelElement.GetChild(BulletMLElementType.vertical);
            var termElement = _accelElement.GetChild(BulletMLElementType.term);

            if (termElement == null)
            {
                Debug.LogError("Invalid accel command structure - missing term");
                return true;
            }

            // パラメータを設定
            m_ExpressionEvaluator.SetParameters(_actionRunner.Parameters);

            float horizontalAccel = 0f;
            float verticalAccel = 0f;

            if (horizontalElement != null)
            {
                horizontalAccel = CalculateAcceleration(horizontalElement, _bullet, true);
            }

            if (verticalElement != null)
            {
                verticalAccel = CalculateAcceleration(verticalElement, _bullet, false);
            }

            int duration = Mathf.RoundToInt(EvaluateExpression(termElement.Value));

            // 加速度を開始
            var accelInfo = _bullet.AccelInfo;
            
            accelInfo.HorizontalAccel = horizontalAccel;
            accelInfo.VerticalAccel = verticalAccel;
            accelInfo.Duration = duration;
            accelInfo.CurrentFrame = 0;
            accelInfo.IsActive = true;

            return true;
        }

        /// <summary>
        /// 加速度を計算する
        /// </summary>
        private float CalculateAcceleration(BulletMLElement _accelElement, BulletMLBullet _bullet, bool _isHorizontal)
        {
            float value = EvaluateExpression(_accelElement.Value);
            var accelType = _accelElement.GetAccelType();

            switch (accelType)
            {
                case AccelType.absolute:
                    return value;

                case AccelType.relative:
                    // 現在の加速度との相対値
                    float currentAccel = _isHorizontal ? _bullet.AccelInfo.HorizontalAccel : _bullet.AccelInfo.VerticalAccel;
                    return currentAccel + value;

                case AccelType.sequence:
                    // 連続的に変化する加速度を計算
                    float lastAccel = _isHorizontal ? m_LastSequenceHorizontalAccel : m_LastSequenceVerticalAccel;
                    float newAccel = lastAccel + value;
                    
                    
                    // 最後の値を更新
                    if (_isHorizontal)
                    {
                        m_LastSequenceHorizontalAccel = newAccel;
                    }
                    else
                    {
                        m_LastSequenceVerticalAccel = newAccel;
                    }
                    
                    return newAccel;

                default:
                    return value;
            }
        }

        /// <summary>
        /// 変更値を計算する（テスト用）
        /// </summary>
        public float CalculateChangeValue(BulletMLChangeInfo _changeInfo)
        {
            if (_changeInfo.CurrentFrame >= _changeInfo.Duration)
            {
                return _changeInfo.TargetValue;
            }

            float t = (float)_changeInfo.CurrentFrame / _changeInfo.Duration;
            return Mathf.Lerp(_changeInfo.StartValue, _changeInfo.TargetValue, t);
        }

        /// <summary>
        /// 加速度を計算する（テスト用）
        /// </summary>
        public Vector3 CalculateAcceleration(BulletMLAccelInfo _accelInfo)
        {
            if (_accelInfo.CurrentFrame >= _accelInfo.Duration)
            {
                return new Vector3(_accelInfo.HorizontalAccel, _accelInfo.VerticalAccel, 0f);
            }

            float t = (float)_accelInfo.CurrentFrame / _accelInfo.Duration;
            return new Vector3(
                _accelInfo.HorizontalAccel * t,
                _accelInfo.VerticalAccel * t,
                0f
            );
        }

        /// <summary>
        /// 数式を評価する
        /// </summary>
        private float EvaluateExpression(string _expression)
        {
            if (string.IsNullOrEmpty(_expression))
                return 0f;

            return m_ExpressionEvaluator.Evaluate(_expression);
        }

        /// <summary>
        /// 弾のactionRunnerのパラメータを使用してExpression評価する
        /// </summary>
        private float EvaluateExpressionWithBulletParameters(string _expression, BulletMLBullet _bullet)
        {
            if (string.IsNullOrEmpty(_expression))
                return 0f;

            // 現在のパラメータを保存
            var originalParameters = m_ExpressionEvaluator.GetParameters();
            
            // 弾のactionRunnerのパラメータを取得
            var currentAction = _bullet.GetCurrentAction();
            if (currentAction?.Parameters != null && currentAction.Parameters.Count > 0)
            {
                // 弾のパラメータを一時的に設定
                m_ExpressionEvaluator.SetParameters(currentAction.Parameters);
            }
            
            // Expression評価
            float result = m_ExpressionEvaluator.Evaluate(_expression);
            
            // 元のパラメータを復元
            m_ExpressionEvaluator.SetParameters(originalParameters);
            
            return result;
        }
    }
}