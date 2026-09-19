using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RandomDrawer
{
    /// <summary>
    /// 隐藏设置面板：在"开始"框里输入 1016 再点"随机抽号"打开。
    /// 可以配置垫底号码、权重号码，查看中签概率预览，并设定界面默认值。
    /// </summary>
    internal sealed class AdminPanel : Form
    {
        /// <summary>概率预览最多列多少个普通号码（防止几万个号码把面板拖死）。</summary>
        private const int MaxPreviewRows = 200;

        private TextBox _txtBottom;
        private TextBox _txtWeights;
        private TextBox _txtPreview;
        private TextBox _txtStart;
        private TextBox _txtEnd;
        private TextBox _txtCount;
        private CheckBox _chkNoRepeat;
        private CheckBox _chkEffect;

        internal AdminPanel()
        {
            Text = "设置";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 600);
            Font = new Font("宋体", 9f);

            Label lblBottom = new Label();
            lblBottom.Text = "垫底号码（这些号码最后才会被抽中，逗号/空格分隔）：";
            lblBottom.SetBounds(12, 10, 536, 16);

            _txtBottom = new TextBox();
            _txtBottom.Multiline = true;
            _txtBottom.ScrollBars = ScrollBars.Vertical;
            _txtBottom.SetBounds(12, 30, 536, 70);

            Label lblWeights = new Label();
            lblWeights.Text = "权重号码（号码=倍数，每行一个或用逗号分隔；如 6=5 表示 6 号以 5 倍概率抽出）：";
            lblWeights.SetBounds(12, 108, 536, 16);

            _txtWeights = new TextBox();
            _txtWeights.Multiline = true;
            _txtWeights.ScrollBars = ScrollBars.Vertical;
            _txtWeights.SetBounds(12, 128, 536, 70);

            Label lblPreview = new Label();
            lblPreview.Text = "中签概率预览（按单次抽取计算，用来检查权重是否合理）：";
            lblPreview.SetBounds(12, 206, 536, 16);

            _txtPreview = new TextBox();
            _txtPreview.Multiline = true;
            _txtPreview.ReadOnly = true;
            _txtPreview.WordWrap = false;
            _txtPreview.ScrollBars = ScrollBars.Both;
            _txtPreview.BackColor = SystemColors.Window;
            _txtPreview.Font = new Font("Consolas", 9f);
            _txtPreview.SetBounds(12, 226, 536, 190);

            GroupBox grpDefaults = new GroupBox();
            grpDefaults.Text = "界面默认值（下次打开程序时的初始值）";
            grpDefaults.SetBounds(12, 424, 536, 124);

            Label lblStart = new Label();
            lblStart.Text = "开始:";
            lblStart.TextAlign = ContentAlignment.MiddleRight;
            lblStart.SetBounds(16, 28, 44, 24);

            _txtStart = new TextBox();
            _txtStart.TextAlign = HorizontalAlignment.Center;
            _txtStart.SetBounds(64, 26, 70, 26);

            Label lblEnd = new Label();
            lblEnd.Text = "结束:";
            lblEnd.TextAlign = ContentAlignment.MiddleRight;
            lblEnd.SetBounds(146, 28, 44, 24);

            _txtEnd = new TextBox();
            _txtEnd.TextAlign = HorizontalAlignment.Center;
            _txtEnd.SetBounds(194, 26, 70, 26);

            Label lblCount = new Label();
            lblCount.Text = "每次抽取:";
            lblCount.TextAlign = ContentAlignment.MiddleRight;
            lblCount.SetBounds(272, 28, 60, 24);

            _txtCount = new TextBox();
            _txtCount.TextAlign = HorizontalAlignment.Center;
            _txtCount.SetBounds(336, 26, 56, 26);

            Label lblUnit = new Label();
            lblUnit.Text = "个";
            lblUnit.TextAlign = ContentAlignment.MiddleLeft;
            lblUnit.SetBounds(396, 28, 28, 24);

            _chkNoRepeat = new CheckBox();
            _chkNoRepeat.Text = "不允许重复";
            _chkNoRepeat.SetBounds(16, 66, 110, 26);

            _chkEffect = new CheckBox();
            _chkEffect.Text = "摇号特效";
            _chkEffect.SetBounds(146, 66, 110, 26);

            grpDefaults.Controls.Add(lblStart);
            grpDefaults.Controls.Add(_txtStart);
            grpDefaults.Controls.Add(lblEnd);
            grpDefaults.Controls.Add(_txtEnd);
            grpDefaults.Controls.Add(lblCount);
            grpDefaults.Controls.Add(_txtCount);
            grpDefaults.Controls.Add(lblUnit);
            grpDefaults.Controls.Add(_chkNoRepeat);
            grpDefaults.Controls.Add(_chkEffect);

            Button btnSave = new Button();
            btnSave.Text = "保存";
            btnSave.SetBounds(300, 556, 76, 30);
            btnSave.Click += OnSave;

            Button btnRestore = new Button();
            btnRestore.Text = "恢复默认";
            btnRestore.SetBounds(384, 556, 88, 30);
            btnRestore.Click += OnRestoreDefaults;

            Button btnClose = new Button();
            btnClose.Text = "关闭";
            btnClose.SetBounds(480, 556, 68, 30);
            btnClose.Click += OnClose;

            Controls.Add(lblBottom);
            Controls.Add(_txtBottom);
            Controls.Add(lblWeights);
            Controls.Add(_txtWeights);
            Controls.Add(lblPreview);
            Controls.Add(_txtPreview);
            Controls.Add(grpDefaults);
            Controls.Add(btnSave);
            Controls.Add(btnRestore);
            Controls.Add(btnClose);

            _txtBottom.TextChanged += OnInputChanged;
            _txtWeights.TextChanged += OnInputChanged;
            _txtStart.TextChanged += OnInputChanged;
            _txtEnd.TextChanged += OnInputChanged;

            LoadCurrentSettings();
        }

        /// <summary>把当前设置填进各个控件。</summary>
        private void LoadCurrentSettings()
        {
            Settings settings = Settings.Load();

            List<int> bottom = Config.ParseNumbers(settings.BottomText);
            HashSet<int> unique = new HashSet<int>();
            List<string> bottomTexts = new List<string>();
            for (int i = 0; i < bottom.Count; i++)
            {
                if (unique.Add(bottom[i]))
                {
                    bottomTexts.Add(bottom[i].ToString());
                }
            }
            _txtBottom.Text = string.Join(",", bottomTexts.ToArray());

            Dictionary<int, double> weights = Config.ParseWeights(settings.WeightsText);
            List<string> lines = new List<string>();
            foreach (KeyValuePair<int, double> kv in weights)
            {
                lines.Add(kv.Key + "=" + kv.Value);
            }
            _txtWeights.Text = string.Join("\r\n", lines.ToArray());

            _txtStart.Text = settings.Start.ToString();
            _txtEnd.Text = settings.End.ToString();
            _txtCount.Text = settings.Count.ToString();
            _chkNoRepeat.Checked = settings.NoRepeat;
            _chkEffect.Checked = settings.Effect;

            RefreshPreview();
        }

        private void OnInputChanged(object sender, EventArgs e)
        {
            RefreshPreview();
        }

        /// <summary>按单次抽取算出每个号码的中签概率（垫底号码记 0）。</summary>
        private void RefreshPreview()
        {
            int start;
            int end;
            if (!int.TryParse(_txtStart.Text.Trim(), out start)
                || !int.TryParse(_txtEnd.Text.Trim(), out end)
                || start > end)
            {
                _txtPreview.Text = "（“开始 / 结束”不是有效区间，无法预览）";
                return;
            }

            long span = (long)end - (long)start + 1;
            HashSet<int> bottom = Config.ParseNumberSet(_txtBottom.Text);
            Dictionary<int, double> weights = Config.ParseWeights(_txtWeights.Text);

            List<int> normals = new List<int>();
            List<int> bottomNumbers = new List<int>();
            double normalTotal = 0.0;
            int weightedCount = 0;
            for (int n = start; n <= end; n++)
            {
                if (bottom.Contains(n))
                {
                    bottomNumbers.Add(n);
                    continue;
                }
                normals.Add(n);
                normalTotal += Config.WeightOf(n, weights);
                if (Math.Abs(Config.WeightOf(n, weights) - 1.0) > 0.000001)
                {
                    weightedCount++;
                }
            }

            normals.Sort(delegate(int a, int b)
            {
                int c = Config.WeightOf(b, weights).CompareTo(Config.WeightOf(a, weights));
                return c != 0 ? c : a.CompareTo(b);
            });
            bottomNumbers.Sort();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("号码区间 " + start + " ~ " + end + "：共 " + span + " 个号码");
            sb.AppendLine("其中普通 " + normals.Count + " 个（有权重加成 " + weightedCount + " 个），垫底 " + bottomNumbers.Count + " 个");
            sb.AppendLine("百分比 = 单次抽取被抽中的概率；垫底号码记 0（普通号码抽完才轮到）");
            sb.AppendLine();
            sb.AppendLine("    号码        权重       单次概率");

            int shown = 0;
            for (int i = 0; i < normals.Count; i++)
            {
                if (shown >= MaxPreviewRows)
                {
                    break;
                }
                int n = normals[i];
                double weight = Config.WeightOf(n, weights);
                double probability = normalTotal > 0.0 ? weight / normalTotal : 0.0;
                sb.AppendLine("  " + n.ToString().PadLeft(6)
                    + "  " + ("×" + weight.ToString("0.###")).PadLeft(10)
                    + "  " + ((probability * 100.0).ToString("0.00") + "%").PadLeft(10));
                shown++;
            }
            if (normals.Count > shown)
            {
                sb.AppendLine("  …（其余 " + (normals.Count - shown) + " 个同概率，省略）");
            }

            for (int i = 0; i < bottomNumbers.Count; i++)
            {
                sb.AppendLine("  " + bottomNumbers[i].ToString().PadLeft(6)
                    + "  " + "垫底".PadLeft(10)
                    + "  " + "0.00%".PadLeft(10) + "   ← 最后才抽");
            }

            _txtPreview.Text = sb.ToString();
        }

        /// <summary>把面板上的内容写进设置文件。</summary>
        private void SaveSettings()
        {
            Settings settings = Settings.Load();
            settings.BottomText = _txtBottom.Text;
            settings.WeightsText = _txtWeights.Text;

            int value;
            if (int.TryParse(_txtStart.Text.Trim(), out value))
            {
                settings.Start = value;
            }
            if (int.TryParse(_txtEnd.Text.Trim(), out value))
            {
                settings.End = value;
            }
            if (int.TryParse(_txtCount.Text.Trim(), out value) && value >= 1)
            {
                settings.Count = value;
            }
            settings.NoRepeat = _chkNoRepeat.Checked;
            settings.Effect = _chkEffect.Checked;
            settings.Save();
        }

        private void OnSave(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("已保存。", "设置", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        private void OnRestoreDefaults(object sender, EventArgs e)
        {
            DialogResult answer = MessageBox.Show(
                "将清空垫底号码与权重号码，并把界面默认值恢复为 1 / 44 / 1、勾选两项。\r\n确定要恢复默认吗？",
                "恢复默认",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
            {
                return;
            }

            _txtBottom.Text = string.Empty;
            _txtWeights.Text = string.Empty;
            _txtStart.Text = "1";
            _txtEnd.Text = "44";
            _txtCount.Text = "1";
            _chkNoRepeat.Checked = true;
            _chkEffect.Checked = true;
            RefreshPreview();
            SaveSettings();
            MessageBox.Show("已恢复默认并保存。", "设置", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnClose(object sender, EventArgs e)
        {
            Close();
        }
    }
}
