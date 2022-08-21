using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using SpeckleQueryExtensions;

namespace BCFSpeckleLibrary
{
  public class BCFSheets : BCFStream
  {
    public string Material { get; set; }
    public double Thickness { get; set; }
    public StreamWrapper SheetBranchQueue { get; set; }
    public StreamWrapper SheetBranchDone { get; set; }
    public Dictionary<string, List<dynamic>> Sheets { get; set; } = new Dictionary<string, List<dynamic>>();
    public Dictionary<string, List<dynamic>> SheetParts { get; set; } = new Dictionary<string, List<dynamic>>();

    public BCFSheets(Account account, string material, double thickness) : base(account)
    {
      Material = material;
      Thickness = thickness;
      GetSheetsBranch();
    }

    private void GetSheetsBranch()
    {
      SheetBranchQueue = new StreamWrapper($"{account.serverInfo.url}/streams/{SheetsStream.StreamId}/branches/queue/{Material}/{(int)Thickness}");
      SheetBranchDone = new StreamWrapper($"{account.serverInfo.url}/streams/{SheetsStream.StreamId}/branches/done/{Material}/{(int)Thickness}");
    }

    public async Task<bool> ValidateSheets()
    {
      await GetSheetNames().ConfigureAwait(false);
      await GetSheetParts().ConfigureAwait(false);

      // Check for Duplicate Parts
      var dupParts = SheetParts.ToList()
        .Where(kv => kv.Key.Contains("queue"))
        .SelectMany(kv => kv.Value)
        .Select(p => (string)p.project_name + "::" + (string)p.part_name)
        .GroupBy(x => x)
        .Where(g => g.Count() > 1)
        .Select(y => y.Key).ToList();
      if (dupParts.Count > 0)
        throw new Exception("[Doubles Alert] Sheets have duplicate parts: " + string.Join(",", dupParts));

      // Check for materials and thicknesses
      var imposters = SheetParts.ToList()
        .Where(kv => kv.Key.Contains("queue"))
        .SelectMany(kv => kv.Value)
        .Where(p => (string)p.material != Material || (int)p.thickness != (int)Thickness).ToList();
      if (imposters.Count > 0)
        throw new Exception("[Imposters Alert] Nested part have different material or thickness: "
          + string.Join(",", imposters.GroupBy(p => p.project_name).Select(i => $"[{i.Key}::{string.Join(",", i.Select(p => p.part_name))}]")));

      // Check for parts in projects' MAIN.
      var orphans = SheetParts.ToList()
        .Where(kv => kv.Key.Contains("queue"))
        .SelectMany(kv => kv.Value)
        .GroupBy(p => p.project_name)
        .Select(g =>
        {
          var prjStream = new StreamWrapper(streams.First(s => s.name == (string)g.Key).id);
          var projectParts = GetSOByField(prjStream).Result;
          return new KeyValuePair<string, List<string>>((string)g.Key,
            g.Select(p => (string)p.part_name).Except(projectParts.Select(pp => (string)pp.part_name)).ToList());
        }).ToList();
      if (orphans.Any(l => l.Value.Count > 0))
        throw new Exception("[Orphans Alert] Nested part not in the MAIN branch of the project: "
          + string.Join(",", orphans.Where(o => o.Value.Count > 0).Select(o => $"[{o.Key}::{string.Join(",", o.Value)}]")));

      return true;
    }

    private async Task GetSheetNames()
    {
      var sheetsQueueTask = GetSOByField(SheetBranchQueue, byField: "sheet_name").ConfigureAwait(false);
      var sheetsDoneTask = GetSOByField(SheetBranchDone, byField: "sheet_name").ConfigureAwait(false);

      var sheetsInQueue = await sheetsQueueTask;
      Sheets.Add(SheetBranchQueue.BranchName, sheetsInQueue);
      var sheetsInDone = await sheetsDoneTask;
      Sheets.Add(SheetBranchDone.BranchName, sheetsInDone);
    }

    private async Task GetSheetParts()
    {
      var sheetsQueueTask = GetSOByField(SheetBranchQueue, new List<string> { "project_name", "material", "thickness" }).ConfigureAwait(false);
      var sheetsDoneTask = GetSOByField(SheetBranchDone, new List<string> { "project_name", "material", "thickness" }).ConfigureAwait(false);

      var sheetsPartsQueue = await sheetsQueueTask;
      SheetParts.Add(SheetBranchQueue.BranchName, sheetsPartsQueue);
      var sheetsPartsDone = await sheetsDoneTask;
      SheetParts.Add(SheetBranchDone.BranchName, sheetsPartsDone);
    }
  }
}
