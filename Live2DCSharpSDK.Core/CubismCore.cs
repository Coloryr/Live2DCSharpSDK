using System.Numerics;
using System.Runtime.InteropServices;

namespace Live2DCSharpSDK.Core;

using csmVersion = UInt32;
using csmFlags = Byte;
using csmMocVersion = UInt32;
using csmParameterType = Int32;

/// <summary>
/// Cubism moc.
/// </summary>
public struct csmMoc
{

}

/// <summary>
/// Cubism model.
/// </summary>
public struct csmModel
{

}


public static class csmEnum
{
    //Alignment constraints.

    /// <summary>
    /// Necessary alignment for mocs (in bytes).
    /// </summary>
    public const int csmAlignofMoc = 64;
    /// <summary>
    /// Necessary alignment for models (in bytes).
    /// </summary>
    public const int csmAlignofModel = 16;

    //Bit masks for non-dynamic drawable flags.

    /// <summary>
    /// Additive blend mode mask.
    /// </summary>
    public const byte csmBlendAdditive = 1 << 0;
    /// <summary>
    /// Multiplicative blend mode mask.
    /// </summary>
    public const byte csmBlendMultiplicative = 1 << 1;
    /// <summary>
    /// Double-sidedness mask.
    /// </summary>
    public const byte csmIsDoubleSided = 1 << 2;
    /// <summary>
    /// Clipping mask inversion mode mask.
    /// </summary>
    public const byte csmIsInvertedMask = 1 << 3;

    //Bit masks for dynamic drawable flags.

    /// <summary>
    /// Flag set when visible.
    /// </summary>
    public const byte csmIsVisible = 1 << 0;
    /// <summary>
    /// Flag set when visibility did change.
    /// </summary>
    public const byte csmVisibilityDidChange = 1 << 1;
    /// <summary>
    /// Flag set when opacity did change.
    /// </summary>
    public const byte csmOpacityDidChange = 1 << 2;
    /// <summary>
    /// Flag set when draw order did change.
    /// </summary>
    public const byte csmDrawOrderDidChange = 1 << 3;
    /// <summary>
    /// Flag set when render order did change.
    /// </summary>
    public const byte csmRenderOrderDidChange = 1 << 4;
    /// <summary>
    /// Flag set when vertex positions did change.
    /// </summary>
    public const byte csmVertexPositionsDidChange = 1 << 5;
    /// <summary>
    /// Flag set when blend color did change.
    /// </summary>
    public const byte csmBlendColorDidChange = 1 << 6;

    //moc3 file format version.

    /// <summary>
    /// unknown
    /// </summary>
    public const int csmMocVersion_Unknown = 0;
    /// <summary>
    /// moc3 file version 3.0.00 - 3.2.07
    /// </summary>
    public const int csmMocVersion_30 = 1;
    /// <summary>
    /// moc3 file version 3.3.00 - 3.3.03
    /// </summary>
    public const int csmMocVersion_33 = 2;
    /// <summary>
    /// moc3 file version 4.0.00 - 4.1.05
    /// </summary>
    public const int csmMocVersion_40 = 3;
    /// <summary>
    /// moc3 file version 4.2.00 -
    /// </summary>
    public const int csmMocVersion_42 = 4;

    //Parameter types.

    /// <summary>
    /// Normal parameter.
    /// </summary>
    public const int csmParameterType_Normal = 0;
    /// <summary>
    /// Parameter for blend shape.
    /// </summary>
    public const int csmParameterType_BlendShape = 1;
}

/// <summary>
/// Log handler.
/// </summary>
/// <param name="message">Null-terminated string message to log.</param>
public delegate void csmLogFunction(string message);

public static class CubismCore
{
    //VERSION

    /// <summary>
    /// Queries Core version.
    /// </summary>
    /// <returns>Core version.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static csmVersion csmGetVersion();

    /// <summary>
    /// Gets Moc file supported latest version.
    /// </summary>
    /// <returns>csmMocVersion (Moc file latest format version).</returns>
    [DllImport("Live2DCubismCore")]
    public extern static csmMocVersion csmGetLatestMocVersion();

    /// <summary>
    /// Gets Moc file format version.
    /// </summary>
    /// <param name="address">Address of moc.</param>
    /// <param name="size">Size of moc (in bytes).</param>
    /// <returns>csmMocVersion</returns>
    [DllImport("Live2DCubismCore")]
    public extern static csmMocVersion csmGetMocVersion(IntPtr address, int size);

    //CONSISTENCY

    /// <summary>
    /// Checks consistency of a moc.
    /// </summary>
    /// <param name="address">Address of unrevived moc. The address must be aligned to 'csmAlignofMoc'.</param>
    /// <param name="size">Size of moc (in bytes).</param>
    /// <returns>'1' if Moc is valid; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static int csmHasMocConsistency(IntPtr address, int size);

    //LOGGING

    /// <summary>
    /// Queries log handler.
    /// </summary>
    /// <returns>Log handler.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static csmLogFunction csmGetLogFunction();

    /// <summary>
    /// Sets log handler.
    /// </summary>
    /// <param name="handler">Handler to use.</param>
    [DllImport("Live2DCubismCore")]
    public extern static void csmSetLogFunction(csmLogFunction handler);

    //MOC

    /// <summary>
    /// Tries to revive a moc from bytes in place.
    /// </summary>
    /// <param name="address">Address of unrevived moc. The address must be aligned to 'csmAlignofMoc'.</param>
    /// <param name="size">Size of moc (in bytes).</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe csmMoc* csmReviveMocInPlace(IntPtr address, int size);

    //MODEL

    /// <summary>
    /// Queries size of a model in bytes.
    /// </summary>
    /// <param name="moc">Moc to query.</param>
    /// <returns>Valid size on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe int csmGetSizeofModel(csmMoc* moc);

