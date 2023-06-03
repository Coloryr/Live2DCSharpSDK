﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Effect;

public record PartData
{
    /// <summary>
    /// パーツID
    /// </summary>
    public string PartId;
    /// <summary>
    /// パラメータのインデックス
    /// </summary>
    public int ParameterIndex;
    /// <summary>
    ///  パーツのインデックス
    /// </summary>
    public int PartIndex;
    /// <summary>
    ///  連動するパラメータ
    /// </summary>
    public readonly List<PartData> Link = new();

    public PartData()
    {

    }

    public PartData(PartData data)
    {
        PartId = data.PartId;

        Link.AddRange(data.Link);
    }

    /// <summary>
    /// 初期化する。
    /// </summary>
    /// <param name="model">初期化に使用するモデル</param>
    public void Initialize(CubismModel model)
    {
        ParameterIndex = model.GetParameterIndex(PartId);
        PartIndex = model.GetPartIndex(PartId);

        model.SetParameterValue(ParameterIndex, 1);
    }
}

public class CubismPose
{
    public const float Epsilon = 0.001f;
    public const float DefaultFadeInSeconds = 0.5f;

    // Pose.jsonのタグ
    public const string FadeIn = "FadeInTime";
    public const string Link = "Link";
    public const string Groups = "Groups";
    public const string Id = "Id";

    /// <summary>
    /// パーツグループ
    /// </summary>
    private List<PartData> _partGroups;
    /// <summary>
    /// それぞれのパーツグループの個数
    /// </summary>
    private List<int> _partGroupCounts;
    /// <summary>
    /// フェード時間[秒]
    /// </summary>
    private float _fadeTimeSeconds;
    /// <summary>
    /// 前回操作したモデル
    /// </summary>
    private CubismModel _lastModel;

    /// <summary>
    /// インスタンスを作成する。
    /// </summary>
    /// <param name="pose3json">pose3.jsonのデータ</param>
    public CubismPose(string pose3json)
    {
        var json = JObject.Parse(pose3json);

        // フェード時間の指定
        if (json[FadeIn] != null)
        {
            _fadeTimeSeconds = (float)json[FadeIn];

            if (_fadeTimeSeconds < 0.0f)
            {
                _fadeTimeSeconds = DefaultFadeInSeconds;
            }
        }

        // パーツグループ
        var poseListInfo = json[Groups] as JArray;

        foreach (var item in poseListInfo)
        {
            int idCount = item.Count();
            int groupCount = 0;

            for (int groupIndex = 0; groupIndex < idCount; ++groupIndex)
            {
                var partInfo = item[groupIndex];
                PartData partData = new();
                string parameterId = CubismFramework.GetIdManager().GetId(partInfo[Id].ToString());

                partData.PartId = parameterId;

                // リンクするパーツの設定
                if (partInfo[Link] != null)
                {
                    var linkListInfo = partInfo[Link];
                    int linkCount = linkListInfo.Count();

                    for (int linkIndex = 0; linkIndex < linkCount; ++linkIndex)
                    {
                        PartData linkPart = new();
                        string linkId = CubismFramework.GetIdManager().GetId(linkListInfo[linkIndex].ToString());

                        linkPart.PartId = linkId;

                        partData.Link.Add(linkPart);
                    }
                }

                _partGroups.Add(partData);

                ++groupCount;
            }

            _partGroupCounts.Add(groupCount);
        }
    }

    /// <summary>
    /// モデルのパラメータを更新する。
    /// </summary>
    /// <param name="model">対象のモデル</param>
    /// <param name="deltaTimeSeconds">デルタ時間[秒]</param>
    public void UpdateParameters(CubismModel model, float deltaTimeSeconds)
    {
        // 前回のモデルと同じではないときは初期化が必要
        if (model != _lastModel)
        {
            // パラメータインデックスの初期化
            Reset(model);
        }

        _lastModel = model;

        // 設定から時間を変更すると、経過時間がマイナスになることがあるので、経過時間0として対応。
        if (deltaTimeSeconds < 0.0f)
        {
            deltaTimeSeconds = 0.0f;
        }

        int beginIndex = 0;

        foreach (var item in _partGroupCounts)
        {
            DoFade(model, deltaTimeSeconds, beginIndex, item);

            beginIndex += item;
        }

        CopyPartOpacities(model);
    }

