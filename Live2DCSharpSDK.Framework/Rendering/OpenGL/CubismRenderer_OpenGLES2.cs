﻿using Live2DCSharpSDK.Framework.Model;
using System.Numerics;
using System.Runtime.InteropServices;
using static Live2DCSharpSDK.Framework.Rendering.OpenGL.CubismShader_OpenGLES2;

namespace Live2DCSharpSDK.Framework.Rendering.OpenGL;

public class CubismRenderer_OpenGLES2 : CubismRenderer
{
    public const int ColorChannelCount = 4;   // 実験時に1チャンネルの場合は1、RGBだけの場合は3、アルファも含める場合は4
    public const int ClippingMaskMaxCountOnDefault = 36;  // 通常のフレームバッファ1枚あたりのマスク最大数
    public const int ClippingMaskMaxCountOnMultiRenderTexture = 32;   // フレームバッファが2枚以上ある場合のフレームバッファ1枚あたりのマスク最大数

    private readonly OpenGLApi GL;

    /// <summary>
    /// モデルが参照するテクスチャとレンダラでバインドしているテクスチャとのマップ
    /// </summary>
    private readonly Dictionary<int, int> _textures;
    /// <summary>
    /// 描画オブジェクトのインデックスを描画順に並べたリスト
    /// </summary>
    private int[] _sortedDrawableIndexList = Array.Empty<int>();
    /// <summary>
    /// OpenGLのステートを保持するオブジェクト
    /// </summary>
    private CubismRendererProfile_OpenGLES2 _rendererProfile;
    /// <summary>
    /// クリッピングマスク管理オブジェクト
    /// </summary>
    private CubismClippingManager_OpenGLES2? _clippingManager;
    /// <summary>
    /// マスクテクスチャに描画するためのクリッピングコンテキスト
    /// </summary>
    internal CubismClippingContext? ClippingContextBufferForMask { get; set; }
    /// <summary>
    /// 画面上描画するためのクリッピングコンテキスト
    /// </summary>
    internal CubismClippingContext? ClippingContextBufferForDraw { get; set; }

    internal int VertexArray { get; private set; }
    internal int VertexBuffer { get; private set; }
    internal int IndexBuffer { get; private set; }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct VBO
    {
        public float ver0;
        public float ver1;
        public float uv0;
        public float uv1;
    }

    internal VBO[] vbo = Array.Empty<VBO>();

    /// <summary>
    /// マスク描画用のフレームバッファ
    /// </summary>
    private readonly List<CubismOffscreenFrame_OpenGLES2> _offscreenFrameBuffers = new();

    /// <summary>
    /// Tegraプロセッサ対応。拡張方式による描画の有効・無効
    /// </summary>
    /// <param name="extMode">trueなら拡張方式で描画する</param>
    /// <param name="extPAMode">trueなら拡張方式のPA設定を有効にする</param>
    public static void SetExtShaderMode(bool extMode, bool extPAMode = false)
    {
        CubismShader_OpenGLES2.SetExtShaderMode(extMode, extPAMode);
        CubismShader_OpenGLES2.DeleteInstance();
    }

    public unsafe CubismRenderer_OpenGLES2(OpenGLApi gl, CubismModel model, int maskBufferCount = 1) : base(model)
    {
        GL = gl;
        _rendererProfile = new(gl);
        _textures = new Dictionary<int, int>(32);

        VertexArray = GL.glGenVertexArray();
        VertexBuffer = GL.glGenBuffer();
        IndexBuffer = GL.glGenBuffer();

        // 1未満は1に補正する
        if (maskBufferCount < 1)
        {
            maskBufferCount = 1;
            CubismLog.CubismLogWarning("[Live2D SDK]The number of render textures must be an integer greater than or equal to 1. Set the number of render textures to 1.");
        }

        if (model.IsUsingMasking())
        {
            _clippingManager = new CubismClippingManager_OpenGLES2(GL);  //クリッピングマスク・バッファ前処理方式を初期化
            _clippingManager.Initialize(
            model,
                model.GetDrawableCount(),
                model.GetDrawableMasks(),
                model.GetDrawableMaskCounts(),
                maskBufferCount
            );

            _offscreenFrameBuffers.Clear();
            for (int i = 0; i < maskBufferCount; ++i)
            {
                CubismOffscreenFrame_OpenGLES2 offscreenSurface = new(GL);
                offscreenSurface.CreateOffscreenFrame((int)_clippingManager.GetClippingMaskBufferSize().X, (int)_clippingManager.GetClippingMaskBufferSize().Y);
                _offscreenFrameBuffers.Add(offscreenSurface);
            }
        }

        _sortedDrawableIndexList = new int[model.GetDrawableCount()];
    }

