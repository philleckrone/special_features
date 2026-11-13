using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SpecialFeatures
{
    /// <summary>
    /// The places the user added (AddedPlaces): add, change, delete, and share them as a file.
    /// Every change is saved at once and the open lists are built again.
    /// </summary>
    public sealed class AddedPlacesForm : Form
    {
        private readonly Strings s;
        private readonly int defaultBook;
        private readonly DataGridView grid = new DataGridView();
        private readonly Button btnAdd = new Button();
        private readonly Button btnEdit = new Button();
        private readonly Button btnDelete = new Button();
        private readonly Button btnImport = new Button();
        private readonly Button btnExport = new Button();
        private readonly Button btnClose = new Button();

        public AddedPlacesForm(Strings strings, int defaultBook)
        {
            s = strings;
            this.defaultBook = defaultBook;
            Text = s.AddedTitle;
            Name = "AddedPlacesForm";
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 9f);
            ClientSize = new Size(900, 480);
            MinimumSize = new Size(600, 350);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;

            var table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.Padding = new Padding(10);
            table.ColumnCount = 1;
            table.RowCount = 3;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var intro = new Label();
            intro.AutoSize = true;
            intro.MaximumSize = new Size(860, 0);
            intro.Margin = new Padding(3, 0, 3, 8);
            intro.Text = string.Format(s.AddedIntro, AddedPlaces.FilePath);

            grid.Name = "Added";
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.MultiSelect = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.BackgroundColor = SystemColors.Window;
            AddColumn("Book", s.ColBook, 60);
            AddColumn("References", s.ColReference, 150);
            int kindWidth = 170;
            if (s.Language == Lang.French)   // the French kinds are longer
                foreach (Category c in Categories.All)
                    kindWidth = Math.Max(kindWidth, TextRenderer.MeasureText(c.Name(s.Language), Font).Width + 14);
            AddColumn("Kind", s.ColCategory, kindWidth);
            AddColumn("Description", s.ColDescription, 300);
            AddColumn("Source", s.ColSource, 120);
            grid.Columns["Description"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0) EditSelected(); };

            var buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(0, 8, 0, 0);
            Setup(btnAdd, s.AddButton, "Add", delegate { AddPlace(); });
            Setup(btnEdit, s.EditButton, "Edit", delegate { EditSelected(); });
            Setup(btnDelete, s.DeleteButton, "Delete", delegate { DeleteSelected(); });
            Setup(btnImport, s.ImportButton, "Import", delegate { ImportFile(); });
            Setup(btnExport, s.ExportButton, "Export", delegate { ExportFile(); });
            Setup(btnClose, s.Close, "Close", delegate { Close(); });
            buttons.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnImport, btnExport, btnClose });
            CancelButton = btnClose;

            table.Controls.Add(intro, 0, 0);
            table.Controls.Add(grid, 0, 1);
            table.Controls.Add(buttons, 0, 2);
            Controls.Add(table);
            Fill();
        }

        private void AddColumn(string name, string header, int width)
        {
            var col = new DataGridViewTextBoxColumn();
            col.Name = name;
            col.HeaderText = header;
            col.Width = width;
            grid.Columns.Add(col);
        }

        private static void Setup(Button b, string text, string name, EventHandler click)
        {
            b.Text = text;
            b.Name = name;
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.Padding = new Padding(8, 2, 8, 2);
            b.Margin = new Padding(0, 0, 8, 0);
            b.Click += click;
        }

        private void Fill()
        {
            grid.Rows.Clear();
            foreach (CatalogEntry e in AddedPlaces.Entries)
                grid.Rows.Add(Books.Code(e.BookNum), e.References, e.Category.Name(s.Language),
                    e.Description(s.Language), string.Join(", ", e.Publications));
            bool any = AddedPlaces.Entries.Count > 0;
            btnEdit.Enabled = btnDelete.Enabled = btnExport.Enabled = any;
        }

        private int SelectedIndex
        {
            get { return grid.CurrentRow != null ? grid.CurrentRow.Index : -1; }
        }

        private void Select(int index)
        {
            if (index >= 0 && index < grid.Rows.Count) grid.CurrentCell = grid.Rows[index].Cells[0];
        }

        private void AddPlace()
        {
            int book = defaultBook;
            string source = AddedPlaces.DefaultSource;
            if (AddedPlaces.Entries.Count > 0)
            {
                // A new place usually comes from the same source as the last one.
                CatalogEntry last = AddedPlaces.Entries[AddedPlaces.Entries.Count - 1];
                source = string.Join(",", last.Publications);
                if (book == 0) book = last.BookNum;
            }
            using (var edit = new PlaceEditForm(s, null, book, source))
            {
                if (edit.ShowDialog(this) != DialogResult.OK) return;
                AddedPlaces.Entries.Add(edit.Result);
            }
            Save();
            Select(AddedPlaces.Entries.Count - 1);
        }

        private void EditSelected()
        {
            int i = SelectedIndex;
            if (i < 0 || i >= AddedPlaces.Entries.Count) return;
            using (var edit = new PlaceEditForm(s, AddedPlaces.Entries[i], 0, null))
            {
                if (edit.ShowDialog(this) != DialogResult.OK) return;
                AddedPlaces.Entries[i] = edit.Result;
            }
            Save();
            Select(i);
        }

        private void DeleteSelected()
        {
            int i = SelectedIndex;
            if (i < 0 || i >= AddedPlaces.Entries.Count) return;
            CatalogEntry e = AddedPlaces.Entries[i];
            string what = Books.Code(e.BookNum) + " " + e.References + " (" + e.Category.Name(s.Language) + ")";
            if (MessageBox.Show(this, string.Format(s.ConfirmDelete, what), s.AddedTitle,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            AddedPlaces.Entries.RemoveAt(i);
            Save();
            Select(Math.Min(i, AddedPlaces.Entries.Count - 1));
        }

        private void Save()
        {
            try
            {
                AddedPlaces.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format(s.Error, ex.Message), s.AddedTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            Fill();
        }

        private void ImportFile()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = s.FileFilter;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    int[] counts = AddedPlaces.Import(dlg.FileName);
                    Fill();
                    MessageBox.Show(this, string.Format(s.ImportDone, counts[0], counts[1]), s.AddedTitle,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, string.Format(s.Error, ex.Message), s.AddedTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void ExportFile()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = s.FileFilter;
                dlg.FileName = "SpecialFeatures_added.tsv";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    AddedPlaces.Write(dlg.FileName, AddedPlaces.Entries);
                    MessageBox.Show(this, string.Format(s.ExportDone, AddedPlaces.Entries.Count, dlg.FileName), s.AddedTitle,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, string.Format(s.Error, ex.Message), s.AddedTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }

    /// <summary>Adds or changes one added place.</summary>
    public sealed class PlaceEditForm : Form
    {
        private readonly Strings s;
        private readonly ComboBox cboBook = new ComboBox();
        private readonly TextBox txtRefs = new TextBox();
        private readonly ComboBox cboKind = new ComboBox();
        private readonly TextBox txtDescription = new TextBox();
        private readonly TextBox txtSource = new TextBox();
        private readonly ComboBox cboVrs = new ComboBox();
        private readonly Label lblSuggested = new Label();

        /// <summary>The place as typed (after OK).</summary>
        public CatalogEntry Result { get; private set; }

        public PlaceEditForm(Strings strings, CatalogEntry entry, int defaultBook, string defaultSource)
        {
            s = strings;
            Text = s.EditTitle;
            Name = "PlaceEditForm";
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var table = new TableLayoutPanel();
            table.AutoSize = true;
            table.Padding = new Padding(10);
            table.ColumnCount = 2;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420f));

            cboBook.DropDownStyle = ComboBoxStyle.DropDownList;
            cboBook.Width = 120;
            for (int b = 1; b <= 66; b++) cboBook.Items.Add(Books.Code(b));
            cboKind.DropDownStyle = ComboBoxStyle.DropDownList;
            cboKind.Width = 300;
            foreach (Category c in Categories.All) cboKind.Items.Add(c.Name(s.Language));
            cboVrs.DropDownStyle = ComboBoxStyle.DropDownList;
            cboVrs.Width = 250;
            cboVrs.Items.Add(s.VrsEnglish);
            cboVrs.Items.Add(s.VrsOriginal);
            txtRefs.Width = txtDescription.Width = 410;
            txtSource.Width = 250;
            lblSuggested.AutoSize = true;
            lblSuggested.ForeColor = SystemColors.GrayText;
            cboKind.SelectedIndexChanged += delegate
            {
                Category c = cboKind.SelectedIndex >= 0 ? Categories.All[cboKind.SelectedIndex] : null;
                lblSuggested.Text = c != null ? string.Format(s.TipSuggested, c.Suggested) : "";
            };

            int row = 0;
            AddRow(table, ref row, s.LabelBook, cboBook, null);
            AddRow(table, ref row, s.LabelReferences, txtRefs, s.ReferencesHelp);
            AddRow(table, ref row, s.LabelKind, cboKind, null);
            table.Controls.Add(lblSuggested, 1, row++);
            AddRow(table, ref row, s.LabelDescription, txtDescription, null);
            AddRow(table, ref row, s.LabelSource, txtSource, s.SourceHelp);
            AddRow(table, ref row, s.LabelVersification, cboVrs, null);

            var buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(0, 10, 0, 0);
            var ok = new Button();
            ok.Text = s.Ok;
            ok.Name = "Ok";
            ok.AutoSize = true;
            ok.Click += delegate { Accept(); };
            var cancel = new Button();
            cancel.Text = s.Cancel;
            cancel.Name = "Cancel";
            cancel.AutoSize = true;
            cancel.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            table.Controls.Add(buttons, 0, row);
            table.SetColumnSpan(buttons, 2);
            AcceptButton = ok;
            CancelButton = cancel;
            Controls.Add(table);

            if (entry != null)
            {
                cboBook.SelectedIndex = entry.BookNum - 1;
                txtRefs.Text = entry.References;
                cboKind.SelectedIndex = Categories.IndexOf(entry.Category);
                txtDescription.Text = entry.Description(s.Language);
                txtSource.Text = string.Join(",", entry.Publications);
                cboVrs.SelectedIndex = entry.Versification == "org" ? 1 : 0;
            }
            else
            {
                cboBook.SelectedIndex = defaultBook >= 1 && defaultBook <= 66 ? defaultBook - 1 : 0;
                cboKind.SelectedIndex = Categories.IndexOf(Categories.Get("list"));
                txtSource.Text = defaultSource ?? AddedPlaces.DefaultSource;
                cboVrs.SelectedIndex = 0;
            }
        }

        private static void AddRow(TableLayoutPanel table, ref int row, string label, Control control, string help)
        {
            var l = new Label();
            l.Text = label;
            l.AutoSize = true;
            l.Margin = new Padding(3, 6, 8, 3);
            table.Controls.Add(l, 0, row);
            table.Controls.Add(control, 1, row++);
            if (help != null)
            {
                var h = new Label();
                h.Text = help;
                h.AutoSize = true;
                h.MaximumSize = new Size(410, 0);
                h.ForeColor = SystemColors.GrayText;
                h.Margin = new Padding(3, 0, 3, 6);
                table.Controls.Add(h, 1, row++);
            }
        }

        private void Accept()
        {
            string refs = txtRefs.Text.Trim();
            try
            {
                FeatureCatalog.ParseReferences(refs);
            }
            catch (FormatException ex)
            {
                MessageBox.Show(this, string.Format(s.BadReferences, ex.Message), s.EditTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRefs.Focus();
                return;
            }
            Category kind = Categories.All[Math.Max(0, cboKind.SelectedIndex)];
            string description = txtDescription.Text.Trim();
            if (description.Length == 0) description = kind.Name(s.Language);
            string[] sources = txtSource.Text.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
            if (sources.Length == 0) sources = new[] { AddedPlaces.DefaultSource };

            string line = string.Join("\t", new[]
            {
                kind.Key, Books.Code(cboBook.SelectedIndex + 1), refs, string.Join(",", sources),
                description, description, cboVrs.SelectedIndex == 1 ? "org" : "eng", description,
            });
            CatalogEntry parsed = FeatureCatalog.Parse(line.Replace('\r', ' ').Replace('\n', ' '))[0];
            parsed.Added = true;
            Result = parsed;
            DialogResult = DialogResult.OK;
        }
    }
}
