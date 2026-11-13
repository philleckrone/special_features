using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SpecialFeatures
{
    /// <summary>
    /// The places the user added: one list for every project on this computer, in
    /// %APPDATA%\ParatextSpecialFeatures\added.tsv (same columns as features.tsv; the publications
    /// column holds the source name, e.g. a country). Export and Import share it as a file.
    /// SF_ADDED overrides the path, in the test host only (SF_TESTHOST): in the plugin an
    /// environment variable must not redirect where the list is read and written.
    /// </summary>
    public static class AddedPlaces
    {
        public const string DefaultSource = "Added";

        private static List<CatalogEntry> entries;

        /// <summary>The list was changed: every open window builds its places again.</summary>
        public static event EventHandler Changed;

        /// <summary>Goes up with every change, so a window opened later knows its places are out of date.</summary>
        public static int Version { get; private set; }

        public static string FilePath
        {
            get
            {
#if SF_TESTHOST
                string over = Environment.GetEnvironmentVariable("SF_ADDED");
                if (!string.IsNullOrEmpty(over)) return over;
#endif
                return Path.Combine(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ParatextSpecialFeatures"), "added.tsv");
            }
        }

        /// <summary>The added places (read once; an unreadable file gives an empty list and LoadError).</summary>
        public static List<CatalogEntry> Entries
        {
            get
            {
                if (entries == null)
                {
                    entries = new List<CatalogEntry>();
                    LoadError = null;
                    string path = FilePath;
                    if (File.Exists(path))
                    {
                        try { entries = Read(path); }
                        catch (Exception ex) { LoadError = ex.Message; }
                    }
                }
                return entries;
            }
        }

        /// <summary>Why the saved list could not be read, or null.</summary>
        public static string LoadError { get; private set; }

        /// <summary>Reads a list file (features.tsv format); every entry is marked as added.</summary>
        public static List<CatalogEntry> Read(string path)
        {
            List<CatalogEntry> list = FeatureCatalog.Parse(File.ReadAllText(path, Encoding.UTF8));
            foreach (CatalogEntry e in list) e.Added = true;
            return list;
        }

        public static void Save()
        {
            Write(FilePath, Entries);
            Version++;
            EventHandler handler = Changed;
            if (handler != null) handler(null, EventArgs.Empty);
        }

        /// <summary>Writes entries as a list file that Import (or features.tsv) can read.</summary>
        public static void Write(string path, IEnumerable<CatalogEntry> list)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var sb = new StringBuilder();
            sb.Append("# Special Features: added places. Columns (tab-separated): kind, book, references, source, English description, Spanish description, versification (eng or org), French description.\r\n");
            foreach (CatalogEntry e in list) sb.Append(ToLine(e)).Append("\r\n");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        public static string ToLine(CatalogEntry e)
        {
            return string.Join("\t", new[]
            {
                e.Category.Key, Books.Code(e.BookNum), Clean(e.References), Clean(string.Join(",", e.Publications)),
                Clean(e.DescriptionEn), Clean(e.DescriptionEs), e.Versification ?? "eng", Clean(e.DescriptionFr),
            });
        }

        private static string Clean(string s)
        {
            return (s ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();
        }

        /// <summary>Adds the entries of a list file that are not in the list yet. Returns (added, skipped).</summary>
        public static int[] Import(string path)
        {
            List<CatalogEntry> incoming = Read(path);
            var have = new HashSet<string>(Entries.Select(ToLine), StringComparer.Ordinal);
            int added = 0, skipped = 0;
            foreach (CatalogEntry e in incoming)
            {
                if (have.Add(ToLine(e)))
                {
                    Entries.Add(e);
                    added++;
                }
                else skipped++;
            }
            if (added > 0) Save();
            return new[] { added, skipped };
        }
    }
}
