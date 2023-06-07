using Live2DCSharpSDK.Framework;
using Live2DCSharpSDK.Framework.Effect;
using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Model;
using Live2DCSharpSDK.Framework.Motion;
using Live2DCSharpSDK.Framework.Rendering.OpenGL;
using Live2DCSharpSDK.Framework.Type;
using System;
using System.IO;

namespace Live2DCSharpSDK.App;

public class LAppModel : CubismUserModel
{
    /// <summary>
    /// モデルセッティング情報
    /// </summary>
    private CubismModelSettingJson _modelSetting;
    /// <summary>
    /// モデルセッティングが置かれたディレクトリ
    /// </summary>
    private string _modelHomeDir;
    /// <summary>
    /// デルタ時間の積算値[秒]
    /// </summary>
    private float _userTimeSeconds;
    /// <summary>
    /// モデルに設定されたまばたき機能用パラメータID
    /// </summary>
    private readonly List<string> _eyeBlinkIds = new();
    /// <summary>
    /// モデルに設定されたリップシンク機能用パラメータID
    /// </summary>
    private readonly List<string> _lipSyncIds = new();
    /// <summary>
    /// 読み込まれているモーションのリスト
    /// </summary>
    private readonly Dictionary<string, ACubismMotion> _motions = new();
    /// <summary>
    /// 読み込まれている表情のリスト
    /// </summary>
    private readonly Dictionary<string, ACubismMotion> _expressions = new();

    public List<string> Motions => new(_motions.Keys);
    public List<string> Expressions => new(_expressions.Keys);

    /// <summary>
    /// パラメータID: ParamAngleX
    /// </summary>
    private string _idParamAngleX;
    /// <summary>
    /// パラメータID: ParamAngleY
    /// </summary>
    private string _idParamAngleY;
    /// <summary>
    /// パラメータID: ParamAngleZ
    /// </summary>
    private string _idParamAngleZ;
    /// <summary>
    /// パラメータID: ParamBodyAngleX
    /// </summary>
    private string _idParamBodyAngleX;
    /// <summary>
    /// パラメータID: ParamEyeBallX
    /// </summary>
    private string _idParamEyeBallX;
    /// <summary>
    /// パラメータID: ParamEyeBallXY
    /// </summary>
    private string _idParamEyeBallY;

    /// <summary>
    /// wavファイルハンドラ
    /// </summary>
    //LAppWavFileHandler _wavFileHandler;

    private readonly LAppDelegate Lapp;

    private readonly Random random = new();

    public event Action<LAppModel, string>? Motion;

