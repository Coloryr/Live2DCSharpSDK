using Live2DCSharpSDK.Core;
using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Physics;

public record CubismPhysicsObj
{
    public record MetaObj
    {
        public record EffectiveForce
        {
            public Vector2 Gravity { get; set; }
            public Vector2 Wind { get; set; }
        }

        public EffectiveForce EffectiveForces { get; set; }
        public float Fps { get; set; }
        public int PhysicsSettingCount { get; set; }
        public int TotalInputCount { get; set; }
        public int TotalOutputCount { get; set; }
        public int VertexCount { get; set; }
    }
    public record PhysicsSetting
    {
        public record NormalizationObj
        {
            public record PositionObj
            { 
                public float Minimum { get; set; }
                public float Maximum { get; set; }
                public float Default { get; set; }
            }
            public PositionObj Position { get; set; }
            public PositionObj Angle { get; set; }
        }
        public record InputObj
        {
            public record SourceObj
            { 
                public string Id { get; set; }
            }
            public float Weight { get; set; }
            public bool Reflect { get; set; }
            public string Type { get; set; }
            public SourceObj Source { get; set; }
        }
        public record OutputObj
        {
            public record DestinationObj
            {
                public string Id { get; set; }
            }
            public int VertexIndex { get; set; }
            public float Scale { get; set; }
            public float Weight { get; set; }
            public DestinationObj Destination { get; set; }
            public string Type { get; set; }
            public bool Reflect { get; set; }
        }
        public record Vertice 
        {
            public float Mobility { get; set; }
            public float Delay { get; set; }
            public float Acceleration { get; set; }
            public float Radius { get; set; }
            public Vector2 Position { get; set; }
        }
        public NormalizationObj Normalization { get; set; }
        public List<InputObj> Input { get; set; }
        public List<OutputObj> Output { get; set; }
        public List<Vertice> Vertices { get; set; }
    }
    public MetaObj Meta { get; set; }
    public List<PhysicsSetting> PhysicsSettings { get; set; }
}

/// <summary>
/// physics3.jsonのコンテナ。
/// </summary>
public record CubismPhysicsJson
{
    public const string Position = "Position";
    public const string X = "X";
    public const string Y = "Y";
    public const string Angle = "Angle";
    public const string Type = "Type";
    public const string Id = "Id";

    // Meta
    public const string Meta = "Meta";
    public const string EffectiveForces = "EffectiveForces";
    public const string TotalInputCount = "TotalInputCount";
    public const string TotalOutputCount = "TotalOutputCount";
    public const string PhysicsSettingCount = "PhysicsSettingCount";
    public const string Gravity = "Gravity";
    public const string Wind = "Wind";
    public const string VertexCount = "VertexCount";
    public const string Fps = "Fps";

    // PhysicsSettings
    public const string PhysicsSettings = "PhysicsSettings";
    public const string Normalization = "Normalization";
    public const string Minimum = "Minimum";
    public const string Maximum = "Maximum";
    public const string Default = "Default";
    public const string Reflect = "Reflect";
    public const string Weight = "Weight";

    // Input
    public const string Input = "Input";
    public const string Source = "Source";

    // Output
    public const string Output = "Output";
    public const string Scale = "Scale";
    public const string VertexIndex = "VertexIndex";
    public const string Destination = "Destination";

    // Particle
    public const string Vertices = "Vertices";
    public const string Mobility = "Mobility";
    public const string Delay = "Delay";
    public const string Radius = "Radius";
    public const string Acceleration = "Acceleration";
}

/// <summary>
/// 物理演算のクラス。
/// </summary>
public class CubismPhysics
{
    /// <summary>
    /// 物理演算のオプション。
    /// </summary>
    public record Options
    {
        /// <summary>
        /// 重力方向
        /// </summary>
        public Vector2 Gravity;
        /// <summary>
        /// 風の方向
        /// </summary>
        public Vector2 Wind; 
    }

    /// <summary>
    /// パラメータに適用する前の物理演算の出力結果
    /// </summary>
    public record PhysicsOutput
    {
        public float[] outputs;
    }

    /// physics types tags.
    public const string PhysicsTypeTagX = "X";
    public const string PhysicsTypeTagY = "Y";
    public const string PhysicsTypeTagAngle = "Angle";

    /// Constant of air resistance.
    public const float AirResistance = 5.0f;

    /// Constant of maximum weight of input and output ratio.
    public const float MaximumWeight = 100.0f;

    /// Constant of threshold of movement.
    public const float MovementThreshold = 0.001f;

    /// Constant of maximum allowed delta time
    public const float MaxDeltaTime = 5.0f;

    /// <summary>
    /// 物理演算のデータ
    /// </summary>
    private CubismPhysicsRig _physicsRig;
    /// <summary>
    /// オプション
    /// </summary>
    private Options _options;

    /// <summary>
    /// 最新の振り子計算の結果
    /// </summary>
    private readonly List<PhysicsOutput> _currentRigOutputs = new();
    /// <summary>
    /// 一つ前の振り子計算の結果
    /// </summary>
    private readonly List<PhysicsOutput> _previousRigOutputs = new();

    /// <summary>
    /// 物理演算が処理していない時間
    /// </summary>
    private float _currentRemainTime;

    /// <summary>
    /// Evaluateで利用するパラメータのキャッシュs
    /// </summary>
    private float[] _parameterCaches = Array.Empty<float>();
    /// <summary>
    /// UpdateParticlesが動くときの入力をキャッシュ
    /// </summary>
    private float[] _parameterInputCaches = Array.Empty<float>();

    /// <summary>
    /// 正しくJsonデータが取得出来たか
    /// </summary>
    private bool _isJsonValid;

