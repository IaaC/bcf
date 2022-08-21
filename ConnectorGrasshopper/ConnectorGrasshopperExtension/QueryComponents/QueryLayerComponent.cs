using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using SpeckleQueryExtensions;

namespace ConnectorGrasshopperExtension.QueryComponents
{
  public class QueryLayerComponent : GH_Component
  {
    /// <summary>
    /// Initializes a new instance of the MyComponent1 class.
    /// </summary>
    public QueryLayerComponent()
      : base("Query Layer", "QLay",
          "Make a layer (filter) from a set of query parameters.",
          "Speckle 2", "Query")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Layer", "L", "Parent layer for chaining queries; Enables querying nested objects.", GH_ParamAccess.item);
      pManager.AddGenericParameter("Params", "P", "Query Parameters.", GH_ParamAccess.list);
      pManager.AddTextParameter("Select", "S", "Parameters to Return.", GH_ParamAccess.list);
      pManager.AddIntegerParameter("Depth", "D", "Nested Depth of the objects to query", GH_ParamAccess.item, 10);
      pManager[0].Optional = true;
      pManager[2].Optional = true;
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Layer", "L", "Query Layer.", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var qDepth = -1;
      DA.GetData(3, ref qDepth);
      var paramsList = new List<QueryAgent.QueryParam>();
      if (!DA.GetDataList(1, paramsList)) return;

      var selectList = new List<string>();
      DA.GetDataList(2, selectList);

      var newLayer = new QueryAgent.Layer
      {
        queryParams = paramsList,
        depth = qDepth,
        fields = selectList
      };

      var parentLayer = new List<QueryAgent.Layer>();
      DA.GetData(0, ref parentLayer);

      var layers = new List<QueryAgent.Layer>();
      layers.AddRange(parentLayer);
      layers.Add(newLayer);

      DA.SetData(0, layers);
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
        return Properties.Resources.FictionFactoryLogo;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("F302F154-533F-49E5-8E38-1973BA729B04"); }
    }
  }
}
