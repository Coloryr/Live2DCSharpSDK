using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Type;

namespace Live2DCSharpSDK.Framework.Rendering.OpenGL;

public enum ShaderNames : int
{
    // SetupMask
    ShaderNames_SetupMask,

    //Normal
    ShaderNames_Normal,
    ShaderNames_NormalMasked,
    ShaderNames_NormalMaskedInverted,
    ShaderNames_NormalPremultipliedAlpha,
    ShaderNames_NormalMaskedPremultipliedAlpha,
    ShaderNames_NormalMaskedInvertedPremultipliedAlpha,

    //Add
    ShaderNames_Add,
    ShaderNames_AddMasked,
    ShaderNames_AddMaskedInverted,
    ShaderNames_AddPremultipliedAlpha,
    ShaderNames_AddMaskedPremultipliedAlpha,
    ShaderNames_AddMaskedPremultipliedAlphaInverted,

    //Mult
    ShaderNames_Mult,
    ShaderNames_MultMasked,
    ShaderNames_MultMaskedInverted,
    ShaderNames_MultPremultipliedAlpha,
    ShaderNames_MultMaskedPremultipliedAlpha,
    ShaderNames_MultMaskedPremultipliedAlphaInverted,
};

internal record CubismShaderSet
{
    /// <summary>
    /// シェーダプログラムのアドレス
    /// </summary>
    internal int ShaderProgram;
    /// <summary>
    /// シェーダプログラムに渡す変数のアドレス(Position)
    /// </summary>
    internal int AttributePositionLocation;
    /// <summary>
    /// シェーダプログラムに渡す変数のアドレス(TexCoord)
    /// </summary>
    internal int AttributeTexCoordLocation;
    /// <summary>
    /// シェーダプログラムに渡す変数のアドレス(Matrix)
    /// </summary>
    internal int UniformMatrixLocation;
    /// <summary>
    /// シェーダプログラムに渡す変数のアドレス(ClipMatrix)
    /// </summary>
    internal int UniformClipMatrixLocation;
    /// <summary>
    /// シェーダプログラムに渡す変数のアドレス(Texture0)
    /// </summary>
    internal int SamplerTexture0Location;
    /// <summary>
    /// シェーダプログラムに渡す変数のアドレス(Texture1)
    /// </summary>
    internal int SamplerTexture1Location;
    /// <summary>
    /// シェーダプログラムに渡す変数のアドレス(BaseColor)
    /// </summary>
    internal int UniformBaseColorLocation;
    /// <summary>
    /// シェーダプログラムに渡す変数のアドレス(MultiplyColor)
    /// </summary>
    internal int UniformMultiplyColorLocation;
    /// <summary>
    /// シェーダプログラムに渡す変数のアドレス(ScreenColor)
    /// </summary>
    internal int UniformScreenColorLocation;
    /// <summary>
    /// シェーダプログラムに渡す変数のアドレス(ChannelFlag)
    /// </summary>
    internal int UnifromChannelFlagLocation;
};

internal class CubismShader_OpenGLES2 : IDisposable
{
    private const string ES2 = "#version 100\n";
    private const string ES2C = ES2 + "precision " + OpenGLApi.CSM_FRAGMENT_SHADER_FP_PRECISION + " float;\n";
    private const string Normal = "#version 120\n";

    // SetupMask
    public const string VertShaderSrcSetupMask_ES2 =
        ES2 + VertShaderSrcSetupMask_Base;
    public const string VertShaderSrcSetupMask_Normal =
        Normal + VertShaderSrcSetupMask_Base;
    private const string VertShaderSrcSetupMask_Base =
        @"attribute vec4 a_position;
        attribute vec2 a_texCoord;
        varying vec2 v_texCoord;
        varying vec4 v_myPos;
        uniform mat4 u_clipMatrix;
        void main()
        {
        gl_Position = u_clipMatrix * a_position;
        v_myPos = u_clipMatrix * a_position;
        v_texCoord = a_texCoord;
        v_texCoord.y = 1.0 - v_texCoord.y;
        }";

    public const string FragShaderSrcSetupMask_ES2 = ES2C + FragShaderSrcSetupMask_Base;
    public const string FragShaderSrcSetupMask_Normal = Normal + FragShaderSrcSetupMask_Base;
    private const string FragShaderSrcSetupMask_Base =
        @"varying vec2 v_texCoord;
        varying vec4 v_myPos;
        uniform sampler2D s_texture0;
        uniform vec4 u_channelFlag;
        uniform vec4 u_baseColor;
        void main()
        {
        float isInside = 
          step(u_baseColor.x, v_myPos.x/v_myPos.w)
        * step(u_baseColor.y, v_myPos.y/v_myPos.w)
        * step(v_myPos.x/v_myPos.w, u_baseColor.z)
        * step(v_myPos.y/v_myPos.w, u_baseColor.w);
        gl_FragColor = u_channelFlag * texture2D(s_texture0 , v_texCoord).a * isInside;
        }";

    public const string FragShaderSrcSetupMaskTegra =
        @"#version 100
        #extension GL_NV_shader_framebuffer_fetch : enable
        precision " + OpenGLApi.CSM_FRAGMENT_SHADER_FP_PRECISION + " float;\n"
        + FragShaderSrcSetupMask_Base;

    //----- バーテックスシェーダプログラム -----
    // Normal & Add & Mult 共通
    public const string VertShaderSrc_ES2 =
        ES2 + VertShaderSrc_Base;
    public const string VertShaderSrc_Normal =
        Normal + VertShaderSrc_Base;
    private const string VertShaderSrc_Base =
        @"attribute vec4 a_position;
        attribute vec2 a_texCoord;
        varying vec2 v_texCoord;
        uniform mat4 u_matrix;
        void main()
        {
        gl_Position = u_matrix * a_position;
        v_texCoord = a_texCoord;
        v_texCoord.y = 1.0 - v_texCoord.y;
        }";

    // Normal & Add & Mult 共通（クリッピングされたものの描画用）
    public const string VertShaderSrcMasked_ES2 =
        ES2 + VertShaderSrcMasked_Base;
    public const string VertShaderSrcMasked_Normal =
        Normal + VertShaderSrcMasked_Base;
    private const string VertShaderSrcMasked_Base =
        @"attribute vec4 a_position;
        attribute vec2 a_texCoord;
        varying vec2 v_texCoord;
        varying vec4 v_clipPos;
        uniform mat4 u_matrix;
        uniform mat4 u_clipMatrix;
        void main()
        {
        gl_Position = u_matrix * a_position;
        v_clipPos = u_clipMatrix * a_position;
        v_texCoord = a_texCoord;
        v_texCoord.y = 1.0 - v_texCoord.y;
        }";