    /// <summary>
    /// インスタンスを作成する。
    /// </summary>
    /// <param name="buffer">physics3.jsonが読み込まれいるバッファ</param>
    public CubismPhysics(string buffer)
    {
        // set default options.
        _options = new();
        _options.Gravity.Y = -1.0f;
        _options.Gravity.X = 0;
        _options.Wind.X = 0;
        _options.Wind.Y = 0;
        _currentRemainTime = 0.0f;

        _physicsRig = new CubismPhysicsRig();

        var obj = JsonConvert.DeserializeObject<CubismPhysicsObj>(buffer);

        _isJsonValid = obj != null;

        if (obj == null)
        {
            return;
        }

        _physicsRig.Gravity = obj.Meta.EffectiveForces.Gravity;
        _physicsRig.Wind = obj.Meta.EffectiveForces.Wind;
        _physicsRig.SubRigCount = obj.Meta.PhysicsSettingCount;

        _physicsRig.Fps = obj.Meta.Fps;

        _physicsRig.Settings = new CubismPhysicsSubRig[_physicsRig.SubRigCount];
        _physicsRig.Inputs = new CubismPhysicsInput[obj.Meta.TotalInputCount];
        _physicsRig.Outputs = new CubismPhysicsOutput[obj.Meta.TotalOutputCount];
        _physicsRig.Particles = new CubismPhysicsParticle[obj.Meta.VertexCount];

        _currentRigOutputs.Clear();
        _previousRigOutputs.Clear();

        int inputIndex = 0, outputIndex = 0, particleIndex = 0;
        for (int i = 0; i < _physicsRig.Settings.Length; ++i)
        {
            var set = obj.PhysicsSettings[i];
            _physicsRig.Settings[i].NormalizationPosition.Minimum = set.Normalization.Position.Minimum;
            _physicsRig.Settings[i].NormalizationPosition.Maximum = set.Normalization.Position.Maximum;
            _physicsRig.Settings[i].NormalizationPosition.Default = set.Normalization.Position.Default;

            _physicsRig.Settings[i].NormalizationAngle.Minimum = set.Normalization.Angle.Minimum;
            _physicsRig.Settings[i].NormalizationAngle.Maximum = set.Normalization.Angle.Maximum;
            _physicsRig.Settings[i].NormalizationAngle.Default = set.Normalization.Angle.Default;

            // Input
            _physicsRig.Settings[i].InputCount = set.Input.Count;
            _physicsRig.Settings[i].BaseInputIndex = inputIndex;
            for (int j = 0; j < _physicsRig.Settings[i].InputCount; ++j)
            {
                var input = set.Input[j];
                _physicsRig.Inputs[inputIndex + j].SourceParameterIndex = -1;
                _physicsRig.Inputs[inputIndex + j].Weight = input.Weight;
                _physicsRig.Inputs[inputIndex + j].Reflect = input.Reflect;

                if (input.Type == PhysicsTypeTagX)
                {
                    _physicsRig.Inputs[inputIndex + j].Type = CubismPhysicsSource.CubismPhysicsSource_X;
                    _physicsRig.Inputs[inputIndex + j].GetNormalizedParameterValue = 
                        GetInputTranslationXFromNormalizedParameterValue;
                }
                else if (input.Type == PhysicsTypeTagY)
                {
                    _physicsRig.Inputs[inputIndex + j].Type = CubismPhysicsSource.CubismPhysicsSource_Y;
                    _physicsRig.Inputs[inputIndex + j].GetNormalizedParameterValue = 
                        GetInputTranslationYFromNormalizedParameterValue;
                }
                else if (input.Type == PhysicsTypeTagAngle)
                {
                    _physicsRig.Inputs[inputIndex + j].Type = CubismPhysicsSource.CubismPhysicsSource_Angle;
                    _physicsRig.Inputs[inputIndex + j].GetNormalizedParameterValue = 
                        GetInputAngleFromNormalizedParameterValue;
                }

                _physicsRig.Inputs[inputIndex + j].Source.TargetType =
                    CubismPhysicsTargetType.CubismPhysicsTargetType_Parameter;
                _physicsRig.Inputs[inputIndex + j].Source.Id = input.Source.Id;
            }
            inputIndex += _physicsRig.Settings[i].InputCount;

            // Output
            _physicsRig.Settings[i].OutputCount = set.Output.Count;
            _physicsRig.Settings[i].BaseOutputIndex = outputIndex;

            PhysicsOutput currentRigOutput = new()
            {
                outputs = new float[set.Output.Count]
            };
            _currentRigOutputs.Add(currentRigOutput);

            PhysicsOutput previousRigOutput = new()
            {
                outputs = new float[set.Output.Count]
            };
            _previousRigOutputs.Add(previousRigOutput);

            for (int j = 0; j < _physicsRig.Settings[i].OutputCount; ++j)
            {
                var output = set.Output[j];
                _physicsRig.Outputs[outputIndex + j].DestinationParameterIndex = -1;
                _physicsRig.Outputs[outputIndex + j].VertexIndex = output.VertexIndex;
                _physicsRig.Outputs[outputIndex + j].AngleScale = output.Scale;
                _physicsRig.Outputs[outputIndex + j].Weight = output.Weight;
                _physicsRig.Outputs[outputIndex + j].Destination.TargetType = 
                    CubismPhysicsTargetType.CubismPhysicsTargetType_Parameter;

                _physicsRig.Outputs[outputIndex + j].Destination.Id = output.Destination.Id;
                var key = output.Type;
                if (key == PhysicsTypeTagX)
                {
                    _physicsRig.Outputs[outputIndex + j].Type = CubismPhysicsSource.CubismPhysicsSource_X;
                    _physicsRig.Outputs[outputIndex + j].GetValue = GetOutputTranslationX;
                    _physicsRig.Outputs[outputIndex + j].GetScale = GetOutputScaleTranslationX;
                }
                else if (key == PhysicsTypeTagY)
                {
                    _physicsRig.Outputs[outputIndex + j].Type = CubismPhysicsSource.CubismPhysicsSource_Y;
                    _physicsRig.Outputs[outputIndex + j].GetValue = GetOutputTranslationY;
                    _physicsRig.Outputs[outputIndex + j].GetScale = GetOutputScaleTranslationY;
                }
                else if (key == PhysicsTypeTagAngle)
                {
                    _physicsRig.Outputs[outputIndex + j].Type = CubismPhysicsSource.CubismPhysicsSource_Angle;
                    _physicsRig.Outputs[outputIndex + j].GetValue = GetOutputAngle;
                    _physicsRig.Outputs[outputIndex + j].GetScale = GetOutputScaleAngle;
                }

                _physicsRig.Outputs[outputIndex + j].Reflect = output.Reflect;
            }
            outputIndex += _physicsRig.Settings[i].OutputCount;

            // Particle
            _physicsRig.Settings[i].ParticleCount = set.Vertices.Count;
            _physicsRig.Settings[i].BaseParticleIndex = particleIndex;
            for (int j = 0; j < _physicsRig.Settings[i].ParticleCount; ++j)
            {
                var par = set.Vertices[j];
                _physicsRig.Particles[particleIndex + j].Mobility = par.Mobility;
                _physicsRig.Particles[particleIndex + j].Delay = par.Delay;
                _physicsRig.Particles[particleIndex + j].Acceleration = par.Acceleration;
                _physicsRig.Particles[particleIndex + j].Radius = par.Radius;
                _physicsRig.Particles[particleIndex + j].Position = par.Position;
            }

            particleIndex += _physicsRig.Settings[i].ParticleCount;
        }

        Initialize();
    }

