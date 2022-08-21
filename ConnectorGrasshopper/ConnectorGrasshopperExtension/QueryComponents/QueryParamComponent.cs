using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using SpeckleQueryExtensions;
using System;
using System.Collections.Generic;

namespace ConnectorGrasshopperExtension
{
  public class QueryParamComponent : GH_Component
  {
    /// <summary>
    /// Each implementation of GH_Component must provide a public 
    /// constructor without any arguments.
    /// Category represents the Tab in which the component will appear, 
    /// Subcategory the panel. If you use non-existing tab or panel names, 
    /// new tabs/panels will automatically be created.
    /// </summary>
    public QueryParamComponent()
      : base("Query Parameter", "QPrm",
        "Define a query parameter",
        "Speckle 2", "Query")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Key", "K", "Query Key", GH_ParamAccess.item);
      pManager.AddGenericParameter("Value", "V", "Query Value", GH_ParamAccess.item);
      pManager.AddIntegerParameter("Operator", "O", "Operator type [0: =, 1: !=, 2: <, 3: >, 4: <=, 5: >=]", GH_ParamAccess.item, 0);
      pManager.AddIntegerParameter("Value Type", "T", "Value type [Text | Numeric]", GH_ParamAccess.item, 0);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Param", "P", "Query Parameter", GH_ParamAccess.item);
    }

    static readonly string[] opts = { "=", "!=", "<", ">", "<=", ">=" };

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
    /// to store data in output parameters.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      string key = "";
      dynamic value = null;
      int optType = 0;
      int valType = 0;

      if (!DA.GetData(0, ref key)) return;
      if (!DA.GetData(1, ref value)) return;
      if (!DA.GetData(2, ref optType)) return;
      if (!DA.GetData(3, ref valType)) return;

      switch (valType)
      {
        case 0:
          {
            value = (string)value.Value;
            break;
          }
        case 1:
          {
            value = (double)value.Value;
            break;
          }
        default:
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Value type out of range: No casting applied.");
            break;
          }
      }

      var qPar = new QueryAgent.QueryParam
      {
        field = key,
        value = value,
        Operator = opts[optType]
      };
      
      DA.SetData(0, qPar);
    }

    /// <summary>
    /// Provides an Icon for every component that will be visible in the User Interface.
    /// Icons need to be 24x24 pixels.
    /// You can add image files to your project resources and access them like this:
    /// return Resources.IconForThisComponent;
    /// </summary>
    protected override System.Drawing.Bitmap Icon => Properties.Resources.FictionFactoryLogo;

    /// <summary>
    /// Each component must have a unique Guid to identify it. 
    /// It is vital this Guid doesn't change otherwise old ghx files 
    /// that use the old ID will partially fail during loading.
    /// </summary>
    public override Guid ComponentGuid => new Guid("09736C07-9911-4ACC-BAF9-C93587996270");
  }
}
