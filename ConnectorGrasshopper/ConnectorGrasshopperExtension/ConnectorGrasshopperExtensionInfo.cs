using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace ConnectorGrasshopperExtension
{
    public class ConnectorGrasshopperExtensionInfo : GH_AssemblyInfo
    {
        public override string Name => "ConnectorGrasshopperExtension";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "An extension to the Speckle plugin to add query and update possibilities.";

        public override Guid Id => new Guid("19178D70-11F0-432F-9E51-915F5706BC72");

        //Return a string identifying you or your company.
        public override string AuthorName => "Ardeshir Talaei";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "ardeshir.talaei@iaac.net";
    }
}
