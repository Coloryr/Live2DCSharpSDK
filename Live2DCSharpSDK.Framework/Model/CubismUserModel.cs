using Live2DCSharpSDK.Framework.Effect;
using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Motion;
using Live2DCSharpSDK.Framework.Physics;
using Live2DCSharpSDK.Framework.Rendering;

namespace Live2DCSharpSDK.Framework.Model;

/// <summary>
/// ユーザーが実際に使用するモデルの基底クラス。これを継承してユーザーが実装する。
/// </summary>
public class CubismUserModel : IDisposable
{
    /// <summary>
    /// レンダラ
    /// </summary>
    private CubismRenderer _renderer;

    /// <summary>
    /// Mocデータ
    /// </summary>
    protected CubismMoc _moc;
    /// <summary>
    /// Modelインスタンス
    /// </summary>
    protected CubismModel _model;

    /// <summary>
    /// モーション管理
    /// </summary>
    protected CubismMotionManager _motionManager;
    /// <summary>
    /// 表情管理
    /// </summary>
    protected CubismMotionManager _expressionManager;
    /// <summary>
    /// 自動まばたき
    /// </summary>
    protected CubismEyeBlink _eyeBlink;
    /// <summary>
    /// 呼吸
    /// </summary>
    protected CubismBreath _breath;
    /// <summary>
    /// モデル行列
    /// </summary>
    protected CubismModelMatrix _modelMatrix;
    /// <summary>
    /// ポーズ管理
    /// </summary>
    protected CubismPose _pose;
    /// <summary>
    /// マウスドラッグ
    /// </summary>
    protected CubismTargetPoint _dragManager;
    /// <summary>
    /// 物理演算
    /// </summary>
    protected CubismPhysics _physics;
    /// <summary>
    /// ユーザデータ
    /// </summary>
    protected CubismModelUserData _modelUserData;

    /// <summary>
    /// 初期化されたかどうか
    /// </summary>
    protected bool _initialized;
    /// <summary>
    /// 更新されたかどうか
    /// </summary>
    protected bool _updating;
    /// <summary>
    /// 不透明度
    /// </summary>
    protected float _opacity;
    /// <summary>
    /// リップシンクするかどうか
    /// </summary>
    protected bool _lipSync;
    /// <summary>
    /// 最後のリップシンクの制御値
    /// </summary>
    protected float _lastLipSyncValue;
    /// <summary>
    /// マウスドラッグのX位置
    /// </summary>
    protected float _dragX;
    /// <summary>
    /// マウスドラッグのY位置
    /// </summary>
    protected float _dragY;
    /// <summary>
    /// X軸方向の加速度
    /// </summary>
    protected float _accelerationX;
    /// <summary>
    /// Y軸方向の加速度
    /// </summary>
    protected float _accelerationY;
    /// <summary>
    /// Z軸方向の加速度
    /// </summary>
    protected float _accelerationZ;
    /// <summary>
    /// MOC3整合性検証するかどうか
    /// </summary>
    protected bool _mocConsistency;
    /// <summary>
    /// デバッグモードかどうか
    /// </summary>
    protected bool _debugMode;

    /// <summary>
    /// CubismMotionQueueManagerにイベント用に登録するためのCallback。
    /// CubismUserModelの継承先のEventFiredを呼ぶ。
    /// </summary>
    /// <param name="caller">発火したイベントを管理していたモーションマネージャー、比較用</param>
    /// <param name="eventValue">発火したイベントの文字列データ</param>
    /// <param name="customData">CubismUserModelを継承したインスタンスを想定</param>
    public static void CubismDefaultMotionEventCallback(CubismMotionQueueManager caller, string eventValue, dynamic customData)
    {
        CubismUserModel model = customData as CubismUserModel;
        if (model != null)
        {
            model.MotionEventFired(eventValue);
        }
    }

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    public CubismUserModel()
    {
        _opacity = 1.0f;

        // モーションマネージャーを作成
        // MotionQueueManagerクラスからの継承なので使い方は同じ
        _motionManager = new CubismMotionManager();
        _motionManager.SetEventCallback(CubismDefaultMotionEventCallback, this);

        // 表情モーションマネージャを作成
        _expressionManager = new CubismMotionManager();

        // ドラッグによるアニメーション
        _dragManager = new CubismTargetPoint();
    }

