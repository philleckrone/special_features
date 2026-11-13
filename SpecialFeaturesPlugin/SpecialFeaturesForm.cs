using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SpecialFeatures
{
    /// <summary>
    /// Lists the places that may need special formatting, with their state in the project's text.
    /// It never changes the text.
    /// </summary>
    public sealed class SpecialFeaturesForm : Form
    {
        public static readonly Color PlainBack = Color.FromArgb(0xFF, 0xF2, 0xC6);
        public static readonly Color FormattedBack = Color.FromArgb(0xDD, 0xF2, 0xDD);
        public static readonly Color QuoteMarksBack = Color.FromArgb(0xDD, 0xEB, 0xF7);

        private readonly FeatureSession session;
        private readonly Strings s;
        private List<Feature> shown = new List<Feature>();
        /// <summary>True while the controls are being set (their events change nothing then).</summary>
        private bool filling;

        private readonly Label lblProject = new Label();
        private readonly Label lblIntro = new Label();
        private readonly ComboBox cboBooks = new ComboBox();
        private readonly Button btnKinds = new Button();
        private readonly CheckBox chkHideFormatted = new CheckBox();
        private readonly CheckBox chkShowHidden = new CheckBox();
        private readonly List<CheckBox> chkPublications = new List<CheckBox>();
        private readonly Label lblSummary = new Label();
        private readonly DataGridView grid = new DataGridView();
        private readonly Button btnGoTo = new Button();
        private readonly Button btnHide = new Button();
        private readonly Button btnUnhide = new Button();
        private readonly Button btnRescan = new Button();
        private readonly Button btnAdded = new Button();
        private readonly FlowLayoutPanel pubs = NewFlow();
        private readonly Button btnClose = new Button();
        private readonly ToolTip tips = new ToolTip();

        public SpecialFeaturesForm(FeatureSession session, Strings strings)
        {
            this.session = session;
            s = strings;
            BuildLayout();
            Shown += delegate
            {
                if (AddedPlaces.LoadError != null)
                    MessageBox.Show(this, string.Format(s.LoadErrorText, AddedPlaces.FilePath, AddedPlaces.LoadError),
                        s.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // Places may have been added in another project's window since this project's were built.
                if (session.PlacesOutOfDate) PlacesChanged();
                else Fill();
            };
            EventHandler changed = delegate
            {
                // The text changed in Paratext, or the filters changed.
                if (IsHandleCreated && !IsDisposed) BeginInvoke(new MethodInvoker(Fill));
            };
            EventHandler added = delegate
            {
                if (IsHandleCreated && !IsDisposed) BeginInvoke(new MethodInvoker(PlacesChanged));
            };
            session.Changed += changed;
            AddedPlaces.Changed += added;
            FormClosed += delegate
            {
                session.Changed -= changed;
                AddedPlaces.Changed -= added;
            };
        }

        /// <summary>The added places changed: new places, and maybe new sources to filter by.</summary>
        private void PlacesChanged()
        {
            if (IsDisposed) return;
            BuildPublicationBoxes();
            session.ReloadPlaces();   // raises Changed, which fills the list
            FillFilterControls();
        }

        private void ShowAddedPlaces()
        {
            int book = 0;
            if (grid.CurrentRow != null && grid.CurrentRow.Index < shown.Count) book = shown[grid.CurrentRow.Index].BookNum;
            using (var dlg = new AddedPlacesForm(s, book))
                dlg.ShowDialog(this);
        }

        /// <summary>One tick box per publication and per source of the added places.</summary>
        private void BuildPublicationBoxes()
        {
            foreach (CheckBox c in chkPublications)
            {
                pubs.Controls.Remove(c);
                c.Dispose();
            }
            chkPublications.Clear();
            foreach (string p in session.AllPublications)
            {
                var c = new CheckBox();
                c.Name = "Pub" + p;
                c.Text = s.PublicationName(p);
                c.Tag = p;
                c.AutoSize = true;
                c.Margin = new Padding(3, 4, 6, 3);
                c.Checked = !session.Options.ExcludedPublications.Contains(p);
                tips.SetToolTip(c, s.PublicationsHint);
                c.CheckedChanged += delegate { FilterChanged(); };
                chkPublications.Add(c);
                pubs.Controls.Add(c);
            }
        }

        public IList<Feature> ShownFeatures
        {
            get { return shown; }
        }

        // ------------------------------------------------------------ layout

        private void BuildLayout()
        {
            Text = s.Title + " " + Strings.ToolVersion + " - " + session.Source.ProjectName;
            Name = "SpecialFeaturesForm";
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 9f);
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            ClientSize = new Size(Math.Min(1250, area.Width - 40), Math.Min(720, area.Height - 60));
            MinimumSize = new Size(760, 480);
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;

            var table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.Padding = new Padding(10);
            table.ColumnCount = 1;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < 7; i++) table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles[5] = new RowStyle(SizeType.Percent, 100f);

            lblProject.AutoSize = true;
            lblProject.Font = new Font(Font.FontFamily, 11f, FontStyle.Bold);
            lblProject.Text = string.Format(s.ProjectLabel, session.Source.ProjectName);
            lblProject.Margin = new Padding(3, 0, 3, 4);

            ConfigureWrappingLabel(lblIntro, table);
            lblIntro.Text = s.Intro;

            // Books, kind, hide formatted.
            var filters = NewFlow();
            cboBooks.Name = "Books";
            cboBooks.DropDownStyle = ComboBoxStyle.DropDownList;
            cboBooks.Width = 170;
            SetupButton(btnKinds, "", "Kinds");
            btnKinds.Margin = new Padding(12, 1, 3, 3);
            chkHideFormatted.Name = "HideFormatted";
            chkHideFormatted.Text = s.HideFormatted;
            chkHideFormatted.AutoSize = true;
            chkHideFormatted.Margin = new Padding(12, 4, 3, 3);
            filters.Controls.Add(FlowLabel(s.BooksLabel));
            filters.Controls.Add(cboBooks);
            filters.Controls.Add(btnKinds);
            filters.Controls.Add(chkHideFormatted);
            chkShowHidden.Name = "ShowHidden";
            chkShowHidden.AutoSize = true;
            chkShowHidden.Margin = new Padding(12, 4, 3, 3);
            filters.Controls.Add(chkShowHidden);

            // Publications, then the sources of the added places.
            Label pubLabel = FlowLabel(s.PublicationsLabel);
            tips.SetToolTip(pubLabel, s.PublicationsHint);
            pubs.Controls.Add(pubLabel);
            BuildPublicationBoxes();

            FillFilterControls();
            cboBooks.SelectedIndexChanged += delegate { FilterChanged(); };
            btnKinds.Click += delegate { ChooseKinds(); };
            chkHideFormatted.CheckedChanged += delegate { FilterChanged(); };
            chkShowHidden.CheckedChanged += delegate { FilterChanged(); };

            ConfigureWrappingLabel(lblSummary, table);
            lblSummary.Name = "Summary";
            lblSummary.Font = new Font(Font, FontStyle.Bold);
            lblSummary.Margin = new Padding(3, 8, 3, 4);

            BuildGrid();

            var buttons = NewFlow();
            buttons.Margin = new Padding(0, 6, 0, 0);
            SetupButton(btnGoTo, s.GoToButton, "GoTo");
            SetupButton(btnHide, s.HideSelected, "Hide");
            SetupButton(btnUnhide, s.UnhideSelected, "Unhide");
            SetupButton(btnRescan, s.Rescan, "Rescan");
            SetupButton(btnAdded, s.AddedButton, "AddedPlaces");
            SetupButton(btnClose, s.Close, "Close");
            buttons.Controls.Add(btnGoTo);
            buttons.Controls.Add(btnHide);
            buttons.Controls.Add(btnUnhide);
            buttons.Controls.Add(btnRescan);
            buttons.Controls.Add(btnAdded);
            buttons.Controls.Add(btnClose);
            btnGoTo.Click += delegate { GoToSelected(); };
            btnHide.Click += delegate { HideSelected(true); };
            btnUnhide.Click += delegate { HideSelected(false); };
            btnRescan.Click += delegate { Rescan(); };
            btnAdded.Click += delegate { ShowAddedPlaces(); };
            btnClose.Click += delegate { Close(); };

            table.Controls.Add(lblProject, 0, 0);
            table.Controls.Add(lblIntro, 0, 1);
            table.Controls.Add(filters, 0, 2);
            table.Controls.Add(pubs, 0, 3);
            table.Controls.Add(lblSummary, 0, 4);
            table.Controls.Add(grid, 0, 5);
            table.Controls.Add(buttons, 0, 6);
            Controls.Add(table);
        }

        private static FlowLayoutPanel NewFlow()
        {
            var flow = new FlowLayoutPanel();
            flow.AutoSize = true;
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.WrapContents = true;
            flow.Dock = DockStyle.Fill;
            flow.Margin = new Padding(0, 4, 0, 0);
            return flow;
        }

        private static Label FlowLabel(string text)
        {
            var l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.Margin = new Padding(3, 7, 3, 3);
            return l;
        }

        private void ConfigureWrappingLabel(Label label, TableLayoutPanel table)
        {
            label.AutoSize = true;
            label.Margin = new Padding(3, 2, 3, 2);
            // Wraps at the window's width, so a long text never widens the layout past the window.
            // Set again once the window is shown, because auto-scaling also scales MaximumSize.
            label.MaximumSize = new Size(Math.Max(200, ClientSize.Width - 50), 0);
            EventHandler fit = delegate
            {
                label.MaximumSize = new Size(Math.Max(200, table.ClientSize.Width - table.Padding.Horizontal - 10), 0);
            };
            table.SizeChanged += fit;
            Load += fit;
        }

        private static void SetupButton(Button b, string text, string name)
        {
            b.Text = text;
            b.Name = name;
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.Padding = new Padding(8, 2, 8, 2);
            b.Margin = new Padding(0, 0, 8, 0);
        }

        private void BuildGrid()
        {
            grid.Name = "Places";
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.MultiSelect = true;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.BackgroundColor = SystemColors.Window;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            grid.ShowCellToolTips = true;
            grid.DataError += delegate(object sender, DataGridViewDataErrorEventArgs e) { e.ThrowException = false; };

            // Wide enough for the longest status and kind (the French ones are longer).
            var statuses = new[] { s.StatusPlain, s.StatusFormatted, s.StatusNotDrafted, s.StatusQuoteMarks };
            int statusWidth = FitWidth(140, statuses);
            // A hidden place's status ends in "(hidden)", in italic: wide enough for that too.
            using (var italic = new Font(Font, FontStyle.Italic))
                foreach (string t in statuses)
                    statusWidth = Math.Max(statusWidth, TextRenderer.MeasureText(t + s.HiddenSuffix, italic).Width + 14);
            AddTextColumn("Status", s.ColStatus, statusWidth);
            AddTextColumn("Reference", s.ColReference, 115);
            AddTextColumn("Found", s.ColFound, 160);
            AddTextColumn("Suggested", s.ColSuggested, 200);
            AddTextColumn("Description", s.ColDescription, 280);
            AddTextColumn("Category", s.ColCategory, FitWidth(160, Categories.All.Select(c => c.Name(s.Language))));
            AddTextColumn("Publications", s.ColPublications, 150);
            grid.Columns["Description"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["Description"].MinimumWidth = 150;

            grid.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0 && e.RowIndex < shown.Count) GoTo(shown[e.RowIndex]);
            };
            grid.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    GoToSelected();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Delete)
                {
                    HideSelected(true);
                    e.Handled = true;
                }
            };
            grid.SelectionChanged += delegate { UpdateHideButtons(); };
        }

        /// <summary>
        /// The width, or in French more when one of the texts would not fit in it
        /// (the English and Spanish widths were chosen for their texts and stay as they are).
        /// </summary>
        private int FitWidth(int width, IEnumerable<string> texts)
        {
            if (s.Language != Lang.French) return width;
            foreach (string t in texts)
                width = Math.Max(width, TextRenderer.MeasureText(t, Font).Width + 14);
            return width;
        }

        private void AddTextColumn(string name, string header, int width)
        {
            var col = new DataGridViewTextBoxColumn();
            col.Name = name;
            col.HeaderText = header;
            col.Width = width;
            col.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(col);
        }

        // ------------------------------------------------------------ filters

        private sealed class Choice
        {
            public readonly string Key;
            public readonly string Text;

            public Choice(string key, string text)
            {
                Key = key;
                Text = text;
            }

            public override string ToString()
            {
                return Text;
            }
        }

        private void FillFilterControls()
        {
            filling = true;
            ScanOptions o = session.Options;

            cboBooks.Items.Clear();
            cboBooks.Items.Add(new Choice(ScanOptions.AllBooks, s.BooksAll));
            cboBooks.Items.Add(new Choice(ScanOptions.OldTestament, s.BooksOT));
            cboBooks.Items.Add(new Choice(ScanOptions.NewTestament, s.BooksNT));
            foreach (int book in session.Features.Select(f => f.BookNum).Distinct().OrderBy(b => b))
                cboBooks.Items.Add(new Choice(Books.Code(book), Books.Code(book)));
            cboBooks.SelectedIndex = Math.Max(0, IndexOfChoice(cboBooks, o.Books));

            UpdateKindsButton();
            chkHideFormatted.Checked = o.HideFormatted;
            chkShowHidden.Checked = o.ShowHidden;
            foreach (CheckBox c in chkPublications)
                c.Checked = !o.ExcludedPublications.Contains((string)c.Tag);
            filling = false;
        }

        private static int IndexOfChoice(ComboBox box, string key)
        {
            for (int i = 0; i < box.Items.Count; i++)
                if (string.Equals(((Choice)box.Items[i]).Key, key, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        private void FilterChanged()
        {
            if (filling) return;
            ScanOptions o = session.Options;
            var book = cboBooks.SelectedItem as Choice;
            o.Books = book != null ? book.Key : ScanOptions.AllBooks;
            o.HideFormatted = chkHideFormatted.Checked;
            o.ShowHidden = chkShowHidden.Checked;
            o.ExcludedPublications.Clear();
            foreach (CheckBox c in chkPublications)
                if (!c.Checked) o.ExcludedPublications.Add((string)c.Tag);
            session.SaveOptions();
        }

        /// <summary>"Kinds: 16 of 18...", with the kinds not shown in its tooltip.</summary>
        private void UpdateKindsButton()
        {
            ScanOptions o = session.Options;
            int total = Categories.All.Count;
            int shownKinds = Categories.All.Count(c => !o.ExcludedCategories.Contains(c.Key));
            btnKinds.Text = string.Format(s.KindsButton, shownKinds, total);
            string off = string.Join(", ", Categories.All.Where(c => o.ExcludedCategories.Contains(c.Key))
                .Select(c => c.Name(s.Language)).ToArray());
            tips.SetToolTip(btnKinds, off.Length > 0 ? string.Format(s.KindsTip, off) : "");
        }

        private void ChooseKinds()
        {
            using (var dlg = new KindChooserForm(session.Options.ExcludedCategories, s))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                session.Options.ExcludedCategories.Clear();
                session.Options.ExcludedCategories.UnionWith(dlg.ExcludedKinds);
            }
            UpdateKindsButton();
            session.SaveOptions();
        }

        // ------------------------------------------------------------ hidden places

        /// <summary>The place to select after the next fill, when the selected ones were hidden.</summary>
        private int keepAfterHide;

        private List<Feature> SelectedFeatures()
        {
            return grid.SelectedRows.Cast<DataGridViewRow>().Where(r => r.Index < shown.Count)
                .OrderBy(r => r.Index).Select(r => shown[r.Index]).ToList();
        }

        private void UpdateHideButtons()
        {
            List<Feature> selected = SelectedFeatures();
            btnHide.Enabled = selected.Any(f => !session.IsHidden(f));
            btnUnhide.Enabled = selected.Any(f => session.IsHidden(f));
        }

        /// <summary>Hides the selected places, or shows them again.</summary>
        private void HideSelected(bool hide)
        {
            List<Feature> selected = SelectedFeatures().Where(f => session.IsHidden(f) != hide).ToList();
            if (selected.Count == 0) return;
            keepAfterHide = 0;
            if (hide && !session.Options.ShowHidden)
            {
                // The hidden rows leave the list: select the next place that stays (or the one before).
                int last = grid.SelectedRows.Cast<DataGridViewRow>().Max(r => r.Index);
                var gone = new HashSet<Feature>(selected);
                Feature next = shown.Skip(last + 1).FirstOrDefault(f => !gone.Contains(f))
                    ?? shown.Take(last).LastOrDefault(f => !gone.Contains(f));
                if (next != null) keepAfterHide = next.Id;
            }
            session.SetHidden(selected, hide);   // raises Changed, which fills the list
        }

        // ------------------------------------------------------------ rows

        private void Rescan()
        {
            session.Invalidate(0);
        }

        /// <summary>Reads the books the filters need and lists the places that pass them.</summary>
        private void Fill()
        {
            if (IsDisposed) return;
            Cursor old = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                List<int> keepIds = keepAfterHide != 0 ? new List<int> { keepAfterHide }
                    : SelectedFeatures().Select(f => f.Id).ToList();
                int currentId = keepAfterHide != 0 ? keepAfterHide
                    : grid.CurrentRow != null && grid.CurrentRow.Index < shown.Count ? shown[grid.CurrentRow.Index].Id : 0;
                int firstShown = grid.FirstDisplayedScrollingRowIndex;
                keepAfterHide = 0;
                var list = new List<Feature>();
                int lastBook = 0;
                foreach (Feature f in session.Features)
                {
                    if (!session.Options.ShowsBook(f.BookNum) || !session.Options.Shows(f)) continue;
                    if (f.BookNum != lastBook)
                    {
                        lastBook = f.BookNum;
                        lblSummary.Text = string.Format(s.Scanning, f.BookCode);
                        lblSummary.Refresh();
                    }
                    if (session.Passes(f)) list.Add(f);
                }
                shown = list;

                grid.Rows.Clear();
                foreach (Feature f in shown)
                {
                    FeatureResult r = session.Result(f);
                    int row = grid.Rows.Add();
                    DataGridViewRow gr = grid.Rows[row];
                    DataGridViewCellCollection c = gr.Cells;
                    c["Status"].Value = s.StatusName(r.Status);
                    c["Reference"].Value = f.Reference;
                    c["Category"].Value = f.Category.Name(s.Language);
                    c["Description"].Value = f.Description(s.Language);
                    c["Publications"].Value = s.PublicationsText(f.Publications);
                    c["Suggested"].Value = f.Category.Suggested;
                    c["Found"].Value = s.FoundText(r);
                    string tip = s.Details(f, r);
                    foreach (DataGridViewCell cell in c) cell.ToolTipText = tip;
                    if (r.Status == FeatureStatus.Plain) c["Status"].Style.BackColor = PlainBack;
                    else if (r.Status == FeatureStatus.Formatted) c["Status"].Style.BackColor = FormattedBack;
                    else if (r.Status == FeatureStatus.QuoteMarks) c["Status"].Style.BackColor = QuoteMarksBack;
                    else gr.DefaultCellStyle.ForeColor = SystemColors.GrayText;
                    if (session.IsHidden(f))
                    {
                        c["Status"].Value = s.StatusName(r.Status) + s.HiddenSuffix;
                        gr.DefaultCellStyle.ForeColor = SystemColors.GrayText;
                        gr.DefaultCellStyle.Font = HiddenFont;
                        foreach (DataGridViewCell cell in c) cell.ToolTipText = s.TipHidden + "\n\n" + tip;
                    }
                }

                int plain = shown.Count(f => session.Result(f).Status == FeatureStatus.Plain);
                int quotes = shown.Count(f => session.Result(f).Status == FeatureStatus.QuoteMarks);
                int formatted = shown.Count(f => session.Result(f).Status == FeatureStatus.Formatted);
                int hidden = session.HiddenCount();
                lblSummary.Text = (shown.Count == 0 ? s.NothingShown
                    : string.Format(s.Summary, shown.Count, plain, quotes, formatted, shown.Count - plain - quotes - formatted))
                    + (hidden > 0 ? string.Format(s.SummaryHidden, hidden) : "");
                chkShowHidden.Text = string.Format(s.ShowHidden, hidden);
                if (firstShown >= 0 && firstShown < grid.Rows.Count) grid.FirstDisplayedScrollingRowIndex = firstShown;
                if (currentId != 0) Select(currentId, keepIds);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format(s.Error, ex.Message), s.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                Cursor.Current = old;
                btnGoTo.Enabled = shown.Count > 0;
                UpdateHideButtons();
            }
        }

        private Font hiddenFont;

        private Font HiddenFont
        {
            get { return hiddenFont ?? (hiddenFont = new Font(grid.Font, FontStyle.Italic)); }
        }

        /// <summary>Makes a place current, and selects it and the other given places still listed.</summary>
        private bool Select(int featureId, ICollection<int> alsoSelected)
        {
            int i = shown.FindIndex(f => f.Id == featureId);
            if (i < 0) return false;
            grid.CurrentCell = grid.Rows[i].Cells[0];
            grid.ClearSelection();
            for (int k = 0; k < shown.Count; k++)
                if (k == i || alsoSelected.Contains(shown[k].Id)) grid.Rows[k].Selected = true;
            return true;
        }

        private void GoToSelected()
        {
            if (grid.CurrentRow != null && grid.CurrentRow.Index < shown.Count)
                GoTo(shown[grid.CurrentRow.Index]);
        }

        private void GoTo(Feature f)
        {
            try { session.Source.GoTo(f.BookNum, f.StartChapter, f.StartVerse); }
            catch (Exception) { }
        }
    }
}
