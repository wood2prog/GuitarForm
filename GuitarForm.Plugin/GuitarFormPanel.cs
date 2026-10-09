using System;
using System.IO;
using System.Runtime.InteropServices;
using Eto.Forms;
using GuitarForm.Data;
using Rhino;

namespace GuitarForm.Plugin
{
    // The dockable GuitarForm panel. For now it only shows whether the design library could be opened.
    [Guid("b27065af-ab08-4c16-aeef-415d7616b93c")]
    public class GuitarFormPanel : Panel
    {
        static string DefaultLibraryPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GuitarForm", "GuitarForm.db");

        public GuitarFormPanel()
        {
            Content = new StackLayout
            {
                Padding = 10,
                Spacing = 6,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Items = { new Label { Text = LibraryStatus(), Wrap = WrapMode.Word } },
            };
        }

        static string LibraryStatus()
        {
            string path = DefaultLibraryPath;
            try
            {
                using var library = Library.Open(path);
                RhinoApp.WriteLine($"GuitarForm: opened library {path} (SQLite {library.SqliteVersion}).");
                return $"Library: {path}\nSQLite {library.SqliteVersion}";
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine($"GuitarForm: couldn't open library {path}: {e}");
                return $"Couldn't open the library at {path}:\n{e.Message}";
            }
        }
    }
}