    public void Dispose()
    {
        _moc?.DeleteModel(_model);

        DeleteRenderer();
    }

    /// <summary>
    /// 初期化されている状態か？
    /// </summary>
    /// <returns>true    初期化されている
    /// false   初期化されていない</returns>
    public bool IsInitialized()
    {
        return _initialized;
    }

    /// <summary>
    /// 初期化状態を設定する。
    /// </summary>
    /// <param name="v">初期化状態</param>
    public void IsInitialized(bool v)
    {
        _initialized = v;
    }

    /// <summary>
    /// 更新されている状態か？
    /// </summary>
    /// <returns>true    更新されている
    /// false   更新されていない</returns>
    public bool IsUpdating()
    {
        return _updating;
    }

    /// <summary>
    /// 更新状態を設定する。
    /// </summary>
    /// <param name="v">更新状態</param>
    public void IsUpdating(bool v)
    {
        _updating = v;
    }

    /// <summary>
    /// マウスドラッグの情報を設定する。
    /// </summary>
    /// <param name="x">ドラッグしているカーソルのX位置</param>
    /// <param name="y">ドラッグしているカーソルのY位置</param>
    public void SetDragging(float x, float y)
    {
        _dragManager.Set(x, y);
    }

    /// <summary>
    /// 加速度の情報を設定する。
    /// </summary>
    /// <param name="x">X軸方向の加速度</param>
    /// <param name="y">Y軸方向の加速度</param>
    /// <param name="z">Z軸方向の加速度</param>
    public void SetAcceleration(float x, float y, float z)
    {
        _accelerationX = x;
        _accelerationY = y;
        _accelerationZ = z;
    }

    /// <summary>
    /// モデル行列を取得する。
    /// </summary>
    /// <returns>モデル行列</returns>
    public CubismModelMatrix GetModelMatrix()
    {
        return _modelMatrix;
    }

    /// <summary>
    /// 不透明度を設定する。
    /// </summary>
    /// <param name="a">不透明度</param>
    public void SetOpacity(float a)
    {
        _opacity = a;
    }

    /// <summary>
    /// 不透明度を取得する。
    /// </summary>
    /// <returns>不透明度</returns>
    public float GetOpacity()
    {
        return _opacity;
    }

    /// <summary>
    /// モデルデータを読み込む。
    /// </summary>
    /// <param name="buffer">moc3ファイルが読み込まれているバッファ</param>
    /// <param name="shouldCheckMocConsistency">MOCの整合性チェックフラグ(初期値 : false)</param>
    public void LoadModel(byte[] buffer, bool shouldCheckMocConsistency = false)
    {
        _moc = CubismMoc.Create(buffer, shouldCheckMocConsistency);

        if (_moc == null)
        {
            CubismLog.CubismLogError("Failed to CubismMoc::Create().");
            return;
        }

        _model = _moc.CreateModel();

        if (_model == null)
        {
            CubismLog.CubismLogError("Failed to CreateModel().");
            return;
        }

        _model.SaveParameters();
        _modelMatrix = new CubismModelMatrix(_model.GetCanvasWidth(), _model.GetCanvasHeight());
    }

    /// <summary>
    /// モーションデータを読み込む。
    /// </summary>
    /// <param name="buffer">motion3.jsonファイルが読み込まれているバッファ</param>
    /// <param name="name">モーションの名前</param>
    /// <param name="onFinishedMotionHandler">モーション再生終了時に呼び出されるコールバック関数。NULLの場合、呼び出されない。</param>
    /// <returns>モーションクラス</returns>
    public ACubismMotion LoadMotion(string buffer, string name, FinishedMotionCallback onFinishedMotionHandler = null)
    {
        return new CubismMotion(buffer, onFinishedMotionHandler);
    }

