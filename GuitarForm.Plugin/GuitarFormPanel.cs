using System;
using System.Runtime.InteropServices;
using Eto.Forms;
using Rhino;

namespace GuitarForm.Plugin
{
    // The dockable GuitarForm panel. For now it only shows whether the design library could be opened.
    [Guid("b27065af-ab08-4c16-aeef-415d7616b93c")]
    public class GuitarFormPanel : Panel
    {
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
            string path = Library.DefaultPath;
            try
            {
                using var connection = Library.Open(path);
                string status = $"Library: {path}\nSQLite {connection.ServerVersion}";
                RhinoApp.WriteLine($"GuitarForm: opened library {path} (SQLite {connection.ServerVersion}).");
                return status;
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine($"GuitarForm: couldn't open library {path}: {e}");
                return $"Couldn't open the library at {path}:\n{e.Message}";
            }
        }
    }
}
