using System;
using System.Collections.Generic;
using ConnectorGrasshopper.Extras;
using BCFSpeckleKit.Models;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Linq;

namespace ConnectorGrasshopperExtension
{
  public class SheetComponent : GH_Component
  {
    /// <summary>
    /// Initializes a new instance of the StatusComponent class.
    /// </summary>
    public SheetComponent()
      : base("Sheet", "SHT",
        "Create a Sheet to host part object",
        "Speckle 2", "BCF Objects")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Sheet Name", "NAM", "Sheet identifier", GH_ParamAccess.item);
      pManager.AddTextParameter("Material", "MAT", "Sheet Material", GH_ParamAccess.item);
      pManager.AddNumberParameter("Thickness", "TCK", "Sheet Thickness in millimeter", GH_ParamAccess.item);
      pManager.AddGenericParameter("Outline", "OTL", "Sheet outline", GH_ParamAccess.item);
      pManager.AddTextParameter("Deadline", "DLN", "Deadline as Text", GH_ParamAccess.item);
      pManager.AddNumberParameter("Efficiency", "EFC", "Nesting Efficiency of the sheet in percents.", GH_ParamAccess.item, 0);
      pManager.AddNumberParameter("Spacing", "SPC", "Nesting Spacing of the sheet.", GH_ParamAccess.item, 0);
      pManager.AddGenericParameter("Parts", "PTS", "Nested Parts", GH_ParamAccess.list);

      pManager[4].Optional = true;
      pManager[6].Optional = true;
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Sheet", "SHT", "Sheet object", GH_ParamAccess.item));
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      string sheetName = null;
      string material = null;
      double thickness = 0;
      GH_SpeckleBase outline = null;
      string deadline = null;
      double efficiency = 0;
      double spacing = 0;
      var geom = new List<GH_SpeckleBase>();

      if (!DA.GetData(0, ref sheetName)) return;
      if (!DA.GetData(1, ref material)) return;
      if (!DA.GetData(2, ref thickness)) return;
      if (!DA.GetData(3, ref outline)) return;
      if (!DA.GetData(4, ref deadline)) return;
      if (!DA.GetData(5, ref efficiency)) return;
      if (!DA.GetData(6, ref spacing)) return;
      if (!DA.GetDataList(7, geom)) return;

      var sheet = new SheetObject
      {
        sheet_name = sheetName,
        deadline = deadline,
        material = material,
        thickness = thickness.ToString(),
        efficiency = efficiency,
        outline = outline.Value,
        spacing = spacing,
        parts = geom.Select(x => x.Value).ToList()
      };

      DA.SetData(0, sheet);
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        //You can add image files to your project resources and access them like this:
        // return Resources.IconForThisComponent;
        return Properties.Resources.Sheet;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("FF2A1DEA-09E2-4269-936C-D3868C4F40A2"); }
    }
  }
}
