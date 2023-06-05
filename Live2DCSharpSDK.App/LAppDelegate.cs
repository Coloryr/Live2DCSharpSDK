using Live2DCSharpSDK.Framework;
using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Rendering.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.App;

/// <summary>
/// アプリケーションクラス。
/// Cubism SDK の管理を行う。
/// </summary>
public class LAppDelegate : IDisposable
{
    private static LAppDelegate? s_instance;
    /// <summary>
    /// Cubism SDK Allocator
    /// </summary>
    private LAppAllocator _cubismAllocator;
    /// <summary>
    /// Cubism SDK Option
    /// </summary>
    private Option _cubismOption;
    /// <summary>
    /// View情報
    /// </summary>
    private LAppView _view;
    /// <summary>
    /// クリックしているか
    /// </summary>
    private bool _captured;
    /// <summary>
    /// マウスX座標
    /// </summary>
    private float _mouseX;
    /// <summary>
    /// マウスY座標
    /// </summary>
    private float _mouseY;
    /// <summary>
    /// APP終了しているか
    /// </summary>
    private bool _isEnd;
    /// <summary>
    /// テクスチャマネージャー
    /// </summary>
    private LAppTextureManager _textureManager;

    /// <summary>
    /// Initialize関数で設定したウィンドウ幅
    /// </summary>
    private int _windowWidth;
    /// <summary>
    /// Initialize関数で設定したウィンドウ高さ
    /// </summary>
    private int _windowHeight;                          

    /// <summary>
    /// APPに必要なものを初期化する。
    /// </summary>
    public bool Initialize(OpenGLApi GL)
    {
        if (LAppDefine.DebugLogEnable)
        {
            LAppPal.PrintLog("START");
        }

        //テクスチャサンプリング設定
        GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
        GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR);

        //透過設定
        GL.glEnable(GL.GL_BLEND);
        GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);

        // ウィンドウサイズ記憶
        GL.GetWindowSize(out int width, out int height);
        _windowWidth = width;
        _windowHeight = height;

        //AppViewの初期化
        _view.Initialize();

        // Cubism SDK の初期化
        InitializeCubism();

        return true;
    }

    private void InitializeCubism()
    {
        //setup cubism
        _cubismOption = new()
        {
            LogFunction = LAppPal.PrintLog,
            LoggingLevel = LAppDefine.CubismLoggingLevel
        };
        CubismFramework.StartUp(_cubismAllocator, _cubismOption);

        //Initialize cubism
        CubismFramework.Initialize();

        //load model
        LAppLive2DManager.GetInstance();

        //default proj
        CubismMatrix44 projection;

        LAppPal.UpdateTime(0);

        _view->InitializeSprite();
    }

    /// <summary>
    /// 解放する。
    /// </summary>
    public void Dispose()
    {
        
    }

    /// <summary>
    /// 実行処理。
    /// </summary>
    public void Run()
    { 
    
    }

    /// <summary>
    /// OpenGL用 glfwSetMouseButtonCallback用関数。
    /// </summary>
    /// <param name="button">ボタン種類</param>
    /// <param name="action">実行結果</param>
    public void OnMouseCallBack(ButtonType button, ButtonFuntion action)
    {
        if (_view == null)
        {
            return;
        }
        if (button != ButtonType.LEFT)
        {
            return;
        }

        if (action == ButtonFuntion.PRESS)
        {
            _captured = true;
            _view.OnTouchesBegan(_mouseX, _mouseY);
        }
        else if (action == ButtonFuntion.RELEASE)
        {
            if (_captured)
            {
                _captured = false;
                _view.OnTouchesEnded(_mouseX, _mouseY);
            }
        }
    }

    /// <summary>
    /// OpenGL用 glfwSetCursorPosCallback用関数。
    /// </summary>
    /// <param name="x">x座標</param>
    /// <param name="y">x座標</param>
    public void OnMouseCallBack(float x, float y)
    {
        if (!_captured)
        {
            return;
        }
        if (_view == null)
        {
            return;
        }

        _view.OnTouchesMoved(_mouseX, _mouseY);
    }

    /// <summary>
    /// View情報を取得する。
    /// </summary>
    public LAppView GetView()
    {
        return _view;
    }

    /// <summary>
    /// アプリケーションを終了するかどうか。
    /// </summary>
    /// <returns></returns>
    public bool GetIsEnd()
    {
        return _isEnd;
    }

    /// <summary>
    /// アプリケーションを終了する。
    /// </summary>
    public void AppEnd()
    {
        _isEnd = true;
    }

    public LAppTextureManager GetTextureManager()
    {
        return _textureManager;
    }
}
