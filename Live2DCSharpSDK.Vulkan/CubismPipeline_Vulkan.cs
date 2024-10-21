using Silk.NET.Vulkan;

namespace Live2DCSharpSDK.Vulkan;

public class CubismPipeline_Vulkan(Vk vk, Device device)
{
    private const int ShaderCount = 19;
    private PipelineResource[] _pipelineResource;
    /// <summary>
    /// シェーダーごとにPipelineResourceのインスタンスを作成する
    /// </summary>
    /// <param name="descriptorSetLayout">ディスクリプタセットレイアウト</param>
    public void CreatePipelines(DescriptorSetLayout descriptorSetLayout)
    {
        //パイプラインを作成済みの場合は生成する必要なし
        if (_pipelineResource != null)
        {
            return;
        }

        _pipelineResource = new PipelineResource[ShaderCount];

        for (int a = 0; a < 7; a++)
        {
            _pipelineResource[a] = new(vk, device);
        }

        _pipelineResource[0].CreateGraphicsPipeline("VertShaderSrcSetupMask.spv", "FragShaderSrcSetupMask.spv", descriptorSetLayout);
        _pipelineResource[1].CreateGraphicsPipeline("VertShaderSrc.spv", "FragShaderSrc.spv", descriptorSetLayout);
        _pipelineResource[2].CreateGraphicsPipeline("VertShaderSrcMasked.spv", "FragShaderSrcMask.spv", descriptorSetLayout);
        _pipelineResource[3].CreateGraphicsPipeline("VertShaderSrcMasked.spv", "FragShaderSrcMaskInverted.spv", descriptorSetLayout);
        _pipelineResource[4].CreateGraphicsPipeline("VertShaderSrc.spv", "FragShaderSrcPremultipliedAlpha.spv", descriptorSetLayout);
        _pipelineResource[5].CreateGraphicsPipeline("VertShaderSrcMasked.spv", "FragShaderSrcMaskPremultipliedAlpha.spv", descriptorSetLayout);
        _pipelineResource[6].CreateGraphicsPipeline("VertShaderSrcMasked.spv", "FragShaderSrcMaskInvertedPremultipliedAlpha.spv", descriptorSetLayout);

        _pipelineResource[7] = _pipelineResource[1];
        _pipelineResource[8] = _pipelineResource[2];
        _pipelineResource[9] = _pipelineResource[3];
        _pipelineResource[10] = _pipelineResource[4];
        _pipelineResource[11] = _pipelineResource[5];
        _pipelineResource[12] = _pipelineResource[6];

        _pipelineResource[13] = _pipelineResource[1];
        _pipelineResource[14] = _pipelineResource[2];
        _pipelineResource[15] = _pipelineResource[3];
        _pipelineResource[16] = _pipelineResource[4];
        _pipelineResource[17] = _pipelineResource[5];
        _pipelineResource[18] = _pipelineResource[6];
    }

    /// <summary>
    /// 指定したシェーダーのグラフィックスパイプラインを取得する
    /// </summary>
    /// <param name="shaderIndex">シェーダインデックス</param>
    /// <param name="blendIndex"> ブレンドモードのインデックス</param>
    /// <returns>指定したシェーダーのグラフィックスパイプライン</returns>
    public Pipeline GetPipeline(int shaderIndex, int blendIndex)
    {
        return _pipelineResource[shaderIndex].GetPipeline(blendIndex);
    }

    /// <summary>
    /// 指定したブレンド方法のパイプラインレイアウトを取得する
    /// </summary>
    /// <param name="shaderIndex">シェーダインデックス</param>
    /// <param name="blendIndex">ブレンドモードのインデックス</param>
    /// <returns>指定したブレンド方法のパイプラインレイアウト</returns>
    public PipelineLayout GetPipelineLayout(int shaderIndex, int blendIndex)
    {
        return _pipelineResource[shaderIndex].GetPipelineLayout(blendIndex);
    }

    /// <summary>
    /// リソースを開放する
    /// </summary>
    public void ReleaseShaderProgram()
    {
        for (int i = 0; i < 7; i++)
        {
            _pipelineResource[i].Release();
        }
    }
}
