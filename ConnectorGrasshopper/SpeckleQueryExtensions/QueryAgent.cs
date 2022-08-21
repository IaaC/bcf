using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Newtonsoft.Json.Linq;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace SpeckleQueryExtensions
{
  /// <summary>
  /// Get the ID of the referencedObject from a StreamWrapper.
  /// </summary>
  /// <param name="stream">StreamWrapper to extract the ReferencedObject from.</param>
  /// <returns>
  /// The raw result of the query as a JSON string.
  /// </returns>
  public class QueryAgent : Client
  {
    public QueryAgent() : base()
    {
      //QueryVariables = new ExpandoObject();
      layers = new List<Layer> { };
    }
    public QueryAgent(Account account) : base(account)
    {
      //QueryVariables = new ExpandoObject();
      layers = new List<Layer> { };
    }

    public class Layer
    {
      public List<QueryParam> queryParams = new List<QueryParam>();
      public int depth;
      public List<string> fields = new List<string>();

      public void AddQuery(string key, object value, string opt)
      {
        queryParams.Add(new QueryParam
        {
          field = key,
          value = value,
          Operator = opt,
        });
      }
    }

    public struct QueryParam
    {
      public string field;
      public object value;
      public string Operator;
    }

    public StreamWrapper Stream { get; set; }
    public string ObjectId { get; set; }
    public string QueryString { get; set; }

    public dynamic QueryVariables { get; set; }

    public List<Layer> layers;

    public async Task<string> GetRefObjId()
    {
      var commits = await StreamGetCommits(Stream.StreamId).ConfigureAwait(false);
      if (commits.Count <= 0) return null;

      switch (Stream.Type)
      {
        case StreamWrapperType.Commit:
          return commits.First(a => a.id == Stream.CommitId).referencedObject;
        case StreamWrapperType.Stream:
          return commits[0].referencedObject;
        case StreamWrapperType.Branch:
          var com = commits.FirstOrDefault(a => a.branchName == Stream.BranchName);
          if (com == null) return null;
          return com.referencedObject;
        case StreamWrapperType.Object:
          return Stream.ObjectId;
        default:
          throw new SpeckleException($"Input is not an Stream, Commit, or Branch. Cannot query {Stream.Type}");
      }
    }

    public Task<Dictionary<string, dynamic>> RunQuery()
    {
      return RunQuery(CancellationToken.None);
    }

    public async Task<Dictionary<string, dynamic>> RunQuery(CancellationToken cancellationToken)
    {
      var objRefs = new Dictionary<string, dynamic>();

      GenerateQueryString();
      await GenerateQueryVariables().ConfigureAwait(false);

      //if (string.IsNullOrEmpty(agent.QueryString))
      //  throw new Exception("Query String is null or empty.");
      //if (agent.QueryVariables == null)
      //  throw new Exception("Query Variables is null.");

      if (string.IsNullOrEmpty(QueryString) || QueryVariables == null)
        return objRefs;

      var request = new GraphQLRequest
      {
        Query = QueryString,
        Variables = QueryVariables
      };

      try
      {
        var res = await GQLClient.SendQueryAsync<dynamic>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null)
        {
          if (res.Errors.ToList().Any(e => e.Message.Contains("cannot cast")))
          {
            throw new SpeckleException("Query failed due to casting error, check types");
          }
          else
          {
            throw new SpeckleException(res.Errors.ToList().First().Message, res.Errors);
          }
        }

        //var resJson = JObject.Parse(res.Data.ToString());
        var resJson = JObject.Parse(res.Data.ToString());

        var resData = resJson.SelectTokens("$..result_data");
        foreach (var objData in resData)
        {
          objRefs[(string)((dynamic)objData).id] = (dynamic)objData;
          //var objRef = objData.ToObject<ObjectReference>();
          //objRef.referencedId = objData.SelectToken(".id").ToString();
          //objRefs[objRef] = objData.ToString();
        }

        return objRefs;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.InnerException.Message, e);
      }
    }

    public Layer AddLayer(int depth)
    {
      var layer = new Layer
      {
        depth = depth
      };
      layers.Add(layer);
      return layer;
    }

    public void GenerateQueryString()
    {
      var requestString = "result_data: data";
      var paramsString = "";
      for (var i = layers.Count - 1; i >= 0; i--)
      {
        paramsString += $", $params{i}: [JSONObject!]";

        var selectList = "";

        // TODO: Convert to LINQ code.
        if (layers[i].fields.Count > 0)  
          selectList = $", select:[{string.Join(",", layers[i].fields.Select(s => $"\"{s}\""))}]";
        //{
        //  selectList = ", select:[";
        //  foreach (var field in layers[i].fields)
        //  {
        //    selectList += $"\"{field}\", ";
        //  }
        //  selectList += "]";
        //}

        requestString = $@"
          result_layer{i}: data
          children (query: $params{i}, depth: {layers[i].depth}{selectList}) {{
            objects {{

          {requestString}

            }}
          }}
          ";
      }

      requestString = $@"
        query ($streamID: String!, $objectID: String!{paramsString}) {{
          stream(id: $streamID) {{
            name
            object (id: $objectID) {{
      
        {requestString}

            }}
          }}
        }}
        ";

      QueryString = requestString;
    }

    public async Task GenerateQueryVariables()
    {
      if (string.IsNullOrEmpty(ObjectId))
        ObjectId = await GetRefObjId().ConfigureAwait(false);

      if (string.IsNullOrEmpty(ObjectId))
      {
        return;
      }

      QueryVariables = new ExpandoObject();

      dynamic requestVariables = new ExpandoObject();

      requestVariables.streamID = $"{Stream.StreamId}";
      requestVariables.objectID = $"{ObjectId}";

      var reqValsColl = (IDictionary<string, object>)requestVariables;
      for (var i = 0; i < layers.Count; i++)
      {
        reqValsColl.Add($"params{i}", layers[i].queryParams);
      }

      QueryVariables = requestVariables;
    }
  }
}