    public LAppModel(LAppDelegate lapp, string dir, string fileName)
    {
        Lapp = lapp;

        if (LAppDefine.MocConsistencyValidationEnable)
        {
            _mocConsistency = true;
        }

        if (LAppDefine.DebugLogEnable)
        {
            _debugMode = true;
        }

        _idParamAngleX = CubismFramework.GetIdManager()
            .GetId(CubismDefaultParameterId.ParamAngleX);
        _idParamAngleY = CubismFramework.GetIdManager()
            .GetId(CubismDefaultParameterId.ParamAngleY);
        _idParamAngleZ = CubismFramework.GetIdManager().
            GetId(CubismDefaultParameterId.ParamAngleZ);
        _idParamBodyAngleX = CubismFramework.GetIdManager()
            .GetId(CubismDefaultParameterId.ParamBodyAngleX);
        _idParamEyeBallX = CubismFramework.GetIdManager()
            .GetId(CubismDefaultParameterId.ParamEyeBallX);
        _idParamEyeBallY = CubismFramework.GetIdManager()
            .GetId(CubismDefaultParameterId.ParamEyeBallY);

        _modelHomeDir = dir;

        if (_debugMode)
        {
            LAppPal.PrintLog($"[APP]load model setting: {fileName}");
        }

        var setting = new CubismModelSettingJson(File.ReadAllText(fileName));

        Updating = true;
        Initialized = false;

        _modelSetting = setting;

        //Cubism Model
        if (!string.IsNullOrWhiteSpace(_modelSetting.GetModelFileName()))
        {
            var path = _modelSetting.GetModelFileName();
            path = Path.GetFullPath(_modelHomeDir + path);

            if (_debugMode)
            {
                LAppPal.PrintLog($"[APP]create model: {path}");
            }

            LoadModel(File.ReadAllBytes(path), _mocConsistency);
        }

        //Expression
        if (_modelSetting.GetExpressionCount() > 0)
        {
            int count = _modelSetting.GetExpressionCount();
            for (int i = 0; i < count; i++)
            {
                string name = _modelSetting.GetExpressionName(i);
                string path = _modelSetting.GetExpressionFileName(i);
                path = Path.GetFullPath(_modelHomeDir + path);

                ACubismMotion motion = new CubismExpressionMotion(File.ReadAllText(path));

                if (_expressions.ContainsKey(name))
                {
                    _expressions[name] = motion;
                }
                else
                {
                    _expressions.Add(name, motion);
                }
            }
        }

        //Physics
        if (!string.IsNullOrWhiteSpace(_modelSetting.GetPhysicsFileName()))
        {
            var path = _modelSetting.GetPhysicsFileName();
            path = Path.GetFullPath(_modelHomeDir + path);

            LoadPhysics(File.ReadAllText(path));
        }

        //Pose
        if (!string.IsNullOrWhiteSpace(_modelSetting.GetPoseFileName()))
        {
            var path = _modelSetting.GetPoseFileName();
            path = Path.GetFullPath(_modelHomeDir + path);

            LoadPose(File.ReadAllText(path));
        }

        //EyeBlink
        if (_modelSetting.GetEyeBlinkParameterCount() > 0)
        {
            _eyeBlink = new CubismEyeBlink(_modelSetting);
        }

        //Breath
        {
            _breath = new CubismBreath()
            {
                Parameters = new()
                {
                    new BreathParameterData()
                    {
                        ParameterId = _idParamAngleX,
                        Offset = 0.0f,
                        Peak = 15.0f,
                        Cycle = 6.5345f,
                        Weight = 0.5f
                    },
                    new BreathParameterData()
                    {
                        ParameterId = _idParamAngleY,
                        Offset = 0.0f,
                        Peak = 8.0f,
                        Cycle = 3.5345f,
                        Weight = 0.5f
                    },
                    new BreathParameterData()
                    {
                        ParameterId = _idParamAngleZ,
                        Offset = 0.0f,
                        Peak = 10.0f,
                        Cycle = 5.5345f,
                        Weight = 0.5f
                    },
                    new BreathParameterData()
                    {
                        ParameterId = _idParamBodyAngleX,
                        Offset = 0.0f,
                        Peak = 4.0f,
                        Cycle = 15.5345f,
                        Weight = 0.5f
                    },
                    new BreathParameterData()
                    {
                        ParameterId = CubismFramework.GetIdManager().GetId(CubismDefaultParameterId.ParamBreath),
                        Offset = 0.5f,
                        Peak = 0.5f,
                        Cycle = 3.2345f,
                        Weight = 0.5f
                    }
                }
            };
        }

        //UserData
        if (!string.IsNullOrWhiteSpace(_modelSetting.GetUserDataFile()))
        {
            string path = _modelSetting.GetUserDataFile();
            path = Path.GetFullPath(_modelHomeDir + path);

            LoadUserData(File.ReadAllText(path));
        }

        // EyeBlinkIds
        {
            int eyeBlinkIdCount = _modelSetting.GetEyeBlinkParameterCount();
            for (int i = 0; i < eyeBlinkIdCount; ++i)
            {
                _eyeBlinkIds.Add(_modelSetting.GetEyeBlinkParameterId(i));
            }
        }

        // LipSyncIds
        {
            int lipSyncIdCount = _modelSetting.GetLipSyncParameterCount();
            for (int i = 0; i < lipSyncIdCount; ++i)
            {
                _lipSyncIds.Add(_modelSetting.GetLipSyncParameterId(i));
            }
        }

        if (_modelSetting == null || ModelMatrix == null)
        {
            LAppPal.PrintLog("Failed to SetupModel().");
            return;
        }

        //Layout
        Dictionary<string, float> layout = new();
        _modelSetting.GetLayoutMap(layout);
        ModelMatrix.SetupFromLayout(layout);

        Model.SaveParameters();

        for (int i = 0; i < _modelSetting.GetMotionGroupCount(); i++)
        {
            var group = _modelSetting.GetMotionGroupName(i);
            PreloadMotionGroup(group);
        }

        _motionManager.StopAllMotions();

        Updating = false;
        Initialized = true;

        if (Model == null)
        {
            LAppPal.PrintLog("Failed to LoadAssets().");
            return;
        }

        CreateRenderer(new CubismRenderer_OpenGLES2(Lapp.GL));

        SetupTextures();
    }

