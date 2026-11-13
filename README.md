# Special Features

*This page is the English user guide, [SpecialFeatures_UserGuide.rtf](plugin/SpecialFeatures_UserGuide.rtf). The guide is also available in Spanish ([SpecialFeatures_UserGuide_sp.rtf](plugin/SpecialFeatures_UserGuide_sp.rtf)) and French ([SpecialFeatures_UserGuide_fr.rtf](plugin/SpecialFeatures_UserGuide_fr.rtf)).*

## Purpose

Special Features is a Paratext plugin. It points out the places where published Bibles often use special formatting: genealogies, lists, tables, letters and decrees, prayers, special prose, inscriptions, acrostics, centered lines, speaker names, major and minor divisions, textual notes, and Old Testament quotations in the New Testament. For each place it shows whether the project already formats it, and suggests USFM markers. It never changes the text.

The places come from the paper "Special Layout Features found in Old and New Testaments" (August 2002, revised April 2004). It compared seven publications: NIV, NVI, NLT, GNB, TEV, BFC (La Bible en français courant) and CEV. Entries of the same kind whose verses overlap or touch are shown as one place (for example, the Genesis 10 genealogy of the NIV, NVI and NLT is one place, GEN 10:2-31).

A second list gives the places where the New Testament quotes the Old Testament (MAT 1:23, Isaiah 7:14, and about 260 more). Its description is the Old Testament reference, and it has its own tick box, "OT quotations", beside the publications.

## Installing

1. Download Install_Phils_tools.exe from the Paratext_Plugins folder. If your browser warns about the file, choose to keep it.

2. Close Paratext.

3. Double-click Install_Phils_tools.exe. If Windows says it protected your PC, click "More info", then "Run anyway" (the installer is not signed yet).

4. Tick Special Features and click "Install / update ticked". It installs into every Paratext 9 on the computer. If Windows asks for permission, click Yes. The same window updates the plugin later (its status then says "Update available") or removes it ("Remove ticked"), and its "User guide" button opens this guide.

## The list: Tools > Special Features...

1. Open the project in a Scripture window and choose Tools > Special Features...

2. The window lists one row per place: its status in the project, the reference, the markers found there, the suggested markers, the description from the paper, the kind, and the publications that format it specially. Hover over a row to see everything, including each entry of the paper behind the place.

3. Double-click a row, or select it and click "Go to in Paratext", to go to its first verse. The window stays open beside Paratext.

4. Filters: the books (all, Old Testament, New Testament or one book), the kinds of place ("Kinds: 18 of 18..." opens a list where you untick the kinds you never want to see, for example Minor divisions), "Hide places that already have special formatting", and the publications. A place is shown when at least one of its publications is ticked. Untick all but NIV, for example, to see only what the NIV formats specially. Untick "OT quotations" to hide the quotations.

5. To hide places your team has decided about, select them (Ctrl+click or Shift+click for several) and click "Hide selected" or press Delete. Tick "Show hidden places (n)" to list them again in grey italic; select them and click "Show selected again" to bring them back. Hidden places are kept for this project only.

6. After editing the text, click "Read the text again" (the list also updates when Paratext saves a chapter).

The filters and hidden places are saved for each project in its local\SpecialFeatures folder, which Send/Receive does not share.

## Status

- Plain paragraphs (yellow): the verses have text, but none of the markers this kind of place is usually formatted with is used there yet.
- Has special formatting (green): at least one such marker is used in the verses, for example \li1 in a genealogy, \pc or \sc in an inscription, \qa in an acrostic psalm.
- Quotation marks only (blue): an inscription or Old Testament quotation that has none of those markers, but whose words are in quotation marks (“ ” « » „ “ ‹ › or straight "). "Markers found" then shows the first quoted words, and (+2) when there are more. Rows with only a mark of a quotation that began before the place show just the mark. These rows stay visible when "Hide places that already have special formatting" is ticked.
- Not drafted (grey): the book or chapter is missing, or the verses have no text.

"Markers found" first lists the markers of that kind, then (as "other") the other markers used in the verses. A marker just before a verse (a heading, \li1 before \v 2, \ms before \c) counts for that verse.

## Adding your own places

A country, region or team may format other passages in a special way. Click "Added places..." to keep a list of them:

- Add...: choose the book, type the references (3 = the whole chapter, 3-4, 3:1-12, 1:3-2:4, or several separated by commas and semicolons as in 10:2-18,22-29; 11:10-26), choose the kind (its markers decide the status and are the suggestion), type a description and a source name (for example "Peru"). The references are in English versification unless you choose Original.
- Edit... (or double-click) changes a place; Delete removes it.
- Export... saves the list to a file to send to others; Import... adds the places of such a file (places already in the list are skipped).

The added places are listed with the paper's places in every project on this computer (they are kept in %APPDATA%\ParatextSpecialFeatures\added.tsv). Each source name gets its own tick box beside NIV, NVI and the others, so you can show only your own places.

## Suggested markers

- Genealogy, list: \li1 \li2 \li3 (\lh before, \lf after; \lim, \lik, \liv).
- Table: \tr \th1 \th2 \tc1 \tcr2.
- Letter, decree or message: \pmo \pm \pmc \pmr.
- Prayer, special prose, days of creation, Ten Commandments: \pi1 \mi \li1 \q1 \pm.
- Inscription: \pc, \qc or \sc...\sc\*. Centered text: \qc \pc \qd.
- Hebrew letters (acrostic): \qa. Speakers named: \sp. Refrain: \qr, \q2 or \it...\it\*.
- Major divisions: \ms1 \mr. Minor divisions and numbered sayings: \s2, \b, \li1. Textual note: a heading (\s1, \r), \b, or a footnote.
- Old Testament quotation: \qt...\qt\* around the quoted words, or poetry lines (\q1 \q2) for a poetic quotation. \qt, \q, \qm or \pi there counts as formatted.

These are suggestions only: choose what fits the project's stylesheet and typesetting.

## Notes

- The references are written in English versification (a few BFC ones in Original). The plugin converts them to the project's versification, so, for example, Psalm 41:13 is found at 41:14 in a project with Original versification.
- A verse part in the paper (Gen 46:8b) counts as the whole verse.
- Features of the printed page only (the BFC prints the Psalms in one column, Job's narrative in two) are not listed.
