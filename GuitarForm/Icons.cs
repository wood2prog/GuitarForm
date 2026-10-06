using System.Drawing;
using System.Reflection;

namespace GuitarForm
{
    // Loads the 24x24 component icons embedded from Resources/Icons.
    static class Icons
    {
        public static Bitmap Load(string fileName)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"GuitarForm.Icons.{fileName}");
            return stream == null ? null : new Bitmap(stream);
        }
    }
}
