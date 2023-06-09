﻿namespace Live2DCSharpSDK.Framework.Rendering.OpenGL;

/// <summary>
/// オフスクリーン描画用構造体
/// </summary>
public class CubismOffscreenFrame_OpenGLES2
{
    private readonly OpenGLApi GL;
    /// <summary>
    /// レンダリングターゲットとしてのアドレス
    /// </summary>
    public int RenderTexture { get; private set; }
    /// <summary>
    /// 描画の際使用するテクスチャとしてのアドレス
    /// </summary>
    public int ColorBuffer { get; private set; }

    /// <summary>
    /// 旧フレームバッファ
    /// </summary>
    private int _oldFBO;

    /// <summary>
    /// Create時に指定された幅
    /// </summary>
    public int BufferWidth { get; private set; }
    /// <summary>
    /// Create時に指定された高さ
    /// </summary>
    public int BufferHeight { get; private set; }
    /// <summary>
    /// 引数によって設定されたカラーバッファか？
    /// </summary>
    private bool _isColorBufferInherited;

    public CubismOffscreenFrame_OpenGLES2(OpenGLApi gl)
    {
        GL = gl;
    }

    /// <summary>
    /// 指定の描画ターゲットに向けて描画開始
    /// </summary>
    /// <param name="restoreFBO">0以上の場合、EndDrawでこの値をglBindFramebufferする</param>
    public void BeginDraw(int restoreFBO = -1)
    {
        if (RenderTexture == 0)
        {
            return;
        }

        // バックバッファのサーフェイスを記憶しておく
        if (restoreFBO < 0)
        {
            GL.glGetIntegerv(GL.GL_FRAMEBUFFER_BINDING, out _oldFBO);
        }
        else
        {
            _oldFBO = restoreFBO;
        }

        //マスク用RenderTextureをactiveにセット
        GL.glBindFramebuffer(GL.GL_FRAMEBUFFER, RenderTexture);
    }

    /// <summary>
    /// 描画終了
    /// </summary>
    public void EndDraw()
    {
        if (RenderTexture == 0)
        {
            return;
        }

        // 描画対象を戻す
        GL.glBindFramebuffer(GL.GL_FRAMEBUFFER, _oldFBO);
    }

    /// <summary>
    /// レンダリングターゲットのクリア
    /// 呼ぶ場合はBeginDrawの後で呼ぶこと
    /// </summary>
    /// <param name="r">赤(0.0~1.0)</param>
    /// <param name="g">緑(0.0~1.0)</param>
    /// <param name="b">青(0.0~1.0)</param>
    /// <param name="a">α(0.0~1.0)</param>
    public void Clear(float r, float g, float b, float a)
    {
        // マスクをクリアする
        GL.glClearColor(r, g, b, a);
        GL.glClear(GL.GL_COLOR_BUFFER_BIT);
    }

    /// <summary>
    /// CubismOffscreenFrame作成
    /// </summary>
    /// <param name="displayBufferWidth">作成するバッファ幅</param>
    /// <param name="displayBufferHeight">作成するバッファ高さ</param>
    /// <param name="colorBuffer">0以外の場合、ピクセル格納領域としてcolorBufferを使用する</param>
    public unsafe bool CreateOffscreenFrame(int displayBufferWidth, int displayBufferHeight, int colorBuffer = 0)
    {
        // 一旦削除
        DestroyOffscreenFrame();

        // 新しく生成する
        if (colorBuffer == 0)
        {
            ColorBuffer = GL.glGenTexture();

            GL.glBindTexture(GL.GL_TEXTURE_2D, ColorBuffer);
            GL.glTexImage2D(GL.GL_TEXTURE_2D, 0, GL.GL_RGBA, displayBufferWidth, displayBufferHeight, 0, GL.GL_RGBA, GL.GL_UNSIGNED_BYTE, 0);
            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_WRAP_S, GL.GL_CLAMP_TO_EDGE);
            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_WRAP_T, GL.GL_CLAMP_TO_EDGE);
            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR);
            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
            GL.glBindTexture(GL.GL_TEXTURE_2D, 0);

            _isColorBufferInherited = false;
        }
        else
        {
            // 指定されたものを使用
            ColorBuffer = colorBuffer;

            _isColorBufferInherited = true;
        }

        GL.glGetIntegerv(GL.GL_FRAMEBUFFER_BINDING, out int tmpFramebufferObject);

        int ret = GL.glGenFramebuffer();
        GL.glBindFramebuffer(GL.GL_FRAMEBUFFER, ret);
        GL.glFramebufferTexture2D(GL.GL_FRAMEBUFFER, GL.GL_COLOR_ATTACHMENT0, GL.GL_TEXTURE_2D, ColorBuffer, 0);
        GL.glBindFramebuffer(GL.GL_FRAMEBUFFER, tmpFramebufferObject);

        RenderTexture = ret;

        BufferWidth = displayBufferWidth;
        BufferHeight = displayBufferHeight;

        // 成功
        return true;
    }

    /// <summary>
    /// CubismOffscreenFrameの削除
    /// </summary>
    public void DestroyOffscreenFrame()
    {
        if (!_isColorBufferInherited && (ColorBuffer != 0))
        {
            GL.glDeleteTexture(ColorBuffer);
            ColorBuffer = 0;
        }

        if (RenderTexture != 0)
        {
            GL.glDeleteFramebuffer(RenderTexture);
            RenderTexture = 0;
        }
    }

    /// <summary>
    /// 現在有効かどうか
    /// </summary>
    public bool IsValid()
    {
        return RenderTexture != 0;
    }
}
