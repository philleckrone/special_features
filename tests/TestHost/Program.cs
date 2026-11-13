using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SpecialFeatures;

namespace SpecialFeaturesTests
{
    /// <summary>
    /// Test host for the plugin's Paratext-free code.
    ///   catalog                 the places after merging (no project)
    ///   scan FOLDER             each place's state in a folder of SFM files
    ///   shown FOLDER            the places the window lists with the folder's saved filters (kinds, hidden places)
    ///   import FILE            adds the places of a list file to the added places (SF_ADDED)
    ///   form FOLDER [LANG]      the window (go-to requests are printed when it closes; SF_GOTOLOG logs them)
    /// Output is UTF-8 on standard output.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false));
            output.AutoFlush = true;
            try
            {
                string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "";
                switch (mode)
                {
                    case "catalog":
                        Catalog(output);
                        return 0;
                    case "scan":
                        Scan(output, args[1]);
                        return 0;
                    case "shown":
                        Shown(output, args[1]);
                        return 0;
                    case "import":
                        int[] counts = AddedPlaces.Import(args[1]);
                        output.WriteLine("IMPORTED " + counts[0] + " SKIPPED " + counts[1] + " TOTAL " + AddedPlaces.Entries.Count);
                        return 0;
                    case "form":
                        Application.EnableVisualStyles();
                        var source = new FolderBookSource(args[1]);
                        var form = new SpecialFeaturesForm(new FeatureSession(source), Strings.For(args.Length > 2 ? args[2] : "en"));
                        Application.Run(form);
                        foreach (string g in source.GoTos) output.WriteLine("GOTO " + g);
                        return 0;
                    default:
                        output.WriteLine("usage: catalog | scan FOLDER | shown FOLDER | import FILE | form FOLDER [LANG]");
                        return 2;
                }
            }
            catch (Exception ex)
            {
                output.WriteLine("ERROR " + ex);
                return 1;
            }
        }

        private static void Catalog(TextWriter output)
        {
            var features = FeatureBuilder.Build(FeatureCatalog.Embedded().Concat(AddedPlaces.Entries), null);
            foreach (Feature f in features)
                output.WriteLine(f.Id + "\t" + f.Reference + "\t" + f.Category.Key + "\t" + string.Join(",", f.Publications.ToArray())
                    + "\t" + f.Sources.Count + "\t" + f.Description(Lang.English));
            output.WriteLine("# entries " + FeatureCatalog.Embedded().Count + ", ranges "
                + FeatureCatalog.Embedded().Sum(e => e.Ranges.Count) + ", places " + features.Count);
            foreach (var g in features.GroupBy(f => f.Category.Key))
                output.WriteLine("# " + g.Key + " " + g.Count());
        }

        private static void Scan(TextWriter output, string folder)
        {
            var session = new FeatureSession(new FolderBookSource(folder));
            Strings s = Strings.English();
            foreach (Feature f in session.Features)
            {
                FeatureResult r = session.Result(f);
                output.WriteLine(f.Reference + "\t" + f.Category.Key + "\t" + r.Status + "\t" + s.FoundText(r));
            }
            foreach (var g in session.Features.GroupBy(f => session.Result(f).Status))
                output.WriteLine("# " + g.Key + " " + g.Count());
        }

        /// <summary>The places the window would list with the project's saved filters (local\SpecialFeatures\settings.xml).</summary>
        private static void Shown(TextWriter output, string folder)
        {
            var session = new FeatureSession(new FolderBookSource(folder));
            int count = 0;
            foreach (Feature f in session.Features)
            {
                if (!session.Options.ShowsBook(f.BookNum) || !session.Passes(f)) continue;
                count++;
                output.WriteLine(f.HideKey + "\t" + session.Result(f).Status + (session.IsHidden(f) ? "\tHIDDEN" : ""));
            }
            output.WriteLine("# shown " + count + ", hidden " + session.HiddenCount());
        }
    }
}