    /// <summary>
    /// OpenGLテクスチャのバインド処理
    /// CubismRendererにテクスチャを設定し、CubismRenderer中でその画像を参照するためのIndex値を戻り値とする
    /// </summary>
    /// <param name="modelTextureNo">セットするモデルテクスチャの番号</param>
    /// <param name="glTextureNo">OpenGLテクスチャの番号</param>
    public void BindTexture(int modelTextureNo, int glTextureNo)
    {
        _textures[modelTextureNo] = glTextureNo;
    }

    /// <summary>
    /// OpenGLにバインドされたテクスチャのリストを取得する
    /// </summary>
    /// <returns>テクスチャのアドレスのリスト</returns>
    public Dictionary<int, int> GetBindedTextures()
    {
        return _textures;
    }

    /// <summary>
    /// クリッピングマスクバッファのサイズを設定する
    /// マスク用のFrameBufferを破棄・再作成するため処理コストは高い。
    /// </summary>
    /// <param name="width">クリッピングマスクバッファのサイズ</param>
    /// <param name="height">クリッピングマスクバッファのサイズ</param>
    public unsafe void SetClippingMaskBufferSize(float width, float height)
    {
        if (_clippingManager == null)
        {
            return;
        }

        // インスタンス破棄前にレンダーテクスチャの数を保存
        int renderTextureCount = _clippingManager.GetRenderTextureCount();

        _clippingManager = new CubismClippingManager_OpenGLES2(GL);

        _clippingManager.SetClippingMaskBufferSize(width, height);

        _clippingManager.Initialize(
            Model,
            Model.GetDrawableCount(),
            Model.GetDrawableMasks(),
            Model.GetDrawableMaskCounts(),
            renderTextureCount
        );
    }

    /// <summary>
    /// レンダーテクスチャの枚数を取得する。
    /// </summary>
    /// <returns>レンダーテクスチャの枚数</returns>
    public int GetRenderTextureCount()
    {
        return _clippingManager!.GetRenderTextureCount();
    }

    /// <summary>
    /// クリッピングマスクバッファのサイズを取得する
    /// </summary>
    /// <returns>クリッピングマスクバッファのサイズ</returns>
    public Vector2 GetClippingMaskBufferSize()
    {
        return _clippingManager!.GetClippingMaskBufferSize();
    }

    /// <summary>
    /// クリッピングマスクのバッファを取得する
    /// </summary>
    /// <returns>クリッピングマスクのバッファへのポインタ</returns>
    public CubismOffscreenFrame_OpenGLES2 GetMaskBuffer(int index)
    {
        return _offscreenFrameBuffers[index];
    }


    internal override unsafe void DrawMesh(int textureNo, int indexCount, int vertexCount
            , ushort* indexArray, float* vertexArray, float* uvArray
            , float opacity, CubismBlendMode colorBlendMode, bool invertedMask)
    {
        throw new Exception("[Live2D Core]Use 'DrawMeshOpenGL' function");
    }