    public new void Dispose()
    {
        base.Dispose();

        _motions.Clear();
        _expressions.Clear();

        for (int i = 0; i < _modelSetting.GetMotionGroupCount(); i++)
        {
            var group = _modelSetting.GetMotionGroupName(i);
            ReleaseMotionGroup(group);
        }
    }

    /// <summary>
    /// レンダラを再構築する
    /// </summary>
    public void ReloadRenderer()
    {
        DeleteRenderer();

        CreateRenderer(new CubismRenderer_OpenGLES2(Lapp.GL));

        SetupTextures();
    }

    /// <summary>
    /// モデルの更新処理。モデルのパラメータから描画状態を決定する。
    /// </summary>
    public void Update()
    {
        float deltaTimeSeconds = LAppPal.GetDeltaTime();
        _userTimeSeconds += deltaTimeSeconds;

        _dragManager.Update(deltaTimeSeconds);
        _dragX = _dragManager.FaceX;
        _dragY = _dragManager.FaceY;

        // モーションによるパラメータ更新の有無
        bool motionUpdated = false;

        //-----------------------------------------------------------------
        Model.LoadParameters(); // 前回セーブされた状態をロード
        if (_motionManager.IsFinished())
        {
            // モーションの再生がない場合、待機モーションの中からランダムで再生する
            StartRandomMotion(LAppDefine.MotionGroupIdle, MotionPriority.PriorityIdle);
        }
        else
        {
            motionUpdated = _motionManager.UpdateMotion(Model, deltaTimeSeconds); // モーションを更新
        }
        Model.SaveParameters(); // 状態を保存

        //-----------------------------------------------------------------

        // 不透明度
        Opacity = Model.GetModelOpacity();

        // まばたき
        if (!motionUpdated)
        {
            // メインモーションの更新がないとき
            //_eyeBlink?.UpdateParameters(Model, deltaTimeSeconds); // 目パチ
        }

        _expressionManager?.UpdateMotion(Model, deltaTimeSeconds); // 表情でパラメータ更新（相対変化）

        //ドラッグによる変化
        //ドラッグによる顔の向きの調整
        Model.AddParameterValue(_idParamAngleX, _dragX * 30); // -30から30の値を加える
        Model.AddParameterValue(_idParamAngleY, _dragY * 30);
        Model.AddParameterValue(_idParamAngleZ, _dragX * _dragY * -30);

        //ドラッグによる体の向きの調整
        Model.AddParameterValue(_idParamBodyAngleX, _dragX * 10); // -10から10の値を加える

        //ドラッグによる目の向きの調整
        Model.AddParameterValue(_idParamEyeBallX, _dragX); // -1から1の値を加える
        Model.AddParameterValue(_idParamEyeBallY, _dragY);

        // 呼吸など
        _breath?.UpdateParameters(Model, deltaTimeSeconds);

        // 物理演算の設定
        _physics?.Evaluate(Model, deltaTimeSeconds);

        // リップシンクの設定
        if (_lipSync)
        {
            // リアルタイムでリップシンクを行う場合、システムから音量を取得して0〜1の範囲で値を入力します。
            float value = 0.0f;

            // 状態更新/RMS値取得
            //_wavFileHandler.Update(deltaTimeSeconds);
            //value = _wavFileHandler.GetRms();

            for (int i = 0; i < _lipSyncIds.Count; ++i)
            {
                Model.AddParameterValue(_lipSyncIds[i], value, 0.8f);
            }
        }

        // ポーズの設定
        _pose?.UpdateParameters(Model, deltaTimeSeconds);

        Model.Update();
    }

    /// <summary>
    /// モデルを描画する処理。モデルを描画する空間のView-Projection行列を渡す。
    /// </summary>
    /// <param name="matrix">View-Projection行列</param>
    public void Draw(CubismMatrix44 matrix)
    {
        if (Model == null)
        {
            return;
        }

        matrix.MultiplyByMatrix(ModelMatrix);

        (Renderer as CubismRenderer_OpenGLES2)?.SetMvpMatrix(matrix);

        DoDraw();
    }