    //----- フラグメントシェーダプログラム -----
    // Normal & Add & Mult 共通
    public const string FragShaderSrc_ES2 = ES2C + FragShaderSrc_Base;
    public const string FragShaderSrc_Normal = Normal + FragShaderSrc_Base;
    public const string FragShaderSrc_Base =
    @"varying vec2 v_texCoord;
        uniform sampler2D s_texture0;
        uniform vec4 u_baseColor;
        uniform vec4 u_multiplyColor;
        uniform vec4 u_screenColor;
        void main()
        {
        vec4 texColor = texture2D(s_texture0 , v_texCoord);
        texColor.rgb = texColor.rgb * u_multiplyColor.rgb;
        texColor.rgb = texColor.rgb + u_screenColor.rgb - (texColor.rgb * u_screenColor.rgb);
        vec4 color = texColor * u_baseColor;
        gl_FragColor = vec4(color.rgb * color.a,  color.a);
        }";

    public const string FragShaderSrcTegra =
        @"#version 100
        #extension GL_NV_shader_framebuffer_fetch : enable
        precision " + OpenGLApi.CSM_FRAGMENT_SHADER_FP_PRECISION + " float;\n"
        + FragShaderSrc_Base;

    // Normal & Add & Mult 共通 （PremultipliedAlpha）
    public const string FragShaderSrcPremultipliedAlpha_ES2 = ES2C + FragShaderSrcPremultipliedAlpha_Base;
    public const string FragShaderSrcPremultipliedAlpha_Normal = Normal + FragShaderSrcPremultipliedAlpha_Base;
    public const string FragShaderSrcPremultipliedAlpha_Base =
        @"varying vec2 v_texCoord;
        uniform sampler2D s_texture0;
        uniform vec4 u_baseColor;
        uniform vec4 u_multiplyColor;
        uniform vec4 u_screenColor;
        void main()
        {
        vec4 texColor = texture2D(s_texture0 , v_texCoord);
        texColor.rgb = texColor.rgb * u_multiplyColor.rgb;
        texColor.rgb = (texColor.rgb + u_screenColor.rgb * texColor.a) - (texColor.rgb * u_screenColor.rgb);
        gl_FragColor = texColor * u_baseColor;
        }";

    public const string FragShaderSrcPremultipliedAlphaTegra =
        @"#version 100
        #extension GL_NV_shader_framebuffer_fetch : enable
        precision " + OpenGLApi.CSM_FRAGMENT_SHADER_FP_PRECISION + " float;\n"
        + FragShaderSrcPremultipliedAlpha_Base;

    // Normal & Add & Mult 共通（クリッピングされたものの描画用）
    public const string FragShaderSrcMask_ES2 = ES2C + FragShaderSrcMask_Base;
    public const string FragShaderSrcMask_Normal = Normal + FragShaderSrcMask_Base;
    public const string FragShaderSrcMask_Base =
        @"varying vec2 v_texCoord;
        varying vec4 v_clipPos;
        uniform sampler2D s_texture0;
        uniform sampler2D s_texture1;
        uniform vec4 u_channelFlag;
        uniform vec4 u_baseColor;
        uniform vec4 u_multiplyColor;
        uniform vec4 u_screenColor;
        void main()
        {
        vec4 texColor = texture2D(s_texture0 , v_texCoord);
        texColor.rgb = texColor.rgb * u_multiplyColor.rgb;
        texColor.rgb = texColor.rgb + u_screenColor.rgb - (texColor.rgb * u_screenColor.rgb);
        vec4 col_formask = texColor * u_baseColor;
        col_formask.rgb = col_formask.rgb  * col_formask.a ;
        vec4 clipMask = (1.0 - texture2D(s_texture1, v_clipPos.xy / v_clipPos.w)) * u_channelFlag;
        float maskVal = clipMask.r + clipMask.g + clipMask.b + clipMask.a;
        col_formask = col_formask * maskVal;
        gl_FragColor = col_formask;
        }";

    public const string FragShaderSrcMaskTegra =
        @"#version 100
        #extension GL_NV_shader_framebuffer_fetch : enable
        precision " + OpenGLApi.CSM_FRAGMENT_SHADER_FP_PRECISION + " float;\n"
        + FragShaderSrcMask_Base;

    // Normal & Add & Mult 共通（クリッピングされて反転使用の描画用）
    public const string FragShaderSrcMaskInverted_ES2 = ES2C + FragShaderSrcMaskInverted_Base;
    public const string FragShaderSrcMaskInverted_Normal = Normal + FragShaderSrcMaskInverted_Base;
    public const string FragShaderSrcMaskInverted_Base =
        @"varying vec2 v_texCoord;
        varying vec4 v_clipPos;
        uniform sampler2D s_texture0;
        uniform sampler2D s_texture1;
        uniform vec4 u_channelFlag;
        uniform vec4 u_baseColor;
        uniform vec4 u_multiplyColor;
        uniform vec4 u_screenColor;
        void main()
        {
        vec4 texColor = texture2D(s_texture0 , v_texCoord);
        texColor.rgb = texColor.rgb * u_multiplyColor.rgb;
        texColor.rgb = texColor.rgb + u_screenColor.rgb - (texColor.rgb * u_screenColor.rgb);
        vec4 col_formask = texColor * u_baseColor;
        col_formask.rgb = col_formask.rgb  * col_formask.a ;
        vec4 clipMask = (1.0 - texture2D(s_texture1, v_clipPos.xy / v_clipPos.w)) * u_channelFlag;
        float maskVal = clipMask.r + clipMask.g + clipMask.b + clipMask.a;
        col_formask = col_formask * (1.0 - maskVal);
        gl_FragColor = col_formask;
        }";

    public const string FragShaderSrcMaskInvertedTegra =
        @"#version 100
        #extension GL_NV_shader_framebuffer_fetch : enable
        precision " + OpenGLApi.CSM_FRAGMENT_SHADER_FP_PRECISION + " float;\n"
        + FragShaderSrcMaskInverted_Base;

    // Normal & Add & Mult 共通（クリッピングされたものの描画用、PremultipliedAlphaの場合）
    public const string FragShaderSrcMaskPremultipliedAlpha_ES2 = ES2C + FragShaderSrcMaskPremultipliedAlpha_Base;
    public const string FragShaderSrcMaskPremultipliedAlpha_Normal = Normal + FragShaderSrcMaskPremultipliedAlpha_Base;
    public const string FragShaderSrcMaskPremultipliedAlpha_Base =
        @"varying vec2 v_texCoord;
        varying vec4 v_clipPos;
        uniform sampler2D s_texture0;
        uniform sampler2D s_texture1;
        uniform vec4 u_channelFlag;
        uniform vec4 u_baseColor;
        uniform vec4 u_multiplyColor;
        uniform vec4 u_screenColor;
        void main()
        {
        vec4 texColor = texture2D(s_texture0 , v_texCoord);
        texColor.rgb = texColor.rgb * u_multiplyColor.rgb;
        texColor.rgb = (texColor.rgb + u_screenColor.rgb * texColor.a) - (texColor.rgb * u_screenColor.rgb);
        vec4 col_formask = texColor * u_baseColor;
        vec4 clipMask = (1.0 - texture2D(s_texture1, v_clipPos.xy / v_clipPos.w)) * u_channelFlag;
        float maskVal = clipMask.r + clipMask.g + clipMask.b + clipMask.a;
        col_formask = col_formask * maskVal;
        gl_FragColor = col_formask;
        }";

