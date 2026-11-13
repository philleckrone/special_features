using System;
using System.Collections.Generic;
using System.Linq;

namespace SpecialFeatures
{
    /// <summary>One line of the paper behind a place: its reference, publications and description.</summary>
    public sealed class FeatureSource
    {
        public CatalogEntry Entry;
        public PaperRange Range;

        public string Label
        {
            get { return Range.WholeBook ? Books.Code(Entry.BookNum) : Books.Code(Entry.BookNum) + " " + Range.Label; }
        }
    }

    /// <summary>
    /// A place that may need special formatting: the paper's entries of one category whose
    /// verses overlap or touch, joined into one stretch, in the project's versification.
    /// </summary>
    public sealed class Feature
    {
        public int Id;
        public Category Category;
        public int BookNum;
        public int StartChapter;
        public int StartVerse;
        /// <summary>PaperRange.EndOfChapter when the place runs to the end of the chapter.</summary>
        public int EndChapter;
        public int EndVerse;
        public bool WholeBook;
        public readonly List<FeatureSource> Sources = new List<FeatureSource>();

        public string BookCode
        {
            get { return Books.Code(BookNum); }
        }

        public int StartKey
        {
            get { return FeatureCatalog.Key(StartChapter, StartVerse); }
        }

        public int EndKey
        {
            get { return FeatureCatalog.Key(EndChapter, EndVerse); }
        }

        public bool Contains(int chapter, int verse)
        {
            int key = FeatureCatalog.Key(chapter, verse);
            return key >= StartKey && key <= EndKey;
        }

        /// <summary>Whether a verse bridge (verse to lastVerse) overlaps the place.</summary>
        public bool Overlaps(int chapter, int verse, int lastVerse)
        {
            return FeatureCatalog.Key(chapter, lastVerse) >= StartKey && FeatureCatalog.Key(chapter, verse) <= EndKey;
        }

        /// <summary>The publications of all its entries, in the paper's column order, then the added sources.</summary>
        public IList<string> Publications
        {
            get
            {
                var all = new HashSet<string>(Sources.SelectMany(s => s.Entry.Publications), StringComparer.OrdinalIgnoreCase);
                var list = FeatureCatalog.Publications.Where(all.Contains).ToList();
                list.AddRange(all.Where(p => !FeatureCatalog.Publications.Contains(p, StringComparer.OrdinalIgnoreCase))
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase));
                return list;
            }
        }

        /// <summary>
        /// Names the place for the hidden list: kind, book and range ("division|GEN 2:4-4:26").
        /// Unlike Id, it stays the same when added places change.
        /// </summary>
        public string HideKey
        {
            get { return Category.Key + "|" + Reference; }
        }

        public string Reference
        {
            get
            {
                if (WholeBook) return BookCode;
                return BookCode + " " + RangeText(StartChapter, StartVerse, EndChapter, EndVerse);
            }
        }

        public static string RangeText(int startChapter, int startVerse, int endChapter, int endVerse)
        {
            if (startVerse == 1 && endVerse == PaperRange.EndOfChapter)
                return startChapter == endChapter ? startChapter.ToString() : startChapter + "-" + endChapter;
            string end = endVerse == PaperRange.EndOfChapter ? endChapter + ":end" : endChapter + ":" + endVerse;
            if (startChapter != endChapter || endVerse == PaperRange.EndOfChapter)
                return startChapter + ":" + startVerse + "-" + end;
            return startVerse == endVerse ? startChapter + ":" + startVerse : startChapter + ":" + startVerse + "-" + endVerse;
        }

        /// <summary>The entries' different descriptions, joined.</summary>
        public string Description(Lang lang)
        {
            var seen = new List<string>();
            foreach (FeatureSource s in Sources)
            {
                string d = s.Entry.Description(lang);
                if (!seen.Contains(d)) seen.Add(d);
            }
            return string.Join("; ", seen.ToArray());
        }

        /// <summary>Each entry of the paper behind the place, one per line: reference (publications) description.</summary>
        public string SourceDetails(Strings strings)
        {
            var lines = new List<string>();
            foreach (FeatureSource s in Sources)
            {
                string line = s.Label + " (" + strings.PublicationsText(s.Entry.Publications) + ") " + s.Entry.Description(strings.Language);
                if (!lines.Contains(line)) lines.Add(line);
            }
            return string.Join("\n", lines.ToArray());
        }
    }

    /// <summary>Builds the places from the catalog, in the project's versification.</summary>
    public static class FeatureBuilder
    {
        public static List<Feature> Build(IEnumerable<CatalogEntry> entries, IBookSource source)
        {
            // Each range of each entry, mapped to the project.
            var pieces = new List<Feature>();
            foreach (CatalogEntry e in entries)
            {
                foreach (PaperRange r in e.Ranges)
                {
                    var f = new Feature();
                    f.Category = e.Category;
                    f.BookNum = e.BookNum;
                    f.WholeBook = r.WholeBook;
                    f.StartChapter = r.StartChapter;
                    f.StartVerse = r.StartVerse;
                    f.EndChapter = r.EndChapter;
                    f.EndVerse = r.EndVerse;
                    if (!r.WholeBook && source != null) Map(source, e, f);
                    f.Sources.Add(new FeatureSource { Entry = e, Range = r });
                    pieces.Add(f);
                }
            }

            // Same category and book, overlapping or next to each other: one place.
            var result = new List<Feature>();
            foreach (var group in pieces.GroupBy(p => p.Category.Key + "|" + p.BookNum))
            {
                Feature current = null;
                foreach (Feature p in group.OrderBy(x => x.StartKey).ThenByDescending(x => x.EndKey))
                {
                    if (current != null && p.StartKey <= current.EndKey + 1)
                    {
                        if (p.EndKey > current.EndKey)
                        {
                            current.EndChapter = p.EndChapter;
                            current.EndVerse = p.EndVerse;
                        }
                        current.WholeBook |= p.WholeBook;
                        current.Sources.AddRange(p.Sources);
                        continue;
                    }
                    current = p;
                    result.Add(p);
                }
            }

            result = result.OrderBy(f => f.BookNum).ThenBy(f => f.StartKey)
                .ThenBy(f => Categories.IndexOf(f.Category)).ToList();
            for (int i = 0; i < result.Count; i++) result[i].Id = i + 1;
            return result;
        }

        private static void Map(IBookSource source, CatalogEntry e, Feature f)
        {
            int c, v;
            if (source.MapVerse(e.Versification, e.BookNum, f.StartChapter, f.StartVerse, out c, out v))
            {
                f.StartChapter = c;
                f.StartVerse = v;
            }
            if (f.EndVerse == PaperRange.EndOfChapter)
            {
                // Whole chapters: map the chapter only.
                if (source.MapVerse(e.Versification, e.BookNum, f.EndChapter, 1, out c, out v))
                    f.EndChapter = c;
            }
            else if (source.MapVerse(e.Versification, e.BookNum, f.EndChapter, f.EndVerse, out c, out v))
            {
                f.EndChapter = c;
                f.EndVerse = v;
            }
            if (f.EndKey < f.StartKey)
            {
                f.EndChapter = f.StartChapter;
                f.EndVerse = f.StartVerse;
            }
        }
    }
}