    /// <summary>
    /// パラメータをリセットする。
    /// </summary>
    public void Reset()
    {
        // set default options.
        _options = new();
        _options.Gravity.Y = -1.0f;
        _options.Gravity.X = 0.0f;
        _options.Wind.X = 0.0f;
        _options.Wind.Y = 0.0f;

        _physicsRig.Gravity.X = 0.0f;
        _physicsRig.Gravity.Y = 0.0f;
        _physicsRig.Wind.X = 0.0f;
        _physicsRig.Wind.Y = 0.0f;

        Initialize();
    }

    /// <summary>
    /// 現在のパラメータ値で物理演算が安定化する状態を演算する。
    /// </summary>
    /// <param name="model">物理演算の結果を適用するモデル</param>
    public unsafe void Stabilization(CubismModel model)
    {
        float totalAngle;
        float weight;
        float radAngle;
        float outputValue;
        Vector2 totalTranslation;
        int i, settingIndex, particleIndex;

        float* parameterValues;
        float* parameterMaximumValues;
        float* parameterMinimumValues;
        float* parameterDefaultValues;

        parameterValues = CubismCore.csmGetParameterValues(model.GetModel());
        parameterMaximumValues = CubismCore.csmGetParameterMaximumValues(model.GetModel());
        parameterMinimumValues = CubismCore.csmGetParameterMinimumValues(model.GetModel());
        parameterDefaultValues = CubismCore.csmGetParameterDefaultValues(model.GetModel());

        if (_parameterCaches.Length < model.GetParameterCount())
        {
            _parameterCaches = new float[model.GetParameterCount()];
        }
        if (_parameterInputCaches.Length < model.GetParameterCount())
        {
            _parameterInputCaches = new float[model.GetParameterCount()];
        }

        for (int j = 0; j < model.GetParameterCount(); ++j)
        {
            _parameterCaches[j] = parameterValues[j];
            _parameterInputCaches[j] = parameterValues[j];
        }

        for (settingIndex = 0; settingIndex < _physicsRig.SubRigCount; ++settingIndex)
        {
            totalAngle = 0.0f;
            totalTranslation.X = 0.0f;
            totalTranslation.Y = 0.0f;
            
            var currentSetting = _physicsRig.Settings[settingIndex];
            var currentInputIndex = currentSetting.BaseInputIndex;
            var currentOutputIndex = currentSetting.BaseOutputIndex;
            var currentParticleIndex = currentSetting.BaseParticleIndex;

            // Load input parameters
            for (i = 0; i < currentSetting.InputCount; ++i)
            {
                weight = _physicsRig.Inputs[i + currentInputIndex].Weight / MaximumWeight;

                if (_physicsRig.Inputs[i + currentInputIndex].SourceParameterIndex == -1)
                {
                    _physicsRig.Inputs[i + currentInputIndex].SourceParameterIndex = model.GetParameterIndex(_physicsRig.Inputs[i + currentInputIndex].Source.Id);
                }

                _physicsRig.Inputs[i + currentInputIndex].GetNormalizedParameterValue(
                    ref totalTranslation,
                    ref totalAngle,
                    parameterValues[_physicsRig.Inputs[i + currentInputIndex].SourceParameterIndex],
                    parameterMinimumValues[_physicsRig.Inputs[i + currentInputIndex].SourceParameterIndex],
                    parameterMaximumValues[_physicsRig.Inputs[i + currentInputIndex].SourceParameterIndex],
                    parameterDefaultValues[_physicsRig.Inputs[i + currentInputIndex].SourceParameterIndex],
                    currentSetting.NormalizationPosition,
                    currentSetting.NormalizationAngle,
                    _physicsRig.Inputs[i + currentInputIndex].Reflect,
                    weight
                );

                _parameterCaches[_physicsRig.Inputs[i + currentInputIndex].SourceParameterIndex] =
                    parameterValues[_physicsRig.Inputs[i + currentInputIndex].SourceParameterIndex];
            }

            radAngle = CubismMath.DegreesToRadian(-totalAngle);

            totalTranslation.X = totalTranslation.X * MathF.Cos(radAngle) - totalTranslation.Y * MathF.Sin(radAngle);
            totalTranslation.Y = totalTranslation.X * MathF.Sin(radAngle) + totalTranslation.Y * MathF.Cos(radAngle);

            // Calculate particles position.
            UpdateParticlesForStabilization(
                _physicsRig.Particles,
                currentSetting.BaseParticleIndex,
                currentSetting.ParticleCount,
                totalTranslation,
                totalAngle,
                _options.Wind,
                MovementThreshold * currentSetting.NormalizationPosition.Maximum
            );

            // Update output parameters.
            for (i = 0; i < currentSetting.OutputCount; ++i)
            {
                particleIndex = _physicsRig.Outputs[i + currentOutputIndex].VertexIndex;

                if (_physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex == -1)
                {
                    _physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex = model.GetParameterIndex(
                        _physicsRig.Outputs[i + currentOutputIndex].Destination.Id);
                }

                if (particleIndex < 1 || particleIndex >= currentSetting.ParticleCount)
                {
                    continue;
                }

                Vector2 translation = new()
                {
                    X = _physicsRig.Particles[particleIndex + currentParticleIndex].Position.X - _physicsRig.Particles[particleIndex - 1 + currentParticleIndex].Position.X,
                    Y = _physicsRig.Particles[particleIndex + currentParticleIndex].Position.Y - _physicsRig.Particles[particleIndex - 1 + currentParticleIndex].Position.Y
                };

                outputValue = _physicsRig.Outputs[i + currentOutputIndex].GetValue(
                    translation,
                    _physicsRig.Particles,
                    currentParticleIndex,
                    particleIndex,
                    _physicsRig.Outputs[i + currentOutputIndex].Reflect,
                    _options.Gravity
                );

                _currentRigOutputs[settingIndex].outputs[i] = outputValue;
                _previousRigOutputs[settingIndex].outputs[i] = outputValue;

                UpdateOutputParameterValue(
                    &parameterValues[_physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex],
                    parameterMinimumValues[_physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex],
                    parameterMaximumValues[_physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex],
                    outputValue,
                    _physicsRig.Outputs[i + currentOutputIndex]);

                _parameterCaches[_physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex] = parameterValues[_physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex];
            }
        }
    }

