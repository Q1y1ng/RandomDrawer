using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace RandomDrawer
{
    /// <summary>
    /// 配置读写（基线版：与原始程序一致，全部存在注册表 HKCU\Software\随机抽号器）。
    /// </summary>
    internal static class Config
    {
        /// <summary>隐藏设置面板的入口码（在"开始"框里输入它再点"随机抽号"）。</summary>
        internal const string MAGIC_CODE = "1016";

        internal const string REG_SUBKEY = @"Software\随机抽号器";

        /// <summary>号码分隔符（与原始程序一致，共 9 个）。</summary>
        private static readonly char[] NumberSeparators =
            new char[] { ',', '，', ' ', '\t', '\r', '\n', '、', '/', ';' };

        /// <summary>"号码=倍数" 的分隔符（与原始程序一致）。</summary>
        private static readonly char[] WeightSeparators = new char[] { '=', ':', ' ' };

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

        internal static HashSet<int> LoadBlacklist()
        {
            return new HashSet<int>(ParseNumbers(ReadReg("Blacklist")));
        }

        internal static Dictionary<int, double> LoadWeights()
        {
            Dictionary<int, double> result = new Dictionary<int, double>();
            string text = ReadReg("Weights");
            if (text == null)
            {
                return result;
            }
            string[] lines = text.Split(new char[] { '\n' });
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                {
                    continue;
                }
                string[] kv = line.Split(WeightSeparators);
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
            return result;
        }

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

        internal static void Save(string blacklist, string weights)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REG_SUBKEY))
            {
                key.SetValue("Blacklist", blacklist ?? string.Empty, RegistryValueKind.String);
                key.SetValue("Weights", weights ?? string.Empty, RegistryValueKind.String);
            }
        }
    }
}