    public const string FragShaderSrcMaskPremultipliedAlphaTegra =
        @"#version 100
        #extension GL_NV_shader_framebuffer_fetch : enable\n
        precision " + OpenGLApi.CSM_FRAGMENT_SHADER_FP_PRECISION + " float;\n"
        + FragShaderSrcMaskPremultipliedAlpha_Base;

    // Normal & Add & Mult 共通（クリッピングされて反転使用の描画用、PremultipliedAlphaの場合）
    public const string FragShaderSrcMaskInvertedPremultipliedAlpha_ES2 = ES2C + FragShaderSrcMaskInvertedPremultipliedAlpha_Base;
    public const string FragShaderSrcMaskInvertedPremultipliedAlpha_Normal = Normal + FragShaderSrcMaskInvertedPremultipliedAlpha_Base;
    public const string FragShaderSrcMaskInvertedPremultipliedAlpha_Base =
        @"varying vec2 v_texCoord;
        varying vec4 v_clipPos;
        uniform sampler2D s_texture0;
        uniform sampler2D s_texture1;
        uniform vec4 u_channelFlag;
        uniform vec4 u_baseColor;
        uniform vec4 u_multiplyColor;
        uniform vec4 u_screenColor;
        void main()
        {
        vec4 texColor = texture2D(s_texture0 , v_texCoord);
        texColor.rgb = texColor.rgb * u_multiplyColor.rgb;
        texColor.rgb = (texColor.rgb + u_screenColor.rgb * texColor.a) - (texColor.rgb * u_screenColor.rgb);
        vec4 col_formask = texColor * u_baseColor;
        vec4 clipMask = (1.0 - texture2D(s_texture1, v_clipPos.xy / v_clipPos.w)) * u_channelFlag;
        float maskVal = clipMask.r + clipMask.g + clipMask.b + clipMask.a;
        col_formask = col_formask * (1.0 - maskVal);
        gl_FragColor = col_formask;
        }";

    public const string FragShaderSrcMaskInvertedPremultipliedAlphaTegra =
        @"#version 100
        #extension GL_NV_shader_framebuffer_fetch : enable
        precision " + OpenGLApi.CSM_FRAGMENT_SHADER_FP_PRECISION + " float;\n"
        + FragShaderSrcMaskInvertedPremultipliedAlpha_Base;

    public const int ShaderCount = 19; ///< シェーダの数 = マスク生成用 + (通常 + 加算 + 乗算) * (マスク無 + マスク有 + マスク有反転 + マスク無の乗算済アルファ対応版 + マスク有の乗算済アルファ対応版 + マスク有反転の乗算済アルファ対応版)
    public static CubismShader_OpenGLES2 s_instance;

    private readonly OpenGLApi GL;
    /// <summary>
    /// Tegra対応.拡張方式で描画
    /// </summary>
    internal static bool s_extMode;

    /// <summary>
    /// 拡張方式のPA設定用の変数
    /// </summary>
    internal static bool s_extPAMode;

    /// <summary>
    /// ロードしたシェーダプログラムを保持する変数
    /// </summary>
    private List<CubismShaderSet> _shaderSets = new();

    public CubismShader_OpenGLES2(OpenGLApi gl)
    {
        GL = gl;
    }

    public void Dispose()
    {
        ReleaseShaderProgram();
    }

    /// <summary>
    /// インスタンスを取得する（シングルトン）。
    /// </summary>
    /// <returns>インスタンスのポインタ</returns>
    internal static CubismShader_OpenGLES2 GetInstance(OpenGLApi gl)
    {
        if (s_instance == null)
        {
            s_instance = new CubismShader_OpenGLES2(gl);
        }
        return s_instance;
    }

    /// <summary>
    /// インスタンスを解放する（シングルトン）。
    /// </summary>
    internal static void DeleteInstance()
    {
        if (s_instance != null)
        {
            s_instance.Dispose();
            s_instance = null;
        }
    }

