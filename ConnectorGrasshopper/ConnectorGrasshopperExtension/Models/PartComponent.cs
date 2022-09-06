using System;
using System.Collections.Generic;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Speckle.Core.Models;
using BCFSpeckleKit.Models;

namespace ConnectorGrasshopperExtension
{
  public class PartComponent : GH_Component
  {
    /// <summary>
    /// Initializes a new instance of the PartComponent class.
    /// </summary>
    public PartComponent()
      : base("Part", "PRT",
          "Create a part and its metadata",
          "Speckle 2", "BCF Objects")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Part Name", "NAM", "Part identifier", GH_ParamAccess.item);
      pManager.AddTextParameter("Material", "MAT", "Part Material", GH_ParamAccess.item);
      pManager.AddNumberParameter("Thickness", "TCK", "Part Thickness in millimeter", GH_ParamAccess.item);
      pManager.AddTextParameter("Project Name", "PRJ", "Project name", GH_ParamAccess.item);
      pManager.AddTextParameter("Project Code", "PID", "Project ID", GH_ParamAccess.item);
      pManager.AddTextParameter("Deadline", "DLN", "Deadline as Text", GH_ParamAccess.item);
      pManager.AddNumberParameter("Area", "ARE", "Part Area", GH_ParamAccess.item, 0);
      pManager.AddGenericParameter("Geometry", "GEO", "Part geometry", GH_ParamAccess.item);
      pManager.AddGenericParameter("Original Ref", "REF", "Original Part object", GH_ParamAccess.item);
      pManager.AddTextParameter("Client", "CLI", "Client of the Part object", GH_ParamAccess.item, "unknown");

      pManager[5].Optional = true;
      pManager[8].Optional = true;
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Part Object", "P", "Created part object", GH_ParamAccess.item));
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      string partName = null;
      string material = null;
      double thickness = 0;
      string prjName = null;
      string prjId = null;
      string deadline = null;
      var client = "";
      double area = 0;
      GH_SpeckleBase geom = null;
      GH_SpeckleBase refObj = null;

      if (!DA.GetData(0, ref partName)) return;
      if (!DA.GetData(1, ref material)) return;
      if (!DA.GetData(2, ref thickness)) return;
      if (!DA.GetData(3, ref prjName)) return;
      if (!DA.GetData(4, ref prjId)) return;
      if (!DA.GetData(5, ref deadline)) return;
      if (!DA.GetData(6, ref area)) return;
      if (!DA.GetData(7, ref geom)) return;
      if (!DA.GetData(9, ref client)) return;
      DA.GetData(8, ref refObj);

      var part = new PartObject
      {
        part_name = partName,
        project_code = prjId,
        project_deadline = deadline,
        project_name = prjName,
        material = material,
        thickness = thickness.ToString(),
        part_area = area,
        part_geometry = geom.Value,
        client = client
      };

      if (refObj != null)
      {
        part.original = refObj.Value;
      }

      DA.SetData(0, part);
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
        return Properties.Resources.Part;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("9992CFE2-CF45-4D79-8405-6F5AE86EEC65"); }
    }
  }
}