    /// <summary>
    /// 表示を初期化する。
    /// 
    /// 不透明度の初期値が0でないパラメータは、不透明度を1に設定する。
    /// </summary>
    /// <param name="model">対象のモデル</param>
    private void Reset(CubismModel model)
    {
        int beginIndex = 0;

        foreach (var item in _partGroupCounts)
        {
            for (int j = beginIndex; j < beginIndex + item; ++j)
            {
                _partGroups[j].Initialize(model);

                int partsIndex = _partGroups[j].PartIndex;
                int paramIndex = _partGroups[j].ParameterIndex;

                if (partsIndex < 0)
                {
                    continue;
                }

                model->SetPartOpacity(partsIndex, j == beginIndex ? 1.0f : 0.0f);
                model->SetParameterValue(paramIndex, j == beginIndex ? 1.0f : 0.0f);

                for (int k = 0; k < _partGroups[j].Link.Count; ++k)
                {
                    _partGroups[j].Link[k].Initialize(model);
                }
            }

            beginIndex += item;
        }
    }

    /// <summary>
    /// パーツの不透明度をコピーし、リンクしているパーツへ設定する。
    /// </summary>
    /// <param name="model">対象のモデル</param>
    private void CopyPartOpacities(CubismModel model)
    {
        foreach (var item in _partGroups)
        {
            if (item.Link.Count == 0)
            {
                continue; // 連動するパラメータはない
            }

            int partIndex = item.PartIndex;
            float opacity = model.GetPartOpacity(partIndex);

            foreach (var item1 in item.Link)
            {
                int linkPartIndex = item1.PartIndex;

                if (linkPartIndex < 0)
                {
                    continue;
                }

                model.SetPartOpacity(linkPartIndex, opacity);
            }
        }
    }

    private void DoFade(CubismModel model, float deltaTimeSeconds, int beginIndex, int partGroupCount)
    {
        int visiblePartIndex = -1;
        float newOpacity = 1.0f;

        float Phi = 0.5f;
        float BackOpacityThreshold = 0.15f;

        // 現在、表示状態になっているパーツを取得
        for (int i = beginIndex; i < beginIndex + partGroupCount; ++i)
        {
            int partIndex = _partGroups[i].PartIndex;
            int paramIndex = _partGroups[i].ParameterIndex;

            if (model.GetParameterValue(paramIndex) > Epsilon)
            {
                if (visiblePartIndex >= 0)
                {
                    break;
                }

                visiblePartIndex = i;
                newOpacity = model.GetPartOpacity(partIndex);

                // 新しい不透明度を計算
                newOpacity += (deltaTimeSeconds / _fadeTimeSeconds);

                if (newOpacity > 1.0f)
                {
                    newOpacity = 1.0f;
                }
            }
        }

        if (visiblePartIndex < 0)
        {
            visiblePartIndex = 0;
            newOpacity = 1.0f;
        }

        //  表示パーツ、非表示パーツの不透明度を設定する
        for (int i = beginIndex; i < beginIndex + partGroupCount; ++i)
        {
            int partsIndex = _partGroups[i].PartIndex;

            //  表示パーツの設定
            if (visiblePartIndex == i)
            {
                model.SetPartOpacity(partsIndex, newOpacity); // 先に設定
            }
            // 非表示パーツの設定
            else
            {
                float opacity = model.GetPartOpacity(partsIndex);
                float a1;          // 計算によって求められる不透明度

                if (newOpacity < Phi)
                {
                    a1 = newOpacity * (Phi - 1) / Phi + 1.0f; // (0,1),(phi,phi)を通る直線式
                }
                else
                {
                    a1 = (1 - newOpacity) * Phi / (1.0f - Phi); // (1,0),(phi,phi)を通る直線式
                }

                // 背景の見える割合を制限する場合
                float backOpacity = (1.0f - a1) * (1.0f - newOpacity);

                if (backOpacity > BackOpacityThreshold)
                {
                    a1 = 1.0f - BackOpacityThreshold / (1.0f - newOpacity);
                }

                if (opacity > a1)
                {
                    opacity = a1; // 計算の不透明度よりも大きければ（濃ければ）不透明度を上げる
                }

                model.SetPartOpacity(partsIndex, opacity);
            }
        }
    }
}
