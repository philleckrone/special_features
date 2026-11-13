using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace SpecialFeatures
{
    /// <summary>
    /// The window's filters. Saved per project in &lt;project&gt;\local\SpecialFeatures\settings.xml,
    /// which Send/Receive does not share.
    /// </summary>
    public sealed class ScanOptions
    {
        public const string AllBooks = "ALL";
        public const string OldTestament = "OT";
        public const string NewTestament = "NT";

        public readonly HashSet<string> ExcludedPublications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> ExcludedCategories = new HashSet<string>(StringComparer.Ordinal);
        public bool HideFormatted;
        /// <summary>The places the user hid in this project (Feature.HideKey).</summary>
        public readonly HashSet<string> HiddenPlaces = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>Whether hidden places are listed anyway (in grey).</summary>
        public bool ShowHidden;
        /// <summary>ALL, OT, NT or a book code.</summary>
        public string Books = AllBooks;

        /// <summary>Whether a place passes the publication and category filters.</summary>
        public bool Shows(Feature f)
        {
            if (ExcludedCategories.Contains(f.Category.Key)) return false;
            return f.Publications.Any(p => !ExcludedPublications.Contains(p));
        }

        public bool ShowsBook(int bookNum)
        {
            if (Books == OldTestament) return bookNum < SpecialFeatures.Books.FirstNT;
            if (Books == NewTestament) return bookNum >= SpecialFeatures.Books.FirstNT;
            if (Books == AllBooks || string.IsNullOrEmpty(Books)) return true;
            return SpecialFeatures.Books.Number(Books) == bookNum;
        }

        public static string SettingsPath(string projectDirectory)
        {
            return projectDirectory == null ? null
                : Path.Combine(Path.Combine(Path.Combine(projectDirectory, "local"), "SpecialFeatures"), "settings.xml");
        }

        /// <summary>The saved options, or the defaults when there are none (or they cannot be read).</summary>
        public static ScanOptions Load(string path)
        {
            var o = new ScanOptions();
            if (path == null || !File.Exists(path)) return o;
            try
            {
                var doc = new XmlDocument();
                // No DTD processing and nothing fetched from outside the file.
                using (XmlReader r = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null }))
                    doc.Load(r);
                XmlElement root = doc.DocumentElement;
                o.ExcludedPublications.UnionWith(Words(Text(root, "ExcludedPublications", "")));
                o.ExcludedCategories.UnionWith(Words(Text(root, "ExcludedCategories", "")));
                o.HideFormatted = Text(root, "HideFormatted", "No") == "Yes";
                o.Books = Text(root, "Books", AllBooks);
                o.ShowHidden = Text(root, "ShowHidden", "No") == "Yes";
                XmlNodeList hidden = root != null ? root.SelectNodes("HiddenPlaces/Place") : null;
                if (hidden != null)
                    foreach (XmlNode n in hidden)
                    {
                        string key = n.InnerText.Trim();
                        if (key.Length > 0) o.HiddenPlaces.Add(key);
                    }
            }
            catch (Exception)
            {
                return new ScanOptions();
            }
            return o;
        }

        private static IEnumerable<string> Words(string s)
        {
            return s.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string Text(XmlElement root, string name, string fallback)
        {
            XmlNode node = root != null ? root.SelectSingleNode(name) : null;
            return node != null ? node.InnerText.Trim() : fallback;
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding(false);
            using (XmlWriter w = XmlWriter.Create(path, settings))
            {
                w.WriteStartElement("SpecialFeaturesSettings");
                w.WriteElementString("ExcludedPublications", string.Join(" ", ExcludedPublications.OrderBy(x => x).ToArray()));
                w.WriteElementString("ExcludedCategories", string.Join(" ", ExcludedCategories.OrderBy(x => x).ToArray()));
                w.WriteElementString("HideFormatted", HideFormatted ? "Yes" : "No");
                w.WriteElementString("Books", Books ?? AllBooks);
                w.WriteElementString("ShowHidden", ShowHidden ? "Yes" : "No");
                w.WriteStartElement("HiddenPlaces");
                foreach (string key in HiddenPlaces.OrderBy(x => x, StringComparer.Ordinal))
                    w.WriteElementString("Place", key);
                w.WriteEndElement();
                w.WriteEndElement();
            }
        }
    }
}
