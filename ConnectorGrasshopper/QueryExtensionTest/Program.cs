using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Credentials;
using SpeckleQueryExtensions;
using System.Data.SQLite;
using Speckle.Core.Models;
using Speckle.Core.Serialisation;
using Speckle.Core.Transports;
using Newtonsoft.Json.Linq;
using BCFSpeckleLibrary;

namespace QueryExtensionTest
{
  internal class Program
  {
    static void Main()
    {
      Console.WriteLine("Hello World");

      var acc = AccountManager.GetDefaultAccount();
      Console.WriteLine(acc.userInfo.name);
      Console.WriteLine(acc.serverInfo.url);
      var stream = new StreamWrapper("http://bettercncfactory.iaac.net/streams/92a0ebb273");

      Console.ForegroundColor = ConsoleColor.Cyan;
      Console.WriteLine("\n\n======== BRANCH TEST\n");
      Console.ResetColor();

      var branchMan = new BCFBranch(acc, "TestProject");
      branchMan.MoveToBranch("nested", new List<string> { "tp_mdf_6", "tp_mdf_7" }).Wait();


      Console.ForegroundColor = ConsoleColor.Cyan;
      Console.WriteLine("\n\n======== QUERY TEST\n");
      Console.ResetColor();

      var agent = new QueryAgent(acc)
      {
        Stream = stream
      };
      agent.AddLayer(10);
      agent.layers[0].AddQuery("material", "mdf", "=");
      agent.layers[0].fields = new List<string> { "part_name", "material", "speckle_type" };
      agent.GenerateQueryVariables().Wait();
      agent.GenerateQueryString();
      var queryResult = agent.RunQuery().Result;
      foreach (var kvp in queryResult)
      {
        Console.WriteLine(kvp.Key);
        Console.WriteLine(kvp.Value.ToString());
        Console.WriteLine();
      }

      foreach (var kvp in queryResult)
      {
        //var jRes = JObject.Parse(kvp.Value);
        Console.WriteLine("Testing . . .");
        //Console.WriteLine(string.Join(",, ", jRes.Children().Select(r => r.First.ToString()).ToList()));
        Console.WriteLine(string.Join(",, ", ((JObject)kvp.Value).Children().Select(r => r.First.ToString()).ToList()));
      }



      Console.ForegroundColor = ConsoleColor.Cyan;
      Console.WriteLine("\n\n======== PROJECT TEST\n");
      Console.ResetColor();

      var project = new BCFProject(acc, "project2");
      Console.WriteLine(project.StreamId);
      project.branches.ToList().ForEach(branch => Console.WriteLine(branch.Key));
      project.projectParts.ToList().ForEach(part => Console.WriteLine(part.Key));
      project.ValidateProject().Wait();



      Console.ForegroundColor = ConsoleColor.Cyan;
      Console.WriteLine("\n\n======== SHEETS TEST\n");
      Console.ResetColor();

      var sheetsMan = new BCFSheets(acc, "mdf", 12);
      Console.WriteLine(sheetsMan.SheetsStream);
      Console.WriteLine(sheetsMan.SheetBranchQueue);
      Console.WriteLine(sheetsMan.SheetBranchDone);
      
      sheetsMan.ValidateSheets().Wait();
      sheetsMan.Sheets.Keys.ToList().ForEach(n => Console.WriteLine(n));


      Console.WriteLine("Press any key to exit . . .");
      Console.ReadKey();
    }
  }
}
