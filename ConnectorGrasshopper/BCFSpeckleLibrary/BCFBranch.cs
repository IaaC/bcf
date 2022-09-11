using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;

namespace BCFSpeckleLibrary
{
  public class BCFBranch : BCFProject
  {
    public BCFBranch(Account account, string projectName) : base(account, projectName, false)
    {
      // Get the objects list in all branches.
      ValidateProject().Wait();
    }

    public async Task<List<string>> MoveToBranch(string targetBranch, List<string> parts)
    {
      if (!branches.Keys.ToList().Contains(targetBranch))
        throw new Exception("Branch not found.");

      // Check if the objects are in the Main.
      var imposters = parts
        .Except(projectParts["main"].Select(x => (string)x.part_name))
        .Select(a => a);
      if (imposters.Any())
        throw new Exception("Some parts are not in the project: "
          + string.Join(",", imposters.ToList()));


      // Download the Main Branch.
      var mainBranchParts = await GetBranchParts(branches["main"]).ConfigureAwait(false);

      // Update Parts on the Target Branch
      var targetBranchParts = mainBranchParts
        .Where(p =>
          parts.Union(projectParts[targetBranch].Select(x => (string)x.part_name)).Contains((string)p["part_name"]))
        .Select(a => a).ToList();
      var targetTask = UploadUpdated(branches[targetBranch], targetBranchParts);

      // Update Parts on Other Branches
      var otherTasks = projectParts
        .Where(kvp => kvp.Key != "main" && kvp.Key != targetBranch)
        .Where(kvp => parts.Intersect(kvp.Value.Select(x => (string)x.part_name)).Any())
        .Select(kvp =>
        {
          var branchParts = mainBranchParts
            .Where(p => projectParts[kvp.Key].Select(x => (string)x.part_name).Except(parts).Contains((string)p["part_name"]))
            .Select(a => a).ToList();
          return UploadUpdated(branches[kvp.Key], branchParts);
        });
      var allTasks = new List<Task<string>> { targetTask };
      allTasks.AddRange(otherTasks);

      var results = new List<string>();
      while (allTasks.Count > 0)
      {
        var t = await Task.WhenAny(allTasks).ConfigureAwait(false);
        allTasks.Remove(t);
        try
        {
          results.Add(await t.ConfigureAwait(false));
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { }
      }

      return results;
    }

    private async Task<string> UploadUpdated(StreamWrapper branch, List<Base> branchParts)
    {
      var toUpload = new Base();
      toUpload["@{0}"] = branchParts;
      //var res = await Helpers.Send(branch.ToString(), toUpload, "GH Modified Commit", "GH").ConfigureAwait(false);
      var t = Task.Run(() => Helpers.Send(branch.ToString(), toUpload, "GH Modified Commit", "GH"));
      var res = await t.ConfigureAwait(false);
      return res;
    }

    private async Task<List<Base>> GetBranchParts(StreamWrapper branch)
    {
      var t = Task.Run(() => Helpers.Receive(branch.ToString()));
      var mainBranchBase = await t.ConfigureAwait(false);
      //var mainBranchBase = await Helpers.Receive(branch.ToString()).ConfigureAwait(false);
      var mainBranchObjects = (mainBranchBase["@data"] ?? mainBranchBase["@Data"]) as Base;
      var mainBranchParts = mainBranchObjects[mainBranchObjects.GetDynamicMembers().ToList()[0]] as List<object>;

      return mainBranchParts.Select(o => (Base)o).ToList();
    }
  }

  public class PartComparer : IEqualityComparer<object>
  {
    public new bool Equals(object x, object y)
    {
      return (string)((Base)x)["part_name"] == (string)((Base)y)["part_name"];
    }

    public int GetHashCode(object obj)
    {
      return ((Base)obj)["part_name"].GetHashCode();
    }
  }
}
