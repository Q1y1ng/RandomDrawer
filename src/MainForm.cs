using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RandomDrawer
{
    /// <summary>
    /// 主窗口：填区间和个数，点"随机抽号"抽号。
    /// 界面坐标沿用原始程序（已验证与原程序逐像素一致）。
    /// </summary>
    internal sealed class MainForm : Form
    {
        /// <summary>允许的号码区间上限（防止填出天文数字把界面卡死）。</summary>
        private const int MaxRange = 100000;

        /// <summary>摇号特效的滚动次数（每 60 ms 一次）。</summary>
        private const int EffectTicks = 22;

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
        private Picker _effectPicker;
        private int _effectTake;
        private int _effectTicks;
        private bool _animating;
        private HashSet<int> _bottom = new HashSet<int>();
        private Dictionary<int, double> _weights = new Dictionary<int, double>();

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

            ApplySettings(Settings.Load());
        }

        /// <summary>把设置里的界面默认值套到控件上。</summary>
        private void ApplySettings(Settings settings)
        {
            _tbStart.Text = settings.Start.ToString();
            _tbEnd.Text = settings.End.ToString();
            _tbCount.Text = settings.Count.ToString();
            _chkNoRepeat.Checked = settings.NoRepeat;
            _chkEffect.Checked = settings.Effect;
            _lastStart = _tbStart.Text.Trim();
        }

        /// <summary>把界面上的当前值写回设置文件（垫底/权重不动）。</summary>
        private void SaveUiState()
        {
            string startText = _tbStart.Text.Trim();
            if (startText == Config.MAGIC_CODE)
            {
                return; // 输入框里是隐藏入口码，别当成号码存下来
            }
            int value;
            if (!int.TryParse(startText, out value))
            {
                return;
            }

            try
            {
                Settings settings = Settings.Load();
                settings.Start = value;
                if (int.TryParse(_tbEnd.Text.Trim(), out value))
                {
                    settings.End = value;
                }
                if (int.TryParse(_tbCount.Text.Trim(), out value) && value >= 1)
                {
                    settings.Count = value;
                }
                settings.NoRepeat = _chkNoRepeat.Checked;
                settings.Effect = _chkEffect.Checked;
                settings.Save();
            }
            catch (Exception)
            {
                // 存不进去就算了，不影响抽号
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveUiState();
            base.OnFormClosing(e);
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
                ApplySettings(Settings.Load());
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
            if ((long)end - (long)start + 1 > MaxRange)
            {
                MessageBox.Show(
                    "号码区间太大了，最多支持 " + MaxRange + " 个号码。",
                    "随机抽号器",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            SaveUiState();
            _lastStart = _tbStart.Text.Trim();

            Settings settings = Settings.Load();
            _bottom = Config.ParseNumberSet(settings.BottomText);
            _weights = Config.ParseWeights(settings.WeightsText);

            // 垫底号码也进池子，只是排到最后才轮得到
            List<int> candidates = new List<int>();
            for (int n = start; n <= end; n++)
            {
                candidates.Add(n);
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
            Picker picker = new Picker(_rnd, _bottom, _weights);
            List<int> picked = picker.Pick(pool, take);
            if (_chkEffect.Checked)
            {
                StartEffect(picked, pool, take, picker);
            }
            else
            {
                FinishDraw(picked);
            }
        }

        private void StartEffect(List<int> finalPick, List<int> pool, int take, Picker picker)
        {
            _finalPick = finalPick;
            _effectPool = pool;
            _effectPicker = picker;
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

            // 滚动数字也在"还剩的池子"里抽，不会闪出本轮已经抽过的号码
            List<int> picked = _effectPicker.Pick(_effectPool, _effectTake);
            List<string> parts = new List<string>();
            foreach (int num in picked)
            {
                parts.Add(num.ToString());
            }
            _lblRoll.Text = string.Join("  ", parts.ToArray());

            if (_effectTicks >= EffectTicks)
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
                }
                _totalDrawn++;
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
