using System.Drawing;
using System.Reflection;
using Rhino.PlugIns;
using Rhino.UI;

namespace GuitarForm.Plugin
{
    public class GuitarFormPlugin : PlugIn
    {
        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            Panels.RegisterPanel(this, typeof(GuitarFormPanel), "GuitarForm", LoadIcon());
            return LoadReturnCode.Success;
        }

        static System.Drawing.Icon LoadIcon()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GuitarForm.Plugin.GuitarForm.png");
            using var bitmap = new Bitmap(stream);
            return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }
    }
}
