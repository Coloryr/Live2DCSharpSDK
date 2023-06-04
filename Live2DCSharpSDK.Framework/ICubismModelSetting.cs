using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework;

public interface ICubismModelSetting
{
    /// <summary>
    /// Mocファイルの名前を取得する
    /// </summary>
    /// <returns>Mocファイルの名前</returns>
    string GetModelFileName();

    /// <summary>
    /// モデルが使用するテクスチャの数を取得する
    /// </summary>
    /// <returns>テクスチャの数</returns>
    int GetTextureCount();

    /// <summary>
    /// テクスチャが配置されたディレクトリの名前を取得する
    /// </summary>
    /// <returns>テクスチャが配置されたディレクトリの名前</returns>
    string GetTextureDirectory();

    /// <summary>
    /// モデルが使用するテクスチャの名前を取得する
    /// </summary>
    /// <param name="index">配列のインデックス値</param>
    /// <returns>テクスチャの名前</returns>
    string GetTextureFileName(int index);

    /// <summary>
    /// モデルに設定された当たり判定の数を取得する
    /// </summary>
    /// <returns>モデルに設定された当たり判定の数</returns>
    int GetHitAreasCount();

    /// <summary>
    /// 当たり判定に設定されたIDを取得する
    /// </summary>
    /// <param name="index">配列のインデックス値</param>
    /// <returns>当たり判定に設定されたID</returns>
    string GetHitAreaId(int index);

    /// <summary>
    /// 当たり判定に設定された名前を取得する
    /// </summary>
    /// <param name="index">配列のインデックス値</param>
    /// <returns>当たり判定に設定された名前</returns>
    string GetHitAreaName(int index);

    /// <summary>
    /// 物理演算設定ファイルの名前を取得する
    /// </summary>
    /// <returns>物理演算設定ファイルの名前</returns>
    string GetPhysicsFileName();

    /// <summary>
    /// パーツ切り替え設定ファイルの名前を取得する
    /// </summary>
    /// <returns>パーツ切り替え設定ファイルの名前</returns>
    string GetPoseFileName();

    /// <summary>
    /// 表示名称設定ファイルの名前を取得する
    /// </summary>
    /// <returns>表示名称設定ファイルの名前</returns>
    string GetDisplayInfoFileName();

    /// <summary>
    /// 表情設定ファイルの数を取得する
    /// </summary>
    /// <returns>表情設定ファイルの数</returns>
    int GetExpressionCount();

    /// <summary>
    /// 表情設定ファイルを識別する名前（別名）を取得する
    /// </summary>
    /// <param name="index">配列のインデックス値</param>
    /// <returns>表情の名前</returns>
    string GetExpressionName(int index);

    /// <summary>
    /// 表情設定ファイルの名前を取得する
    /// </summary>
    /// <param name="index">配列のインデックス値</param>
    /// <returns>表情設定ファイルの名前</returns>
    string GetExpressionFileName(int index);

    /// <summary>
    /// モーショングループの数を取得する
    /// </summary>
    /// <returns>モーショングループの数</returns>
    int GetMotionGroupCount();

    /// <summary>
    /// モーショングループの名前を取得する
    /// </summary>
    /// <param name="index">配列のインデックス値</param>
    /// <returns>モーショングループの名前</returns>
    string GetMotionGroupName(int index);

    /// <summary>
    /// モーショングループに含まれるモーションの数を取得する
    /// </summary>
    /// <param name="groupName">モーショングループの名前</param>
    /// <returns>モーショングループの名前</returns>
    int GetMotionCount(string groupName);

    /// <summary>
    /// グループ名とインデックス値からモーションファイルの名前を取得する
    /// </summary>
    /// <param name="groupName">モーショングループの名前</param>
    /// <param name="index">配列のインデックス値</param>
    /// <returns>モーションファイルの名前</returns>
    string GetMotionFileName(string groupName, int index);

    /// <summary>
    /// モーションに対応するサウンドファイルの名前を取得する
    /// </summary>
    /// <param name="groupName">モーショングループの名前</param>
    /// <param name="index">配列のインデックス値</param>
    /// <returns>サウンドファイルの名前</returns>
    string GetMotionSoundFileName(string groupName, int index);

    /// <summary>
    /// モーション開始時のフェードイン処理時間を取得する
    /// </summary>
    /// <param name="groupName">モーショングループの名前</param>
    /// <param name="index">配列のインデックス値</param>
    /// <returns>フェードイン処理時間[秒]</returns>
    float GetMotionFadeInTimeValue(string groupName, int index);

    /// <summary>
    /// モーション終了時のフェードアウト処理時間を取得する
    /// </summary>
    /// <param name="groupName">モーショングループの名前</param>
    /// <param name="index">配列のインデックス値</param>
    /// <returns>フェードアウト処理時間[秒]</returns>
    float GetMotionFadeOutTimeValue(string groupName, int index);

    /// <summary>
    /// ユーザデータのファイル名を取得する
    /// </summary>
    /// <returns>ユーザデータのファイル名</returns>
    string GetUserDataFile();

    /// <summary>
    /// レイアウト情報を取得する
    /// </summary>
    /// <param name="outLayoutMap">csmMapクラスのインスタンス</param>
    /// <returns>true  -> レイアウト情報が存在する
    /// false -> レイアウト情報が存在しない</returns>
    bool GetLayoutMap(Dictionary<string, float> outLayoutMap);

    /// <summary>
    /// 目パチに関連付けられたパラメータの数を取得する
    /// </summary>
    /// <returns>目パチに関連付けられたパラメータの数</returns>
    int GetEyeBlinkParameterCount();

    /// <summary>
    /// 目パチに関連付けられたパラメータのIDを取得する
    /// </summary>
    /// <param name="index">配列のインデックス値</param>
    /// <returns>パラメータID</returns>
    string GetEyeBlinkParameterId(int index);

    /// <summary>
    /// リップシンクに関連付けられたパラメータの数を取得する
    /// </summary>
    /// <returns>リップシンクに関連付けられたパラメータの数</returns>
    int GetLipSyncParameterCount();

    /// <summary>
    /// リップシンクに関連付けられたパラメータのIDを取得する
    /// </summary>
    /// <param name="index">配列のインデックス値</param>
    /// <returns>パラメータID</returns>
    string GetLipSyncParameterId(int index);
}