    /// <summary>
    /// 物理演算を評価する。
    /// Pendulum interpolation weights
    ///
    /// 振り子の計算結果は保存され、パラメータへの出力は保存された前回の結果で補間されます。
    /// The result of the pendulum calculation is saved and
    /// the output to the parameters is interpolated with the saved previous result of the pendulum calculation.
    ///
    /// 図で示すと[1]と[2]で補間されます。
    /// The figure shows the interpolation between [1] and [2].
    ///
    /// 補間の重みは最新の振り子計算タイミングと次回のタイミングの間で見た現在時間で決定する。
    /// The weight of the interpolation are determined by the current time seen between
    /// the latest pendulum calculation timing and the next timing.
    ///
    /// 図で示すと[2]と[4]の間でみた(3)の位置の重みになる。
    /// Figure shows the weight of position (3) as seen between [2] and [4].
    ///
    /// 解釈として振り子計算のタイミングと重み計算のタイミングがズレる。
    /// As an interpretation, the pendulum calculation and weights are misaligned.
    ///
    /// physics3.jsonにFPS情報が存在しない場合は常に前の振り子状態で設定される。
    /// If there is no FPS information in physics3.json, it is always set in the previous pendulum state.
    ///
    /// この仕様は補間範囲を逸脱したことが原因の震えたような見た目を回避を目的にしている。
    /// The purpose of this specification is to avoid the quivering appearance caused by deviations from the interpolation range.
    ///
    /// ------------ time -------------->
    ///
    ///    　　　　　　　　|+++++|------| <- weight
    /// ==[1]====#=====[2]---(3)----(4)
    ///          ^ output contents
    ///
    /// 1:_previousRigOutputs
    /// 2:_currentRigOutputs
    /// 3:_currentRemainTime (now rendering)
    /// 4:next particles timing
    ///
    /// @param model
    /// @param deltaTimeSeconds  rendering delta time.
    /// </summary>
    /// <param name="model">物理演算の結果を適用するモデル</param>
    /// <param name="deltaTimeSeconds">デルタ時間[秒]</param>
    public unsafe void Evaluate(CubismModel model, float deltaTimeSeconds)
    {
        float totalAngle;
        float weight;
        float radAngle;
        float outputValue;
        Vector2 totalTranslation;
        int i, settingIndex, particleIndex;

        if (0.0f >= deltaTimeSeconds)
        {
            return;
        }

        float* parameterValues;
        float* parameterMaximumValues;
        float* parameterMinimumValues;
        float* parameterDefaultValues;

        float physicsDeltaTime;
        _currentRemainTime += deltaTimeSeconds;
        if (_currentRemainTime > MaxDeltaTime)
        {
            _currentRemainTime = 0.0f;
        }

        parameterValues = CubismCore.csmGetParameterValues(model.GetModel());
        parameterMaximumValues = CubismCore.csmGetParameterMaximumValues(model.GetModel());
        parameterMinimumValues = CubismCore.csmGetParameterMinimumValues(model.GetModel());
        parameterDefaultValues = CubismCore.csmGetParameterDefaultValues(model.GetModel());

        if (_parameterCaches.Length < model.GetParameterCount())
        {
            _parameterCaches = new float[model.GetParameterCount()];
        }
        if (_parameterInputCaches.Length < model.GetParameterCount())
        {
            _parameterInputCaches = new float[model.GetParameterCount()];
            for (int j = 0; j < model.GetParameterCount(); ++j)
            {
                _parameterInputCaches[j] = parameterValues[j];
            }
        }

        if (_physicsRig.Fps > 0.0f)
        {
            physicsDeltaTime = 1.0f / _physicsRig.Fps;
        }
        else
        {
            physicsDeltaTime = deltaTimeSeconds;
        }

        while (_currentRemainTime >= physicsDeltaTime)
        {
            CubismPhysicsSubRig currentSetting;
            CubismPhysicsOutput currentOutputs;

            // copyRigOutputs _currentRigOutputs to _previousRigOutputs
            for (settingIndex = 0; settingIndex < _physicsRig.SubRigCount; ++settingIndex)
            {
                currentSetting = _physicsRig.Settings[settingIndex];
                currentOutputs = _physicsRig.Outputs[currentSetting.BaseOutputIndex];
                for (i = 0; i < currentSetting.OutputCount; ++i)
                {
                    _previousRigOutputs[settingIndex].outputs[i] = _currentRigOutputs[settingIndex].outputs[i];
                }
            }

            // 入力キャッシュとパラメータで線形補間してUpdateParticlesするタイミングでの入力を計算する。
            // Calculate the input at the timing to UpdateParticles by linear interpolation with the _parameterInputCaches and parameterValues.
            // _parameterCachesはグループ間での値の伝搬の役割があるので_parameterInputCachesとの分離が必要。
            // _parameterCaches needs to be separated from _parameterInputCaches because of its role in propagating values between groups.
            float inputWeight = physicsDeltaTime / _currentRemainTime;
            for (int j = 0; j < model.GetParameterCount(); ++j)
            {
                _parameterCaches[j] = _parameterInputCaches[j] * (1.0f - inputWeight) + parameterValues[j] * inputWeight;
                _parameterInputCaches[j] = _parameterCaches[j];
            }

            for (settingIndex = 0; settingIndex < _physicsRig.SubRigCount; ++settingIndex)
            {
                totalAngle = 0.0f;
                totalTranslation.X = 0.0f;
                totalTranslation.Y = 0.0f;
                currentSetting = _physicsRig.Settings[settingIndex];
                var currentInputIndex = currentSetting.BaseInputIndex;
                var currentOutputIndex = currentSetting.BaseOutputIndex;
                var currentParticleIndex = currentSetting.BaseParticleIndex;

                // Load input parameters.
                for (i = 0; i < currentSetting.InputCount; ++i)
                {
                    weight = _physicsRig.Inputs[i + currentInputIndex].Weight / MaximumWeight;

                    if (_physicsRig.Inputs[i + currentInputIndex].SourceParameterIndex == -1)
                    {
                        _physicsRig.Inputs[i + currentInputIndex].SourceParameterIndex = model.GetParameterIndex(_physicsRig.Inputs[i + currentInputIndex].Source.Id);
                    }

                    _physicsRig.Inputs[i + currentInputIndex].GetNormalizedParameterValue(
                        ref totalTranslation,
                        ref totalAngle,
                        _parameterCaches[_physicsRig.Inputs[i + currentInputIndex].SourceParameterIndex],
                        parameterMinimumValues[_physicsRig.Inputs[i + currentInputIndex].SourceParameterIndex],
                        parameterMaximumValues[_physicsRig.Inputs[i + currentInputIndex].SourceParameterIndex],
                        parameterDefaultValues[_physicsRig.Inputs[i + currentInputIndex].SourceParameterIndex],
                        currentSetting.NormalizationPosition,
                        currentSetting.NormalizationAngle,
                        _physicsRig.Inputs[i + currentInputIndex].Reflect,
                        weight
                    );
                }

                radAngle = CubismMath.DegreesToRadian(-totalAngle);

                totalTranslation.X = (totalTranslation.X * MathF.Cos(radAngle) - totalTranslation.Y * MathF.Sin(radAngle));
                totalTranslation.Y = (totalTranslation.X * MathF.Sin(radAngle) + totalTranslation.Y * MathF.Cos(radAngle));

                // Calculate particles position.
                UpdateParticles(
                    _physicsRig.Particles,
                    currentParticleIndex,
                    currentSetting.ParticleCount,
                    totalTranslation,
                    totalAngle,
                    _options.Wind,
                    MovementThreshold * currentSetting.NormalizationPosition.Maximum,
                    physicsDeltaTime,
                    AirResistance
                );

                // Update output parameters.
                for (i = 0; i < currentSetting.OutputCount; ++i)
                {
                    particleIndex = _physicsRig.Outputs[i + currentOutputIndex].VertexIndex;

                    if (_physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex == -1)
                    {
                        _physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex = model.GetParameterIndex(_physicsRig.Outputs[i + currentOutputIndex].Destination.Id);
                    }

                    if (particleIndex < 1 || particleIndex >= currentSetting.ParticleCount)
                    {
                        continue;
                    }

                    Vector2 translation;
                    translation.X = _physicsRig.Particles[particleIndex + currentParticleIndex].Position.X - _physicsRig.Particles[particleIndex - 1 + currentParticleIndex].Position.X;
                    translation.Y = _physicsRig.Particles[particleIndex + currentParticleIndex].Position.Y - _physicsRig.Particles[particleIndex - 1 + currentParticleIndex].Position.Y;

                    outputValue = _physicsRig.Outputs[i + currentOutputIndex].GetValue(
                        translation,
                        _physicsRig.Particles,
                        currentParticleIndex,
                        particleIndex,
                        _physicsRig.Outputs[i + currentOutputIndex].Reflect,
                        _options.Gravity
                    );

                    _currentRigOutputs[settingIndex].outputs[i] = outputValue;

                    fixed (float* prt = _parameterCaches)
                        UpdateOutputParameterValue(
                                &prt[_physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex],
                                parameterMinimumValues[_physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex],
                                parameterMaximumValues[_physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex],
                                outputValue,
                                _physicsRig.Outputs[i + currentOutputIndex]);
                }
            }

            _currentRemainTime -= physicsDeltaTime;
        }

        float alpha = _currentRemainTime / physicsDeltaTime;
        Interpolate(model, alpha);
    }

