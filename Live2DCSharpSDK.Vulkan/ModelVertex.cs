using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Vulkan;

namespace Live2DCSharpSDK.Vulkan;

[StructLayout(LayoutKind.Sequential)]
public struct ModelVertex
{
    /// <summary>
    /// Position
    /// </summary>
    public Vector2 Pos;
    /// <summary>
    /// UVs
    /// </summary>
    public Vector2 TexCoord;

    public static VertexInputBindingDescription GetBindingDescription()
    {
        return new()
        {
            Binding = 0,
            Stride = (uint)Marshal.SizeOf<ModelVertex>(),
            InputRate = VertexInputRate.Vertex
        };
    }

    public static unsafe void GetAttributeDescriptions(VertexInputAttributeDescription* attributeDescriptions)
    {
        attributeDescriptions[0].Binding = 0;
        attributeDescriptions[0].Location = 0;
        attributeDescriptions[0].Format = Format.R32G32Sfloat;
        attributeDescriptions[0].Offset = 0;

        attributeDescriptions[1].Binding = 0;
        attributeDescriptions[1].Location = 1;
        attributeDescriptions[1].Format = Format.R32G32Sfloat;
        attributeDescriptions[1].Offset = (uint)Marshal.SizeOf<Vector2>();
    }
}
