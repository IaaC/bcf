using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using SpeckleQueryExtensions;

namespace BCFSpeckleLibrary
{
  public class BCFProject : BCFStream
  {
    //readonly Account account;
    //private Client Client { get; set; }
    public string ProjectName { get; set; }
    //public Stream Stream { get; set; }
    //public StreamWrapper SheetsStream { get; set; }
    public string StreamId { get; set; }
    
    public Dictionary<string, StreamWrapper> branches = new Dictionary<string, StreamWrapper>();
    public Dictionary<string, List<dynamic>> projectParts = new Dictionary<string, List<dynamic>>();

    public BCFProject(Account account, string projectName, bool create = false) : base (account)
    {
      ProjectName = projectName;
      GetOrCreateStream(create).Wait();
      GetOrCreateBranches(create).Wait();
    }

    private async Task GetOrCreateStream(bool create)
    {
      var stream = streams.FirstOrDefault(a => a.name == ProjectName);
      StreamId = stream?.id;

      if (StreamId == null)
      {
        if (create)
        {
          var sInput = new StreamCreateInput
          {
            name = ProjectName,
            description = "Auto-generated Project Stream",
            isPublic = true
          };
          StreamId = await client.StreamCreate(sInput).ConfigureAwait(false);
        }
        else
        {
          throw new Exception("Stream not found.");
        }
      }
    }
    private async Task GetOrCreateBranches(bool create = false, List<string> branchNames = null)
    {
      if (StreamId == null)
        throw new Exception("Stream id is missing");
      if (create)
      {
        var branches = await client.StreamGetBranches(StreamId).ConfigureAwait(false);

        var branchToAdd = (branchNames ?? new List<string> { "uploaded", "nested", "done" }).Where(bn => !branches.Any(bs => bs.name == bn)).ToList();

        branchToAdd?.ForEach(bName => client.BranchCreate(new BranchCreateInput
        {
          name = bName,
          streamId = StreamId,
          description = "Auto-generated Branch"
        }));
      }

      var outputBranches = await client.StreamGetBranches(StreamId).ConfigureAwait(false);
      if (!outputBranches.Any()) throw new Exception("No branch found");
      outputBranches.ForEach(b => branches.Add(b.name, new StreamWrapper($"{account.serverInfo.url}/streams/{StreamId}/branches/{b.name}")));
    }

    public async Task<bool> ValidateProject()
    {
      var reqBrn = new List<string> { "main", "uploaded", "nested", "done" };
      if (!reqBrn.All(b => branches.Keys.Contains(b)))
        throw new Exception("Stream has missing branches.");

      var mainPartsTask = GetSOByField(branches["main"], new List<string> { "material", "thickness" }).ConfigureAwait(false);
      var uploadedPartsTask = GetSOByField(branches["uploaded"]).ConfigureAwait(false);
      var nestedPartsTask = GetSOByField(branches["nested"]).ConfigureAwait(false);
      var completedPartsTask = GetSOByField(branches["done"]).ConfigureAwait(false);

      var mainParts = await mainPartsTask;
      projectParts.Add("main", mainParts);

      var sheetList = mainParts.Select(x => new Tuple<string, string>((string)x.material, (string)x.thickness)).Distinct().ToList();
      var sheetsTasks = new Dictionary<Tuple<string, string>, ConfiguredTaskAwaitable<List<dynamic>>>();
      sheetList.ForEach(sh => sheetsTasks.Add(sh, GetSheetsParts(sh.Item1, sh.Item2).ConfigureAwait(false)));

      var orderedBySheet = mainParts.GroupBy(x => new Tuple<string, string>((string)x.material, (string)x.thickness));

      var uploadedParts = await uploadedPartsTask;
      projectParts.Add("uploaded", uploadedParts);
      // Uploaded is in main.
      if (uploadedParts.Select(x => x.part_name).Except(mainParts.Select(x => x.part_name)).Any())
        throw new Exception("Not all UPLOADED branch parts are in the MAIN branch.");

      var nestedParts = await nestedPartsTask;
      projectParts.Add("nested", nestedParts);

      // Nested Parts in main?
      if (nestedParts.Select(x => x.part_name).Except(mainParts.Select(x => x.part_name)).Any())
        throw new Exception("Not all NESTED branch parts are in the MAIN branch.");
      // Uploaded and Nested don't overlap.
      if (uploadedParts.Select(x => x.part_name).Intersect(nestedParts.Select(x => x.part_name)).Any())
        throw new Exception("NESTED and UPLOADED branches have overlaps.");

      // Nested Parts are in Sheets
      // Uploaded parts not in sheets.
      foreach (var kvp in orderedBySheet)
      {
        var allParts = kvp.Select(x => x.part_name);
        var sheetVals = await sheetsTasks[kvp.Key];
        if (allParts.Intersect(uploadedParts.Select(x => x.part_name)).Intersect(sheetVals.Select(x => x.part_name)).Any())
          throw new Exception("UPLOADED branch part already nested sheets.");
        if (allParts.Intersect(nestedParts.Select(x => x.part_name)).Except(sheetVals.Select(x => x.part_name)).Any())
          throw new Exception("NESTED branch part not in nested sheets.");
      }

      var doneParts = await completedPartsTask;
      projectParts.Add("done", doneParts);

      // Uploaded and completed don't overlap
      if (uploadedParts.Select(x => x.part_name).Intersect(doneParts.Select(x => x.part_name)).Any())
        throw new Exception("DONE and UPLOADED branches have overlaps.");
      // Nested and completed don't overlap
      if (nestedParts.Select(x => x.part_name).Intersect(doneParts.Select(x => x.part_name)).Any())
        throw new Exception("NESTED and DONE branches have overlaps.");

      // main has no extra parts that is not in one of these branches
      if (mainParts.Select(x => x.part_name).Except(nestedParts.Union(uploadedParts).Union(doneParts).Select(x => x.part_name)).Any())
        throw new Exception("MAIN branch has parts that are not included in sub branches.");

      return true;
    }

    private async Task<List<dynamic>> GetSheetsParts(string material, string thickness)
    {
      var sheetBranchQueue = new StreamWrapper($"{account.serverInfo.url}/streams/{SheetsStream.StreamId}/branches/queue/{material}/{thickness}");
      var sheetPartsQueue = await GetSOByField(sheetBranchQueue, new List<string> { "material", "thickness" }).ConfigureAwait(false);
      
      var sheetBranchDone = new StreamWrapper($"{account.serverInfo.url}/streams/{SheetsStream.StreamId}/branches/done/{material}/{thickness}");
      var sheetPartsDone = await GetSOByField(sheetBranchDone, new List<string> { "material", "thickness" }).ConfigureAwait(false);

      return sheetPartsQueue.Concat(sheetPartsDone).ToList();
    }

    //public async Task<List<dynamic>> GetSOByField(StreamWrapper stream, List<string> fields = default)
    //{
    //  var agent = new QueryAgent(account)
    //  {
    //    Stream = stream,
    //  };
    //  agent.AddLayer(10).AddQuery("part_name", "", "!=");
    //  agent.layers[0].fields.AddRange(new List<string> { "part_name", "id" });
    //  if (fields != null) agent.layers[0].fields.AddRange(fields);

    //  var queryResult = await agent.RunQuery().ConfigureAwait(false);
    //  var prts = queryResult?.Select(kvp => (dynamic)JObject.Parse(kvp.Value)).ToList();

    //  return prts;
    //}
  }
}
