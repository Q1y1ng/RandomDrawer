using System;
using System.Collections.Generic;

namespace RandomDrawer
{
    /// <summary>
    /// 抽号算法：分层 + 权重。
    /// 每次抽号优先在"非垫底"号码里抽；只有当非垫底号码都抽完（或都不在池子里）时，才轮到垫底号码。
    /// 同一层内按权重轮盘抽取。
    /// </summary>
    internal sealed class Picker
    {
        private readonly Random _rnd;
        private readonly HashSet<int> _bottom;
        private readonly Dictionary<int, double> _weights;

        internal Picker(Random rnd, HashSet<int> bottom, Dictionary<int, double> weights)
        {
            _rnd = rnd;
            _bottom = bottom ?? new HashSet<int>();
            _weights = weights ?? new Dictionary<int, double>();
        }

        /// <summary>从 pool 里抽 take 个（抽出即移出池子，所以不会重复）。</summary>
        internal List<int> Pick(List<int> pool, int take)
        {
            List<int> remaining = new List<int>(pool);
            List<int> normal = new List<int>();
            List<int> result = new List<int>();
            for (int i = 0; i < take && remaining.Count > 0; i++)
            {
                normal.Clear();
                for (int j = 0; j < remaining.Count; j++)
                {
                    if (!_bottom.Contains(remaining[j]))
                    {
                        normal.Add(remaining[j]);
                    }
                }
                List<int> source = normal.Count > 0 ? normal : remaining;
                int index = PickOneIndex(source);
                int picked = source[index];
                result.Add(picked);
                remaining.Remove(picked);
            }
            return result;
        }

        /// <summary>在 candidates 里按权重选一个，返回下标。</summary>
        internal int PickOneIndex(List<int> candidates)
        {
            double total = 0.0;
            for (int i = 0; i < candidates.Count; i++)
            {
                total += Config.WeightOf(candidates[i], _weights);
            }
            if (total <= 0.0)
            {
                return _rnd.Next(candidates.Count);
            }

            double r = _rnd.NextDouble() * total;
            double acc = 0.0;
            for (int i = 0; i < candidates.Count - 1; i++)
            {
                acc += Config.WeightOf(candidates[i], _weights);
                if (r < acc)
                {
                    return i;
                }
            }
            return candidates.Count - 1;
        }
    }
}
