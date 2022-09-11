using System;
using System.Collections.Generic;
using System.Linq;
using BCFSpeckleLibrary;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopperExtension.Streams
{
  public class SheetsStreamComponent : GH_Component
  {
    /// <summary>
    /// Initializes a new instance of the MyComponent1 class.
    /// </summary>
    public SheetsStreamComponent()
      : base("Sheets", "Sht",
      "Get a Sheets stream.",
      "Speckle 2", "BCF Repo")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Account", "A", "Account", GH_ParamAccess.item);
      pManager.AddTextParameter("Material", "M", "Sheet Material", GH_ParamAccess.item);
      pManager.AddNumberParameter("Thickness", "T", "Sheet Thickness", GH_ParamAccess.item);
      pManager.AddBooleanParameter("Validate", "V", "Validate project", GH_ParamAccess.item, false);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Stream", "S", "Sheet stream ID", GH_ParamAccess.item);
      pManager.AddGenericParameter("Branch", "B", "Sheet Branch name", GH_ParamAccess.list);
      pManager.AddGenericParameter("Sheets", "S", "Sheets Names", GH_ParamAccess.list);
      pManager.AddBooleanParameter("Valid", "V", "Sheets Validity", GH_ParamAccess.list);
      pManager.AddTextParameter("Parts", "P", "Nested Parts", GH_ParamAccess.tree);
      pManager.AddTextParameter("Errors", "E", "Nesting Errors", GH_ParamAccess.tree);
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

      var material = "";
      if (!DA.GetData(1, ref material)) return;
      double thickness = 0;
      if (!DA.GetData(2, ref thickness)) return;


      BCFSheets sheets = null;
      try
      {
        sheets = new BCFSheets(account, material, (int)thickness);
      }
      catch (Exception ex)
      {
        throw new Exception(ex.Message, ex);
      }

      DA.SetData(0, sheets.SheetsStream);

      var sheetBranches = new List<StreamWrapper> { sheets.SheetBranchQueue, sheets.SheetBranchDone };
      DA.SetDataList(1, sheetBranches);


      var validate = false;
      DA.GetData(3, ref validate);

      if (validate)
      {
        var isValid = false;
        try
        {
          isValid = sheets.ValidateSheets().Result;

          DA.SetData(5, "");
        }
        catch (Exception ex)
        {
          DA.SetData(5, ex.InnerException.Message);
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ex.InnerException.Message);
        }

        var sheetsTree = new GH_Structure<GH_String>();
        var p1 = 0;
        sheets.Sheets.ToList().ForEach(kvp =>
        {
          sheetsTree.AppendRange(
            kvp.Value.Select(s => new GH_String((string)s.sheet_name)).ToList(),
            DA.ParameterTargetPath(1).AppendElement(p1));
          p1++;
        });

        var sheetNames = new List<dynamic>();
        sheets.Sheets.Values.ToList().ForEach(s => sheetNames.AddRange(s));
        DA.SetDataTree(2, sheetsTree);

        var partsTree = new GH_Structure<GH_String>();
        if (isValid)
        {
          var p2 = 0;
          foreach (var branchPartsPair in sheets.SheetParts)
          {
            partsTree.AppendRange(branchPartsPair.Value.Select(s => new GH_String((string)s.project_name + "::" + (string)s.part_name)).ToList(), DA.ParameterTargetPath(1).AppendElement(p2));
            p2++;
          }
        }

        DA.SetData(3, isValid);
        partsTree.EnsurePath(DA.ParameterTargetPath(1).AppendElement(0));
        DA.SetDataTree(4, partsTree);
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
        return Properties.Resources.Sheets;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("E13037D4-C977-4240-9B11-E6FBFFCE7804"); }
    }
  }
}
