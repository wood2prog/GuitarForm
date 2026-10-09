using Rhino;
using Rhino.Commands;
using Rhino.UI;

namespace GuitarForm
{
    // Opens the GuitarForm panel.
    public class GuitarFormCommand : Command
    {
        public override string EnglishName => "GuitarForm";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Panels.OpenPanel(typeof(GuitarFormPanel).GUID);
            return Result.Success;
        }
    }
}
