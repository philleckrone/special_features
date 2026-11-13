using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpecialFeatures
{
    /// <summary>
    /// The window's text in English, Spanish or French, picked from Paratext's interface language.
    /// Keep this file ASCII: run tools\asciify_cs.py after typing accented letters.
    /// </summary>
    public sealed class Strings
    {
        /// <summary>The tool's version: shown in the window title and reported to Paratext.</summary>
        public const string ToolVersion = "1.7";

        public Lang Language;
        public string MenuText;
        public string Title;
        public string Description;
        public string ProjectLabel;
        public string Intro;
        public string Scanning;
        public string Summary;
        public string NothingShown;
        public string ColStatus;
        public string ColReference;
        public string ColCategory;
        public string ColDescription;
        public string ColPublications;
        public string ColSuggested;
        public string ColFound;
        public string StatusPlain;
        public string StatusFormatted;
        public string StatusNotDrafted;
        public string StatusQuoteMarks;
        public string BooksLabel;
        public string BooksAll;
        public string BooksOT;
        public string BooksNT;
        public string CategoryLabel;
        public string CategoryAll;
        public string PublicationsLabel;
        public string PublicationsHint;
        public string PubQuotations;
        public string HideFormatted;
        public string GoToButton;
        public string Rescan;
        public string Close;
        public string FoundOther;
        public string TipStatus;
        public string TipSuggested;
        public string TipFound;
        public string TipFromPaper;
        public string OpenProjectFirst;
        public string Error;
        public string AddedButton;
        public string AddedTitle;
        public string AddedIntro;
        public string ColBook;
        public string ColSource;
        public string AddButton;
        public string EditButton;
        public string DeleteButton;
        public string ImportButton;
        public string ExportButton;
        public string EditTitle;
        public string LabelBook;
        public string LabelReferences;
        public string ReferencesHelp;
        public string LabelKind;
        public string LabelDescription;
        public string LabelSource;
        public string SourceHelp;
        public string LabelVersification;
        public string VrsEnglish;
        public string VrsOriginal;
        public string Ok;
        public string Cancel;
        public string BadReferences;
        public string ConfirmDelete;
        public string ImportDone;
        public string ExportDone;
        public string FileFilter;
        public string LoadErrorText;
        public string HideSelected;
        public string UnhideSelected;
        public string ShowHidden;
        public string HiddenSuffix;
        public string SummaryHidden;
        public string TipHidden;
        public string KindsButton;
        public string KindsTip;
        public string KindsTitle;
        public string KindsIntro;
        public string ChooseAll;
        public string ChooseNone;
        public string KindsNone;

        public static Strings For(string locale)
        {
            switch (LanguageOf(locale))
            {
                case Lang.Spanish: return MakeSpanish();
                case Lang.French: return MakeFrench();
                default: return English();
            }
        }

        /// <summary>The interface language for a Paratext locale: "es..." Spanish, "fr..." French, anything else English.</summary>
        public static Lang LanguageOf(string locale)
        {
            string lang = string.IsNullOrEmpty(locale) ? "" : locale.ToLowerInvariant();
            if (lang.StartsWith("es", StringComparison.Ordinal)) return Lang.Spanish;
            if (lang.StartsWith("fr", StringComparison.Ordinal)) return Lang.French;
            return Lang.English;
        }

        public string StatusName(FeatureStatus status)
        {
            switch (status)
            {
                case FeatureStatus.Formatted: return StatusFormatted;
                case FeatureStatus.NotDrafted: return StatusNotDrafted;
                case FeatureStatus.QuoteMarks: return StatusQuoteMarks;
                default: return StatusPlain;
            }
        }

        /// <summary>The name shown for a publication: the quotations' key gets a readable name.</summary>
        public string PublicationName(string key)
        {
            return string.Equals(key, FeatureCatalog.QuotationSource, StringComparison.OrdinalIgnoreCase) ? PubQuotations : key;
        }

        /// <summary>The publications of a place, as shown.</summary>
        public string PublicationsText(IEnumerable<string> keys)
        {
            return string.Join(", ", keys.Select(PublicationName).ToArray());
        }

        /// <summary>The markers found in a place: those of its kind (or the quoted words) first, then the others.</summary>
        public string FoundText(FeatureResult r)
        {
            string found = r.Status == FeatureStatus.QuoteMarks ? r.Quoted
                : string.Join(" ", r.ExpectedFound.Select(m => "\\" + m).ToArray());
            if (r.OtherFound.Count > 0)
            {
                string other = string.Format(FoundOther, string.Join(" ", r.OtherFound.Select(m => "\\" + m).ToArray()));
                found = found.Length > 0 ? found + "  (" + other + ")" : "(" + other + ")";
            }
            return found;
        }

        /// <summary>Everything about a place, for tooltips.</summary>
        public string Details(Feature f, FeatureResult r)
        {
            var sb = new StringBuilder();
            sb.Append(f.Category.Name(Language)).Append(", ").Append(f.Reference)
              .Append(" (").Append(PublicationsText(f.Publications)).Append(")\n");
            sb.Append(f.Description(Language)).Append('\n');
            sb.Append(string.Format(TipStatus, StatusName(r.Status))).Append('\n');
            sb.Append(string.Format(TipSuggested, f.Category.Suggested));
            string found = FoundText(r);
            if (found.Length > 0) sb.Append('\n').Append(string.Format(TipFound, found));
            if (f.Sources.Count > 1)
                sb.Append("\n\n").Append(TipFromPaper).Append('\n').Append(f.SourceDetails(this));
            return sb.ToString();
        }

        public static Strings English()
        {
            var s = new Strings();
            s.MenuText = "Special Features...";
            s.Title = "Special Features";
            s.Description = "Points out the places where published Bibles often use special formatting (genealogies, lists, tables, letters, prayers, inscriptions, acrostics, centered text...), shows whether the project already formats them, and suggests USFM markers.";
            s.ProjectLabel = "Project: {0}";
            s.Intro = "These places come from the paper \"Special Layout Features found in Old and New Testaments\" (2002, revised 2004), which compared the NIV, NVI, NLT, GNB, TEV, BFC and CEV. " +
                "\"OT quotations\" adds the places where the New Testament quotes the Old Testament. " +
                "Entries of the same kind whose verses overlap are shown as one place. " +
                "\"Plain paragraphs\" means none of the markers this kind of place is usually formatted with is used there yet. " +
                "Double-click a row to go to it.";
            s.Scanning = "Reading {0}...";
            s.Summary = "{0} places shown: {1} plain paragraphs, {2} quotation marks only, {3} with special formatting, {4} not drafted.";
            s.NothingShown = "No place matches the filters.";
            s.ColStatus = "Status";
            s.ColReference = "Reference";
            s.ColCategory = "Kind";
            s.ColDescription = "Description";
            s.ColPublications = "Publications";
            s.ColSuggested = "Suggested markers";
            s.ColFound = "Markers found";
            s.StatusPlain = "Plain paragraphs";
            s.StatusQuoteMarks = "Quotation marks only";
            s.StatusFormatted = "Has special formatting";
            s.StatusNotDrafted = "Not drafted";
            s.BooksLabel = "Books:";
            s.BooksAll = "All books";
            s.BooksOT = "Old Testament";
            s.BooksNT = "New Testament";
            s.CategoryLabel = "Kind:";
            s.CategoryAll = "All kinds";
            s.PublicationsLabel = "Formatted specially in:";
            s.PublicationsHint = "A place is shown when at least one of its publications is ticked.";
            s.PubQuotations = "OT quotations";
            s.HideFormatted = "Hide places that already have special formatting";
            s.GoToButton = "Go to in Paratext";
            s.Rescan = "Read the text again";
            s.Close = "Close";
            s.FoundOther = "other: {0}";
            s.TipStatus = "Status: {0}";
            s.TipSuggested = "Suggested: {0}";
            s.TipFound = "Found: {0}";
            s.TipFromPaper = "In the paper:";
            s.OpenProjectFirst = "Open a project's text window first, then choose Tools > Special Features... there.";
            s.Error = "Special Features could not continue:\n{0}";
            s.AddedButton = "Added places...";
            s.AddedTitle = "Added places";
            s.AddedIntro = "Places you add here are listed with the paper's places in every project on this computer. Give each one a source name (for example a country or region) to filter by it. Export saves the list to a file you can send to others; Import adds the places of such a file.\nSaved in: {0}";
            s.ColBook = "Book";
            s.ColSource = "Source";
            s.AddButton = "Add...";
            s.EditButton = "Edit...";
            s.DeleteButton = "Delete";
            s.ImportButton = "Import...";
            s.ExportButton = "Export...";
            s.EditTitle = "Place";
            s.LabelBook = "Book:";
            s.LabelReferences = "References:";
            s.ReferencesHelp = "Examples: 3 (whole chapter), 3-4 (chapters), 3:1-12, 1:3-2:4, 10:2-18,22-29; 11:10-26";
            s.LabelKind = "Kind:";
            s.LabelDescription = "Description:";
            s.LabelSource = "Source:";
            s.SourceHelp = "Who formats it this way, for example a country, region or publication.";
            s.LabelVersification = "Versification of the references:";
            s.VrsEnglish = "English";
            s.VrsOriginal = "Original (Hebrew/Greek)";
            s.Ok = "OK";
            s.Cancel = "Cancel";
            s.BadReferences = "The references cannot be read: {0}";
            s.ConfirmDelete = "Delete {0}?";
            s.ImportDone = "{0} places added; {1} were already in the list.";
            s.ExportDone = "{0} places saved to {1}.";
            s.FileFilter = "Special Features list (*.tsv)|*.tsv|All files (*.*)|*.*";
            s.LoadErrorText = "The added places could not be read from {0}:\n{1}";
            s.HideSelected = "Hide selected";
            s.UnhideSelected = "Show selected again";
            s.ShowHidden = "Show hidden places ({0})";
            s.HiddenSuffix = " (hidden)";
            s.SummaryHidden = " {0} hidden.";
            s.TipHidden = "You hid this place in this project.";
            s.KindsButton = "Kinds: {0} of {1}...";
            s.KindsTip = "Not shown: {0}";
            s.KindsTitle = "Kinds of places";
            s.KindsIntro = "Tick the kinds of places to list.";
            s.ChooseAll = "All";
            s.ChooseNone = "None";
            s.KindsNone = "Tick at least one kind.";
            return s;
        }

        public static Strings MakeSpanish()
        {
            var s = new Strings();
            s.Language = Lang.Spanish;
            s.MenuText = "Rasgos especiales...";
            s.Title = "Rasgos especiales";
            s.Description = "Indica los lugares donde las Biblias publicadas suelen usar un formato especial (genealog\u00EDas, listas, tablas, cartas, oraciones, inscripciones, acr\u00F3sticos, texto centrado...), muestra si el proyecto ya les da formato y sugiere marcadores USFM.";
            s.ProjectLabel = "Proyecto: {0}";
            s.Intro = "Estos lugares vienen del estudio \"Special Layout Features found in Old and New Testaments\" (2002, revisado en 2004), que compar\u00F3 la NIV, NVI, NLT, GNB, TEV, BFC y CEV. " +
                "\"Citas del AT\" a\u00F1ade los lugares donde el Nuevo Testamento cita el Antiguo. " +
                "Las entradas del mismo tipo cuyos vers\u00EDculos se solapan se muestran como un solo lugar. " +
                "\"P\u00E1rrafos sencillos\" quiere decir que all\u00ED todav\u00EDa no se usa ninguno de los marcadores con que se suele dar formato a ese tipo de lugar. " +
                "Haga doble clic en una fila para ir a ella.";
            s.Scanning = "Leyendo {0}...";
            s.Summary = "{0} lugares mostrados: {1} con p\u00E1rrafos sencillos, {2} solo con comillas, {3} con formato especial, {4} sin borrador.";
            s.NothingShown = "Ning\u00FAn lugar coincide con los filtros.";
            s.ColStatus = "Estado";
            s.ColReference = "Referencia";
            s.ColCategory = "Tipo";
            s.ColDescription = "Descripci\u00F3n";
            s.ColPublications = "Publicaciones";
            s.ColSuggested = "Marcadores sugeridos";
            s.ColFound = "Marcadores encontrados";
            s.StatusPlain = "P\u00E1rrafos sencillos";
            s.StatusQuoteMarks = "Solo comillas";
            s.StatusFormatted = "Tiene formato especial";
            s.StatusNotDrafted = "Sin borrador";
            s.BooksLabel = "Libros:";
            s.BooksAll = "Todos los libros";
            s.BooksOT = "Antiguo Testamento";
            s.BooksNT = "Nuevo Testamento";
            s.CategoryLabel = "Tipo:";
            s.CategoryAll = "Todos los tipos";
            s.PublicationsLabel = "Con formato especial en:";
            s.PublicationsHint = "Se muestra un lugar cuando al menos una de sus publicaciones est\u00E1 marcada.";
            s.PubQuotations = "Citas del AT";
            s.HideFormatted = "Ocultar los lugares que ya tienen formato especial";
            s.GoToButton = "Ir en Paratext";
            s.Rescan = "Volver a leer el texto";
            s.Close = "Cerrar";
            s.FoundOther = "otros: {0}";
            s.TipStatus = "Estado: {0}";
            s.TipSuggested = "Sugerido: {0}";
            s.TipFound = "Encontrado: {0}";
            s.TipFromPaper = "En el estudio:";
            s.OpenProjectFirst = "Abra primero la ventana de texto de un proyecto y elija all\u00ED Herramientas > Rasgos especiales...";
            s.Error = "Rasgos especiales no pudo continuar:\n{0}";
            s.AddedButton = "Lugares a\u00F1adidos...";
            s.AddedTitle = "Lugares a\u00F1adidos";
            s.AddedIntro = "Los lugares que a\u00F1ada aqu\u00ED aparecen junto con los del estudio en todos los proyectos de este equipo. D\u00E9 a cada uno un nombre de origen (por ejemplo, un pa\u00EDs o una regi\u00F3n) para poder filtrar por \u00E9l. Exportar guarda la lista en un archivo que puede enviar a otros; Importar a\u00F1ade los lugares de un archivo as\u00ED.\nSe guarda en: {0}";
            s.ColBook = "Libro";
            s.ColSource = "Origen";
            s.AddButton = "A\u00F1adir...";
            s.EditButton = "Modificar...";
            s.DeleteButton = "Eliminar";
            s.ImportButton = "Importar...";
            s.ExportButton = "Exportar...";
            s.EditTitle = "Lugar";
            s.LabelBook = "Libro:";
            s.LabelReferences = "Referencias:";
            s.ReferencesHelp = "Ejemplos: 3 (cap\u00EDtulo entero), 3-4 (cap\u00EDtulos), 3:1-12, 1:3-2:4, 10:2-18,22-29; 11:10-26";
            s.LabelKind = "Tipo:";
            s.LabelDescription = "Descripci\u00F3n:";
            s.LabelSource = "Origen:";
            s.SourceHelp = "Qui\u00E9n le da este formato, por ejemplo un pa\u00EDs, una regi\u00F3n o una publicaci\u00F3n.";
            s.LabelVersification = "Versificaci\u00F3n de las referencias:";
            s.VrsEnglish = "Inglesa";
            s.VrsOriginal = "Original (hebreo/griego)";
            s.Ok = "Aceptar";
            s.Cancel = "Cancelar";
            s.BadReferences = "No se pueden leer las referencias: {0}";
            s.ConfirmDelete = "\u00BFEliminar {0}?";
            s.ImportDone = "Se a\u00F1adieron {0} lugares; {1} ya estaban en la lista.";
            s.ExportDone = "Se guardaron {0} lugares en {1}.";
            s.FileFilter = "Lista de Rasgos especiales (*.tsv)|*.tsv|Todos los archivos (*.*)|*.*";
            s.LoadErrorText = "No se pudieron leer los lugares a\u00F1adidos de {0}:\n{1}";
            s.HideSelected = "Ocultar los seleccionados";
            s.UnhideSelected = "Volver a mostrar los seleccionados";
            s.ShowHidden = "Mostrar los lugares ocultos ({0})";
            s.HiddenSuffix = " (oculto)";
            s.SummaryHidden = " {0} ocultos.";
            s.TipHidden = "Usted ocult\u00F3 este lugar en este proyecto.";
            s.KindsButton = "Tipos: {0} de {1}...";
            s.KindsTip = "No se muestran: {0}";
            s.KindsTitle = "Tipos de lugares";
            s.KindsIntro = "Marque los tipos de lugares que se deben mostrar.";
            s.ChooseAll = "Todos";
            s.ChooseNone = "Ninguno";
            s.KindsNone = "Marque al menos un tipo.";
            return s;
        }

        public static Strings MakeFrench()
        {
            var s = new Strings();
            s.Language = Lang.French;
            s.MenuText = "Mises en forme sp\u00E9ciales...";
            s.Title = "Mises en forme sp\u00E9ciales";
            s.Description = "Signale les endroits o\u00F9 les Bibles publi\u00E9es utilisent souvent une mise en forme sp\u00E9ciale (g\u00E9n\u00E9alogies, listes, tableaux, lettres, pri\u00E8res, inscriptions, acrostiches, texte centr\u00E9...), indique si le projet les met d\u00E9j\u00E0 en forme et sugg\u00E8re des marqueurs USFM.";
            s.ProjectLabel = "Projet : {0}";
            s.Intro = "Ces endroits viennent de l'\u00E9tude \u00AB Special Layout Features found in Old and New Testaments \u00BB (2002, r\u00E9vis\u00E9e en 2004), qui a compar\u00E9 la NIV, la NVI, la NLT, la GNB, la TEV, la BFC et la CEV. " +
                "\u00AB Citations de l'AT \u00BB ajoute les endroits o\u00F9 le Nouveau Testament cite l'Ancien. " +
                "Les entr\u00E9es du m\u00EAme type dont les versets se chevauchent sont affich\u00E9es comme un seul endroit. " +
                "\u00AB Paragraphes simples \u00BB signifie qu'aucun des marqueurs habituellement utilis\u00E9s pour ce type d'endroit n'y est encore employ\u00E9. " +
                "Double-cliquez sur une ligne pour y aller.";
            s.Scanning = "Lecture de {0}...";
            s.Summary = "{0} endroits affich\u00E9s : {1} en paragraphes simples, {2} avec guillemets seulement, {3} avec une mise en forme sp\u00E9ciale, {4} pas encore r\u00E9dig\u00E9s.";
            s.NothingShown = "Aucun endroit ne correspond aux filtres.";
            s.ColStatus = "\u00C9tat";
            s.ColReference = "R\u00E9f\u00E9rence";
            s.ColCategory = "Type";
            s.ColDescription = "Description";
            s.ColPublications = "Publications";
            s.ColSuggested = "Marqueurs sugg\u00E9r\u00E9s";
            s.ColFound = "Marqueurs trouv\u00E9s";
            s.StatusPlain = "Paragraphes simples";
            s.StatusQuoteMarks = "Guillemets seulement";
            s.StatusFormatted = "A une mise en forme sp\u00E9ciale";
            s.StatusNotDrafted = "Pas encore r\u00E9dig\u00E9";
            s.BooksLabel = "Livres :";
            s.BooksAll = "Tous les livres";
            s.BooksOT = "Ancien Testament";
            s.BooksNT = "Nouveau Testament";
            s.CategoryLabel = "Type :";
            s.CategoryAll = "Tous les types";
            s.PublicationsLabel = "Mis en forme sp\u00E9cialement dans :";
            s.PublicationsHint = "Un endroit est affich\u00E9 quand au moins une de ses publications est coch\u00E9e.";
            s.PubQuotations = "Citations de l'AT";
            s.HideFormatted = "Masquer les endroits qui ont d\u00E9j\u00E0 une mise en forme sp\u00E9ciale";
            s.GoToButton = "Aller dans Paratext";
            s.Rescan = "Relire le texte";
            s.Close = "Fermer";
            s.FoundOther = "autres : {0}";
            s.TipStatus = "\u00C9tat : {0}";
            s.TipSuggested = "Sugg\u00E9r\u00E9 : {0}";
            s.TipFound = "Trouv\u00E9 : {0}";
            s.TipFromPaper = "Dans l'\u00E9tude :";
            s.OpenProjectFirst = "Ouvrez d'abord la fen\u00EAtre de texte d'un projet, puis choisissez-y Outils > Mises en forme sp\u00E9ciales...";
            s.Error = "L'outil Mises en forme sp\u00E9ciales n'a pas pu continuer :\n{0}";
            s.AddedButton = "Endroits ajout\u00E9s...";
            s.AddedTitle = "Endroits ajout\u00E9s";
            s.AddedIntro = "Les endroits que vous ajoutez ici sont affich\u00E9s avec ceux de l'\u00E9tude dans tous les projets de cet ordinateur. Donnez \u00E0 chacun un nom de source (par exemple un pays ou une r\u00E9gion) pour pouvoir filtrer par celui-ci. Exporter enregistre la liste dans un fichier que vous pouvez envoyer \u00E0 d'autres ; Importer ajoute les endroits d'un tel fichier.\nEnregistr\u00E9 dans : {0}";
            s.ColBook = "Livre";
            s.ColSource = "Source";
            s.AddButton = "Ajouter...";
            s.EditButton = "Modifier...";
            s.DeleteButton = "Supprimer";
            s.ImportButton = "Importer...";
            s.ExportButton = "Exporter...";
            s.EditTitle = "Endroit";
            s.LabelBook = "Livre :";
            s.LabelReferences = "R\u00E9f\u00E9rences :";
            s.ReferencesHelp = "Exemples : 3 (chapitre entier), 3-4 (chapitres), 3:1-12, 1:3-2:4, 10:2-18,22-29; 11:10-26";
            s.LabelKind = "Type :";
            s.LabelDescription = "Description :";
            s.LabelSource = "Source :";
            s.SourceHelp = "Qui le met en forme ainsi, par exemple un pays, une r\u00E9gion ou une publication.";
            s.LabelVersification = "Versification des r\u00E9f\u00E9rences :";
            s.VrsEnglish = "Anglaise";
            s.VrsOriginal = "Originale (h\u00E9breu/grec)";
            s.Ok = "OK";
            s.Cancel = "Annuler";
            s.BadReferences = "Impossible de lire les r\u00E9f\u00E9rences : {0}";
            s.ConfirmDelete = "Supprimer {0} ?";
            s.ImportDone = "{0} endroits ajout\u00E9s ; {1} \u00E9taient d\u00E9j\u00E0 dans la liste.";
            s.ExportDone = "{0} endroits enregistr\u00E9s dans {1}.";
            s.FileFilter = "Liste Mises en forme sp\u00E9ciales (*.tsv)|*.tsv|Tous les fichiers (*.*)|*.*";
            s.LoadErrorText = "Impossible de lire les endroits ajout\u00E9s depuis {0} :\n{1}";
            s.HideSelected = "Masquer la s\u00E9lection";
            s.UnhideSelected = "R\u00E9afficher la s\u00E9lection";
            s.ShowHidden = "Afficher les endroits masqu\u00E9s ({0})";
            s.HiddenSuffix = " (masqu\u00E9)";
            s.SummaryHidden = " {0} masqu\u00E9s.";
            s.TipHidden = "Vous avez masqu\u00E9 cet endroit dans ce projet.";
            s.KindsButton = "Types : {0} sur {1}...";
            s.KindsTip = "Non affich\u00E9s : {0}";
            s.KindsTitle = "Types d'endroits";
            s.KindsIntro = "Cochez les types d'endroits \u00E0 afficher.";
            s.ChooseAll = "Tous";
            s.ChooseNone = "Aucun";
            s.KindsNone = "Cochez au moins un type.";
            return s;
        }
    }
}
