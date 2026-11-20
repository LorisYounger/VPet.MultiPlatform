using LinePutScript;
using System;
using System.Security.Cryptography;
using System.Text;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 存档防作弊哈希
/// </summary>
/// <remarks>算法沿用 Windows 版 GameSave_v2。哈希输入是移除 hash 行后的完整 LPS 文本，行序和数字格式都会影响结果。
/// 跨平台宿主应使用 InvariantCulture 格式化数值，以便校验同一份存档。</remarks>
public static class SaveHash
{
    /// <summary>
    /// 当前哈希版本
    /// </summary>
    public const int CurrentVersion = 2;

    /// <summary>
    /// 计算存档哈希
    /// </summary>
    /// <param name="lps">已经去掉 hash 行的存档文档</param>
    public static long Compute(ILPS lps) => Sub.GetHashCode(lps.ToString()!);

    /// <summary>
    /// 把哈希写进存档
    /// </summary>
    /// <param name="lps">存档文档, 里面已有的 hash 行会被替换</param>
    /// <param name="hashCheck">当前存档是否还通过防作弊检查</param>
    /// <remarks>检查失效时写入 -1，保留存档已失去防作弊标记的状态。</remarks>
    public static void Write(ILPS lps, bool hashCheck)
    {
        lps.Remove("hash");
        if (hashCheck)
        {
            lps[(gi64)"hash"] = Compute(lps);
        }
        else
        {
            lps[(gint)"hash"] = -1;
        }
        lps["hash"][(gint)"ver"] = CurrentVersion;
    }

    /// <summary>
    /// 校验存档哈希
    /// </summary>
    /// <param name="lps">完整存档。校验会从文档中移除 hash 行。</param>
    /// <param name="version">读到的哈希版本</param>
    /// <returns>是否通过</returns>
    /// <remarks>当前版本使用存档哈希算法。其他版本先按旧版 MD5 校验，哈希值不匹配时再试当前算法。</remarks>
    public static bool Verify(ILPS lps, out int version)
    {
        long hash = lps.GetInt64("hash");
        version = lps["hash"].GetInt("ver");
        if (!lps.Remove("hash"))
            return false;

        if (version == CurrentVersion)
            return Compute(lps) == hash;

        try
        {
            using (MD5 md5 = MD5.Create())
            {
                if (BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(lps.ToString()!)), 0) == hash)
                    return true;
            }
        }
        catch (Exception)
        {
            return false;
        }
        return Compute(lps) == hash;
    }
}