    /// <summary>
    /// オプションを設定する。
    /// </summary>
    /// <param name="options"> オプション</param>
    public  void SetOptions(Options options)
    {
        _options = options;
    }

    /// <summary>
    /// オプションを取得する。
    /// </summary>
    /// <returns>オプション</returns>
    public Options GetOptions()
    {
        return _options;
    }

    /// <summary>
    /// 初期化する。
    /// </summary>
    private void Initialize()
    {
        CubismPhysicsSubRig currentSetting;
        int i, settingIndex;
        Vector2 radius;

        for (settingIndex = 0; settingIndex < _physicsRig.SubRigCount; ++settingIndex)
        {
            currentSetting = _physicsRig.Settings[settingIndex];
            var index = currentSetting.BaseParticleIndex;

            // Initialize the top of particle.
            _physicsRig.Particles[index].InitialPosition = new Vector2(0.0f, 0.0f);
            _physicsRig.Particles[index].LastPosition = _physicsRig.Particles[index].InitialPosition;
            _physicsRig.Particles[index].LastGravity = new Vector2(0.0f, -1.0f);
            _physicsRig.Particles[index].LastGravity.Y *= -1.0f;
            _physicsRig.Particles[index].Velocity = new Vector2(0.0f, 0.0f);
            _physicsRig.Particles[index].Force = new Vector2(0.0f, 0.0f);

            // Initialize particles.
            for (i = 1; i < currentSetting.ParticleCount; ++i)
            {
                radius = new Vector2(0.0f, _physicsRig.Particles[i + index].Radius);
                _physicsRig.Particles[i + index].InitialPosition = _physicsRig.Particles[i - 1 + index].InitialPosition + radius;
                _physicsRig.Particles[i + index].Position = _physicsRig.Particles[i + index].InitialPosition;
                _physicsRig.Particles[i + index].LastPosition = _physicsRig.Particles[i + index].InitialPosition;
                _physicsRig.Particles[i + index].LastGravity = new Vector2(0.0f, -1.0f);
                _physicsRig.Particles[i + index].LastGravity.Y *= -1.0f;
                _physicsRig.Particles[i + index].Velocity = new Vector2(0.0f, 0.0f);
                _physicsRig.Particles[i + index].Force = new Vector2(0.0f, 0.0f);
            }
        }
    }

