using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BCFSpeckleLibrary;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopperExtension.Streams
{
  public class ProjectStreamComponent : GH_Component
  {
    public bool CreateStreamIfNotExists { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the ProjectStreamComponent class.
    /// </summary>
    public ProjectStreamComponent()
      : base("Project", "Prj",
          "Get/Create a project stream.",
          "Speckle 2", "BCF Repository")
    {
      Attributes = new ProjectStreamComponentAttributes(this);
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Account", "A", "Account", GH_ParamAccess.item);
      pManager.AddTextParameter("Name", "N", "Project Name", GH_ParamAccess.item);
      pManager.AddTextParameter("Branches", "B", "Stream branches to add", GH_ParamAccess.list);
      pManager.AddBooleanParameter("Validate", "V", "Validate project", GH_ParamAccess.item, false);

      pManager[2].Optional = true;
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Stream", "S", "Project stream ID", GH_ParamAccess.item);
      pManager.AddGenericParameter("Branches", "B", "Project Branches names", GH_ParamAccess.list);
      pManager.AddGenericParameter("URLs", "U", "Project Branches URLs", GH_ParamAccess.list);
      pManager.AddBooleanParameter("Valid", "V", "Project validity", GH_ParamAccess.item);    // Was List originally ???
      pManager.AddTextParameter("Parts", "P", "Project Parts", GH_ParamAccess.tree);
      pManager.AddTextParameter("Errors", "E", "Project Errors", GH_ParamAccess.tree);
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

      var name = "";
      if (!DA.GetData(1, ref name)) return;

      var branchNames = new List<string>();
      DA.GetDataList(2, branchNames);
      BCFProject prj = null;
      try
      {
        prj = new BCFProject(account, name, CreateStreamIfNotExists);
      }
      catch (Exception ex)
      {
        throw new Exception(ex.Message, ex);
      }

      DA.SetData(0, prj.StreamId);
      
      var validate = false;
      DA.GetData(3, ref validate);

      if (validate)
      {
        var isValid = false;
        try
        {
          isValid = prj.ValidateProject().Result;

          DA.SetData(5, "");
        }
        catch (Exception ex)
        {
          DA.SetData(5, ex.InnerException.Message);
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, prj.ProjectName + ": " + ex.InnerException.Message);
        }

        DA.SetDataList(1, isValid ? prj.branches.ToList().Select(branch => branch.Key).ToList() : new List<string>());
        DA.SetDataList(2, isValid ? prj.branches.ToList().Select(branch => branch.Value.ToString()).ToList() : new List<string>());

        var partsTree = new GH_Structure<GH_String>();
        if (isValid)
        {
          var p = 0;
          foreach (var branchPartsPair in prj.projectParts)
          {
            partsTree.AppendRange(branchPartsPair.Value.Select(s => new GH_String((string)s.part_name)).ToList(), DA.ParameterTargetPath(1).AppendElement(p));
            p++;
          }
        }
        
        DA.SetData(3, isValid);
        partsTree.EnsurePath(DA.ParameterTargetPath(1).AppendElement(0));
        DA.SetDataTree(4, partsTree);
      }
      else
      {
        DA.SetDataList(1, prj.branches.ToList().Select(branch => branch.Key).ToList());
        DA.SetDataList(2, prj.branches.ToList().Select(branch => branch.Value.ToString()).ToList());
      }
    }

    protected override void AfterSolveInstance()
    {
      CreateStreamIfNotExists = false;
      base.AfterSolveInstance();
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
      get { return new Guid("57C79C92-71FF-4E2C-9DA4-D2C41560B85E"); }
    }
  }

  public class ProjectStreamComponentAttributes : GH_ComponentAttributes
  {
    private bool _selected;

    public ProjectStreamComponentAttributes(GH_Component owner) : base(owner)
    {
    }

    private Rectangle ButtonBounds { get; set; }

    public override bool Selected
    {
      get => _selected;
      set
      {
        //Owner.Params.ToList().ForEach(p => p.Attributes.Selected = value);
        _selected = value;
      }
    }

    protected override void Layout()
    {
      base.Layout();

      var baseRec = GH_Convert.ToRectangle(Bounds);
      baseRec.Height += 26;

      var btnRec = baseRec;
      btnRec.Y = btnRec.Bottom - 26;
      btnRec.Height = 26;
      btnRec.Inflate(-2, -2);

      Bounds = baseRec;
      ButtonBounds = btnRec;
    }

    protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
    {
      base.Render(canvas, graphics, channel);

      if (channel == GH_CanvasChannel.Objects)
      {
        var palette = GH_Palette.Black; // GH_Palette.Transparent;
        var text = "Create";

        var button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, palette, text, 2,
          10);
        button.Render(graphics, Selected, Owner.Locked, false);
        button.Dispose();
      }
    }

    public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
      if (e.Button != MouseButtons.Left)
      {
        return base.RespondToMouseDown(sender, e);
      }

      if (!((RectangleF)ButtonBounds).Contains(e.CanvasLocation))
      {
        return base.RespondToMouseDown(sender, e);
      }

      var owner = (ProjectStreamComponent)Owner;
      owner.CreateStreamIfNotExists = true;
      Owner.ExpireSolution(true);
      return GH_ObjectResponse.Handled;
    }
  }
}
