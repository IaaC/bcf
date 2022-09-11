using System;
using System.Collections.Generic;
using System.Linq;
using BCFSpeckleLibrary;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopperExtension.Streams
{
  public class BranchUpdateComponent : GH_Component
  {
    /// <summary>
    /// Initializes a new instance of the MyComponent1 class.
    /// </summary>
    public BranchUpdateComponent()
      : base("Move To Branch", "MTB",
          "Move parts to a specific branch of a project.",
          "Speckle 2", "BCF Repo")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Account", "A", "Account", GH_ParamAccess.item);
      pManager.AddTextParameter("Project", "P", "Project Name", GH_ParamAccess.item); 
      pManager.AddTextParameter("Branch", "B", "The Branch to add the objects to.", GH_ParamAccess.item);
      pManager.AddTextParameter("Parts", "P", "The parts to add to branch.", GH_ParamAccess.list);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Info", "I", "Info or error messages", GH_ParamAccess.list);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var userId = "";
      DA.GetData(0, ref userId);
      Account account = null;
      account = AccountManager.GetAccounts().FirstOrDefault(a => a.userInfo.id == userId);

      if (account == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
          $"Could not find an account for server. Use the Speckle Manager to add an account.");
        return;
      }

      var prjName = "";
      if (!DA.GetData(1, ref prjName)) return;

      var branchName = "";
      if (!DA.GetData(2, ref branchName)) return;

      List<string> parts = new List<string>();
      if (!DA.GetDataList(3, parts)) return;

      BCFBranch branchMan = null;
      try
      {
        branchMan = new BCFBranch(account, prjName);
        var res = branchMan.MoveToBranch(branchName, parts).Result;
        DA.SetDataList(0, res);
      }
      catch (Exception ex)
      {
        throw new Exception(ex.Message, ex);
      }
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
        return null;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("612B399F-B7A0-49EA-9A23-D3F0CD1CB1D8"); }
    }
  }
}
