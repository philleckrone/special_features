using System;
using System.Collections.Generic;
using System.Linq;

namespace SpecialFeatures
{
    /// <summary>
    /// One project's places, their state in its text and the filters. Books are read only when
    /// first needed.
    /// </summary>
    public sealed class FeatureSession
    {
        public readonly IBookSource Source;
        public readonly ScanOptions Options;

        private List<Feature> features;
        private int builtVersion;
        private Dictionary<int, List<Feature>> byBook;
        private readonly HashSet<int> projectBooks;
        private readonly Dictionary<int, BookIndex> books = new Dictionary<int, BookIndex>();
        private readonly Dictionary<int, FeatureResult> results = new Dictionary<int, FeatureResult>();
        private readonly object sync = new object();

        /// <summary>Filters changed or the text was read again: what is shown may have changed.</summary>
        public event EventHandler Changed;

        public FeatureSession(IBookSource source)
        {
            Source = source;
            projectBooks = new HashSet<int>(source.Books.Select(b => b.Number));
            Options = ScanOptions.Load(ScanOptions.SettingsPath(source.ProjectDirectory));
            BuildFeatures();
        }

        /// <summary>The places: the paper's entries and the added ones, merged.</summary>
        public List<Feature> Features
        {
            get { return features; }
        }

        /// <summary>The publications and added sources of all places, for the filter.</summary>
        public IList<string> AllPublications
        {
            get
            {
                var list = FeatureCatalog.Publications.ToList();
                list.AddRange(AddedPlaces.Entries.SelectMany(e => e.Publications)
                    .Where(p => !list.Contains(p, StringComparer.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.OrdinalIgnoreCase));
                return list;
            }
        }

        private void BuildFeatures()
        {
            builtVersion = AddedPlaces.Version;
            List<Feature> built = FeatureBuilder.Build(FeatureCatalog.Embedded().Concat(AddedPlaces.Entries), Source);
            lock (sync)
            {
                features = built;
                byBook = built.GroupBy(f => f.BookNum).ToDictionary(g => g.Key, g => g.ToList());
                results.Clear();   // ids change
            }
        }

        /// <summary>Whether places were added, changed or deleted since the places were built.</summary>
        public bool PlacesOutOfDate
        {
            get { return builtVersion != AddedPlaces.Version; }
        }

        /// <summary>The added places changed: build the places again (the text read so far is kept).</summary>
        public void ReloadPlaces()
        {
            BuildFeatures();
            RaiseChanged();
        }

        public BookIndex Book(int bookNum)
        {
            lock (sync)
            {
                BookIndex index;
                if (!books.TryGetValue(bookNum, out index))
                {
                    string usfm = projectBooks.Contains(bookNum) ? Source.GetBook(bookNum) : null;
                    index = BookIndex.Build(bookNum, usfm);
                    books[bookNum] = index;
                }
                return index;
            }
        }

        public FeatureResult Result(Feature f)
        {
            lock (sync)
            {
                FeatureResult r;
                if (!results.TryGetValue(f.Id, out r))
                {
                    r = FeatureScanner.Examine(f, Book(f.BookNum));
                    results[f.Id] = r;
                }
                return r;
            }
        }

        /// <summary>Whether the place passes the kind, publication and "hide formatted" filters.</summary>
        public bool Passes(Feature f)
        {
            if (!PassesFilters(f)) return false;
            return Options.ShowHidden || !IsHidden(f);
        }

        /// <summary>The kind, publication and "hide formatted" filters, whether hidden or not.</summary>
        private bool PassesFilters(Feature f)
        {
            if (!Options.Shows(f)) return false;
            return !(Options.HideFormatted && Result(f).Status == FeatureStatus.Formatted);
        }

        /// <summary>Whether the user hid the place in this project.</summary>
        public bool IsHidden(Feature f)
        {
            return Options.HiddenPlaces.Contains(f.HideKey);
        }

        /// <summary>The hidden places that the book and other filters would list.</summary>
        public int HiddenCount()
        {
            if (Options.HiddenPlaces.Count == 0) return 0;
            return features.Count(f => Options.ShowsBook(f.BookNum) && IsHidden(f) && PassesFilters(f));
        }

        /// <summary>Hides or shows places again, saves the choice and tells the window.</summary>
        public void SetHidden(IEnumerable<Feature> places, bool hide)
        {
            foreach (Feature f in places)
            {
                if (hide) Options.HiddenPlaces.Add(f.HideKey);
                else Options.HiddenPlaces.Remove(f.HideKey);
            }
            SaveOptions();
        }

        /// <summary>Forgets what was read from a book (0 = every book), so it is read again.</summary>
        public void Invalidate(int bookNum)
        {
            lock (sync)
            {
                if (bookNum == 0)
                {
                    books.Clear();
                    results.Clear();
                }
                else
                {
                    books.Remove(bookNum);
                    List<Feature> list;
                    if (byBook.TryGetValue(bookNum, out list))
                        foreach (Feature f in list) results.Remove(f.Id);
                }
            }
            RaiseChanged();
        }

        /// <summary>Saves the filters (when the project folder is known) and tells the window.</summary>
        public void SaveOptions()
        {
            string path = ScanOptions.SettingsPath(Source.ProjectDirectory);
            if (path != null)
            {
                try { Options.Save(path); }
                catch (Exception) { }   // the filters still apply for this session
            }
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            EventHandler handler = Changed;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
