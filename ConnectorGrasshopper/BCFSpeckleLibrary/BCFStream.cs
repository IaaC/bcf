using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using SpeckleQueryExtensions;

namespace BCFSpeckleLibrary
{
  abstract public class BCFStream
  {
    public Account account { get; set; }
    protected Client client;
    public StreamWrapper SheetsStream { get; set; }
    protected List<Stream> streams;
    public BCFStream (Account account)
    {
      this.account = account;
      client = new Client (account);
      GetAllStreams().Wait();
    }

    private async Task GetAllStreams ()
    {
      streams = await client.StreamsGet(40).ConfigureAwait(false);
      SheetsStream = new StreamWrapper(streams.First(a => a.name == "sheets").id);
    }

    public async Task<List<dynamic>> GetSOByField(StreamWrapper stream, List<string> fields = default, string byField = "part_name")
    {
      var agent = new QueryAgent(account)
      {
        Stream = stream,
      };
      agent.AddLayer(10).AddQuery(byField, "", "!=");
      agent.layers[0].fields.AddRange(new List<string> { byField, "id" });
      if (fields != null) agent.layers[0].fields.AddRange(fields);

      var queryResult = await agent.RunQuery().ConfigureAwait(false);
      var prts = queryResult?.Select(kvp => kvp.Value).ToList();

      return prts;
    }
  }
}