    /// <summary>
    /// 引数で指定したモーションの再生を開始する。
    /// </summary>
    /// <param name="group">モーショングループ名</param>
    /// <param name="no">グループ内の番号</param>
    /// <param name="priority">優先度</param>
    /// <param name="onFinishedMotionHandler">モーション再生終了時に呼び出されるコールバック関数。NULLの場合、呼び出されない。</param>
    /// <returns>開始したモーションの識別番号を返す。個別のモーションが終了したか否かを判定するIsFinished()の引数で使用する。開始できない時は「-1」</returns>
    public CubismMotionQueueEntry? StartMotion(string name, MotionPriority priority, FinishedMotionCallback? onFinishedMotionHandler = null)
    {
        var temp = name.Split("_");
        if (temp.Length != 2)
        {
            throw new Exception("motion name error");
        }
        return StartMotion(temp[0], int.Parse(temp[1]), priority, onFinishedMotionHandler);
    }

    public CubismMotionQueueEntry? StartMotion(string group, int no, MotionPriority priority, FinishedMotionCallback? onFinishedMotionHandler = null)
    {
        if (priority == MotionPriority.PriorityForce)
        {
            _motionManager.ReservePriority = priority;
        }
        else if (!_motionManager.ReserveMotion(priority))
        {
            if (_debugMode)
            {
                LAppPal.PrintLog("[APP]can't start motion.");
            }
            return null;
        }

        string fileName = _modelSetting.GetMotionFileName(group, no);

        //ex) idle_0
        string name = $"{group}_{no}";

        CubismMotion motion;
        if (!_motions.ContainsKey(name))
        {
            string path = fileName;
            path = Path.GetFullPath(_modelHomeDir + path);

            motion = new CubismMotion(File.ReadAllText(path), onFinishedMotionHandler);
            float fadeTime = _modelSetting.GetMotionFadeInTimeValue(group, no);
            if (fadeTime >= 0.0f)
            {
                motion.FadeInSeconds = fadeTime;
            }

            fadeTime = _modelSetting.GetMotionFadeOutTimeValue(group, no);
            if (fadeTime >= 0.0f)
            {
                motion.FadeOutSeconds = fadeTime;
            }
            motion.SetEffectIds(_eyeBlinkIds, _lipSyncIds);
        }
        else
        {
            motion = (_motions[name] as CubismMotion)!;
            motion.OnFinishedMotion = onFinishedMotionHandler;
        }

        //voice
        string voice = _modelSetting.GetMotionSoundFileName(group, no);
        if (!string.IsNullOrWhiteSpace(voice))
        {
            //string path = voice;
            //path = _modelHomeDir + path;
            //_wavFileHandler.Start(path);
        }

        if (_debugMode)
        {
            LAppPal.PrintLog($"[APP]start motion: [{group}_{no}]");
        }
        return _motionManager.StartMotionPriority(motion, priority);
    }

    /// <summary>
    /// ランダムに選ばれたモーションの再生を開始する。
    /// </summary>
    /// <param name="group">モーショングループ名</param>
    /// <param name="priority">優先度</param>
    /// <param name="onFinishedMotionHandler">モーション再生終了時に呼び出されるコールバック関数。NULLの場合、呼び出されない。</param>
    /// <returns>開始したモーションの識別番号を返す。個別のモーションが終了したか否かを判定するIsFinished()の引数で使用する。開始できない時は「-1」</returns>
    public object? StartRandomMotion(string group, MotionPriority priority, FinishedMotionCallback? onFinishedMotionHandler = null)
    {
        if (_modelSetting.GetMotionCount(group) == 0)
        {
            return null;
        }

        int no = random.Next() % _modelSetting.GetMotionCount(group);

        return StartMotion(group, no, priority, onFinishedMotionHandler);
    }

    /// <summary>
    /// 引数で指定した表情モーションをセットする
    /// </summary>
    /// <param name="expressionID">表情モーションのID</param>
    public void SetExpression(string expressionID)
    {
        ACubismMotion motion = _expressions[expressionID];
        if (_debugMode)
        {
            LAppPal.PrintLog($"[APP]expression: [{expressionID}]");
        }

        if (motion != null)
        {
            _expressionManager.StartMotionPriority(motion, MotionPriority.PriorityForce);
        }
        else
        {
            if (_debugMode) LAppPal.PrintLog($"[APP]expression[{expressionID}] is null ");
        }
    }

    /// <summary>
    /// ランダムに選ばれた表情モーションをセットする
    /// </summary>
    public void SetRandomExpression()
    {
        if (_expressions.Count == 0)
        {
            return;
        }

        int no = random.Next() % _expressions.Count;
        int i = 0;
        foreach (var item in _expressions)
        {
            if (i == no)
            {
                SetExpression(item.Key);
                return;
            }
            i++;
        }
    }