    /// <summary>
    /// 振り子演算の最新の結果と一つ前の結果から指定した重みで適用する。
    /// </summary>
    /// <param name="model">物理演算の結果を適用するモデル</param>
    /// <param name="weight">最新結果の重み</param>
    private unsafe void Interpolate(CubismModel model, float weight)
    {
        int i, settingIndex;
        float* parameterValues;
        float* parameterMaximumValues;
        float* parameterMinimumValues;

        parameterValues = CubismCore.csmGetParameterValues(model.GetModel());
        parameterMaximumValues = CubismCore.csmGetParameterMaximumValues(model.GetModel());
        parameterMinimumValues = CubismCore.csmGetParameterMinimumValues(model.GetModel());

        for (settingIndex = 0; settingIndex < _physicsRig.SubRigCount; ++settingIndex)
        {
            var currentSetting = _physicsRig.Settings[settingIndex];
            var currentOutputIndex = currentSetting.BaseOutputIndex;

            // Load input parameters.
            for (i = 0; i < currentSetting.OutputCount; ++i)
            {
                if (_physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex == -1)
                {
                    continue;
                }

                UpdateOutputParameterValue(
                    &parameterValues[_physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex],
                    parameterMinimumValues[_physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex],
                    parameterMaximumValues[_physicsRig.Outputs[i + currentOutputIndex].DestinationParameterIndex],
                    _previousRigOutputs[settingIndex].outputs[i] * (1 - weight) + _currentRigOutputs[settingIndex].outputs[i] * weight,
                    _physicsRig.Outputs[i + currentOutputIndex]
                );
            }
        }
    }

    private float GetRangeValue(float min, float max)
    {
        float maxValue = CubismMath.Max(min, max);
        float minValue = CubismMath.Min(min, max);

        return MathF.Abs(maxValue - minValue);
    }

    /// Gets sign.
    ///
    /// @param  value  Evaluation target value.
    ///
    /// @return  Sign of value.
    private int Sign(float value)
    {
        int ret = 0;

        if (value > 0.0f)
        {
            ret = 1;
        }
        else if (value < 0.0f)
        {
            ret = -1;
        }

        return ret;
    }

    private float GetDefaultValue(float min, float max)
    {
        float minValue = CubismMath.Min(min, max);
        return minValue + (GetRangeValue(min, max) / 2.0f);
    }

