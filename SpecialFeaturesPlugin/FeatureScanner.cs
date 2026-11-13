using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpecialFeatures
{
    public enum FeatureStatus
    {
        /// <summary>The verses have text, but none of the markers this kind of place is formatted with.</summary>
        Plain,
        /// <summary>A marker this kind of place is formatted with is used in its verses.</summary>
        Formatted,
        /// <summary>The book or chapter is missing, or the verses have no text yet.</summary>
        NotDrafted,
        /// <summary>None of the markers, but quotation marks (only for kinds whose QuotesCount is set).</summary>
        QuoteMarks,
    }

    /// <summary>What one verse of a book holds: the markers that belong to it, and its text.</summary>
    public sealed class VerseInfo
    {
        public readonly HashSet<string> Markers = new HashSet<string>(StringComparer.Ordinal);
        public bool HasText;
        /// <summary>The verse text without markers, notes or headings.</summary>
        public readonly StringBuilder Text = new StringBuilder();
    }

    /// <summary>
    /// The verses of one book. Markers that come between one verse's text and the next verse
    /// (a heading, \li1 before \v 2, \ms before \c) belong to that next verse; markers inside
    /// the text of a verse belong to it. Notes and the book's header are skipped.
    /// </summary>
    public sealed class BookIndex
    {
        public readonly int BookNum;
        public readonly bool Exists;
        private readonly Dictionary<int, VerseInfo> verses = new Dictionary<int, VerseInfo>();

        /// <summary>Markers that begin a heading: the text after them is not verse text.</summary>
        private static readonly Regex Heading = new Regex(
            @"^(?:s\d*|ms\d*|mr|sr|r|sp|d|cl|cd|qa|sd\d*|mte\d*|is\d*|imt\d*|iot|io\d*|ip|im|ipi|imi|ili\d*|ipq|imq|ipr|iq\d*|ib|iex|ie|rem|toc\d*|h|mt\d*|ide|usfm|sts|restore)$",
            RegexOptions.CultureInvariant);

        /// <summary>Paragraph markers: the text after them is verse text.</summary>
        private static readonly Regex Paragraph = new Regex(
            @"^(?:p|m|po|pr|cls|pmo|pm|pmc|pmr|pi\d*|mi|nb|pc|ph\d*|b|q\d*|qr|qc|qm\d*|qd|lh|li\d*|lf|lim\d*|litl|tr|th\d*|thr\d*|thc\d*|tc\d*|tcr\d*|tcc\d*|pb|periph)$",
            RegexOptions.CultureInvariant);

        private static readonly Regex NoteStart = new Regex(@"^(?:f|fe|ef|x|ex|fig)$", RegexOptions.CultureInvariant);

        private static readonly Regex Token = new Regex(@"\\(\+?[A-Za-z0-9_\-]+\*?)", RegexOptions.CultureInvariant);

        private static readonly Regex VerseNumber = new Regex(@"^\s*(\d+)[a-z]?(?:[-\u2013,](\d+))?", RegexOptions.CultureInvariant);

        private BookIndex(int bookNum, bool exists)
        {
            BookNum = bookNum;
            Exists = exists;
        }

        public VerseInfo Verse(int chapter, int verse)
        {
            VerseInfo info;
            return verses.TryGetValue(FeatureCatalog.Key(chapter, verse), out info) ? info : null;
        }

        /// <summary>The verses from start to end (keys), in order.</summary>
        public IEnumerable<VerseInfo> Range(int startKey, int endKey)
        {
            return verses.Where(kv => kv.Key >= startKey && kv.Key <= endKey).OrderBy(kv => kv.Key).Select(kv => kv.Value);
        }

        public static BookIndex Build(int bookNum, string usfm)
        {
            var index = new BookIndex(bookNum, !string.IsNullOrEmpty(usfm));
            if (!index.Exists) return index;

            int chapter = 0;
            var current = new List<VerseInfo>();     // the verse (or the verses of a bridge) being read
            var pending = new List<string>();         // markers waiting for the next verse text or verse
            bool inHeading = false;
            int noteDepth = 0;
            int pos = 0;
            string text;

            foreach (Match m in Token.Matches(usfm))
            {
                if (m.Index < pos) continue;   // inside a stretch already skipped (\ca ...\ca*, \vp ...\vp*)
                text = usfm.Substring(pos, m.Index - pos);
                pos = m.Index + m.Length;
                string marker = m.Groups[1].Value;

                // The text before this marker.
                if (noteDepth == 0 && chapter > 0 && !inHeading && current.Count > 0 && text.Trim().Length > 0)
                    index.TextFound(current, pending, text);

                bool end = marker.EndsWith("*", StringComparison.Ordinal);
                string name = marker.TrimStart('+').TrimEnd('*');
                // Milestones (\qt-s ... \qt-e) count as their style.
                if (name.EndsWith("-s", StringComparison.Ordinal) || name.EndsWith("-e", StringComparison.Ordinal))
                    name = name.Substring(0, name.Length - 2);

                if (noteDepth > 0)
                {
                    if (end && NoteStart.IsMatch(name)) noteDepth--;
                    else if (!end && NoteStart.IsMatch(name)) noteDepth++;
                    continue;
                }
                if (name == "c")
                {
                    Match n = VerseNumber.Match(usfm.Substring(pos));
                    if (n.Success) chapter = int.Parse(n.Groups[1].Value);
                    pos += n.Success ? n.Length : 0;
                    current.Clear();
                    inHeading = false;
                    continue;
                }
                if (chapter == 0) continue;   // the book's header and introduction
                if (name == "v")
                {
                    Match n = VerseNumber.Match(usfm.Substring(pos));
                    if (!n.Success) continue;
                    pos += n.Length;
                    int first = int.Parse(n.Groups[1].Value);
                    int last = n.Groups[2].Success ? Math.Max(first, int.Parse(n.Groups[2].Value)) : first;
                    current.Clear();
                    for (int v = first; v <= last && v - first < 200; v++)
                        current.Add(index.GetOrAdd(chapter, v));
                    foreach (VerseInfo info in current)
                        foreach (string p in pending) info.Markers.Add(p);
                    pending.Clear();
                    inHeading = false;
                    continue;
                }
                if (end) continue;
                if (NoteStart.IsMatch(name))
                {
                    if (name != "fig") Record(current, pending, inHeading, name);
                    noteDepth = 1;
                    continue;
                }
                if (name == "ca" || name == "cp" || name == "va" || name == "vp")
                {
                    // Alternate and published numbers: skip their text.
                    int close = usfm.IndexOf("\\" + name + "*", pos, StringComparison.Ordinal);
                    if (close >= 0 && (name == "ca" || name == "va" || name == "vp")) pos = close + name.Length + 2;
                    else if (name == "cp")
                    {
                        Match n = VerseNumber.Match(usfm.Substring(pos));
                        pos += n.Success ? n.Length : 0;
                    }
                    continue;
                }
                if (Heading.IsMatch(name))
                {
                    pending.Add(name);
                    inHeading = true;
                    continue;
                }
                if (Paragraph.IsMatch(name))
                {
                    pending.Add(name);
                    inHeading = false;
                    continue;
                }
                // A character style (or a marker this list does not know): it marks the text it is in.
                Record(current, pending, inHeading, name);
            }
            text = usfm.Substring(pos);
            if (noteDepth == 0 && chapter > 0 && !inHeading && current.Count > 0 && text.Trim().Length > 0)
                index.TextFound(current, pending, text);
            return index;
        }

        private VerseInfo GetOrAdd(int chapter, int verse)
        {
            int key = FeatureCatalog.Key(chapter, verse);
            VerseInfo info;
            if (!verses.TryGetValue(key, out info))
            {
                info = new VerseInfo();
                verses[key] = info;
            }
            return info;
        }

        /// <summary>Verse text after paragraph markers: those markers belong to the verse being read.</summary>
        private void TextFound(List<VerseInfo> current, List<string> pending, string text)
        {
            // A word's attributes (\w word|lemma="..."\w*) run to the end of the stretch.
            int bar = text.IndexOf('|');
            if (bar >= 0) text = text.Substring(0, bar);
            foreach (VerseInfo info in current)
            {
                info.HasText = true;
                info.Text.Append(text);
                foreach (string p in pending) info.Markers.Add(p);
            }
            pending.Clear();
        }

        private static void Record(List<VerseInfo> current, List<string> pending, bool inHeading, string name)
        {
            if (inHeading || current.Count == 0)
            {
                pending.Add(name);
                return;
            }
            foreach (VerseInfo info in current)
            {
                foreach (string p in pending) info.Markers.Add(p);
                info.Markers.Add(name);
            }
            pending.Clear();
        }
    }

    /// <summary>A place's state in the project's text.</summary>
    public sealed class FeatureResult
    {
        public FeatureStatus Status;
        /// <summary>The markers this kind of place is formatted with that its verses use.</summary>
        public List<string> ExpectedFound = new List<string>();
        /// <summary>Every marker its verses use other than plain paragraphs.</summary>
        public List<string> OtherFound = new List<string>();
        /// <summary>With QuoteMarks: the first quoted words (with their marks), or the lone marks found.</summary>
        public string Quoted = "";
    }

    public static class FeatureScanner
    {
        /// <summary>Markers every text uses: not listed as "found".</summary>
        private static readonly HashSet<string> Ordinary = new HashSet<string>(StringComparer.Ordinal)
        {
            "p", "m", "nb", "w", "wj", "nd", "add", "k", "rq", "fig", "x", "ex", "fe", "ef", "pb", "s", "s1",
        };

        /// <summary>
        /// Quotation marks, as opening and closing pairs. Apostrophes (' and U+2019) are left out:
        /// many orthographies use them as letters.
        /// </summary>
        private static readonly string[][] QuotePairs =
        {
            new[] { "\u201C", "\u201D" },   // English double
            new[] { "\u00AB", "\u00BB" },   // guillemets
            new[] { "\u201E", "\u201C" },   // German low-high
            new[] { "\u2039", "\u203A" },   // single guillemets
            new[] { "\"", "\"" },
        };

        private const int MaxQuoted = 60;

        private static readonly Regex Spaces = new Regex(@"\s+", RegexOptions.CultureInvariant);

        public static FeatureResult Examine(Feature feature, BookIndex book)
        {
            var result = new FeatureResult();
            if (book == null || !book.Exists)
            {
                result.Status = FeatureStatus.NotDrafted;
                return result;
            }
            var markers = new HashSet<string>(StringComparer.Ordinal);
            bool text = false;
            var words = new StringBuilder();
            foreach (VerseInfo v in book.Range(feature.StartKey, feature.EndKey))
            {
                text |= v.HasText;
                markers.UnionWith(v.Markers);
                words.Append(v.Text).Append(' ');
            }
            foreach (string m in markers.OrderBy(x => x, StringComparer.Ordinal))
            {
                if (feature.Category.IsExpected(m)) result.ExpectedFound.Add(m);
                else if (!Ordinary.Contains(m)) result.OtherFound.Add(m);
            }
            if (!text) result.Status = FeatureStatus.NotDrafted;
            else result.Status = result.ExpectedFound.Count > 0 ? FeatureStatus.Formatted : FeatureStatus.Plain;
            if (result.Status == FeatureStatus.Plain && feature.Category.QuotesCount)
            {
                result.Quoted = FindQuoted(Spaces.Replace(words.ToString(), " "));
                if (result.Quoted.Length > 0) result.Status = FeatureStatus.QuoteMarks;
            }
            return result;
        }

        /// <summary>
        /// The first quoted words in the text, with their marks (shortened), and "(+n)" when there
        /// are more; or the lone marks when none is closed there (a quotation that began earlier).
        /// </summary>
        public static string FindQuoted(string text)
        {
            var spans = new List<string>();
            int pos = 0;
            while (pos < text.Length)
            {
                // The earliest opening mark whose closing mark follows.
                int bestStart = -1, bestEnd = -1;
                foreach (string[] pair in QuotePairs)
                {
                    int open = FindMark(text, pair[0], pos, true);
                    if (open < 0 || (bestStart >= 0 && open >= bestStart)) continue;
                    int close = FindMark(text, pair[1], open + pair[0].Length, false);
                    if (close < 0) continue;
                    bestStart = open;
                    bestEnd = close + pair[1].Length;
                }
                if (bestStart < 0) break;
                spans.Add(text.Substring(bestStart, bestEnd - bestStart).Trim());
                pos = bestEnd;
            }
            if (spans.Count > 0)
            {
                string first = spans[0];
                if (first.Length > MaxQuoted) first = first.Substring(0, MaxQuoted - 1).TrimEnd() + "\u2026";
                return spans.Count > 1 ? first + " (+" + (spans.Count - 1) + ")" : first;
            }
            // A lone straight " is too often a letter to count.
            var lone = new List<string>();
            foreach (string[] pair in QuotePairs)
                foreach (string mark in pair)
                    if (mark != "\"" && !lone.Contains(mark) && text.IndexOf(mark, StringComparison.Ordinal) >= 0) lone.Add(mark);
            return string.Join(" ", lone.ToArray());
        }

        /// <summary>
        /// The next quotation mark from start. A straight " counts only at a word's edge: an
        /// opening one not after a letter, a closing one not before a letter. Some orthographies
        /// write tone with it (CTL: nin"n).
        /// </summary>
        private static int FindMark(string text, string mark, int start, bool opening)
        {
            int i = text.IndexOf(mark, start, StringComparison.Ordinal);
            if (mark != "\"") return i;
            for (; i >= 0; i = text.IndexOf(mark, i + 1, StringComparison.Ordinal))
            {
                int next = opening ? i - 1 : i + 1;
                if (next < 0 || next >= text.Length || !IsWordChar(text[next])) return i;
            }
            return -1;
        }

        private static bool IsWordChar(char c)
        {
            if (char.IsLetterOrDigit(c)) return true;
            switch (char.GetUnicodeCategory(c))
            {
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.ModifierSymbol:
                    return true;
                default:
                    return false;
            }
        }
    }
}
