using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace RandomDrawer
{
    /// <summary>
    /// 文本解析与旧版注册表兼容层。
    /// 号码/权重文本的解析规则与原始程序保持一致，方便沿用旧配置。
    /// </summary>
    internal static class Config
    {
        /// <summary>隐藏设置面板的入口码（在"开始"框里输入它再点"随机抽号"）。</summary>
        internal const string MAGIC_CODE = "1016";

        /// <summary>旧版本使用的注册表位置（仅用于首次迁移，不再写入）。</summary>
        internal const string REG_SUBKEY = @"Software\随机抽号器";

        /// <summary>号码分隔符（与原始程序一致，另加一个全角空格，共 10 个）。</summary>
        private static readonly char[] NumberSeparators =
            new char[] { ',', '，', ' ', '\u3000', '\t', '\r', '\n', '、', '/', ';' };

        /// <summary>权重行内 "号码=倍数" 的分隔符（与原始程序一致）。</summary>
        private static readonly char[] WeightSeparators = new char[] { '=', ':', ' ' };

        /// <summary>同一行里多组权重之间的分隔符。</summary>
        private static readonly char[] WeightItemSeparators = new char[] { ',', '，', '、', ';' };

        /// <summary>把文本解析成不重复的号码集合。</summary>
        internal static HashSet<int> ParseNumberSet(string text)
        {
            return new HashSet<int>(ParseNumbers(text));
        }

        /// <summary>把文本解析成号码列表（顺序保留，重复项保留）。</summary>
        internal static List<int> ParseNumbers(string text)
        {
            List<int> result = new List<int>();
            if (text == null)
            {
                return result;
            }
            string[] parts = text.Split(NumberSeparators);
            for (int i = 0; i < parts.Length; i++)
            {
                string s = parts[i].Trim();
                if (s.Length == 0)
                {
                    continue;
                }
                int num;
                if (int.TryParse(s, out num))
                {
                    result.Add(num);
                }
            }
            return result;
        }

        /// <summary>把文本解析成 "号码 → 倍数" 字典（只收正数，非法条目忽略）。</summary>
        internal static Dictionary<int, double> ParseWeights(string text)
        {
            Dictionary<int, double> result = new Dictionary<int, double>();
            if (text == null)
            {
                return result;
            }
            string[] lines = text.Split(new char[] { '\r', '\n' });
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                {
                    continue;
                }
                string[] items = line.Split(WeightItemSeparators);
                for (int j = 0; j < items.Length; j++)
                {
                    string item = items[j].Trim();
                    if (item.Length == 0)
                    {
                        continue;
                    }
                    string[] kv = item.Split(WeightSeparators);
                    if (kv.Length < 2)
                    {
                        continue;
                    }
                    int num;
                    double weight;
                    if (!int.TryParse(kv[0].Trim(), out num))
                    {
                        continue;
                    }
                    if (!double.TryParse(kv[1].Trim(), out weight))
                    {
                        continue;
                    }
                    if (weight <= 0.0)
                    {
                        continue;
                    }
                    result[num] = weight;
                }
            }
            return result;
        }

        /// <summary>取号码的权重：未配置按 1.0，负数按 0。</summary>
        internal static double WeightOf(int number, Dictionary<int, double> weights)
        {
            double weight;
            if (weights == null || !weights.TryGetValue(number, out weight))
            {
                return 1.0;
            }
            return weight < 0.0 ? 0.0 : weight;
        }

        /// <summary>读旧版注册表值（迁移用；没有返回空串）。</summary>
        internal static string ReadReg(string name)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_SUBKEY))
            {
                if (key == null)
                {
                    return string.Empty;
                }
                object value = key.GetValue(name);
                return value == null ? string.Empty : value.ToString();
            }
        }
    }
}
