using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;
using Paratext.PluginInterfaces;

namespace SpecialFeatures
{
    /// <summary>
    /// Paratext plugin that adds Tools > Special Features...: a list of the places where published
    /// Bibles often use special formatting, with their state in the project. It only reads the project.
    /// </summary>
    public class SpecialFeaturesPlugin : IParatextStandalonePlugin
    {
        public const string PluginName = "Special Features";
        public const string PublisherName = "Phil Leckrone (SIL)";

        private const string DefaultProjectsDir = @"C:\My Paratext 9 Projects";

        /// <summary>Each project's places and filters (by project ID).</summary>
        private static readonly Dictionary<string, FeatureSession> Sessions = new Dictionary<string, FeatureSession>();

        /// <summary>The open window of each project (by project ID): a second click brings it to the front.</summary>
        private static readonly Dictionary<string, SpecialFeaturesForm> OpenWindows = new Dictionary<string, SpecialFeaturesForm>();

        private IPluginHost host;

        public SpecialFeaturesPlugin()
        {
        }

        /// <summary>Paratext uses this constructor when there is one (the host converts versifications).</summary>
        public SpecialFeaturesPlugin(IPluginHost host)
        {
            this.host = host;
        }

        public string Name
        {
            get { return PluginName; }
        }

        public Version Version
        {
            get { return new Version(Strings.ToolVersion); }
        }

        public string VersionString
        {
            get { return Strings.ToolVersion; }
        }

        public string Publisher
        {
            get { return PublisherName; }
        }

        public IEnumerable<PluginMenuEntry> PluginMenuEntries
        {
            get
            {
                var entry = new PluginMenuEntry(Strings.English().MenuText, OnMenuClicked, PluginMenuLocation.ScrTextTools);
                entry.LocalizedTextNeeded += LocalizeMenuText;
                yield return entry;
            }
        }

        /// <summary>Menu text in Paratext's interface language; null keeps English.</summary>
        internal static string LocalizeMenuText(string defaultText, string locale)
        {
            return Strings.LanguageOf(locale) != Lang.English ? Strings.For(locale).MenuText : null;
        }

        public string GetDescription(string locale)
        {
            return Strings.For(locale).Description;
        }

        public IDataFileMerger GetMerger(IPluginHost pluginHost, string dataIdentifier)
        {
            return null;   // the plugin keeps no project data
        }

        // ------------------------------------------------------------ sessions

        private FeatureSession SessionFor(IProject project)
        {
            string id = project.ID ?? project.ShortName;
            FeatureSession session;
            if (Sessions.TryGetValue(id, out session)) return session;
            session = new FeatureSession(new ParatextBookSource(host, project));
            Sessions[id] = session;
            // Saving a chapter in Paratext: read that book again next time it is needed.
            project.ScriptureDataChanged += delegate(IProject p, int bookNum, int chapter) { session.Invalidate(bookNum); };
            return session;
        }

        // ------------------------------------------------------------ the window

        private void OnMenuClicked(IPluginHost pluginHost, IParatextChildState state)
        {
            if (host == null) host = pluginHost;
            Strings strings = Strings.For(UiLocale(host));
            try
            {
                IProject project = state != null ? state.Project : null;
                if (project == null || project.IsResource)
                {
                    Report(strings, strings.OpenProjectFirst, null);
                    return;
                }
                OpenWindow(project, strings);
            }
            catch (Exception ex)
            {
                Report(strings, string.Format(strings.Error, ex.Message), ex);
            }
        }

        private void OpenWindow(IProject project, Strings strings)
        {
            string id = project.ID ?? project.ShortName;
            SpecialFeaturesForm open;
            if (OpenWindows.TryGetValue(id, out open) && !open.IsDisposed)
            {
                if (open.WindowState == FormWindowState.Minimized) open.WindowState = FormWindowState.Normal;
                open.Activate();
                return;
            }
            // Not modal, so the user can look at the text in Paratext beside the list.
            var form = new SpecialFeaturesForm(SessionFor(project), strings);
            OpenWindows[id] = form;
            form.FormClosed += delegate
            {
                OpenWindows.Remove(id);
                form.Dispose();
            };
            form.Show();
        }

