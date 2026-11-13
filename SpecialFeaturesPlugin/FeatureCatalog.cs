using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpecialFeatures
{
    /// <summary>One stretch of verses as the paper writes it, e.g. "10:2-18" or "1:43b--4:8".</summary>
    public sealed class PaperRange
    {
        /// <summary>Verse number meaning "to the end of the chapter".</summary>
        public const int EndOfChapter = 999;

        public int StartChapter;
        public int StartVerse;
        public int EndChapter;
        public int EndVerse;
        public bool WholeBook;
        /// <summary>The paper's text for this stretch, with its chapter, e.g. "46:8b-14".</summary>
        public string Label;
    }

    /// <summary>One line of features.tsv: a category, a book, the paper's references and publications.</summary>
    public sealed class CatalogEntry
    {
        public Category Category;
        public int BookNum;
        public string References;
        public List<PaperRange> Ranges = new List<PaperRange>();
        public string[] Publications;
        public string DescriptionEn;
        public string DescriptionEs;
        public string DescriptionFr;
        /// <summary>"eng" or "org": the versification the references are written in.</summary>
        public string Versification;
        /// <summary>Added by the user (AddedPlaces), not from the paper.</summary>
        public bool Added;

        /// <summary>The description in that language, or the English one when it has none.</summary>
        public string Description(Lang lang)
        {
            string d = lang == Lang.Spanish ? DescriptionEs : lang == Lang.French ? DescriptionFr : null;
            return !string.IsNullOrEmpty(d) ? d : DescriptionEn;
        }
    }

    /// <summary>
    /// Reads features.tsv (embedded in the plugin). Columns (tab-separated): category, book,
    /// references, publications, English description, Spanish description, versification
    /// (eng or org; empty = eng), French description. The last three may be missing; an empty
    /// description falls back to the English one.
    /// </summary>
    public static class FeatureCatalog
    {
        public const string ResourceName = "SpecialFeatures.features.tsv";

        /// <summary>The "publication" of the Old Testament quotations: not a Bible, just their own tick box.</summary>
        public const string QuotationSource = "OTQ";

        /// <summary>The publications compared in the paper, in its column order, then the quotations.</summary>
        public static readonly string[] Publications = { "BFC", "CEV", "GNB", "NIV", "NVI", "NLT", "TEV", QuotationSource };

        private static List<CatalogEntry> embedded;

        private static readonly Regex Part = new Regex(
            @"^(?:(\d+):)?(\d+)([a-d]?)(?:(--|-)(?:(\d+):)?(\d+)([a-d]?))?$", RegexOptions.CultureInvariant);

        public static List<CatalogEntry> Embedded()
        {
            if (embedded == null)
            {
                using (Stream stream = typeof(FeatureCatalog).Assembly.GetManifestResourceStream(ResourceName))
                {
                    if (stream == null) throw new InvalidOperationException("Missing resource " + ResourceName);
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                        embedded = Parse(reader.ReadToEnd());
                }
            }
            return embedded;
        }

        public static List<CatalogEntry> Parse(string text)
        {
            var list = new List<CatalogEntry>();
            int lineNo = 0;
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                lineNo++;
                string line = raw.TrimEnd('\r');
                if (line.Trim().Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] f = line.Split('\t');
                if (f.Length < 5) throw new FormatException("features.tsv line " + lineNo + ": too few columns");
                var e = new CatalogEntry();
                e.Category = Categories.Get(f[0].Trim());
                if (e.Category == null) throw new FormatException("features.tsv line " + lineNo + ": unknown category " + f[0]);
                e.BookNum = Books.Number(f[1].Trim());
                if (e.BookNum == 0) throw new FormatException("features.tsv line " + lineNo + ": unknown book " + f[1]);
                e.References = f[2].Trim();
                e.Publications = f[3].Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
                e.DescriptionEn = f[4].Trim();
                e.DescriptionEs = f.Length > 5 ? f[5].Trim() : "";
                e.Versification = f.Length > 6 && f[6].Trim().Length > 0 ? f[6].Trim().ToLowerInvariant() : "eng";
                e.DescriptionFr = f.Length > 7 ? f[7].Trim() : "";
                try
                {
                    e.Ranges = ParseReferences(e.References);
                }
                catch (FormatException ex)
                {
                    throw new FormatException("features.tsv line " + lineNo + ": " + ex.Message);
                }
                list.Add(e);
            }
            return list;
        }

        /// <summary>
        /// Parses the paper's notation: groups separated by ";", parts by ","; a part without a
        /// chapter continues the chapter before it. A group without ":" is whole chapters, "*" the book.
        /// </summary>
        public static List<PaperRange> ParseReferences(string refs)
        {
            var result = new List<PaperRange>();
            foreach (string g in refs.Split(';'))
            {
                string group = g.Trim();
                if (group.Length == 0) continue;
                if (group == "*")
                {
                    result.Add(new PaperRange
                    {
                        StartChapter = 1, StartVerse = 1, EndChapter = PaperRange.EndOfChapter,
                        EndVerse = PaperRange.EndOfChapter, WholeBook = true, Label = "*",
                    });
                    continue;
                }
                if (group.IndexOf(':') < 0)
                {
                    foreach (string p in group.Split(','))
                    {
                        string part = p.Trim();
                        string[] ends = part.Split('-');
                        int first = ParseInt(ends[0], part);
                        int last = ends.Length > 1 ? ParseInt(ends[ends.Length - 1], part) : first;
                        result.Add(new PaperRange
                        {
                            StartChapter = first, StartVerse = 1, EndChapter = last,
                            EndVerse = PaperRange.EndOfChapter, Label = part,
                        });
                    }
                    continue;
                }
                int chapter = 0;
                foreach (string p in group.Split(','))
                {
                    string part = p.Trim();
                    Match m = Part.Match(part);
                    if (!m.Success) throw new FormatException("cannot read \"" + part + "\"");
                    if (m.Groups[1].Success) chapter = ParseInt(m.Groups[1].Value, part);
                    if (chapter == 0) throw new FormatException("no chapter before \"" + part + "\"");
                    var r = new PaperRange();
                    r.StartChapter = chapter;
                    r.StartVerse = ParseInt(m.Groups[2].Value, part);
                    r.Label = m.Groups[1].Success ? part : chapter + ":" + part;
                    if (!m.Groups[4].Success)
                    {
                        r.EndChapter = chapter;
                        r.EndVerse = r.StartVerse;
                    }
                    else if (m.Groups[4].Value == "--")
                    {
                        if (!m.Groups[5].Success) throw new FormatException("no chapter after \"--\" in \"" + part + "\"");
                        r.EndChapter = ParseInt(m.Groups[5].Value, part);
                        r.EndVerse = ParseInt(m.Groups[6].Value, part);
                        chapter = r.EndChapter;
                    }
                    else
                    {
                        if (m.Groups[5].Success) throw new FormatException("use \"--\" between chapters in \"" + part + "\"");
                        r.EndChapter = chapter;
                        r.EndVerse = ParseInt(m.Groups[6].Value, part);
                    }
                    if (Key(r.EndChapter, r.EndVerse) < Key(r.StartChapter, r.StartVerse))
                        throw new FormatException("range ends before it starts: \"" + part + "\"");
                    result.Add(r);
                }
            }
            if (result.Count == 0) throw new FormatException("no references");
            return result;
        }

        private static int ParseInt(string s, string part)
        {
            int n;
            if (!int.TryParse(s.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out n) || n <= 0)
                throw new FormatException("cannot read \"" + part + "\"");
            return n;
        }

        /// <summary>Chapter and verse as one sortable number.</summary>
        public static int Key(int chapter, int verse)
        {
            return chapter * 1000 + verse;
        }
    }
}
