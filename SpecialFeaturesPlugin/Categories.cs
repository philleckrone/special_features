using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SpecialFeatures
{
    /// <summary>
    /// A kind of special layout: its names, the USFM markers suggested for it, and the markers
    /// that count as "already formatted" when one of them is found in the place's verses.
    /// </summary>
    /// <summary>The interface languages.</summary>
    public enum Lang
    {
        English,
        Spanish,
        French,
    }

    public sealed class Category
    {
        public readonly string Key;
        public readonly string NameEn;
        public readonly string NameEs;
        public readonly string NameFr;
        public readonly string Suggested;
        /// <summary>Words only in quotation marks are worth telling apart from plain text (a character style is usual).</summary>
        public readonly bool QuotesCount;
        private readonly Regex expected;

        public Category(string key, string nameEn, string nameEs, string nameFr, string suggested, string expectedPattern, bool quotesCount = false)
        {
            Key = key;
            NameEn = nameEn;
            NameEs = nameEs;
            NameFr = nameFr;
            Suggested = suggested;
            QuotesCount = quotesCount;
            expected = new Regex("^(?:" + expectedPattern + ")$", RegexOptions.CultureInvariant);
        }

        public string Name(Lang lang)
        {
            switch (lang)
            {
                case Lang.Spanish: return NameEs;
                case Lang.French: return NameFr;
                default: return NameEn;
            }
        }

        /// <summary>Whether the marker (without backslash, + or *) is one this kind of place is formatted with.</summary>
        public bool IsExpected(string marker)
        {
            return expected.IsMatch(marker);
        }
    }

    public static class Categories
    {
        // Indented list and poetry paragraphs, and tables: what genealogies and lists are formatted with.
        private const string ListMarkers = @"li\d*|lh|lf|lim\d*|lik|liv\d*|litl|q\d*|pi\d*|ph\d*|mi|pm|pmo|pmc|pmr|tr|th\d*|thr\d*|thc\d*|tc\d*|tcr\d*|tcc\d*";
        private const string IndentMarkers = @"pi\d*|li\d*|lim\d*|lh|lf|q\d*|mi|pm|pmo|pmc|pmr|ph\d*";

        /// <summary>In the order the window lists them.</summary>
        public static readonly IList<Category> All = new List<Category>
        {
            new Category("genealogy", "Genealogy", "Genealog\u00EDa", "G\u00E9n\u00E9alogie",
                @"\li1  \li2  \li3  (\lh before, \lf after)", ListMarkers),
            new Category("list", "List", "Lista", "Liste",
                @"\li1  \li2  \lim1  \lh  \lf  \lik...\lik*  \liv...\liv*", ListMarkers),
            new Category("table", "Table", "Tabla", "Tableau",
                @"\tr  \th1  \th2  \tc1  \tcr2", @"tr|th\d*|thr\d*|thc\d*|tc\d*|tcr\d*|tcc\d*|li\d*|lim\d*"),
            new Category("message", "Letter, decree or message", "Carta, decreto o mensaje", "Lettre, d\u00E9cret ou message",
                @"\pmo  \pm  \pmc  \pmr", IndentMarkers + @"|qm\d*"),
            new Category("prayer", "Prayer", "Oraci\u00F3n", "Pri\u00E8re",
                @"\pi1  \pmo  \pm  \pmc", IndentMarkers),
            new Category("prose", "Special prose", "Prosa especial", "Prose sp\u00E9ciale",
                @"\pi1  \mi  \li1", IndentMarkers),
            new Category("creation", "Days of creation", "D\u00EDas de la creaci\u00F3n", "Jours de la cr\u00E9ation",
                @"\pi1  \li1  \q1", IndentMarkers),
            new Category("decalogue", "Ten Commandments", "Diez Mandamientos", "Dix commandements",
                @"\li1  \pi1  \q1", IndentMarkers),
            new Category("inscription", "Inscription", "Inscripci\u00F3n", "Inscription",
                @"\pc  \qc  \sc...\sc*", @"pc|qc|sc|tl|bd|bdit|em|qt\d*|pmo|pm|mi", true),
            new Category("centered", "Centered text", "Texto centrado", "Texte centr\u00E9",
                @"\qc  \pc  \qd", @"qc|pc|qd|qr"),
            new Category("acrostic", "Hebrew letters (acrostic)", "Letras hebreas (acr\u00F3stico)", "Lettres h\u00E9bra\u00EFques (acrostiche)",
                @"\qa", @"qa"),
            new Category("speaker", "Speakers named", "Se nombra a quien habla", "Locuteurs nomm\u00E9s",
                @"\sp", @"sp"),
            new Category("refrain", "Refrain", "Estribillo", "Refrain",
                @"\qr  \q2  \it...\it*", @"qr|it|em|qs|q2"),
            new Category("numbered", "Numbered sayings", "Dichos numerados", "Paroles num\u00E9rot\u00E9es",
                @"\s2  \li1", @"s\d*|li\d*|sp"),
            new Category("outline", "Major divisions", "Divisiones principales", "Grandes divisions",
                @"\ms1  \mr  \ms2", @"ms\d*|mr"),
            new Category("division", "Minor divisions", "Divisiones menores", "Petites divisions",
                @"\s2  \b  \ms2", @"s\d*|b|ms\d*|sr|sd\d*"),
            new Category("textual", "Textual note", "Nota textual", "Note textuelle",
                @"\s1  \r  \b  \f...\f*", @"s\d*|r|b|ms\d*|f|fe|d"),
            new Category("otquote", "Old Testament quotation", "Cita del Antiguo Testamento", "Citation de l'Ancien Testament",
                @"\qt...\qt*  \q1  \q2", @"qt|q\d*|qm\d*|pi\d*", true),
        };

        public static Category Get(string key)
        {
            foreach (Category c in All)
                if (c.Key == key) return c;
            return null;
        }

        public static int IndexOf(Category category)
        {
            return All.IndexOf(category);
        }
    }
}
