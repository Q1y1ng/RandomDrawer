using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RandomDrawer.Tests
{
    /// <summary>
    /// 抽号算法与配置读写的控制台测试（不依赖界面）。
    /// 用 tests\test.cmd 运行；退出码 = 失败项数。
    /// </summary>
    internal static class PickerTests
    {
        private static int _failed;
        private static readonly Random Rnd = new Random(20260919);

        private static void Check(bool condition, string name)
        {
            Console.WriteLine((condition ? "  [PASS]  " : "  [FAIL]  ") + name);
            if (!condition)
            {
                _failed++;
            }
        }

        private static int Main()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch (Exception)
            {
                // 控制台编码设不了也无所谓
            }

            Console.WriteLine("== 垫底号码：最后才轮到 ==");
            TestBottomLast();
            Console.WriteLine("== 权重：倍数生效 ==");
            TestWeightSkew();
            Console.WriteLine("== 边界情况 ==");
            TestEdgeCases();
            Console.WriteLine("== 文本解析 ==");
            TestParsing();
            Console.WriteLine("== settings.ini 读写 ==");
            TestSettingsFile();

            Console.WriteLine();
            if (_failed == 0)
            {
                Console.WriteLine("全部通过 ✓");
            }
            else
            {
                Console.WriteLine("失败 " + _failed + " 项 ✗");
            }
            return _failed;
        }

        private static List<int> Range(int from, int to)
        {
            List<int> list = new List<int>();
            for (int i = from; i <= to; i++)
            {
                list.Add(i);
            }
            return list;
        }

        /// <summary>把 1..44 全抽完时，18/19 必须落在最后两位。</summary>
        private static void TestBottomLast()
        {
            List<int> pool = Range(1, 44);
            HashSet<int> bottom = new HashSet<int>(new int[] { 18, 19 });
            Picker picker = new Picker(Rnd, bottom, new Dictionary<int, double>());

            bool lastTwoOk = true;
            bool allPicked = true;
            for (int round = 0; round < 200; round++)
            {
                List<int> picked = picker.Pick(pool, 44);
                if (picked.Count != 44)
                {
                    allPicked = false;
                    break;
                }
                HashSet<int> unique = new HashSet<int>(picked);
                if (unique.Count != 44)
                {
                    allPicked = false;
                    break;
                }
                int second = picked[picked.Count - 2];
                int last = picked[picked.Count - 1];
                bool ok = (second == 18 || second == 19) && (last == 18 || last == 19) && second != last;
                if (!ok)
                {
                    lastTwoOk = false;
                    break;
                }
            }
            Check(allPicked, "44 个号码能被完整抽完且不重复（200 轮）");
            Check(lastTwoOk, "垫底号码 18/19 总是最后两个被抽到（200 轮）");

            // 只要还有普通号码，垫底号码就绝不能出现
            bool neverEarly = true;
            for (int round = 0; round < 300 && neverEarly; round++)
            {
                List<int> picked = picker.Pick(pool, 42); // 正好是普通号码的个数
                for (int i = 0; i < picked.Count; i++)
                {
                    if (picked[i] == 18 || picked[i] == 19)
                    {
                        neverEarly = false;
                        break;
                    }
                }
            }
            Check(neverEarly, "抽 42 个（普通号码数量）时垫底号码绝不出现（300 轮）");

            // 垫底号码也可以被抽到（不是"永远不中"）
            bool bottomReachable = false;
            for (int round = 0; round < 50 && !bottomReachable; round++)
            {
                List<int> picked = picker.Pick(pool, 44);
                if (picked.Contains(18) && picked.Contains(19))
                {
                    bottomReachable = true;
                }
            }
            Check(bottomReachable, "垫底号码最终一定会被抽到（不再是「永远不中」）");
        }

        /// <summary>6 号权重 5 倍时，中签率应明显高于其他号码。</summary>
        private static void TestWeightSkew()
        {
            List<int> pool = Range(1, 10); // 9 个普通号 + 6 号
            Dictionary<int, double> weights = new Dictionary<int, double>();
            weights[6] = 5.0;
            Picker picker = new Picker(Rnd, new HashSet<int>(), weights);

            int hit6 = 0;
            int rounds = 40000;
            for (int i = 0; i < rounds; i++)
            {
                if (picker.Pick(pool, 1)[0] == 6)
                {
                    hit6++;
                }
            }
            double expected = 5.0 / 14.0; // 6 号权重 5，其余 9 个各 1
            double actual = (double)hit6 / rounds;
            Console.WriteLine("        6 号实测概率 " + (actual * 100).ToString("0.00") + "%，理论 "
                + (expected * 100).ToString("0.00") + "%");
            Check(Math.Abs(actual - expected) < 0.02, "权重 5 倍的号码中签率接近理论值（±2%）");

            // 没配权重的号码应当等概率
            int[] hits = new int[11];
            for (int i = 0; i < 20000; i++)
            {
                hits[picker.Pick(pool, 1)[0]]++;
            }
            double min = double.MaxValue;
            double max = 0.0;
            for (int n = 1; n <= 10; n++)
            {
                if (n == 6)
                {
                    continue;
                }
                double p = (double)hits[n] / 20000;
                if (p < min)
                {
                    min = p;
                }
                if (p > max)
                {
                    max = p;
                }
            }
            Console.WriteLine("        其他号码概率区间 " + (min * 100).ToString("0.00") + "% ~ " + (max * 100).ToString("0.00") + "%");
            Check(max - min < 0.02, "未配权重的号码概率基本一致（极差 < 2%）");
        }

        private static void TestEdgeCases()
        {
            Picker picker = new Picker(Rnd, new HashSet<int>(), new Dictionary<int, double>());

            Check(picker.Pick(new List<int>(), 5).Count == 0, "空池子返回空结果，不抛异常");

            List<int> pool = Range(1, 10);
            List<int> copy = new List<int>(pool);
            List<int> picked = picker.Pick(pool, 3);
            Check(copy.Count == pool.Count, "抽号不会修改传入的池子");

            Check(picker.Pick(pool, 100).Count == 10, "要抽的个数超过池子大小时，抽完全部号码");

            // 全池都是垫底号码时也要能正常抽
            Picker allBottom = new Picker(Rnd, new HashSet<int>(pool), new Dictionary<int, double>());
            Check(allBottom.Pick(pool, 4).Count == 4, "整个池子都是垫底号码时仍能正常抽");

            // 权重全为负数（会被夹到 0）时退化为等概率，不抛异常
            Dictionary<int, double> negative = new Dictionary<int, double>();
            for (int i = 1; i <= 10; i++)
            {
                negative[i] = -5.0;
            }
            Picker zeroPicker = new Picker(Rnd, new HashSet<int>(), negative);
            Check(zeroPicker.Pick(pool, 5).Count == 5, "权重全为 0 时退化成等概率，仍能抽");
        }

        private static void TestParsing()
        {
            HashSet<int> numbers = Config.ParseNumberSet("18,19 20、21\n22/23;24　25");
            Check(numbers.Count == 8 && numbers.Contains(18) && numbers.Contains(25),
                "号码解析：逗号/空格/顿号/斜杠/分号/换行/全角空格都能切分（18~25 共 8 个）");

            Dictionary<int, double> w1 = Config.ParseWeights("6=5");
            Check(w1.Count == 1 && w1[6] == 5.0, "权重解析：6=5");

            Dictionary<int, double> w2 = Config.ParseWeights("6:5\n8 3\n9、4");
            Check(w2.Count == 2 && w2[6] == 5.0 && w2[8] == 3.0,
                "权重解析：冒号/空格都能切分；顿号分开的 9、4 因为没有等号被忽略");

            Dictionary<int, double> w3 = Config.ParseWeights("abc\n6=0\n7=-1\n8=x\n9=2,10=3");
            Check(w3.Count == 2 && w3.ContainsKey(9) && w3.ContainsKey(10),
                "权重解析：非法条目（文字/0/负数）被忽略，逗号分隔的多组生效");

            Check(Config.WeightOf(6, w1) == 5.0 && Config.WeightOf(7, w1) == 1.0 && Config.WeightOf(8, w1) == 1.0,
                "未配置权重的号码按 1 倍计算");
        }

        private static void TestSettingsFile()
        {
            string path = Settings.FilePath;
            bool existed = File.Exists(path);
            string backup = null;
            if (existed)
            {
                backup = File.ReadAllText(path, Encoding.UTF8);
            }

            try
            {
                Settings saved = new Settings();
                saved.Start = 3;
                saved.End = 52;
                saved.Count = 2;
                saved.NoRepeat = false;
                saved.Effect = true;
                saved.BottomText = "18,19";
                saved.WeightsText = "6=5\r\n7=2"; // 多行权重
                saved.Save();

                Check(File.Exists(path), "保存后生成了 settings.ini");

                Settings loaded = Settings.Load();
                Check(loaded.Start == 3 && loaded.End == 52 && loaded.Count == 2,
                    "数值项往返正确（Start/End/Count）");
                Check(loaded.NoRepeat == false && loaded.Effect == true, "开关项往返正确（NoRepeat/Effect）");
                Check(loaded.BottomText == "18,19", "垫底名单往返正确");

                HashSet<int> bottom = Config.ParseNumberSet(loaded.BottomText);
                Dictionary<int, double> weights = Config.ParseWeights(loaded.WeightsText);
                Check(bottom.Count == 2 && bottom.Contains(18) && bottom.Contains(19), "读回来的垫底号码可用");
                Check(weights.Count == 2 && weights[6] == 5.0 && weights[7] == 2.0,
                    "多行权重被压成一行后仍能解析出全部条目");
            }
            finally
            {
                if (existed && backup != null)
                {
                    File.WriteAllText(path, backup, new UTF8Encoding(true));
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
