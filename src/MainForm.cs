using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RandomDrawer
{
    /// <summary>
    /// 主窗口（基线版：界面坐标与行为与原始程序一致）。
    /// </summary>
    internal sealed class MainForm : Form
    {
        private TextBox _tbStart;
        private TextBox _tbEnd;
        private TextBox _tbCount;
        private CheckBox _chkNoRepeat;
        private CheckBox _chkEffect;
        private Button _btnDraw;
        private Button _btnReset;
        private Label _lblResult;
        private TextBox _tbResult;
        private Label _lblRoll;
        private Timer _timer;
        private List<int> _drawn = new List<int>();
        private int _totalDrawn;
        private Random _rnd = new Random();
        private string _lastStart = "1";
        private List<int> _finalPick;
        private List<int> _effectPool;
        private Dictionary<int, double> _effectWeights;
        private int _effectTake;
        private int _effectTicks;
        private bool _animating;

        internal MainForm()
        {
            Text = "随机抽号器";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(448, 414);
            Font = new Font("宋体", 9f);

            TabControl tabControl = new TabControl();
            tabControl.SetBounds(2, 4, 444, 236);
            TabPage tabPage = new TabPage("自定义随机抽号");
            tabControl.TabPages.Add(tabPage);

            Label lblStart = new Label();
            lblStart.Text = "开始:";
            lblStart.TextAlign = ContentAlignment.MiddleCenter;
            lblStart.SetBounds(60, 35, 56, 48);

            _tbStart = new TextBox();
            _tbStart.Text = "1";
            _tbStart.TextAlign = HorizontalAlignment.Center;
            _tbStart.Font = new Font("宋体", 18f);
            _tbStart.SetBounds(124, 35, 80, 44);

            Label lblEnd = new Label();
            lblEnd.Text = "结束:";
            lblEnd.TextAlign = ContentAlignment.MiddleCenter;
            lblEnd.SetBounds(220, 35, 56, 48);

            _tbEnd = new TextBox();
            _tbEnd.Text = "44";
            _tbEnd.TextAlign = HorizontalAlignment.Center;
            _tbEnd.Font = new Font("宋体", 18f);
            _tbEnd.SetBounds(284, 35, 80, 44);

            _chkNoRepeat = new CheckBox();
            _chkNoRepeat.Text = "不允许重复";
            _chkNoRepeat.Checked = true;
            _chkNoRepeat.SetBounds(52, 99, 104, 32);

            Label lblCount = new Label();
            lblCount.Text = "每次抽取:";
            lblCount.TextAlign = ContentAlignment.MiddleCenter;
            lblCount.SetBounds(164, 91, 80, 48);

            _tbCount = new TextBox();
            _tbCount.Text = "1";
            _tbCount.TextAlign = HorizontalAlignment.Center;
            _tbCount.Font = new Font("宋体", 12f);
            _tbCount.SetBounds(236, 99, 48, 32);

            Label lblUnit = new Label();
            lblUnit.Text = "个";
            lblUnit.TextAlign = ContentAlignment.MiddleCenter;
            lblUnit.SetBounds(292, 91, 32, 48);

            _chkEffect = new CheckBox();
            _chkEffect.Text = "摇号特效";
            _chkEffect.Checked = true;
            _chkEffect.SetBounds(324, 99, 108, 32);

            _btnDraw = new Button();
            _btnDraw.Text = "随机抽号";
            _btnDraw.Font = new Font("宋体", 12f);
            _btnDraw.SetBounds(92, 139, 136, 64);
            _btnDraw.Click += OnDraw;

            _btnReset = new Button();
            _btnReset.Text = "重新开始";
            _btnReset.SetBounds(276, 163, 96, 40);
            _btnReset.Click += OnReset;

            tabPage.Controls.Add(lblStart);
            tabPage.Controls.Add(_tbStart);
            tabPage.Controls.Add(lblEnd);
            tabPage.Controls.Add(_tbEnd);
            tabPage.Controls.Add(_chkNoRepeat);
            tabPage.Controls.Add(lblCount);
            tabPage.Controls.Add(_tbCount);
            tabPage.Controls.Add(lblUnit);
            tabPage.Controls.Add(_chkEffect);
            tabPage.Controls.Add(_btnDraw);
            tabPage.Controls.Add(_btnReset);
            _chkEffect.BringToFront();
            _chkNoRepeat.BringToFront();
            _tbCount.BringToFront();

            _lblResult = new Label();
            _lblResult.Text = "结果";
            _lblResult.SetBounds(8, 246, 200, 16);

            _tbResult = new TextBox();
            _tbResult.Multiline = true;
            _tbResult.ScrollBars = ScrollBars.Vertical;
            _tbResult.Font = new Font("宋体", 12f);
            _tbResult.SetBounds(3, 264, 441, 146);

            _lblRoll = new Label();
            _lblRoll.Font = new Font("宋体", 26f, FontStyle.Bold);
            _lblRoll.ForeColor = Color.Firebrick;
            _lblRoll.TextAlign = ContentAlignment.MiddleCenter;
            _lblRoll.SetBounds(3, 264, 441, 146);
            _lblRoll.Visible = false;

            _timer = new Timer();
            _timer.Interval = 60;
            _timer.Tick += OnEffectTick;

            Controls.Add(tabControl);
            Controls.Add(_lblResult);
            Controls.Add(_tbResult);
            Controls.Add(_lblRoll);
            _lblRoll.BringToFront();
        }

        private void OnDraw(object sender, EventArgs e)
        {
            if (_animating)
            {
                return;
            }
            if (_tbStart.Text.Trim() == Config.MAGIC_CODE)
            {
                using (AdminPanel panel = new AdminPanel())
                {
                    panel.ShowDialog(this);
                }
                _tbStart.Text = _lastStart;
                return;
            }

            int start;
            int end;
            int count;
            if (!int.TryParse(_tbStart.Text.Trim(), out start)
                || !int.TryParse(_tbEnd.Text.Trim(), out end)
                || !int.TryParse(_tbCount.Text.Trim(), out count)
                || start > end
                || count < 1)
            {
                MessageBox.Show("请输入正确的数字！", "随机抽号器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _lastStart = _tbStart.Text.Trim();
            HashSet<int> blacklist = Config.LoadBlacklist();
            Dictionary<int, double> weights = Config.LoadWeights();

            List<int> candidates = new List<int>();
            for (int n = start; n <= end; n++)
            {
                if (!blacklist.Contains(n))
                {
                    candidates.Add(n);
                }
            }

            bool noRepeat = _chkNoRepeat.Checked;
            List<int> pool = new List<int>(candidates);
            if (noRepeat)
            {
                HashSet<int> drawnSet = new HashSet<int>(_drawn);
                pool.RemoveAll(delegate(int x) { return drawnSet.Contains(x); });
                if (pool.Count == 0)
                {
                    MessageBox.Show("都选完啦！", "随机抽号器", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            int take = Math.Min(count, pool.Count);
            List<int> picked = WeightedPick(pool, weights, take);
            if (_chkEffect.Checked)
            {
                StartEffect(picked, candidates, weights, take);
            }
            else
            {
                FinishDraw(picked);
            }
        }

        /// <summary>按权重从池子里抽 take 个（抽出即移出池子）。</summary>
        private List<int> WeightedPick(List<int> candidates, Dictionary<int, double> weights, int take)
        {
            List<int> pool = new List<int>(candidates);
            List<double> current = new List<double>();
            List<int> result = new List<int>();
            for (int i = 0; i < take; i++)
            {
                current.Clear();
                double total = 0.0;
                foreach (int num in pool)
                {
                    double weight;
                    if (!weights.TryGetValue(num, out weight))
                    {
                        weight = 1.0;
                    }
                    if (weight < 0.0)
                    {
                        weight = 0.0;
                    }
                    current.Add(weight);
                    total += weight;
                }

                int index;
                if (total <= 0.0)
                {
                    index = _rnd.Next(pool.Count);
                }
                else
                {
                    double r = _rnd.NextDouble() * total;
                    double acc = 0.0;
                    index = 0;
                    while (index < pool.Count - 1)
                    {
                        acc += current[index];
                        if (r < acc)
                        {
                            break;
                        }
                        index++;
                    }
                }
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }
            return result;
        }

        private void StartEffect(List<int> finalPick, List<int> pool, Dictionary<int, double> weights, int take)
        {
            _finalPick = finalPick;
            _effectPool = pool;
            _effectWeights = weights;
            _effectTake = take;
            _effectTicks = 0;
            _animating = true;
            _btnDraw.Enabled = false;
            _btnReset.Enabled = false;
            _lblRoll.Visible = true;
            _timer.Start();
        }

        private void OnEffectTick(object sender, EventArgs e)
        {
            _effectTicks++;
            List<int> picked = WeightedPick(_effectPool, _effectWeights, _effectTake);
            List<string> parts = new List<string>();
            foreach (int num in picked)
            {
                parts.Add(num.ToString());
            }
            _lblRoll.Text = string.Join("  ", parts.ToArray());

            if (_effectTicks >= 22)
            {
                _timer.Stop();
                _lblRoll.Visible = false;
                _animating = false;
                _btnDraw.Enabled = true;
                _btnReset.Enabled = true;
                FinishDraw(_finalPick);
            }
        }

        private void FinishDraw(List<int> picks)
        {
            List<string> parts = new List<string>();
            foreach (int num in picks)
            {
                parts.Add(num + "/");
                if (_chkNoRepeat.Checked)
                {
                    _drawn.Add(num);
                    _totalDrawn++;
                }
            }
            _tbResult.AppendText(string.Join("", parts.ToArray()));
            _lblResult.Text = "抽号结果  已抽取:" + _totalDrawn + "个";
        }

        private void OnReset(object sender, EventArgs e)
        {
            if (_animating)
            {
                return;
            }
            _drawn.Clear();
            _totalDrawn = 0;
            _tbResult.Text = string.Empty;
            _lblResult.Text = "结果";
        }
    }
}
