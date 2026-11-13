using System;
using System.Collections.Generic;

namespace SpecialFeatures
{
    public sealed class BookId
    {
        public readonly int Number;
        public readonly string Code;

        public BookId(int number, string code)
        {
            Number = number;
            Code = code;
        }
    }

    /// <summary>
    /// What the tool needs from a project. ParatextBookSource wraps the plugin API's IProject;
    /// the tests use a folder of SFM files. The tool only reads.
    /// </summary>
    public interface IBookSource
    {
        string ProjectName { get; }

        /// <summary>The project's folder (for the settings file), or null.</summary>
        string ProjectDirectory { get; }

        /// <summary>The project's books, in canonical order.</summary>
        IList<BookId> Books { get; }

        /// <summary>The whole book, or null.</summary>
        string GetBook(int bookNum);

        /// <summary>
        /// Converts a reference written in a standard versification ("eng" or "org") to the
        /// project's versification. Returns false when it cannot.
        /// </summary>
        bool MapVerse(string versification, int bookNum, int chapter, int verse, out int newChapter, out int newVerse);

        /// <summary>Moves the project's text window to the verse (does nothing when it cannot).</summary>
        void GoTo(int bookNum, int chapter, int verse);
    }

    /// <summary>The 66 books' codes in Paratext's numbering (GEN = 1).</summary>
    public static class Books
    {
        private static readonly string[] Codes =
        {
            "GEN", "EXO", "LEV", "NUM", "DEU", "JOS", "JDG", "RUT", "1SA", "2SA", "1KI", "2KI", "1CH", "2CH",
            "EZR", "NEH", "EST", "JOB", "PSA", "PRO", "ECC", "SNG", "ISA", "JER", "LAM", "EZK", "DAN", "HOS",
            "JOL", "AMO", "OBA", "JON", "MIC", "NAM", "HAB", "ZEP", "HAG", "ZEC", "MAL",
            "MAT", "MRK", "LUK", "JHN", "ACT", "ROM", "1CO", "2CO", "GAL", "EPH", "PHP", "COL", "1TH", "2TH",
            "1TI", "2TI", "TIT", "PHM", "HEB", "JAS", "1PE", "2PE", "1JN", "2JN", "3JN", "JUD", "REV",
        };

        /// <summary>The first New Testament book (MAT).</summary>
        public const int FirstNT = 40;

        public static int Number(string code)
        {
            for (int i = 0; i < Codes.Length; i++)
                if (string.Equals(Codes[i], code, StringComparison.OrdinalIgnoreCase)) return i + 1;
            return 0;
        }

        public static string Code(int number)
        {
            return number >= 1 && number <= Codes.Length ? Codes[number - 1] : number.ToString();
        }
    }
}
