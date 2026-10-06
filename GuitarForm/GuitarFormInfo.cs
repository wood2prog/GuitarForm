using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace GuitarForm
{
    public class GuitarFormInfo : GH_AssemblyInfo
    {
        public override string Name => "GuitarForm";
        public override Bitmap Icon => null;
        public override string Description => "Guitar designer components for Grasshopper.";
        public override Guid Id => new Guid("154dc1a5-f40f-413f-9688-0c8aa64de4f7");
        public override string AuthorName => "";
        public override string AuthorContact => "";
    }
}