    /// <summary>
    /// シェーダプログラムの一連のセットアップを実行する
    /// </summary>
    /// <param name="renderer">レンダラのインスタンス</param>
    /// <param name="textureId">GPUのテクスチャID</param>
    /// <param name="vertexCount">ポリゴンメッシュの頂点数</param>
    /// <param name="vertexArray">ポリゴンメッシュの頂点配列</param>
    /// <param name="uvArray">uv配列</param>
    /// <param name="opacity">不透明度</param>
    /// <param name="colorBlendMode">カラーブレンディングのタイプ</param>
    /// <param name="baseColor">ベースカラー</param>
    /// <param name="multiplyColor"></param>
    /// <param name="screenColor"></param>
    /// <param name="isPremultipliedAlpha">乗算済みアルファかどうか</param>
    /// <param name="matrix4x4">Model-View-Projection行列</param>
    /// <param name="invertedMask">マスクを反転して使用するフラグ</param>
    internal unsafe void SetupShaderProgram(CubismRenderer_OpenGLES2 renderer, int textureId
                            , int vertexCount, float* vertexArray
                            , float* uvArray, float opacity
                            , CubismBlendMode colorBlendMode
                            , CubismTextureColor baseColor
                            , CubismTextureColor multiplyColor
                            , CubismTextureColor screenColor
                            , bool isPremultipliedAlpha, CubismMatrix44 matrix4x4
                            , bool invertedMask)
    {
        if (_shaderSets.Count == 0)
        {
            GenerateShaders();
        }

        // Blending
        int SRC_COLOR;
        int DST_COLOR;
        int SRC_ALPHA;
        int DST_ALPHA;

        if (renderer.GetClippingContextBufferForMask() != null) // マスク生成時
        {
            var shaderSet = _shaderSets[(int)ShaderNames.ShaderNames_SetupMask];
            GL.glUseProgram(shaderSet.ShaderProgram);

            //テクスチャ設定
            GL.glActiveTexture(GL.GL_TEXTURE0);
            GL.glBindTexture(GL.GL_TEXTURE_2D, textureId);
            GL.glUniform1i(shaderSet.SamplerTexture0Location, 0);

            // 頂点配列の設定
            GL.glEnableVertexAttribArray(shaderSet.AttributePositionLocation);
            GL.glVertexAttribPointer(shaderSet.AttributePositionLocation, 2, GL.GL_FLOAT, false, sizeof(float) * 2, vertexArray);
            // テクスチャ頂点の設定
            GL.glEnableVertexAttribArray(shaderSet.AttributeTexCoordLocation);
            GL.glVertexAttribPointer(shaderSet.AttributeTexCoordLocation, 2, GL.GL_FLOAT, false, sizeof(float) * 2, uvArray);

            // チャンネル
            var channelNo = renderer.GetClippingContextBufferForMask()._layoutChannelNo;
            var colorChannel = renderer.GetClippingContextBufferForMask().GetClippingManager().GetChannelFlagAsColor(channelNo);
            GL.glUniform4f(shaderSet.UnifromChannelFlagLocation, colorChannel.R, colorChannel.G, colorChannel.B, colorChannel.A);

            GL.glUniformMatrix4fv(shaderSet.UniformClipMatrixLocation, 1, false, renderer.GetClippingContextBufferForMask()._matrixForMask.GetArray());

            csmRectF rect = renderer.GetClippingContextBufferForMask()._layoutBounds;

            GL.glUniform4f(shaderSet.UniformBaseColorLocation,
                        rect.X * 2.0f - 1.0f,
                        rect.Y * 2.0f - 1.0f,
                        rect.GetRight() * 2.0f - 1.0f,
                        rect.GetBottom() * 2.0f - 1.0f);
            GL.glUniform4f(shaderSet.UniformMultiplyColorLocation, multiplyColor.R, multiplyColor.G, multiplyColor.B, multiplyColor.A);
            GL.glUniform4f(shaderSet.UniformScreenColorLocation, screenColor.R, screenColor.G, screenColor.B, screenColor.A);

            SRC_COLOR = GL.GL_ZERO;
            DST_COLOR = GL.GL_ONE_MINUS_SRC_COLOR;
            SRC_ALPHA = GL.GL_ZERO;
            DST_ALPHA = GL.GL_ONE_MINUS_SRC_ALPHA;
        }
        else // マスク生成以外の場合
        {
            bool masked = renderer.GetClippingContextBufferForDraw() != null;  // この描画オブジェクトはマスク対象か
            var offset = (masked ? (invertedMask ? 2 : 1) : 0) + (isPremultipliedAlpha ? 3 : 0);

            CubismShaderSet shaderSet;
            switch (colorBlendMode)
            {
                case CubismBlendMode.CubismBlendMode_Normal:
                default:
                    shaderSet = _shaderSets[(int)ShaderNames.ShaderNames_Normal + offset];
                    SRC_COLOR = GL.GL_ONE;
                    DST_COLOR = GL.GL_ONE_MINUS_SRC_ALPHA;
                    SRC_ALPHA = GL.GL_ONE;
                    DST_ALPHA = GL.GL_ONE_MINUS_SRC_ALPHA;
                    break;

                case CubismBlendMode.CubismBlendMode_Additive:
                    shaderSet = _shaderSets[(int)ShaderNames.ShaderNames_Add + offset];
                    SRC_COLOR = GL.GL_ONE;
                    DST_COLOR = GL.GL_ONE;
                    SRC_ALPHA = GL.GL_ZERO;
                    DST_ALPHA = GL.GL_ONE;
                    break;

                case CubismBlendMode.CubismBlendMode_Multiplicative:
                    shaderSet = _shaderSets[(int)ShaderNames.ShaderNames_Mult + offset];
                    SRC_COLOR = GL.GL_DST_COLOR;
                    DST_COLOR = GL.GL_ONE_MINUS_SRC_ALPHA;
                    SRC_ALPHA = GL.GL_ZERO;
                    DST_ALPHA = GL.GL_ONE;
                    break;
            }

            GL.glUseProgram(shaderSet.ShaderProgram);

            // 頂点配列の設定
            GL.glEnableVertexAttribArray(shaderSet.AttributePositionLocation);
            GL.glVertexAttribPointer(shaderSet.AttributePositionLocation, 2, GL.GL_FLOAT, false, sizeof(float) * 2, vertexArray);
            // テクスチャ頂点の設定
            GL.glEnableVertexAttribArray(shaderSet.AttributeTexCoordLocation);
            GL.glVertexAttribPointer(shaderSet.AttributeTexCoordLocation, 2, GL.GL_FLOAT, false, sizeof(float) * 2, uvArray);

            if (masked)
            {
                GL.glActiveTexture(GL.GL_TEXTURE1);

                // frameBufferに書かれたテクスチャ
                var tex = renderer.GetMaskBuffer(renderer.GetClippingContextBufferForDraw()._bufferIndex).GetColorBuffer();

                GL.glBindTexture(GL.GL_TEXTURE_2D, tex);
                GL.glUniform1i(shaderSet.SamplerTexture1Location, 1);

                // View座標をClippingContextの座標に変換するための行列を設定
                GL.glUniformMatrix4fv(shaderSet.UniformClipMatrixLocation, 1, false, renderer.GetClippingContextBufferForDraw()._matrixForDraw.GetArray());

                // 使用するカラーチャンネルを設定
                var channelNo = renderer.GetClippingContextBufferForDraw()._layoutChannelNo;
                var colorChannel = renderer.GetClippingContextBufferForDraw().GetClippingManager().GetChannelFlagAsColor(channelNo);
                GL.glUniform4f(shaderSet.UnifromChannelFlagLocation, colorChannel.R, colorChannel.G, colorChannel.B, colorChannel.A);
            }

            //テクスチャ設定
            GL.glActiveTexture(GL.GL_TEXTURE0);
            GL.glBindTexture(GL.GL_TEXTURE_2D, textureId);
            GL.glUniform1i(shaderSet.SamplerTexture0Location, 0);

            //座標変換
            GL.glUniformMatrix4fv(shaderSet.UniformMatrixLocation, 1, false, matrix4x4.GetArray()); //

            GL.glUniform4f(shaderSet.UniformBaseColorLocation, baseColor.R, baseColor.G, baseColor.B, baseColor.A);
            GL.glUniform4f(shaderSet.UniformMultiplyColorLocation, multiplyColor.R, multiplyColor.G, multiplyColor.B, multiplyColor.A);
            GL.glUniform4f(shaderSet.UniformScreenColorLocation, screenColor.R, screenColor.G, screenColor.B, screenColor.A);
        }

        GL.glBlendFuncSeparate((int)SRC_COLOR, (int)DST_COLOR, (int)SRC_ALPHA, (int)DST_ALPHA);
    }

    /// <summary>
    /// シェーダプログラムを解放する
    /// </summary>
    internal void ReleaseShaderProgram()
    {
        for (int i = 0; i < _shaderSets.Count; i++)
        {
            if (_shaderSets[i].ShaderProgram != 0)
            {
                GL.glDeleteProgram(_shaderSets[i].ShaderProgram);
                _shaderSets[i].ShaderProgram = 0;
            }
        }
    }