    /// <summary>
    /// Tries to instantiate a model in place.
    /// </summary>
    /// <param name="moc">Source moc.</param>
    /// <param name="address">Address to place instance at. Address must be aligned to 'csmAlignofModel'.</param>
    /// <param name="size">Size of memory block for instance (in bytes).</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe csmModel* csmInitializeModelInPlace(csmMoc* moc, IntPtr address, int size);

    /// <summary>
    /// Updates a model.
    /// </summary>
    /// <param name="model">Model to update.</param>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe void csmUpdateModel(csmModel* model);

    //CANVAS

    /// <summary>
    /// Reads info on a model canvas.
    /// </summary>
    /// <param name="model">Model query.</param>
    /// <param name="outSizeInPixels">Canvas dimensions.</param>
    /// <param name="outOriginInPixels">Origin of model on canvas.</param>
    /// <param name="outPixelsPerUnit">Aspect used for scaling pixels to units.</param>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe void csmReadCanvasInfo(csmModel* model, Vector2* outSizeInPixels, Vector2* outOriginInPixels, float* outPixelsPerUnit);

    //PARAMETERS

    /// <summary>
    /// Gets number of parameters.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid count on success; '-1' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe int csmGetParameterCount(csmModel* model);

    /// <summary>
    /// Gets parameter IDs.
    /// All IDs are null-terminated ANSI strings.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe string[] csmGetParameterIds(csmModel* model);

    /// <summary>
    /// Gets parameter types.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe csmParameterType* csmGetParameterTypes(csmModel* model);

    /// <summary>
    /// Gets minimum parameter values.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe float*  csmGetParameterMinimumValues(csmModel* model);

    /// <summary>
    /// Gets maximum parameter values.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe float* csmGetParameterMaximumValues(csmModel* model);

    /// <summary>
    /// Gets default parameter values.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe float* csmGetParameterDefaultValues(csmModel* model);

    /// <summary>
    /// Gets read/write parameter values buffer.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe float* csmGetParameterValues(csmModel* model);

    /// <summary>
    /// Gets number of key values of each parameter.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe int* csmGetParameterKeyCounts(csmModel* model);

    /// <summary>
    /// Gets key values of each parameter.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe float** csmGetParameterKeyValues(csmModel* model);

    //PARTS

    /// <summary>
    /// Gets number of parts.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid count on success; '-1' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe int csmGetPartCount(csmModel* model);

    /// <summary>
    /// Gets parts IDs.
    /// All IDs are null-terminated ANSI strings.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe string[] csmGetPartIds(csmModel* model);

    /// <summary>
    /// Gets read/write part opacities buffer.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe float* csmGetPartOpacities(csmModel* model);

    /// <summary>
    /// Gets part's parent part indices.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe int* csmGetPartParentPartIndices(csmModel* model);

    //DRAWABLES

    /// <summary>
    /// Gets number of drawables.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid count on success; '-1' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe int csmGetDrawableCount(csmModel* model);

    /// <summary>
    /// Gets drawable IDs.
    /// All IDs are null-terminated ANSI strings.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe string[] csmGetDrawableIds(csmModel* model);

    /// <summary>
    /// Gets constant drawable flags.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe csmFlags* csmGetDrawableConstantFlags(csmModel* model);

    /// <summary>
    /// Gets dynamic drawable flags.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe csmFlags* csmGetDrawableDynamicFlags(csmModel* model);

    /// <summary>
    /// Gets drawable texture indices.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe int* csmGetDrawableTextureIndices(csmModel* model);

    /// <summary>
    /// Gets drawable draw orders.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe int* csmGetDrawableDrawOrders(csmModel* model);

    /// <summary>
    /// Gets drawable render orders.
    /// The higher the order, the more up front a drawable is.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0'otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe int* csmGetDrawableRenderOrders(csmModel* model);

    /// <summary>
    /// Gets drawable opacities.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe float* csmGetDrawableOpacities(csmModel* model);

    /// <summary>
    /// Gets numbers of masks of each drawable.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe int* csmGetDrawableMaskCounts(csmModel* model);

    /// <summary>
    /// Gets number of vertices of each drawable.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe int**  csmGetDrawableMasks( csmModel* model);

    /// <summary>
    /// Gets number of vertices of each drawable.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe int* csmGetDrawableVertexCounts(csmModel* model);

    /// <summary>
    /// Gets vertex position data of each drawable.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; a null pointer otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe Vector2**  csmGetDrawableVertexPositions( csmModel* model);

    /// <summary>
    /// Gets texture coordinate data of each drawables.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe Vector2**  csmGetDrawableVertexUvs( csmModel* model);

    /// <summary>
    /// Gets number of triangle indices for each drawable.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe int* csmGetDrawableIndexCounts(
        csmModel* model);

    /// <summary>
    /// Gets triangle index data for each drawable.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe ushort** csmGetDrawableIndices(csmModel* model);

    /// <summary>
    /// Gets multiply color data for each drawable.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe Vector4*  csmGetDrawableMultiplyColors( csmModel* model);

    /// <summary>
    /// Gets screen color data for each drawable.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe Vector4*  csmGetDrawableScreenColors( csmModel* model);

    /// <summary>
    /// Gets drawable's parent part indices.
    /// </summary>
    /// <param name="model">Model to query.</param>
    /// <returns>Valid pointer on success; '0' otherwise.</returns>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe int*  csmGetDrawableParentPartIndices( csmModel* model);

    /// <summary>
    /// Resets all dynamic drawable flags.
    /// </summary>
    /// <param name="model">Model containing flags.</param>
    [DllImport("Live2DCubismCore")]
    public extern static unsafe void csmResetDrawableDynamicFlags(csmModel* model);
}