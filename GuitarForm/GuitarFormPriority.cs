using Grasshopper;
using Grasshopper.Kernel;

namespace GuitarForm
{
    // Runs when Grasshopper loads the plug-in, before the components are added: gives the GuitarForm toolbar tab its icon.
    public class GuitarFormPriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.ComponentServer.AddCategoryIcon("GuitarForm", Icons.Load("GuitarFormTab.png"));
            Instances.ComponentServer.AddCategorySymbolName("GuitarForm", 'G');
            return GH_LoadingInstruction.Proceed;
        }
    }
}
