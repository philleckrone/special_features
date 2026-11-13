using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SpecialFeatures
{
    /// <summary>Chooses the kinds of places to list: one tick box per kind, with All and None.</summary>
    public sealed class KindChooserForm : Form
    {
        private readonly IList<Category> kinds;
        private readonly Strings s;
        private readonly CheckedListBox list = new CheckedListBox();

        public KindChooserForm(ICollection<string> excluded, Strings strings)
        {
            kinds = Categories.All.ToList();
            s = strings;
            Text = s.KindsTitle;
            Name = "KindChooserForm";
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 9f);
            ClientSize = new Size(440, 480);
            MinimumSize = new Size(380, 360);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;

            var table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.Padding = new Padding(10);
            table.ColumnCount = 2;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.RowCount = 3;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var intro = new Label();
            intro.AutoSize = true;
            intro.MaximumSize = new Size(410, 0);
            intro.Text = s.KindsIntro;
            intro.Margin = new Padding(3, 0, 3, 6);
            table.Controls.Add(intro, 0, 0);
            table.SetColumnSpan(intro, 2);

            list.Name = "Kinds";
            list.Dock = DockStyle.Fill;
            list.CheckOnClick = true;
            list.IntegralHeight = false;
            foreach (Category c in kinds)
                list.Items.Add(c.Name(s.Language), !excluded.Contains(c.Key));
            table.Controls.Add(list, 0, 1);

            var quick = new FlowLayoutPanel();
            quick.FlowDirection = FlowDirection.TopDown;
            quick.AutoSize = true;
            quick.WrapContents = false;
            quick.Margin = new Padding(8, 0, 0, 0);
            quick.Controls.Add(QuickButton(s.ChooseAll, true));
            quick.Controls.Add(QuickButton(s.ChooseNone, false));
            table.Controls.Add(quick, 1, 1);

            var buttons = new FlowLayoutPanel();
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.AutoSize = true;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(0, 8, 0, 0);
            Button cancel = MakeButton(s.Cancel);
            cancel.DialogResult = DialogResult.Cancel;
            Button ok = MakeButton(s.Ok);
            ok.Click += delegate
            {
                if (list.CheckedItems.Count == 0)
                {
                    MessageBox.Show(this, s.KindsNone, s.KindsTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            table.Controls.Add(buttons, 0, 2);
            table.SetColumnSpan(buttons, 2);
            AcceptButton = ok;
            CancelButton = cancel;
            Controls.Add(table);
        }

        /// <summary>The keys of the kinds left unticked.</summary>
        public IList<string> ExcludedKinds
        {
            get
            {
                var excluded = new List<string>();
                for (int i = 0; i < kinds.Count; i++)
                    if (!list.GetItemChecked(i)) excluded.Add(kinds[i].Key);
                return excluded;
            }
        }

        private Button QuickButton(string text, bool tick)
        {
            Button b = MakeButton(text);
            b.Margin = new Padding(0, 0, 0, 6);
            b.Dock = DockStyle.Fill;
            b.Click += delegate
            {
                for (int i = 0; i < kinds.Count; i++) list.SetItemChecked(i, tick);
            };
            return b;
        }

        private static Button MakeButton(string text)
        {
            var b = new Button();
            b.Text = text;
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.Padding = new Padding(8, 2, 8, 2);
            b.Margin = new Padding(6, 0, 0, 0);
            return b;
        }
    }
}