    private float NormalizeParameterValue(
        float value,
        float parameterMinimum,
        float parameterMaximum,
        float parameterDefault,
        float normalizedMinimum,
        float normalizedMaximum,
        float normalizedDefault,
        bool isInverted)
    {
        float result = 0.0f;

        float maxValue = CubismMath.Max(parameterMaximum, parameterMinimum);

        if (maxValue < value)
        {
            value = maxValue;
        }

        float minValue = CubismMath.Min(parameterMaximum, parameterMinimum);

        if (minValue > value)
        {
            value = minValue;
        }

        float minNormValue = CubismMath.Min(normalizedMinimum, normalizedMaximum);
        float maxNormValue = CubismMath.Max(normalizedMinimum, normalizedMaximum);
        float middleNormValue = normalizedDefault;

        float middleValue = GetDefaultValue(minValue, maxValue);
        float paramValue = value - middleValue;

        switch (Sign(paramValue))
        {
            case 1:
                {
                    float nLength = maxNormValue - middleNormValue;
                    float pLength = maxValue - middleValue;
                    if (pLength != 0.0f)
                    {
                        result = paramValue * (nLength / pLength);
                        result += middleNormValue;
                    }

                    break;
                }
            case -1:
                {
                    float nLength = minNormValue - middleNormValue;
                    float pLength = minValue - middleValue;
                    if (pLength != 0.0f)
                    {
                        result = paramValue * (nLength / pLength);
                        result += middleNormValue;
                    }

                    break;
                }
            case 0:
                {
                    result = middleNormValue;

                    break;
                }
            default:
                {
                    break;
                }
        }

        return isInverted ? result : (result * -1.0f);
    }

    private void GetInputTranslationXFromNormalizedParameterValue(ref Vector2 targetTranslation, ref float targetAngle, float value,
        float parameterMinimumValue, float parameterMaximumValue,
        float parameterDefaultValue,
        CubismPhysicsNormalization normalizationPosition,
        CubismPhysicsNormalization normalizationAngle, bool isInverted,
        float weight)
    {
        targetTranslation.X += NormalizeParameterValue(
            value,
            parameterMinimumValue,
            parameterMaximumValue,
            parameterDefaultValue,
            normalizationPosition.Minimum,
            normalizationPosition.Maximum,
            normalizationPosition.Default,
            isInverted
        ) * weight;
    }

    private void GetInputTranslationYFromNormalizedParameterValue(ref Vector2 targetTranslation, ref float targetAngle, float value,
        float parameterMinimumValue, float parameterMaximumValue,
        float parameterDefaultValue,
        CubismPhysicsNormalization normalizationPosition,
        CubismPhysicsNormalization normalizationAngle,
        bool isInverted, float weight)
    {
        targetTranslation.Y += NormalizeParameterValue(
            value,
            parameterMinimumValue,
            parameterMaximumValue,
            parameterDefaultValue,
            normalizationPosition.Minimum,
            normalizationPosition.Maximum,
            normalizationPosition.Default,
            isInverted
        ) * weight;
    }

    private void GetInputAngleFromNormalizedParameterValue(ref Vector2 targetTranslation, ref float targetAngle, float value,
        float parameterMinimumValue, float parameterMaximumValue,
        float parameterDefaultValue,
        CubismPhysicsNormalization normalizationPosition,
        CubismPhysicsNormalization normalizationAngle,
        bool isInverted, float weight)
    {
        targetAngle += NormalizeParameterValue(
            value,
            parameterMinimumValue,
            parameterMaximumValue,
            parameterDefaultValue,
            normalizationAngle.Minimum,
            normalizationAngle.Maximum,
            normalizationAngle.Default,
            isInverted
        ) * weight;
    }

    private float GetOutputTranslationX(Vector2 translation, CubismPhysicsParticle[] particles, int start, 
        int particleIndex, bool isInverted, Vector2 parentGravity)
    {
        float outputValue = translation.X;

        if (isInverted)
        {
            outputValue *= -1.0f;
        }

        return outputValue;
    }

    private float GetOutputTranslationY(Vector2 translation, CubismPhysicsParticle[] particles, int start, 
        int particleIndex, bool isInverted, Vector2 parentGravity)
    {
        float outputValue = translation.Y;

        if (isInverted)
        {
            outputValue *= -1.0f;
        }

        return outputValue;
    }

    private float GetOutputAngle(Vector2 translation, CubismPhysicsParticle[] particles, int start, 
        int particleIndex, bool isInverted, Vector2 parentGravity)
    {
        float outputValue;

        if (particleIndex >= 2)
        {
            parentGravity = particles[particleIndex - 1 + start].Position - particles[particleIndex - 2 + start].Position;
        }
        else
        {
            parentGravity *= -1.0f;
        }

        outputValue = CubismMath.DirectionToRadian(parentGravity, translation);

        if (isInverted)
        {
            outputValue *= -1.0f;
        }

        return outputValue;
    }

    private float GetOutputScaleTranslationX(Vector2 translationScale, float angleScale)
    {
        return translationScale.X;
    }

    private float GetOutputScaleTranslationY(Vector2 translationScale, float angleScale)
    {
        return translationScale.Y;
    }

    private float GetOutputScaleAngle(Vector2 translationScale, float angleScale)
    {
        return angleScale;
    }

