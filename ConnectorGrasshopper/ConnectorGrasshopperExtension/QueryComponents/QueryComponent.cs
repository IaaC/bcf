using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Speckle.Core.Credentials;
using Newtonsoft.Json.Linq;
using SpeckleQueryExtensions;

namespace ConnectorGrasshopperExtension.QueryComponents
{
  public class QueryComponent : GH_TaskCapableComponent<QueryComponent.SolveResults>
  {
    /// <summary>
    /// Initializes a new instance of the QueryComponent class.
    /// </summary>
    public QueryComponent()
      : base("Query", "Query",
          "Run a query on the selected streams.",
          "Speckle 2", "Query")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Account", "A", "Account to get stream with.", GH_ParamAccess.item);
      pManager.AddParameter(new SpeckleStreamParam(
        "Stream ID/URL", "ID/URL", "Speckle stream ID or URL",
        GH_ParamAccess.item));
      pManager.AddGenericParameter("Query Layers", "L", "Query Layers.", GH_ParamAccess.item);

      pManager[0].Optional = true;
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Stream ID", "SID", "List of Stream IDs.", GH_ParamAccess.item);
      pManager.AddGenericParameter("Object ID", "OID", "List of Objects.", GH_ParamAccess.list);
      pManager.AddGenericParameter("Results", "RES", "Query Result.", GH_ParamAccess.list);
      pManager.AddTextParameter("Fields", "FIL", "Query Result Fields.", GH_ParamAccess.tree);
      pManager.AddGenericParameter("Raw", "out", "Raw Results as Text.", GH_ParamAccess.item);
    }

    public class SolveResults
    {
      public string streamId = null;

      public List<string> rawData = new List<string>();
      public List<string> results = new List<string>();

      public List<string> objectIDs = new List<string>();
      public List<List<string>> fields = new List<List<string>>();
    }

    SolveResults Compute(
      Account account,
      StreamWrapper stream,
      List<QueryAgent.Layer> layersList)
    {
      var agent = new QueryAgent(account)
      {
        Stream = stream,
        layers = layersList
      };
      try
      {
        var rc = new SolveResults
        {
          streamId = stream.StreamId
        };

        agent.GenerateQueryVariables().Wait();
        agent.GenerateQueryString();
        if (string.IsNullOrEmpty(agent.QueryString) || agent.QueryVariables == null)
          return rc;

        var queryResult = agent.RunQuery().Result;

        foreach (var kvp in queryResult)
        {
          rc.results.Add(
              $"{account.serverInfo.url}/streams/{stream.StreamId}/objects/{kvp.Key}"
              );
          rc.objectIDs.Add(kvp.Key);
          rc.rawData.Add(kvp.Value.ToString());

          var jRes = (Newtonsoft.Json.Linq.JObject)kvp.Value;
          rc.fields.Add(jRes.Children().Select(r => r.First.ToString()).ToList());
        }

        return rc;
      }
      catch (Exception ex)
      {
        throw new Exception(ex.InnerException.Message);
      }
      
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (InPreSolve)
      {
        // First pass; collect data and construct tasks
        Task<SolveResults> tsk = null;
        var queryLayers = new List<QueryAgent.Layer>();
        GH_SpeckleStream ghIdWrapper = null;
        string userId = null;
        StreamWrapper idWrapper = null;
        Account account = null;

        if (DA.GetData(1, ref ghIdWrapper))
        {
          idWrapper = ghIdWrapper.Value;

          DA.GetData(0, ref userId);

          account = string.IsNullOrEmpty(userId)
            ? AccountManager.GetAccounts().FirstOrDefault(a => a.serverInfo.url == idWrapper.ServerUrl) // If no user is passed in, get the first account for this server
            : AccountManager.GetAccounts().FirstOrDefault(a => a.userInfo.id == userId); // If user is passed in, get matching user in the db
        }


        if (account == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
            $"Could not find an account for server. Use the Speckle Manager to add an account.");
        }
        else if (DA.GetData(2, ref queryLayers))
        {
          if (queryLayers.Count > 0)
            tsk = Task.Run(() => Compute(account, idWrapper, queryLayers), CancelToken);
        }

        // Add a null task even if data collection fails. This keeps the
        // list size in sync with the iterations
        TaskList.Add(tsk);
        return;
      }

      if (!GetSolveResults(DA, out var results))
      {
        // Compute right here, right now.
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
            $"Error in multi-threading, running in sequence . . .");
        results = null;
        // 1. Collect
        var queryLayers = new List<QueryAgent.Layer>();
        GH_SpeckleStream ghIdWrapper = null;
        string userId = null;
        StreamWrapper idWrapper = null;
        Account account = null;

        if (DA.GetData(1, ref ghIdWrapper))
        {
          idWrapper = ghIdWrapper.Value;
          DA.GetData(0, ref userId);

          account = string.IsNullOrEmpty(userId)
            ? AccountManager.GetAccounts().FirstOrDefault(a => a.serverInfo.url == idWrapper.ServerUrl) // If no user is passed in, get the first account for this server
            : AccountManager.GetAccounts().FirstOrDefault(a => a.userInfo.id == userId); // If user is passed in, get matching user in the db
        }

        if (account == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
            $"Could not find an account for server. Use the Speckle Manager to add an account.");
        }
        else if (DA.GetData(2, ref queryLayers))
        {
          if (queryLayers.Count > 0)
            results = Compute(account, idWrapper, queryLayers);
        }
      }

      // 3. Set
      if (results != null)
      {
        DA.SetData(0, results.streamId);
        DA.SetDataList(1, results.objectIDs);
        DA.SetDataList(2, results.results);

        var fieldsTree = new GH_Structure<GH_String>();
        var p = 0;
        foreach (var field in results.fields)
        {
          fieldsTree.AppendRange(field.Select(s => new GH_String(s)).ToList(), DA.ParameterTargetPath(2).AppendElement(p));
          p++;
        }

        DA.SetDataTree(3, fieldsTree);
        DA.SetData(4, results);
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
        return Properties.Resources.FictionFactoryLogo;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("17281C07-A72B-4A35-96CE-D4EB317A7018"); }
    }
  }
}