    /// <summary>
    /// [オーバーライド]
    /// 描画オブジェクト（アートメッシュ）を描画する。
    /// ポリゴンメッシュとテクスチャ番号をセットで渡す。
    /// </summary>
    /// <param name="textureNo">描画するテクスチャ番号</param>
    /// <param name="indexCount">描画オブジェクトのインデックス値</param>
    /// <param name="vertexCount">ポリゴンメッシュの頂点数</param>
    /// <param name="indexArray">ポリゴンメッシュのインデックス配列</param>
    /// <param name="vertexArray">ポリゴンメッシュの頂点配列</param>
    /// <param name="uvArray">uv配列</param>
    /// <param name="multiplyColor">乗算色</param>
    /// <param name="screenColor">スクリーン色</param>
    /// <param name="opacity">不透明度</param>
    /// <param name="colorBlendMode">カラー合成タイプ</param>
    /// <param name="invertedMask">マスク使用時のマスクの反転使用</param>
    internal unsafe void DrawMeshOpenGL(int textureNo, int indexCount, int vertexCount
                  , ushort* indexArray, float* vertexArray, float* uvArray
                  , CubismTextureColor multiplyColor, CubismTextureColor screenColor
                  , float opacity, CubismBlendMode colorBlendMode, bool invertedMask)
    {
        if (_textures[textureNo] == 0) return;    // モデルが参照するテクスチャがバインドされていない場合は描画をスキップする

        // 裏面描画の有効・無効
        if (IsCulling())
        {
            GL.glEnable(GL.GL_CULL_FACE);
        }
        else
        {
            GL.glDisable(GL.GL_CULL_FACE);
        }

        GL.glFrontFace(GL.GL_CCW);    // Cubism SDK OpenGLはマスク・アートメッシュ共にCCWが表面

        CubismTextureColor modelColorRGBA = GetModelColor();

        if (opacity > 1)
        {
            opacity = 1;
        }
        else if (opacity < 0)
        {
            opacity = 0;
        }

        if (ClippingContextBufferForMask == null) // マスク生成時以外
        {
            modelColorRGBA.A *= opacity;
            if (IsPremultipliedAlpha())
            {
                modelColorRGBA.R *= modelColorRGBA.A;
                modelColorRGBA.G *= modelColorRGBA.A;
                modelColorRGBA.B *= modelColorRGBA.A;
            }
        }

        int drawTextureId;   // シェーダに渡すテクスチャID

        // テクスチャマップからバインド済みテクスチャIDを取得
        // バインドされていなければダミーのテクスチャIDをセットする
        if (_textures[textureNo] != 0)
        {
            drawTextureId = _textures[textureNo];
        }
        else
        {
            drawTextureId = -1;
        }

        GL.glBindVertexArray(VertexArray);

        if (vbo == null || vbo.Length != vertexCount)
        {
            vbo = new VBO[vertexCount];
        }

        for (int a = 0; a < vertexCount; a++)
        {
            vbo[a].ver0 = vertexArray[a * 2];
            vbo[a].ver1 = vertexArray[a * 2 + 1];
            vbo[a].uv0 = uvArray[a * 2];
            vbo[a].uv1 = uvArray[a * 2 + 1];
        }

        GL.glBindBuffer(GL.GL_ARRAY_BUFFER, VertexBuffer);
        fixed (void* p = vbo)
        {
            GL.glBufferData(GL.GL_ARRAY_BUFFER, vertexCount * sizeof(VBO), new IntPtr(p), GL.GL_STATIC_DRAW);
        }

        GL.glBindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, IndexBuffer);

        GL.glBufferData(GL.GL_ELEMENT_ARRAY_BUFFER, indexCount * sizeof(ushort), new IntPtr(indexArray), GL.GL_STATIC_DRAW);

        GetInstance(GL).SetupShaderProgram(
            this, drawTextureId, colorBlendMode, modelColorRGBA,
            multiplyColor, screenColor, IsPremultipliedAlpha(),
            GetMvpMatrix(), invertedMask
        );

        GL.glBindVertexArray(VertexArray);