    /// Updates particles.
    ///
    /// @param  strand            Target array of particle.
    /// @param  strandCount       Count of particle.
    /// @param  totalTranslation  Total translation value.
    /// @param  totalAngle        Total angle.
    /// @param  windDirection              Direction of wind.
    /// @param  thresholdValue    Threshold of movement.
    /// @param  deltaTimeSeconds  Delta time.
    /// @param  airResistance     Air resistance.
    private void UpdateParticles(CubismPhysicsParticle[] strand, int start, int strandCount, Vector2 totalTranslation, float totalAngle,
        Vector2 windDirection, float thresholdValue, float deltaTimeSeconds, float airResistance)
    {
        int i;
        float totalRadian;
        float delay;
        float radian;
        Vector2 currentGravity;
        Vector2 direction;
        Vector2 velocity;
        Vector2 force;
        Vector2 newDirection;

        strand[start].Position = totalTranslation;

        totalRadian = CubismMath.DegreesToRadian(totalAngle);
        currentGravity = CubismMath.RadianToDirection(totalRadian);
        currentGravity.Normalize();

        for (i = 1; i < strandCount; ++i)
        {
            strand[i + start].Force = (currentGravity * strand[i + start].Acceleration) + windDirection;

            strand[i + start].LastPosition = strand[i].Position;

            delay = strand[i + start].Delay * deltaTimeSeconds * 30.0f;

            direction.X = strand[i + start].Position.X - strand[i - 1 + start].Position.X;
            direction.Y = strand[i + start].Position.Y - strand[i - 1 + start].Position.Y;

            radian = CubismMath.DirectionToRadian(strand[i].LastGravity, currentGravity) / airResistance;

            direction.X = (MathF.Cos(radian) * direction.X) - (direction.Y * MathF.Sin(radian));
            direction.Y = (MathF.Sin(radian) * direction.X) + (direction.Y * MathF.Cos(radian));

            strand[i + start].Position = strand[i - 1 + start].Position + direction;

            velocity.X = strand[i + start].Velocity.X * delay;
            velocity.Y = strand[i + start].Velocity.Y * delay;
            force = strand[i + start].Force * delay * delay;

            strand[i + start].Position = strand[i + start].Position + velocity + force;

            newDirection = strand[i + start].Position - strand[i - 1 + start].Position;

            newDirection.Normalize();

            strand[i + start].Position = strand[i - 1 + start].Position + (newDirection * strand[i + start].Radius);

            if (MathF.Abs(strand[i + start].Position.X) < thresholdValue)
            {
                strand[i + start].Position.X = 0.0f;
            }

            if (delay != 0.0f)
            {
                strand[i + start].Velocity.X = strand[i + start].Position.X - strand[i + start].LastPosition.X;
                strand[i + start].Velocity.Y = strand[i + start].Position.Y - strand[i + start].LastPosition.Y;
                strand[i + start].Velocity /= delay;
                strand[i + start].Velocity *= strand[i + start].Mobility;
            }

            strand[i + start].Force = new Vector2(0.0f, 0.0f);
            strand[i + start].LastGravity = currentGravity;
        }
    }

    /**
     * Updates particles for stabilization.
     *
     * @param strand                Target array of particle.
     * @param strandCount           Count of particle.
     * @param totalTranslation      Total translation value.
     * @param totalAngle            Total angle.
     * @param windDirection         Direction of Wind.
     * @param thresholdValue        Threshold of movement.
     */
    private void UpdateParticlesForStabilization(CubismPhysicsParticle[] strand, int start, int strandCount, Vector2 totalTranslation, float totalAngle,
        Vector2 windDirection, float thresholdValue)
    {
        int i;
        float totalRadian;
        Vector2 currentGravity;
        Vector2 force;

        strand[start].Position = totalTranslation;

        totalRadian = CubismMath.DegreesToRadian(totalAngle);
        currentGravity = CubismMath.RadianToDirection(totalRadian);
        currentGravity.Normalize();

        for (i = 1; i < strandCount; ++i)
        {
            strand[i + start].Force = (currentGravity * strand[i + start].Acceleration) + windDirection;

            strand[i + start].LastPosition = strand[i + start].Position;

            strand[i + start].Velocity = new Vector2(0.0f, 0.0f);

            force = strand[i + start].Force;
            force.Normalize();

            force *= strand[i + start].Radius;
            strand[i + start].Position = strand[i - 1].Position + force;

            if (MathF.Abs(strand[i + start].Position.X) < thresholdValue)
            {
                strand[i + start].Position.X = 0.0f;
            }

            strand[i + start].Force = new Vector2(0.0f, 0.0f);
            strand[i + start].LastGravity = currentGravity;
        }
    }

    /// Updates output parameter value.
    ///
    /// @param  parameterValue         Target parameter value.
    /// @param  parameterValueMinimum  Minimum of parameter value.
    /// @param  parameterValueMaximum  Maximum of parameter value.
    /// @param  translation            Translation value.
    private unsafe void UpdateOutputParameterValue(float* parameterValue, float parameterValueMinimum, float parameterValueMaximum,
        float translation, CubismPhysicsOutput output)
    {
        float outputScale;
        float value;
        float weight;

        outputScale = output.GetScale(output.TranslationScale, output.AngleScale);

        value = translation * outputScale;

        if (value < parameterValueMinimum)
        {
            if (value < output.ValueBelowMinimum)
            {
                output.ValueBelowMinimum = value;
            }

            value = parameterValueMinimum;
        }
        else if (value > parameterValueMaximum)
        {
            if (value > output.ValueExceededMaximum)
            {
                output.ValueExceededMaximum = value;
            }

            value = parameterValueMaximum;
        }

        weight = output.Weight / MaximumWeight;

        if (weight >= 1.0f)
        {
            *parameterValue = value;
        }
        else
        {
            value = (*parameterValue * (1.0f - weight)) + (value * weight);
            *parameterValue = value;
        }
    }
}