    /// <summary>
    /// シェーダプログラムを初期化する
    /// </summary>
    internal void GenerateShaders()
    {
        for (int i = 0; i < ShaderCount; i++)
        {
            _shaderSets.Add(new CubismShaderSet());
        }

        if (GL.IsES2)
        {
            if (s_extMode)
            {
                _shaderSets[0].ShaderProgram = LoadShaderProgram(VertShaderSrcSetupMask_ES2, FragShaderSrcSetupMaskTegra);

                _shaderSets[1].ShaderProgram = LoadShaderProgram(VertShaderSrc_ES2, FragShaderSrcTegra);
                _shaderSets[2].ShaderProgram = LoadShaderProgram(VertShaderSrcMasked_ES2, FragShaderSrcMaskTegra);
                _shaderSets[3].ShaderProgram = LoadShaderProgram(VertShaderSrcMasked_ES2, FragShaderSrcMaskInvertedTegra);
                _shaderSets[4].ShaderProgram = LoadShaderProgram(VertShaderSrc_ES2, FragShaderSrcPremultipliedAlphaTegra);
                _shaderSets[5].ShaderProgram = LoadShaderProgram(VertShaderSrcMasked_ES2, FragShaderSrcMaskPremultipliedAlphaTegra);
                _shaderSets[6].ShaderProgram = LoadShaderProgram(VertShaderSrcMasked_ES2, FragShaderSrcMaskInvertedPremultipliedAlphaTegra);
            }
            else
            {
                _shaderSets[0].ShaderProgram = LoadShaderProgram(VertShaderSrcSetupMask_ES2, FragShaderSrcSetupMask_ES2);

                _shaderSets[1].ShaderProgram = LoadShaderProgram(VertShaderSrc_ES2, FragShaderSrc_ES2);
                _shaderSets[2].ShaderProgram = LoadShaderProgram(VertShaderSrcMasked_ES2, FragShaderSrcMask_ES2);
                _shaderSets[3].ShaderProgram = LoadShaderProgram(VertShaderSrcMasked_ES2, FragShaderSrcMaskInverted_ES2);
                _shaderSets[4].ShaderProgram = LoadShaderProgram(VertShaderSrc_ES2, FragShaderSrcPremultipliedAlpha_ES2);
                _shaderSets[5].ShaderProgram = LoadShaderProgram(VertShaderSrcMasked_ES2, FragShaderSrcMaskPremultipliedAlpha_ES2);
                _shaderSets[6].ShaderProgram = LoadShaderProgram(VertShaderSrcMasked_ES2, FragShaderSrcMaskInvertedPremultipliedAlpha_ES2);
            }

            // 加算も通常と同じシェーダーを利用する
            _shaderSets[7].ShaderProgram = _shaderSets[1].ShaderProgram;
            _shaderSets[8].ShaderProgram = _shaderSets[2].ShaderProgram;
            _shaderSets[9].ShaderProgram = _shaderSets[3].ShaderProgram;
            _shaderSets[10].ShaderProgram = _shaderSets[4].ShaderProgram;
            _shaderSets[11].ShaderProgram = _shaderSets[5].ShaderProgram;
            _shaderSets[12].ShaderProgram = _shaderSets[6].ShaderProgram;

            // 乗算も通常と同じシェーダーを利用する
            _shaderSets[13].ShaderProgram = _shaderSets[1].ShaderProgram;
            _shaderSets[14].ShaderProgram = _shaderSets[2].ShaderProgram;
            _shaderSets[15].ShaderProgram = _shaderSets[3].ShaderProgram;
            _shaderSets[16].ShaderProgram = _shaderSets[4].ShaderProgram;
            _shaderSets[17].ShaderProgram = _shaderSets[5].ShaderProgram;
            _shaderSets[18].ShaderProgram = _shaderSets[6].ShaderProgram;
        }
        else
        {
            _shaderSets[0].ShaderProgram = LoadShaderProgram(VertShaderSrcSetupMask_Normal, FragShaderSrcSetupMask_Normal);

            _shaderSets[1].ShaderProgram = LoadShaderProgram(VertShaderSrc_Normal, FragShaderSrc_Normal);
            _shaderSets[2].ShaderProgram = LoadShaderProgram(VertShaderSrcMasked_Normal, FragShaderSrcMask_Normal);
            _shaderSets[3].ShaderProgram = LoadShaderProgram(VertShaderSrcMasked_Normal, FragShaderSrcMaskInverted_Normal);
            _shaderSets[4].ShaderProgram = LoadShaderProgram(VertShaderSrc_Normal, FragShaderSrcPremultipliedAlpha_Normal);
            _shaderSets[5].ShaderProgram = LoadShaderProgram(VertShaderSrcMasked_Normal, FragShaderSrcMaskPremultipliedAlpha_Normal);
            _shaderSets[6].ShaderProgram = LoadShaderProgram(VertShaderSrcMasked_Normal, FragShaderSrcMaskInvertedPremultipliedAlpha_Normal);

            // 加算も通常と同じシェーダーを利用する
            _shaderSets[7].ShaderProgram = _shaderSets[1].ShaderProgram;
            _shaderSets[8].ShaderProgram = _shaderSets[2].ShaderProgram;
            _shaderSets[9].ShaderProgram = _shaderSets[3].ShaderProgram;
            _shaderSets[10].ShaderProgram = _shaderSets[4].ShaderProgram;
            _shaderSets[11].ShaderProgram = _shaderSets[5].ShaderProgram;
            _shaderSets[12].ShaderProgram = _shaderSets[6].ShaderProgram;

            // 乗算も通常と同じシェーダーを利用する
            _shaderSets[13].ShaderProgram = _shaderSets[1].ShaderProgram;
            _shaderSets[14].ShaderProgram = _shaderSets[2].ShaderProgram;
            _shaderSets[15].ShaderProgram = _shaderSets[3].ShaderProgram;
            _shaderSets[16].ShaderProgram = _shaderSets[4].ShaderProgram;
            _shaderSets[17].ShaderProgram = _shaderSets[5].ShaderProgram;
            _shaderSets[18].ShaderProgram = _shaderSets[6].ShaderProgram;
        }

        // SetupMask
        _shaderSets[0].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[0].ShaderProgram, "a_position");
        _shaderSets[0].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[0].ShaderProgram, "a_texCoord");
        _shaderSets[0].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[0].ShaderProgram, "s_texture0");
        _shaderSets[0].UniformClipMatrixLocation = GL.glGetUniformLocation(_shaderSets[0].ShaderProgram, "u_clipMatrix");
        _shaderSets[0].UnifromChannelFlagLocation = GL.glGetUniformLocation(_shaderSets[0].ShaderProgram, "u_channelFlag");
        _shaderSets[0].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[0].ShaderProgram, "u_baseColor");
        _shaderSets[0].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[0].ShaderProgram, "u_multiplyColor");
        _shaderSets[0].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[0].ShaderProgram, "u_screenColor");

        // 通常
        _shaderSets[1].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[1].ShaderProgram, "a_position");
        _shaderSets[1].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[1].ShaderProgram, "a_texCoord");
        _shaderSets[1].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[1].ShaderProgram, "s_texture0");
        _shaderSets[1].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[1].ShaderProgram, "u_matrix");
        _shaderSets[1].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[1].ShaderProgram, "u_baseColor");
        _shaderSets[1].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[1].ShaderProgram, "u_multiplyColor");
        _shaderSets[1].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[1].ShaderProgram, "u_screenColor");

        // 通常（クリッピング）
        _shaderSets[2].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[2].ShaderProgram, "a_position");
        _shaderSets[2].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[2].ShaderProgram, "a_texCoord");
        _shaderSets[2].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[2].ShaderProgram, "s_texture0");
        _shaderSets[2].SamplerTexture1Location = GL.glGetUniformLocation(_shaderSets[2].ShaderProgram, "s_texture1");
        _shaderSets[2].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[2].ShaderProgram, "u_matrix");
        _shaderSets[2].UniformClipMatrixLocation = GL.glGetUniformLocation(_shaderSets[2].ShaderProgram, "u_clipMatrix");
        _shaderSets[2].UnifromChannelFlagLocation = GL.glGetUniformLocation(_shaderSets[2].ShaderProgram, "u_channelFlag");
        _shaderSets[2].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[2].ShaderProgram, "u_baseColor");
        _shaderSets[2].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[2].ShaderProgram, "u_multiplyColor");
        _shaderSets[2].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[2].ShaderProgram, "u_screenColor");

        // 通常（クリッピング・反転）
        _shaderSets[3].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[3].ShaderProgram, "a_position");
        _shaderSets[3].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[3].ShaderProgram, "a_texCoord");
        _shaderSets[3].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[3].ShaderProgram, "s_texture0");
        _shaderSets[3].SamplerTexture1Location = GL.glGetUniformLocation(_shaderSets[3].ShaderProgram, "s_texture1");
        _shaderSets[3].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[3].ShaderProgram, "u_matrix");
        _shaderSets[3].UniformClipMatrixLocation = GL.glGetUniformLocation(_shaderSets[3].ShaderProgram, "u_clipMatrix");
        _shaderSets[3].UnifromChannelFlagLocation = GL.glGetUniformLocation(_shaderSets[3].ShaderProgram, "u_channelFlag");
        _shaderSets[3].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[3].ShaderProgram, "u_baseColor");
        _shaderSets[3].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[3].ShaderProgram, "u_multiplyColor");
        _shaderSets[3].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[3].ShaderProgram, "u_screenColor");

        // 通常（PremultipliedAlpha）
        _shaderSets[4].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[4].ShaderProgram, "a_position");
        _shaderSets[4].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[4].ShaderProgram, "a_texCoord");
        _shaderSets[4].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[4].ShaderProgram, "s_texture0");
        _shaderSets[4].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[4].ShaderProgram, "u_matrix");
        _shaderSets[4].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[4].ShaderProgram, "u_baseColor");
        _shaderSets[4].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[4].ShaderProgram, "u_multiplyColor");
        _shaderSets[4].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[4].ShaderProgram, "u_screenColor");

        // 通常（クリッピング、PremultipliedAlpha）
        _shaderSets[5].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[5].ShaderProgram, "a_position");
        _shaderSets[5].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[5].ShaderProgram, "a_texCoord");
        _shaderSets[5].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[5].ShaderProgram, "s_texture0");
        _shaderSets[5].SamplerTexture1Location = GL.glGetUniformLocation(_shaderSets[5].ShaderProgram, "s_texture1");
        _shaderSets[5].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[5].ShaderProgram, "u_matrix");
        _shaderSets[5].UniformClipMatrixLocation = GL.glGetUniformLocation(_shaderSets[5].ShaderProgram, "u_clipMatrix");
        _shaderSets[5].UnifromChannelFlagLocation = GL.glGetUniformLocation(_shaderSets[5].ShaderProgram, "u_channelFlag");
        _shaderSets[5].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[5].ShaderProgram, "u_baseColor");
        _shaderSets[5].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[5].ShaderProgram, "u_multiplyColor");
        _shaderSets[5].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[5].ShaderProgram, "u_screenColor");

        // 通常（クリッピング・反転、PremultipliedAlpha）
        _shaderSets[6].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[6].ShaderProgram, "a_position");
        _shaderSets[6].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[6].ShaderProgram, "a_texCoord");
        _shaderSets[6].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[6].ShaderProgram, "s_texture0");
        _shaderSets[6].SamplerTexture1Location = GL.glGetUniformLocation(_shaderSets[6].ShaderProgram, "s_texture1");
        _shaderSets[6].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[6].ShaderProgram, "u_matrix");
        _shaderSets[6].UniformClipMatrixLocation = GL.glGetUniformLocation(_shaderSets[6].ShaderProgram, "u_clipMatrix");
        _shaderSets[6].UnifromChannelFlagLocation = GL.glGetUniformLocation(_shaderSets[6].ShaderProgram, "u_channelFlag");
        _shaderSets[6].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[6].ShaderProgram, "u_baseColor");
        _shaderSets[6].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[6].ShaderProgram, "u_multiplyColor");
        _shaderSets[6].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[6].ShaderProgram, "u_screenColor");

        // 加算
        _shaderSets[7].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[7].ShaderProgram, "a_position");
        _shaderSets[7].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[7].ShaderProgram, "a_texCoord");
        _shaderSets[7].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[7].ShaderProgram, "s_texture0");
        _shaderSets[7].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[7].ShaderProgram, "u_matrix");
        _shaderSets[7].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[7].ShaderProgram, "u_baseColor");
        _shaderSets[7].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[7].ShaderProgram, "u_multiplyColor");
        _shaderSets[7].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[7].ShaderProgram, "u_screenColor");

        // 加算（クリッピング）
        _shaderSets[8].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[8].ShaderProgram, "a_position");
        _shaderSets[8].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[8].ShaderProgram, "a_texCoord");
        _shaderSets[8].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[8].ShaderProgram, "s_texture0");
        _shaderSets[8].SamplerTexture1Location = GL.glGetUniformLocation(_shaderSets[8].ShaderProgram, "s_texture1");
        _shaderSets[8].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[8].ShaderProgram, "u_matrix");
        _shaderSets[8].UniformClipMatrixLocation = GL.glGetUniformLocation(_shaderSets[8].ShaderProgram, "u_clipMatrix");
        _shaderSets[8].UnifromChannelFlagLocation = GL.glGetUniformLocation(_shaderSets[8].ShaderProgram, "u_channelFlag");
        _shaderSets[8].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[8].ShaderProgram, "u_baseColor");
        _shaderSets[8].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[8].ShaderProgram, "u_multiplyColor");
        _shaderSets[8].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[8].ShaderProgram, "u_screenColor");

        // 加算（クリッピング・反転）
        _shaderSets[9].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[9].ShaderProgram, "a_position");
        _shaderSets[9].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[9].ShaderProgram, "a_texCoord");
        _shaderSets[9].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[9].ShaderProgram, "s_texture0");
        _shaderSets[9].SamplerTexture1Location = GL.glGetUniformLocation(_shaderSets[9].ShaderProgram, "s_texture1");
        _shaderSets[9].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[9].ShaderProgram, "u_matrix");
        _shaderSets[9].UniformClipMatrixLocation = GL.glGetUniformLocation(_shaderSets[9].ShaderProgram, "u_clipMatrix");
        _shaderSets[9].UnifromChannelFlagLocation = GL.glGetUniformLocation(_shaderSets[9].ShaderProgram, "u_channelFlag");
        _shaderSets[9].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[9].ShaderProgram, "u_baseColor");
        _shaderSets[9].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[9].ShaderProgram, "u_multiplyColor");
        _shaderSets[9].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[9].ShaderProgram, "u_screenColor");

        // 加算（PremultipliedAlpha）
        _shaderSets[10].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[10].ShaderProgram, "a_position");
        _shaderSets[10].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[10].ShaderProgram, "a_texCoord");
        _shaderSets[10].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[10].ShaderProgram, "s_texture0");
        _shaderSets[10].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[10].ShaderProgram, "u_matrix");
        _shaderSets[10].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[10].ShaderProgram, "u_baseColor");
        _shaderSets[10].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[10].ShaderProgram, "u_multiplyColor");
        _shaderSets[10].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[10].ShaderProgram, "u_screenColor");

        // 加算（クリッピング、PremultipliedAlpha）
        _shaderSets[11].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[11].ShaderProgram, "a_position");
        _shaderSets[11].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[11].ShaderProgram, "a_texCoord");
        _shaderSets[11].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[11].ShaderProgram, "s_texture0");
        _shaderSets[11].SamplerTexture1Location = GL.glGetUniformLocation(_shaderSets[11].ShaderProgram, "s_texture1");
        _shaderSets[11].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[11].ShaderProgram, "u_matrix");
        _shaderSets[11].UniformClipMatrixLocation = GL.glGetUniformLocation(_shaderSets[11].ShaderProgram, "u_clipMatrix");
        _shaderSets[11].UnifromChannelFlagLocation = GL.glGetUniformLocation(_shaderSets[11].ShaderProgram, "u_channelFlag");
        _shaderSets[11].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[11].ShaderProgram, "u_baseColor");
        _shaderSets[11].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[11].ShaderProgram, "u_multiplyColor");
        _shaderSets[11].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[11].ShaderProgram, "u_screenColor");

        // 加算（クリッピング・反転、PremultipliedAlpha）
        _shaderSets[12].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[12].ShaderProgram, "a_position");
        _shaderSets[12].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[12].ShaderProgram, "a_texCoord");
        _shaderSets[12].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[12].ShaderProgram, "s_texture0");
        _shaderSets[12].SamplerTexture1Location = GL.glGetUniformLocation(_shaderSets[12].ShaderProgram, "s_texture1");
        _shaderSets[12].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[12].ShaderProgram, "u_matrix");
        _shaderSets[12].UniformClipMatrixLocation = GL.glGetUniformLocation(_shaderSets[12].ShaderProgram, "u_clipMatrix");
        _shaderSets[12].UnifromChannelFlagLocation = GL.glGetUniformLocation(_shaderSets[12].ShaderProgram, "u_channelFlag");
        _shaderSets[12].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[12].ShaderProgram, "u_baseColor");
        _shaderSets[12].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[12].ShaderProgram, "u_multiplyColor");
        _shaderSets[12].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[12].ShaderProgram, "u_screenColor");

        // 乗算
        _shaderSets[13].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[13].ShaderProgram, "a_position");
        _shaderSets[13].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[13].ShaderProgram, "a_texCoord");
        _shaderSets[13].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[13].ShaderProgram, "s_texture0");
        _shaderSets[13].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[13].ShaderProgram, "u_matrix");
        _shaderSets[13].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[13].ShaderProgram, "u_baseColor");
        _shaderSets[13].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[13].ShaderProgram, "u_multiplyColor");
        _shaderSets[13].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[13].ShaderProgram, "u_screenColor");

        // 乗算（クリッピング）
        _shaderSets[14].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[14].ShaderProgram, "a_position");
        _shaderSets[14].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[14].ShaderProgram, "a_texCoord");
        _shaderSets[14].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[14].ShaderProgram, "s_texture0");
        _shaderSets[14].SamplerTexture1Location = GL.glGetUniformLocation(_shaderSets[14].ShaderProgram, "s_texture1");
        _shaderSets[14].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[14].ShaderProgram, "u_matrix");
        _shaderSets[14].UniformClipMatrixLocation = GL.glGetUniformLocation(_shaderSets[14].ShaderProgram, "u_clipMatrix");
        _shaderSets[14].UnifromChannelFlagLocation = GL.glGetUniformLocation(_shaderSets[14].ShaderProgram, "u_channelFlag");
        _shaderSets[14].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[14].ShaderProgram, "u_baseColor");
        _shaderSets[14].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[14].ShaderProgram, "u_multiplyColor");
        _shaderSets[14].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[14].ShaderProgram, "u_screenColor");

        // 乗算（クリッピング・反転）
        _shaderSets[15].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[15].ShaderProgram, "a_position");
        _shaderSets[15].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[15].ShaderProgram, "a_texCoord");
        _shaderSets[15].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[15].ShaderProgram, "s_texture0");
        _shaderSets[15].SamplerTexture1Location = GL.glGetUniformLocation(_shaderSets[15].ShaderProgram, "s_texture1");
        _shaderSets[15].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[15].ShaderProgram, "u_matrix");
        _shaderSets[15].UniformClipMatrixLocation = GL.glGetUniformLocation(_shaderSets[15].ShaderProgram, "u_clipMatrix");
        _shaderSets[15].UnifromChannelFlagLocation = GL.glGetUniformLocation(_shaderSets[15].ShaderProgram, "u_channelFlag");
        _shaderSets[15].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[15].ShaderProgram, "u_baseColor");
        _shaderSets[15].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[15].ShaderProgram, "u_multiplyColor");
        _shaderSets[15].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[15].ShaderProgram, "u_screenColor");

        // 乗算（PremultipliedAlpha）
        _shaderSets[16].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[16].ShaderProgram, "a_position");
        _shaderSets[16].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[16].ShaderProgram, "a_texCoord");
        _shaderSets[16].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[16].ShaderProgram, "s_texture0");
        _shaderSets[16].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[16].ShaderProgram, "u_matrix");
        _shaderSets[16].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[16].ShaderProgram, "u_baseColor");
        _shaderSets[16].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[16].ShaderProgram, "u_multiplyColor");
        _shaderSets[16].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[16].ShaderProgram, "u_screenColor");

        // 乗算（クリッピング、PremultipliedAlpha）
        _shaderSets[17].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[17].ShaderProgram, "a_position");
        _shaderSets[17].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[17].ShaderProgram, "a_texCoord");
        _shaderSets[17].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[17].ShaderProgram, "s_texture0");
        _shaderSets[17].SamplerTexture1Location = GL.glGetUniformLocation(_shaderSets[17].ShaderProgram, "s_texture1");
        _shaderSets[17].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[17].ShaderProgram, "u_matrix");
        _shaderSets[17].UniformClipMatrixLocation = GL.glGetUniformLocation(_shaderSets[17].ShaderProgram, "u_clipMatrix");
        _shaderSets[17].UnifromChannelFlagLocation = GL.glGetUniformLocation(_shaderSets[17].ShaderProgram, "u_channelFlag");
        _shaderSets[17].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[17].ShaderProgram, "u_baseColor");
        _shaderSets[17].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[17].ShaderProgram, "u_multiplyColor");
        _shaderSets[17].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[17].ShaderProgram, "u_screenColor");

        // 乗算（クリッピング・反転、PremultipliedAlpha）
        _shaderSets[18].AttributePositionLocation = GL.glGetAttribLocation(_shaderSets[18].ShaderProgram, "a_position");
        _shaderSets[18].AttributeTexCoordLocation = GL.glGetAttribLocation(_shaderSets[18].ShaderProgram, "a_texCoord");
        _shaderSets[18].SamplerTexture0Location = GL.glGetUniformLocation(_shaderSets[18].ShaderProgram, "s_texture0");
        _shaderSets[18].SamplerTexture1Location = GL.glGetUniformLocation(_shaderSets[18].ShaderProgram, "s_texture1");
        _shaderSets[18].UniformMatrixLocation = GL.glGetUniformLocation(_shaderSets[18].ShaderProgram, "u_matrix");
        _shaderSets[18].UniformClipMatrixLocation = GL.glGetUniformLocation(_shaderSets[18].ShaderProgram, "u_clipMatrix");
        _shaderSets[18].UnifromChannelFlagLocation = GL.glGetUniformLocation(_shaderSets[18].ShaderProgram, "u_channelFlag");
        _shaderSets[18].UniformBaseColorLocation = GL.glGetUniformLocation(_shaderSets[18].ShaderProgram, "u_baseColor");
        _shaderSets[18].UniformMultiplyColorLocation = GL.glGetUniformLocation(_shaderSets[18].ShaderProgram, "u_multiplyColor");
        _shaderSets[18].UniformScreenColorLocation = GL.glGetUniformLocation(_shaderSets[18].ShaderProgram, "u_screenColor");
    }

    /// <summary>
    /// シェーダプログラムをロードしてアドレス返す。
    /// </summary>
    /// <param name="vertShaderSrc">頂点シェーダのソース</param>
    /// <param name="fragShaderSrc">フラグメントシェーダのソース</param>
    /// <returns>シェーダプログラムのアドレス</returns>
    internal unsafe int LoadShaderProgram(string vertShaderSrc, string fragShaderSrc)
    {
        // Create shader program.
        int shaderProgram = GL.glCreateProgram();

        if (!CompileShaderSource(out int vertShader, GL.GL_VERTEX_SHADER, vertShaderSrc))
        {
            CubismLog.CubismLogError("Vertex shader compile error!");
            return 0;
        }

        // Create and compile fragment shader.
        if (!CompileShaderSource(out int fragShader, GL.GL_FRAGMENT_SHADER, fragShaderSrc))
        {
            CubismLog.CubismLogError("Fragment shader compile error!");
            return 0;
        }

        // Attach vertex shader to program.
        GL.glAttachShader(shaderProgram, vertShader);

        // Attach fragment shader to program.
        GL.glAttachShader(shaderProgram, fragShader);

        // Link program.
        if (!LinkProgram(shaderProgram))
        {
            CubismLog.CubismLogError("Failed to link program: %d", shaderProgram);

            if (vertShader != 0)
            {
                GL.glDeleteShader(vertShader);
            }
            if (fragShader != 0)
            {
                GL.glDeleteShader(fragShader);
            }
            if (shaderProgram != 0)
            {
                GL.glDeleteProgram(shaderProgram);
            }

            return 0;
        }

        // Release vertex and fragment shaders.
        if (vertShader != 0)
        {
            GL.glDetachShader(shaderProgram, vertShader);
            GL.glDeleteShader(vertShader);
        }

        if (fragShader != 0)
        {
            GL.glDetachShader(shaderProgram, fragShader);
            GL.glDeleteShader(fragShader);
        }

        return shaderProgram;
    }

    /// <summary>
    /// シェーダプログラムをコンパイルする
    /// </summary>
    /// <param name="outShader">コンパイルされたシェーダプログラムのアドレス</param>
    /// <param name="shaderType">シェーダタイプ(Vertex/Fragment)</param>
    /// <param name="shaderSource">シェーダソースコード</param>
    /// <returns>true         ->      コンパイル成功
    /// false        ->      コンパイル失敗</returns>
    internal unsafe bool CompileShaderSource(out int outShader, int shaderType, string shaderSource)
    {
        int status;

        outShader = GL.glCreateShader(shaderType);
        GL.glShaderSource(outShader, shaderSource);
        GL.glCompileShader(outShader);

        int logLength;
        GL.glGetShaderiv(outShader, GL.GL_INFO_LOG_LENGTH, &logLength);
        if (logLength > 0)
        {
            GL.glGetShaderInfoLog(outShader, out string log);
            CubismLog.CubismLogError($"Shader compile log: {log}");
        }

        GL.glGetShaderiv(outShader, GL.GL_COMPILE_STATUS, &status);
        if (status == GL.GL_FALSE)
        {
            GL.glDeleteShader(outShader);
            return false;
        }

        return true;
    }

    /// <summary>
    /// シェーダプログラムをリンクする
    /// </summary>
    /// <param name="shaderProgram">リンクするシェーダプログラムのアドレス</param>
    /// <returns>true            ->  リンク成功
    /// false           ->  リンク失敗</returns>
    internal unsafe bool LinkProgram(int shaderProgram)
    {
        int status;
        GL.glLinkProgram(shaderProgram);

        int logLength;
        GL.glGetProgramiv(shaderProgram, GL.GL_INFO_LOG_LENGTH, &logLength);
        if (logLength > 0)
        {
            GL.glGetProgramInfoLog(shaderProgram, out string log);
            CubismLog.CubismLogError($"Program link log: {log}");
        }

        GL.glGetProgramiv(shaderProgram, GL.GL_LINK_STATUS, &status);
        if (status == GL.GL_FALSE)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// シェーダプログラムを検証する
    /// </summary>
    /// <param name="shaderProgram">検証するシェーダプログラムのアドレス</param>
    /// <returns>true            ->  正常
    /// false           ->  異常</returns>
    internal unsafe bool ValidateProgram(int shaderProgram)
    {
        int logLength, status;

        GL.glValidateProgram(shaderProgram);
        GL.glGetProgramiv(shaderProgram, GL.GL_INFO_LOG_LENGTH, &logLength);
        if (logLength > 0)
        {
            GL.glGetProgramInfoLog(shaderProgram, out string log);
            CubismLog.CubismLogError($"Validate program log: {log}");
        }

        GL.glGetProgramiv(shaderProgram, GL.GL_VALIDATE_STATUS, &status);
        if (status == GL.GL_FALSE)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tegraプロセッサ対応。拡張方式による描画の有効・無効
    /// </summary>
    /// <param name="extMode">trueなら拡張方式で描画する</param>
    /// <param name="extPAMode">trueなら拡張方式のPA設定を有効にする</param>
    internal static void SetExtShaderMode(bool extMode, bool extPAMode)
    {
        s_extMode = extMode;
        s_extPAMode = extPAMode;
    }
}