        // ポリゴンメッシュを描画する
        GL.glDrawElements(GL.GL_TRIANGLES, indexCount, GL.GL_UNSIGNED_SHORT, 0);

        // 後処理
        GL.glUseProgram(0);
        GL.glBindBuffer(GL.GL_ARRAY_BUFFER, 0);
        GL.glBindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, 0);
        GL.glBindVertexArray(0);
        ClippingContextBufferForDraw = null;
        ClippingContextBufferForMask = null;
    }

    /// <summary>
    /// 描画開始時の追加処理。
    /// モデルを描画する前にクリッピングマスクに必要な処理を実装している。
    /// </summary>
    internal void PreDraw()
    {
        GL.glDisable(GL.GL_SCISSOR_TEST);
        GL.glDisable(GL.GL_STENCIL_TEST);
        GL.glDisable(GL.GL_DEPTH_TEST);

        GL.glEnable(GL.GL_BLEND);
        GL.glColorMask(true, true, true, true);

        if (GL.IsPhoneES2)
        {
            GL.glBindVertexArrayOES(0);
        }

        GL.glBindBuffer(GL.GL_ELEMENT_ARRAY_BUFFER, 0);
        GL.glBindBuffer(GL.GL_ARRAY_BUFFER, 0); //前にバッファがバインドされていたら破棄する必要がある

        //異方性フィルタリング。プラットフォームのOpenGLによっては未対応の場合があるので、未設定のときは設定しない
        if (Anisotropy > 0.0f)
        {
            for (int i = 0; i < _textures.Count; i++)
            {
                GL.glBindTexture(GL.GL_TEXTURE_2D, _textures[i]);
                GL.glTexParameterf(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAX_ANISOTROPY_EXT, Anisotropy);
            }
        }
    }

    /// <summary>
    /// モデル描画直前のOpenGLES2のステートを保持する
    /// </summary>
    protected override void SaveProfile()
    {
        _rendererProfile.Save();
    }

    /// <summary>
    /// モデル描画直前のOpenGLES2のステートを保持する
    /// </summary>
    protected override void RestoreProfile()
    {
        _rendererProfile.Restore();
    }

    protected override unsafe void DoDrawModel()
    {
        //------------ クリッピングマスク・バッファ前処理方式の場合 ------------
        if (_clippingManager != null)
        {
            PreDraw();

            // サイズが違う場合はここで作成しなおし
            for (int i = 0; i < _clippingManager.GetRenderTextureCount(); ++i)
            {
                if (_offscreenFrameBuffers[i].BufferWidth != (uint)_clippingManager.GetClippingMaskBufferSize().X ||
                    _offscreenFrameBuffers[i].BufferHeight != (uint)_clippingManager.GetClippingMaskBufferSize().Y)
                {
                    _offscreenFrameBuffers[i].CreateOffscreenFrame(
                        (int)_clippingManager.GetClippingMaskBufferSize().X, (int)_clippingManager.GetClippingMaskBufferSize().Y);
                }
            }

            _clippingManager.SetupClippingContext(Model, this, _rendererProfile._lastFBO, _rendererProfile._lastViewport);
        }

        // 上記クリッピング処理内でも一度PreDrawを呼ぶので注意!!
        PreDraw();

        var drawableCount = Model.GetDrawableCount();
        var renderOrder = Model.GetDrawableRenderOrders();

        // インデックスを描画順でソート
        for (int i = 0; i < drawableCount; ++i)
        {
            var order = renderOrder[i];
            _sortedDrawableIndexList[order] = i;
        }

        GL.glClearColor(0, 0, 0, 0);
        GL.glClear(GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT);

        // 描画
        for (int i = 0; i < drawableCount; ++i)
        {
            var drawableIndex = _sortedDrawableIndexList[i];

            // Drawableが表示状態でなければ処理をパスする
            if (!Model.GetDrawableDynamicFlagIsVisible(drawableIndex))
            {
                continue;
            }

            // クリッピングマスク
            var clipContext = _clippingManager?.GetClippingContextListForDraw()[drawableIndex];

            if (clipContext != null && IsUsingHighPrecisionMask()) // マスクを書く必要がある
            {
                if (clipContext._isUsing) // 書くことになっていた
                {
                    // 生成したFrameBufferと同じサイズでビューポートを設定
                    GL.glViewport(0, 0, (int)_clippingManager!.GetClippingMaskBufferSize().X, (int)_clippingManager.GetClippingMaskBufferSize().Y);

                    PreDraw(); // バッファをクリアする

                    // ---------- マスク描画処理 ----------
                    // マスク用RenderTextureをactiveにセット
                    GetMaskBuffer(clipContext._bufferIndex).BeginDraw(_rendererProfile._lastFBO);

                    // マスクをクリアする
                    // 1が無効（描かれない）領域、0が有効（描かれる）領域。（シェーダで Cd*Csで0に近い値をかけてマスクを作る。1をかけると何も起こらない）
                    GL.glClearColor(1.0f, 1.0f, 1.0f, 1.0f);
                    GL.glClear(GL.GL_COLOR_BUFFER_BIT);
                }

                {
                    var clipDrawCount = clipContext._clippingIdCount;
                    for (int index = 0; index < clipDrawCount; index++)
                    {
                        var clipDrawIndex = clipContext._clippingIdList[index];

                        // 頂点情報が更新されておらず、信頼性がない場合は描画をパスする
                        if (!Model.GetDrawableDynamicFlagVertexPositionsDidChange(clipDrawIndex))
                        {
                            continue;
                        }

                        IsCulling(Model.GetDrawableCulling(clipDrawIndex));

                        // 今回専用の変換を適用して描く
                        // チャンネルも切り替える必要がある(A,R,G,B)
                        ClippingContextBufferForMask = clipContext;

                        DrawMeshOpenGL(
                            Model.GetDrawableTextureIndex(clipDrawIndex),
                            Model.GetDrawableVertexIndexCount(clipDrawIndex),
                            Model.GetDrawableVertexCount(clipDrawIndex),
                            Model.GetDrawableVertexIndices(clipDrawIndex),
                            Model.GetDrawableVertices(clipDrawIndex),
                            (float*)Model.GetDrawableVertexUvs(clipDrawIndex),
                            Model.GetMultiplyColor(clipDrawIndex),
                            Model.GetScreenColor(clipDrawIndex),
                            Model.GetDrawableOpacity(clipDrawIndex),
                            CubismBlendMode.Normal,   //クリッピングは通常描画を強制
                            false // マスク生成時はクリッピングの反転使用は全く関係がない
                        );
                    }
                }

                {
                    // --- 後処理 ---
                    GetMaskBuffer(clipContext._bufferIndex).EndDraw();
                    ClippingContextBufferForMask = null;
                    GL.glViewport(_rendererProfile._lastViewport[0], _rendererProfile._lastViewport[1], _rendererProfile._lastViewport[2], _rendererProfile._lastViewport[3]);

                    PreDraw(); // バッファをクリアする
                }
            }

            // クリッピングマスクをセットする
            ClippingContextBufferForDraw = clipContext;

            IsCulling(Model.GetDrawableCulling(drawableIndex));

            DrawMeshOpenGL(
                Model.GetDrawableTextureIndex(drawableIndex),
                Model.GetDrawableVertexIndexCount(drawableIndex),
                Model.GetDrawableVertexCount(drawableIndex),
                Model.GetDrawableVertexIndices(drawableIndex),
                Model.GetDrawableVertices(drawableIndex),
                (float*)Model.GetDrawableVertexUvs(drawableIndex),
                Model.GetMultiplyColor(drawableIndex),
                Model.GetScreenColor(drawableIndex),
                Model.GetDrawableOpacity(drawableIndex),
                Model.GetDrawableBlendMode(drawableIndex),
                Model.GetDrawableInvertedMask(drawableIndex) // マスクを反転使用するか
            );
        }
    }
}