    /// <summary>
    /// 表情データを読み込む。
    /// </summary>
    /// <param name="buffer">expファイルが読み込まれているバッファ</param>
    /// <param name="name">表情の名前</param>
    /// <returns>表情データを読み込む。</returns>
    public ACubismMotion LoadExpression(string buffer, string name)
    {
        return new CubismExpressionMotion(buffer);
    }

    /// <summary>
    /// ポーズデータを読み込む。
    /// </summary>
    /// <param name="buffer">pose3.jsonが読み込まれているバッファ</param>
    public void LoadPose(string buffer)
    {
        _pose = new CubismPose(buffer);
    }

    /// <summary>
    /// 物理演算データを読み込む。
    /// </summary>
    /// <param name="buffer">physics3.jsonが読み込まれているバッファ</param>
    public void LoadPhysics(string buffer)
    {
        _physics = new CubismPhysics(buffer);
    }

    /// <summary>
    /// ユーザーデータを読み込む。
    /// </summary>
    /// <param name="buffer">userdata3.jsonが読み込まれているバッファ</param>
    public void LoadUserData(string buffer)
    {
        _modelUserData = new CubismModelUserData(buffer);
    }

    /// <summary>
    /// 指定した位置にDrawableがヒットしているかどうかを取得する。
    /// </summary>
    /// <param name="drawableId">検証したいDrawableのID</param>
    /// <param name="pointX">X位置</param>
    /// <param name="pointY">Y位置</param>
    /// <returns>true    ヒットしている
    /// false   ヒットしていない</returns>
    public unsafe bool IsHit(string drawableId, float pointX, float pointY)
    {
        int drawIndex = _model.GetDrawableIndex(drawableId);

        if (drawIndex < 0)
        {
            return false; // 存在しない場合はfalse
        }

        int count = _model.GetDrawableVertexCount(drawIndex);
        float* vertices = _model.GetDrawableVertices(drawIndex);

        float left = vertices[0];
        float right = vertices[0];
        float top = vertices[1];
        float bottom = vertices[1];

        for (int j = 1; j < count; ++j)
        {
            float x = vertices[CubismFramework.VertexOffset + j * CubismFramework.VertexStep];
            float y = vertices[CubismFramework.VertexOffset + j * CubismFramework.VertexStep + 1];

            if (x < left)
            {
                left = x; // Min x
            }

            if (x > right)
            {
                right = x; // Max x
            }

            if (y < top)
            {
                top = y; // Min y
            }

            if (y > bottom)
            {
                bottom = y; // Max y
            }
        }

        float tx = _modelMatrix.InvertTransformX(pointX);
        float ty = _modelMatrix.InvertTransformY(pointY);

        return ((left <= tx) && (tx <= right) && (top <= ty) && (ty <= bottom));
    }

    /// <summary>
    /// モデルを取得する。
    /// </summary>
    /// <returns>モデル</returns>
    public CubismModel GetModel()
    {
        return _model;
    }

    /// <summary>
    /// レンダラを取得する。
    /// </summary>
    /// <returns>レンダラ</returns>
    public CubismRenderer GetRenderer()
    {
        return _renderer;
    }

    /// <summary>
    /// レンダラを生成して初期化を実行する。
    /// </summary>
    public void CreateRenderer(CubismRenderer renderer, int maskBufferCount = 1)
    {
        if (_renderer != null)
        {
            DeleteRenderer();
        }
        _renderer = renderer;
        _renderer.Initialize(_model, maskBufferCount);
    }

    /// <summary>
    /// レンダラを解放する。
    /// </summary>
    public void DeleteRenderer()
    {
        if (_renderer != null)
        {
            _renderer.Dispose();
            _renderer = null;
        }
    }

    /// <summary>
    /// Eventが再生処理時にあった場合の処理をする。
    /// 継承で上書きすることを想定している。
    /// 上書きしない場合はログ出力をする。
    /// </summary>
    /// <param name="eventValue">発火したイベントの文字列データ</param>
    void MotionEventFired(string eventValue)
    {
        CubismLog.CubismLogInfo(eventValue);
    }
}
