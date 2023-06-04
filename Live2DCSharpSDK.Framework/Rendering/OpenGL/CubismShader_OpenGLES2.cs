using Live2DCSharpSDK.Framework.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Rendering.OpenGL;


internal struct CubismShaderSet
{
    /// <summary>
    /// シェーダプログラムのアドレス
    /// </summary>
    internal uint ShaderProgram;
    /// <summary>
    /// シェーダプログラムに渡す変数のアドレス(Position)
    /// </summary>
    internal uint AttributePositionLocation;
    /// <summary>
    /// シェーダプログラムに渡す変数のアドレス(TexCoord)
    /// </summary>
    internal uint AttributeTexCoordLocation;
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

internal class CubismShader_OpenGLES2
{
    /// <summary>
    /// インスタンスを取得する（シングルトン）。
    /// </summary>
    /// <returns>インスタンスのポインタ</returns>
    internal static CubismShader_OpenGLES2 GetInstance()
    { 

    }

    /// <summary>
    /// インスタンスを解放する（シングルトン）。
    /// </summary>
    internal static void DeleteInstance()
    { 
    
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
    internal void SetupShaderProgram(CubismRenderer_OpenGLES2 renderer, uint textureId
                            , csmInt32 vertexCount, csmFloat32* vertexArray
                            , csmFloat32* uvArray, csmFloat32 opacity
                            , CubismRenderer::CubismBlendMode colorBlendMode
                            , CubismRenderer::CubismTextureColor baseColor
                            , CubismRenderer::CubismTextureColor multiplyColor
                            , CubismRenderer::CubismTextureColor screenColor
                            , csmBool isPremultipliedAlpha, CubismMatrix44 matrix4x4
                            , csmBool invertedMask)
    { 
        
    }

    /// <summary>
    /// シェーダプログラムを解放する
    /// </summary>
    internal void ReleaseShaderProgram()
    { 
    
    }

    /// <summary>
    /// シェーダプログラムを初期化する
    /// </summary>
    internal void GenerateShaders()
    { 
    
    }
}