        private void Report(Strings strings, string message, Exception ex)
        {
            try
            {
                if (host != null)
                    host.Log(this, message + (ex != null ? "\n" + ex : ""));
            }
            catch (Exception) { }
            MessageBox.Show(message, strings.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static string UiLocale(IPluginHost pluginHost)
        {
            try
            {
                if (pluginHost != null && pluginHost.UserSettings != null)
                    return pluginHost.UserSettings.UiLocale;
            }
            catch (Exception) { }
            return null;
        }

        // ------------------------------------------------------------ project folder

        /// <summary>
        /// The Paratext projects folder: Settings_Directory under SOFTWARE\Paratext\8 (Paratext 9
        /// still uses that key), checked in both registry views, then for the current user.
        /// Same as the AutoCorrect plugin.
        /// </summary>
        internal static string ProjectsDirectory()
        {
            foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                string dir = ReadSettingsDirectory(RegistryHive.LocalMachine, view);
                if (dir != null) return dir;
            }
            string user = ReadSettingsDirectory(RegistryHive.CurrentUser, RegistryView.Default);
            return user ?? DefaultProjectsDir;
        }

        private static string ReadSettingsDirectory(RegistryHive hive, RegistryView view)
        {
            try
            {
                using (RegistryKey root = RegistryKey.OpenBaseKey(hive, view))
                using (RegistryKey key = root.OpenSubKey(@"SOFTWARE\Paratext\8"))
                {
                    if (key == null) return null;
                    string dir = key.GetValue("Settings_Directory") as string;
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        return dir.TrimEnd('\\');
                }
            }
            catch (Exception) { }
            return null;
        }

        /// <summary>
        /// The project's folder: projectsDir\shortName when its Settings.xml Guid matches the
        /// project ID (or has no readable Guid); otherwise the subfolder whose Guid matches.
        /// Same as the AutoCorrect plugin.
        /// </summary>
        internal static string ResolveProjectDir(string projectsDir, string shortName, string projectId)
        {
            if (string.IsNullOrEmpty(projectsDir) || !Directory.Exists(projectsDir)) return null;

            string byName = null;
            if (!string.IsNullOrEmpty(shortName))
            {
                string candidate = Path.Combine(projectsDir, shortName);
                string settings = Path.Combine(candidate, "Settings.xml");
                if (File.Exists(settings))
                {
                    byName = candidate;
                    string guid = ReadProjectGuid(settings);
                    if (guid == null || string.IsNullOrEmpty(projectId) || SameId(guid, projectId))
                        return candidate;
                }
            }

            if (string.IsNullOrEmpty(projectId)) return byName;
            try
            {
                foreach (string dir in Directory.GetDirectories(projectsDir))
                {
                    string settings = Path.Combine(dir, "Settings.xml");
                    if (File.Exists(settings) && SameId(ReadProjectGuid(settings), projectId))
                        return dir;
                }
            }
            catch (Exception) { }
            return byName;
        }

        private static bool SameId(string a, string b)
        {
            return a != null && b != null && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        internal static string ReadProjectGuid(string settingsPath)
        {
            try
            {
                var xmlSettings = new XmlReaderSettings();
                xmlSettings.DtdProcessing = DtdProcessing.Ignore;
                using (XmlReader reader = XmlReader.Create(settingsPath, xmlSettings))
                {
                    if (reader.ReadToFollowing("Guid"))
                    {
                        string guid = reader.ReadElementContentAsString();
                        return string.IsNullOrEmpty(guid) ? null : guid.Trim();
                    }
                }
            }
            catch (Exception) { }
            return null;
        }
    }

    /// <summary>
    /// The plugin API's view of a project, for reading only. Text comes through Paratext
    /// (GetUSFM), so it is what Paratext's own editor shows.
    /// </summary>
    internal sealed class ParatextBookSource : IBookSource
    {
        private readonly IPluginHost host;
        private readonly IProject project;
        private readonly List<BookId> books;
        private string projectDirectory;
        private bool projectDirectoryResolved;

        public ParatextBookSource(IPluginHost host, IProject project)
        {
            this.host = host;
            this.project = project;
            books = project.AvailableBooks.OrderBy(b => b.Number).Select(b => new BookId(b.Number, b.Code)).ToList();
        }

        public string ProjectName
        {
            get { return project.ShortName; }
        }

        public IList<BookId> Books
        {
            get { return books; }
        }

        public string GetBook(int bookNum)
        {
            return project.GetUSFM(bookNum, 0);
        }

        public bool MapVerse(string versification, int bookNum, int chapter, int verse, out int newChapter, out int newVerse)
        {
            newChapter = chapter;
            newVerse = verse;
            if (host == null) return false;
            try
            {
                StandardScrVersType type = versification == "org" ? StandardScrVersType.Original : StandardScrVersType.English;
                IVersification from = host.GetStandardVersification(type);
                IVersification to = project.Versification;
                if (from == null || to == null) return false;
                IVerseRef mapped = from.CreateReference(bookNum, chapter, verse).ChangeVersification(to);
                if (mapped == null || mapped.ChapterNum <= 0) return false;
                newChapter = mapped.ChapterNum;
                newVerse = mapped.VerseNum > 0 ? mapped.VerseNum : verse;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>The project's folder (null when it cannot be found).</summary>
        public string ProjectDirectory
        {
            get
            {
                if (!projectDirectoryResolved)
                {
                    projectDirectory = SpecialFeaturesPlugin.ResolveProjectDir(
                        SpecialFeaturesPlugin.ProjectsDirectory(), project.ShortName, project.ID);
                    projectDirectoryResolved = true;
                }
                return projectDirectory;
            }
        }

        /// <summary>Moves the project's open text window (or a new one) to the verse.</summary>
        public void GoTo(int bookNum, int chapter, int verse)
        {
            IVerseRef reference = project.Versification.CreateReference(bookNum, chapter, verse);
            host.OpenTextWindowFor(project, OpenWindowBehavior.UseExisting, reference);
        }
    }
}