    /// <summary>
    /// イベントの発火を受け取る
    /// </summary>
    /// <param name="eventValue"></param>
    protected override void MotionEventFired(string eventValue)
    {
        CubismLog.CubismLogInfo($"{eventValue} is fired on LAppModel!!");
        Motion?.Invoke(this, eventValue);
    }

    /// <summary>
    /// 当たり判定テスト。
    /// 指定IDの頂点リストから矩形を計算し、座標が矩形範囲内か判定する。
    /// </summary>
    /// <param name="hitAreaName">当たり判定をテストする対象のID</param>
    /// <param name="x">判定を行うX座標</param>
    /// <param name="y">判定を行うY座標</param>
    /// <returns></returns>
    public bool HitTest(string hitAreaName, float x, float y)
    {
        // 透明時は当たり判定なし。
        if (Opacity < 1)
        {
            return false;
        }
        int count = _modelSetting.GetHitAreasCount();
        for (int i = 0; i < count; i++)
        {
            if (_modelSetting.GetHitAreaName(i) == hitAreaName)
            {
                return IsHit(_modelSetting.GetHitAreaId(i), x, y);
            }
        }
        return false; // 存在しない場合はfalse
    }

    /// <summary>
    /// モデルを描画する処理。モデルを描画する空間のView-Projection行列を渡す。
    /// </summary>
    protected void DoDraw()
    {
        if (Model == null)
        {
            return;
        }

        (Renderer as CubismRenderer_OpenGLES2)?.DrawModel();
    }

    /// <summary>
    /// モーションデータをグループ名から一括で解放する。
    /// モーションデータの名前は内部でModelSettingから取得する。
    /// </summary>
    /// <param name="group">モーションデータのグループ名</param>
    private void ReleaseMotionGroup(string group)
    {
        var count = _modelSetting.GetMotionCount(group);
        for (int i = 0; i < count; i++)
        {
            string voice = _modelSetting.GetMotionSoundFileName(group, i);
        }
    }

    /// <summary>
    /// OpenGLのテクスチャユニットにテクスチャをロードする
    /// </summary>
    private void SetupTextures()
    {
        for (int modelTextureNumber = 0; modelTextureNumber < _modelSetting.GetTextureCount(); modelTextureNumber++)
        {
            // テクスチャ名が空文字だった場合はロード・バインド処理をスキップ
            if (string.IsNullOrWhiteSpace(_modelSetting.GetTextureFileName(modelTextureNumber)))
            {
                continue;
            }

            //OpenGLのテクスチャユニットにテクスチャをロードする
            var texturePath = _modelSetting.GetTextureFileName(modelTextureNumber);
            texturePath = Path.GetFullPath(_modelHomeDir + texturePath);

            TextureInfo texture = Lapp.TextureManager.CreateTextureFromPngFile(texturePath);
            int glTextueNumber = texture.id;

            //OpenGL
            (Renderer as CubismRenderer_OpenGLES2)?.BindTexture(modelTextureNumber, glTextueNumber);
        }
    }

    /// <summary>
    /// モーションデータをグループ名から一括でロードする。
    /// モーションデータの名前は内部でModelSettingから取得する。
    /// </summary>
    /// <param name="group">モーションデータのグループ名</param>
    private void PreloadMotionGroup(string group)
    {
        // グループに登録されているモーション数を取得
        int count = _modelSetting.GetMotionCount(group);

        for (int i = 0; i < count; i++)
        {
            //ex) idle_0
            // モーションのファイル名とパスの取得
            string name = $"{group}_{i}";
            var path = Path.GetFullPath(_modelHomeDir + _modelSetting.GetMotionFileName(group, i));

            // モーションデータの読み込み
            var tmpMotion = new CubismMotion(File.ReadAllText(path));

            // フェードインの時間を取得
            float fadeTime = _modelSetting.GetMotionFadeInTimeValue(group, i);
            if (fadeTime >= 0.0f)
            {
                tmpMotion.FadeInSeconds = fadeTime;
            }

            // フェードアウトの時間を取得
            fadeTime = _modelSetting.GetMotionFadeOutTimeValue(group, i);
            if (fadeTime >= 0.0f)
            {
                tmpMotion.FadeOutSeconds = fadeTime;
            }
            tmpMotion.SetEffectIds(_eyeBlinkIds, _lipSyncIds);

            if (_motions.ContainsKey(name))
            {
                _motions[name] = tmpMotion;
            }
            else
            {
                _motions.Add(name, tmpMotion);
            }
        }
    }
}
