using Live2DCSharpSDK.Framework;
using Live2DCSharpSDK.Framework.Effect;
using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Model;
using Live2DCSharpSDK.Framework.Motion;
using Live2DCSharpSDK.Framework.Rendering.OpenGL;
using Live2DCSharpSDK.Framework.Type;

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

    private readonly List<RectF> _hitArea = new();
    private readonly List<RectF> _userArea = new();

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

    /// <summary>
    /// フレームバッファ以外の描画先
    /// </summary>
    private CubismOffscreenFrame_OpenGLES2 _renderBuffer;

    private LAppDelegate Lapp;

    private Random random = new();

    public event Action<LAppModel, string>? Motion;

    public LAppModel(LAppDelegate lapp)
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
    }

    public new void Dispose()
    {
        base.Dispose();

        _renderBuffer.DestroyOffscreenFrame();

        ReleaseMotions();
        ReleaseExpressions();

        for (int i = 0; i < _modelSetting.GetMotionGroupCount(); i++)
        {
            var group = _modelSetting.GetMotionGroupName(i);
            ReleaseMotionGroup(group);
        }
    }

    /// <summary>
    /// model3.jsonが置かれたディレクトリとファイルパスからモデルを生成する
    /// </summary>
    public void LoadAssets(string dir, string fileName)
    {
        _modelHomeDir = dir;

        if (_debugMode)
        {
            LAppPal.PrintLog($"[APP]load model setting: {fileName}");
        }

        var setting = new CubismModelSettingJson(File.ReadAllText(fileName));

        SetupModel(setting);

        if (_model == null)
        {
            LAppPal.PrintLog("Failed to LoadAssets().");
            return;
        }

        CreateRenderer(new CubismRenderer_OpenGLES2(Lapp.GL));

        SetupTextures();
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
        _dragX = _dragManager.GetX();
        _dragY = _dragManager.GetY();

        // モーションによるパラメータ更新の有無
        bool motionUpdated = false;

        //-----------------------------------------------------------------
        _model.LoadParameters(); // 前回セーブされた状態をロード
        if (_motionManager.IsFinished())
        {
            // モーションの再生がない場合、待機モーションの中からランダムで再生する
            StartRandomMotion(LAppDefine.MotionGroupIdle, LAppDefine.PriorityIdle);
        }
        else
        {
            motionUpdated = _motionManager.UpdateMotion(_model, deltaTimeSeconds); // モーションを更新
        }
        _model.SaveParameters(); // 状態を保存
                                 //-----------------------------------------------------------------

        // 不透明度
        _opacity = _model.GetModelOpacity();

        // まばたき
        if (!motionUpdated)
        {
            if (_eyeBlink != null)
            {
                // メインモーションの更新がないとき
                _eyeBlink.UpdateParameters(_model, deltaTimeSeconds); // 目パチ
            }
        }

        if (_expressionManager != null)
        {
            _expressionManager.UpdateMotion(_model, deltaTimeSeconds); // 表情でパラメータ更新（相対変化）
        }

        //ドラッグによる変化
        //ドラッグによる顔の向きの調整
        _model.AddParameterValue(_idParamAngleX, _dragX * 30); // -30から30の値を加える
        _model.AddParameterValue(_idParamAngleY, _dragY * 30);
        _model.AddParameterValue(_idParamAngleZ, _dragX * _dragY * -30);

        //ドラッグによる体の向きの調整
        _model.AddParameterValue(_idParamBodyAngleX, _dragX * 10); // -10から10の値を加える

        //ドラッグによる目の向きの調整
        _model.AddParameterValue(_idParamEyeBallX, _dragX); // -1から1の値を加える
        _model.AddParameterValue(_idParamEyeBallY, _dragY);

        // 呼吸など
        if (_breath != null)
        {
            _breath.UpdateParameters(_model, deltaTimeSeconds);
        }

        // 物理演算の設定
        if (_physics != null)
        {
            _physics.Evaluate(_model, deltaTimeSeconds);
        }

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
                _model.AddParameterValue(_lipSyncIds[i], value, 0.8f);
            }
        }

        // ポーズの設定
        if (_pose != null)
        {
            _pose.UpdateParameters(_model, deltaTimeSeconds);
        }

        _model.Update();
    }

    /// <summary>
    /// モデルを描画する処理。モデルを描画する空間のView-Projection行列を渡す。
    /// </summary>
    /// <param name="matrix">View-Projection行列</param>
    public void Draw(CubismMatrix44 matrix)
    {
        if (_model == null)
        {
            return;
        }

        matrix.MultiplyByMatrix(_modelMatrix);

        (GetRenderer() as CubismRenderer_OpenGLES2)?.SetMvpMatrix(matrix);

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
    public dynamic StartMotion(string group, int no, int priority, FinishedMotionCallback onFinishedMotionHandler = null)
    {
        if (priority == LAppDefine.PriorityForce)
        {
            _motionManager.SetReservePriority(priority);
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

        bool autoDelete = false;
        CubismMotion motion;
        if (!_motions.ContainsKey(name))
        {
            string path = fileName;
            path = _modelHomeDir + path;

            motion = (LoadMotion(File.ReadAllText(path), null, onFinishedMotionHandler) as CubismMotion)!;
            float fadeTime = _modelSetting.GetMotionFadeInTimeValue(group, no);
            if (fadeTime >= 0.0f)
            {
                motion.SetFadeInTime(fadeTime);
            }

            fadeTime = _modelSetting.GetMotionFadeOutTimeValue(group, no);
            if (fadeTime >= 0.0f)
            {
                motion.SetFadeOutTime(fadeTime);
            }
            motion.SetEffectIds(_eyeBlinkIds, _lipSyncIds);
            autoDelete = true; // 終了時にメモリから削除
        }
        else
        {
            motion = (_motions[name] as CubismMotion)!;
            motion.SetFinishedMotionHandler(onFinishedMotionHandler);
        }

        //voice
        string voice = _modelSetting.GetMotionSoundFileName(group, no);
        if (!string.IsNullOrWhiteSpace(voice))
        {
            string path = voice;
            path = _modelHomeDir + path;
            //_wavFileHandler.Start(path);
        }

        if (_debugMode)
        {
            LAppPal.PrintLog($"[APP]start motion: [{group}_{no}]");
        }
        return _motionManager.StartMotionPriority(motion, autoDelete, priority);
    }

    /// <summary>
    /// ランダムに選ばれたモーションの再生を開始する。
    /// </summary>
    /// <param name="group">モーショングループ名</param>
    /// <param name="priority">優先度</param>
    /// <param name="onFinishedMotionHandler">モーション再生終了時に呼び出されるコールバック関数。NULLの場合、呼び出されない。</param>
    /// <returns>開始したモーションの識別番号を返す。個別のモーションが終了したか否かを判定するIsFinished()の引数で使用する。開始できない時は「-1」</returns>
    public dynamic StartRandomMotion(string group, int priority, FinishedMotionCallback onFinishedMotionHandler = null)
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
            _expressionManager.StartMotionPriority(motion, false, LAppDefine.PriorityForce);
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
    public void MotionEventFired(string eventValue)
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
        if (_opacity < 1)
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
    /// 別ターゲットに描画する際に使用するバッファの取得
    /// </summary>
    public CubismOffscreenFrame_OpenGLES2 GetRenderBuffer()
    {
        return _renderBuffer;
    }

    /// <summary>
    /// .moc3ファイルの整合性をチェックする
    /// </summary>
    /// <param name="mocFileName">MOC3ファイル名</param>
    /// <returns>MOC3に整合性があれば'true'、そうでなければ'false'。</returns>
    public bool HasMocConsistencyFromFile(string mocFileName)
    {
        if (string.IsNullOrWhiteSpace(mocFileName))
        {
            throw new ArgumentNullException("mocFileName is empty");
        }
        var path = mocFileName;
        path = _modelHomeDir + path;

        var consistency = CubismMoc.HasMocConsistencyFromUnrevivedMoc(File.ReadAllBytes(path));
        if (!consistency)
        {
            CubismLog.CubismLogInfo("Inconsistent MOC3.");
        }
        else
        {
            CubismLog.CubismLogInfo("Consistent MOC3.");
        }

        return consistency;
    }

    /// <summary>
    /// モデルを描画する処理。モデルを描画する空間のView-Projection行列を渡す。
    /// </summary>
    protected void DoDraw()
    {
        if (_model == null)
        {
            return;
        }

        (GetRenderer() as CubismRenderer_OpenGLES2)?.DrawModel();
    }

    /// <summary>
    /// すべてのモーションデータを解放する。
    /// </summary>
    private void ReleaseMotions()
    {
        _motions.Clear();
    }

    /// <summary>
    /// すべての表情データを解放する。
    /// </summary>
    private void ReleaseExpressions()
    {
        _expressions.Clear();
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
    /// model3.jsonからモデルを生成する。
    /// model3.jsonの記述に従ってモデル生成、モーション、物理演算などのコンポーネント生成を行う。
    /// </summary>
    /// <param name="setting">ICubismModelSettingのインスタンス</param>
    private void SetupModel(CubismModelSettingJson setting)
    {
        _updating = true;
        _initialized = false;

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

                ACubismMotion motion = LoadExpression(File.ReadAllText(path), name);

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

        if (_modelSetting == null || _modelMatrix == null)
        {
            LAppPal.PrintLog("Failed to SetupModel().");
            return;
        }

        //Layout
        Dictionary<string, float> layout = new();
        _modelSetting.GetLayoutMap(layout);
        _modelMatrix.SetupFromLayout(layout);

        _model.SaveParameters();

        for (int i = 0; i < _modelSetting.GetMotionGroupCount(); i++)
        {
            var group = _modelSetting.GetMotionGroupName(i);
            PreloadMotionGroup(group);
        }

        _motionManager.StopAllMotions();

        _updating = false;
        _initialized = true;
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

            TextureInfo texture = Lapp.GetTextureManager().CreateTextureFromPngFile(texturePath);
            int glTextueNumber = texture.id;

            //OpenGL
            (GetRenderer() as CubismRenderer_OpenGLES2)?.BindTexture(modelTextureNumber, glTextueNumber);
        }
    }

    /// <summary>
    /// モーションデータをグループ名から一括でロードする。
    /// モーションデータの名前は内部でModelSettingから取得する。
    /// </summary>
    /// <param name="group">モーションデータのグループ名</param>
    private void PreloadMotionGroup(string group)
    {

    }
}
