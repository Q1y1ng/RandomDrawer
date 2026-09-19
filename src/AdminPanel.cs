using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RandomDrawer
{
    /// <summary>
    /// 隐藏设置面板（基线版：由"开始"框输入 1016 打开）。
    /// </summary>
    internal sealed class AdminPanel : Form
    {
        private TextBox _txtBlacklist;
        private TextBox _txtWeights;

        internal AdminPanel()
        {
            Text = "设置";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 330);
            Font = new Font("宋体", 9f);

            Label lblBlacklist = new Label();
            lblBlacklist.Text = "屏蔽号码（这些号码永远不会被抽中），逗号/空格分隔：";
            lblBlacklist.SetBounds(12, 10, 400, 16);

            _txtBlacklist = new TextBox();
            _txtBlacklist.Multiline = true;
            _txtBlacklist.ScrollBars = ScrollBars.Vertical;
            _txtBlacklist.SetBounds(12, 30, 396, 80);

            Label lblWeights = new Label();
            lblWeights.Text = "权重号码（格式：号码=倍数，每行一个，如 18=5 表示以5倍概率抽出）：";
            lblWeights.SetBounds(12, 120, 400, 16);

            _txtWeights = new TextBox();
            _txtWeights.Multiline = true;
            _txtWeights.ScrollBars = ScrollBars.Vertical;
            _txtWeights.SetBounds(12, 140, 396, 110);

            Button btnSave = new Button();
            btnSave.Text = "保存";
            btnSave.SetBounds(230, 285, 80, 28);
            btnSave.Click += OnSave;

            Button btnClose = new Button();
            btnClose.Text = "关闭";
            btnClose.SetBounds(328, 285, 80, 28);
            btnClose.Click += OnClose;

            Controls.Add(lblBlacklist);
            Controls.Add(_txtBlacklist);
            Controls.Add(lblWeights);
            Controls.Add(_txtWeights);
            Controls.Add(btnSave);
            Controls.Add(btnClose);

            List<int> blacklist = Config.ParseNumbers(Config.ReadReg("Blacklist"));
            List<string> texts = new List<string>();
            foreach (int num in blacklist)
            {
                texts.Add(num.ToString());
            }
            _txtBlacklist.Text = string.Join(",", texts.ToArray());

            Dictionary<int, double> weights = Config.LoadWeights();
            List<string> lines = new List<string>();
            foreach (KeyValuePair<int, double> kv in weights)
            {
                lines.Add(kv.Key + "=" + kv.Value);
            }
            _txtWeights.Text = string.Join("\n", lines.ToArray());
        }

        private void OnSave(object sender, EventArgs e)
        {
            Config.Save(_txtBlacklist.Text, _txtWeights.Text);
            MessageBox.Show("已保存。", "设置", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        private void OnClose(object sender, EventArgs e)
        {
            Close();
        }
    }
}
