using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace RandomDrawer
{
    /// <summary>
    /// 程序设置。默认存在 exe 同目录的 settings.ini（UTF-8，可手动编辑）；
    /// 该目录不可写时自动改用 %APPDATA%\RandomDrawer\settings.ini。
    /// </summary>
    internal sealed class Settings
    {
        private const string FileName = "settings.ini";

        // 键名别名（大小写不敏感，兼容中英文手写）
        private static readonly string[] StartKeys = new string[] { "Start", "开始" };
        private static readonly string[] EndKeys = new string[] { "End", "结束" };
        private static readonly string[] CountKeys = new string[] { "Count", "Take", "每次抽取" };
        private static readonly string[] NoRepeatKeys = new string[] { "NoRepeat", "不允许重复" };
        private static readonly string[] EffectKeys = new string[] { "Effect", "摇号特效" };
        private static readonly string[] BottomKeys = new string[] { "Bottom", "BottomList", "垫底", "垫底号码" };
        private static readonly string[] WeightKeys = new string[] { "Weights", "WeightsList", "权重", "权重号码" };

        /// <summary>界面默认值：开始号码。</summary>
        internal int Start = 1;

        /// <summary>界面默认值：结束号码。</summary>
        internal int End = 44;

        /// <summary>界面默认值：每次抽取个数。</summary>
        internal int Count = 1;

        /// <summary>界面默认值：是否勾选"不允许重复"。</summary>
        internal bool NoRepeat = true;

        /// <summary>界面默认值：是否勾选"摇号特效"。</summary>
        internal bool Effect = true;

        /// <summary>垫底号码原文（这些号码最后才会被抽中）。</summary>
        internal string BottomText = string.Empty;

        /// <summary>权重号码原文（"号码=倍数"）。</summary>
        internal string WeightsText = string.Empty;

        /// <summary>exe 同目录的 settings.ini。</summary>
        internal static string FilePath
        {
            get
            {
                string dir = Path.GetDirectoryName(Application.ExecutablePath);
                return Path.Combine(dir, FileName);
            }
        }

        /// <summary>兜底位置（exe 目录不可写时使用）。</summary>
        internal static string FallbackPath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RandomDrawer");
                return Path.Combine(dir, FileName);
            }
        }

        /// <summary>读取设置；两个位置都没有文件时，尝试从旧版注册表迁移。</summary>
        internal static Settings Load()
        {
            Settings settings = new Settings();
            string[] candidates = new string[] { FilePath, FallbackPath };
            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    settings.ReadFile(candidates[i]);
                    return settings;
                }
            }
            settings.MigrateFromRegistry();
            return settings;
        }

        /// <summary>写回设置文件（先试 exe 目录，失败再试 %APPDATA%）。</summary>
        internal void Save()
        {
            string[] lines = new string[]
            {
                "# 幸运之子——摇号机 设置文件（UTF-8，可手动编辑；改完重启程序生效）",
                "# 界面默认值",
                "Start=" + Start,
                "End=" + End,
                "Count=" + Count,
                "NoRepeat=" + (NoRepeat ? "1" : "0"),
                "Effect=" + (Effect ? "1" : "0"),
                "# 垫底号码：最后才会被抽中（逗号/空格分隔）",
                "Bottom=" + OneLine(BottomText),
                "# 权重号码：号码=倍数，如 6=5（可多行或用逗号分隔）",
                "Weights=" + OneLine(WeightsText)
            };

            try
            {
                File.WriteAllLines(FilePath, lines, new UTF8Encoding(true));
                return;
            }
            catch (Exception)
            {
                // exe 目录不可写（例如放在 Program Files），改用 %APPDATA%
            }

            try
            {
                string dir = Path.GetDirectoryName(FallbackPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllLines(FallbackPath, lines, new UTF8Encoding(true));
            }
            catch (Exception)
            {
                // 两个位置都写不进去就放弃（程序仍可正常运行，只是不记忆）
            }
        }

        /// <summary>把多行文本压成一行（换行与逗号在解析时等价）。</summary>
        private static string OneLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            return text.Replace("\r\n", ",").Replace("\r", ",").Replace("\n", ",").Trim();
        }

        private void ReadFile(string path)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path, Encoding.UTF8);
            }
            catch (Exception)
            {
                return;
            }

            string lastKey = null;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';' || line[0] == '[')
                {
                    continue;
                }
                int eq = line.IndexOf('=');
                string key = eq > 0 ? line.Substring(0, eq).Trim() : null;
                if (key != null && IsKnownKey(key))
                {
                    lastKey = key;
                    Apply(key, line.Substring(eq + 1).Trim());
                }
                else if (lastKey != null)
                {
                    // 续行：上一项的值接着写（多行垫底/权重）
                    Append(lastKey, line);
                }
            }
        }

        private void Apply(string key, string value)
        {
            int number;
            if (Match(key, StartKeys))
            {
                if (int.TryParse(value, out number))
                {
                    Start = number;
                }
                return;
            }
            if (Match(key, EndKeys))
            {
                if (int.TryParse(value, out number))
                {
                    End = number;
                }
                return;
            }
            if (Match(key, CountKeys))
            {
                if (int.TryParse(value, out number) && number >= 1)
                {
                    Count = number;
                }
                return;
            }
            if (Match(key, NoRepeatKeys))
            {
                NoRepeat = IsTrue(value);
                return;
            }
            if (Match(key, EffectKeys))
            {
                Effect = IsTrue(value);
                return;
            }
            if (Match(key, BottomKeys))
            {
                BottomText = value;
                return;
            }
            if (Match(key, WeightKeys))
            {
                WeightsText = value;
                return;
            }
        }

        private void Append(string key, string extra)
        {
            if (Match(key, BottomKeys))
            {
                BottomText = Join(BottomText, extra);
            }
            else if (Match(key, WeightKeys))
            {
                WeightsText = Join(WeightsText, extra);
            }
        }

        private static string Join(string first, string second)
        {
            if (string.IsNullOrEmpty(first))
            {
                return second;
            }
            return first + "," + second;
        }

        private static bool IsTrue(string value)
        {
            string v = value.Trim().ToLowerInvariant();
            return v == "1" || v == "true" || v == "yes" || v == "on" || v == "是" || v == "勾选";
        }

        private static bool Match(string key, string[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (string.Equals(key, keys[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsKnownKey(string key)
        {
            return Match(key, StartKeys) || Match(key, EndKeys) || Match(key, CountKeys)
                || Match(key, NoRepeatKeys) || Match(key, EffectKeys)
                || Match(key, BottomKeys) || Match(key, WeightKeys);
        }

        /// <summary>
        /// 旧版把配置写在注册表 HKCU\Software\随机抽号器。
        /// 首次运行（还没有 settings.ini）时把旧值迁移过来并立刻落盘，
        /// 之后就以 settings.ini 为准（旧注册表键保持不动）。
        /// </summary>
        private void MigrateFromRegistry()
        {
            try
            {
                string bottom = Config.ReadReg("Blacklist");
                string weights = Config.ReadReg("Weights");
                if (!string.IsNullOrEmpty(bottom))
                {
                    BottomText = bottom;
                }
                if (!string.IsNullOrEmpty(weights))
                {
                    WeightsText = weights;
                }
            }
            catch (Exception)
            {
                // 读注册表失败不影响使用
            }
            Save();
        }
    }
}
