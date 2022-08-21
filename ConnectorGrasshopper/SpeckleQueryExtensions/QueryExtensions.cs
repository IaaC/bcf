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
  public class QueryExtensions : Client
  {

    public QueryExtensions() : base()
    { }

    public QueryExtensions(Account account) : base(account)
    { }

    /// <summary>
    /// Get the ID of the referencedObject from a StreamWrapper.
    /// </summary>
    /// <param name="stream">StreamWrapper to extract the ReferencedObject from.</param>
    /// <returns>
    /// The raw result of the query as a JSON string.
    /// </returns>
    public async Task<string> GetRefObjId(StreamWrapper stream)
    {
      var commits = await StreamGetCommits(stream.StreamId);

      switch (stream.Type)
      {
        case StreamWrapperType.Commit:
          return commits.First(a => a.id == stream.CommitId).referencedObject;
        case StreamWrapperType.Stream:
          return commits[0].referencedObject;
        case StreamWrapperType.Branch:
          return commits.First(a => a.branchName == stream.BranchName).referencedObject;
        case StreamWrapperType.Object:
          return stream.ObjectId;
        default:
          throw new SpeckleException($"Input is not an Stream, Commit, or Branch. Cannot query {stream.Type}");
      }
    }

    /// <summary>
    /// Query objects in a stream considering the part_status.
    /// </summary>
    /// <param name="streamId">ID of the stream to query</param>
    /// <param name="status">Status of the parts of interest</param>
    /// <param name="queryParams">Query fileds and values to query for</param>
    /// <returns>
    /// The raw result of the query as a JSON string.
    /// </returns>
    public Task<Dictionary<ObjectReference, string>> ObjectsFilterWithStatus(
      StreamWrapper stream,
      String status,
      Dictionary<(string, dynamic), string> queryParams,
      int limit = 10)
    {
      return ObjectsFilterWithStatus(CancellationToken.None, stream, status, queryParams, limit);
    }

    /// <summary>
    /// Query objects in a stream using a part_status.
    /// </summary>
    /// <param name="streamId">ID of the stream to query</param>
    /// <param name="status">Status of the parts of interest</param>
    /// <param name="queryParams">Query fileds and values to query for</param>
    /// <returns>
    /// The raw result of the query as a JSON string.
    /// </returns>
    public async Task<Dictionary<ObjectReference, string>> ObjectsFilterWithStatus(
      CancellationToken cancellationToken,
      StreamWrapper stream,
      String status,
      Dictionary<(string, dynamic), string> queryParams,
      int limit = 10)
    {
      var objectId = await GetRefObjId(stream);
      if (string.IsNullOrEmpty(objectId))
        throw new SpeckleException("referencedObject not found, check stream's commits.");

      var variablesList = new List<object>();
      foreach (var queryParam in queryParams)
      {
        variablesList.Add(
           new
           {
             field = queryParam.Key.Item1,
             value = queryParam.Key.Item2,
             Operator = queryParam.Value
           }
       );
      }

      var statusVariable = new
      {
        field = "part_status",
        value = status,
        Operator = "="
      };

      try
      {
        var request = new GraphQLRequest
        {
          Query = @"
query ($streamID: String!, $objectID: String!, $pcStatus: [JSONObject!], $paramsQr: [JSONObject!]) {
  stream(id: $streamID) {
    name
    object(id: $objectID) {
      children(
        select: [
          ""id"",
          ""part_status""
        ],
        query: $pcStatus
      ) {
        totalCount
        parts_list: objects {
          status_info: data
          children (query: $paramsQr) {
            objects {
              id
              part_object: data
            }
          }
        }
      }
    }
  }
}
          ",
          Variables = new
          {
            streamID = $"{stream.StreamId}",
            objectID = $"{objectId}",
            pcStatus = statusVariable,
            paramsQr = variablesList
          }
        };

        var res = await GQLClient.SendQueryAsync<dynamic>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not get streams", res.Errors);

        var resJson = JObject.Parse(res.Data.ToString()).SelectToken("$..parts_list");

        Dictionary<ObjectReference, string> objRefs = new Dictionary<ObjectReference, string>();
        var resData = resJson.SelectTokens("$..part_object");
        foreach (var objData in resData)
        {
          var objRef = objData.ToObject<ObjectReference>();
          objRef.referencedId = objData.SelectToken(".id").ToString();
          objRefs[objRef] = objData.ToString();
        }

        return objRefs;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    /// Query objects in a stream.
    /// </summary>
    /// <param name="stream">The stream to query</param>
    /// <param name="queryParams">Query fileds and values to query for</param>
    /// <returns>
    /// The raw result of the query as a JSON string.
    /// </returns>
    public Task<Dictionary<ObjectReference, string>> ObjectsFilter(
      StreamWrapper stream,
      Dictionary<(string, dynamic), string> queryParams)
    {
      return ObjectsFilter(CancellationToken.None, stream, queryParams);
    }

    /// <summary>
    /// Query objects in a stream considering the part_status.
    /// </summary>
    /// <param name="stream">The stream to query</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <param name="queryParams">Query fileds and values to query for</param>
    /// <returns>
    /// The raw result of the query as a JSON string.
    /// </returns>
    public async Task<Dictionary<ObjectReference, string>> ObjectsFilter(
            CancellationToken cancellationToken,
            StreamWrapper stream,
            Dictionary<(string, dynamic), string> queryParams)
    {
      var objectId = await GetRefObjId(stream);

      if (string.IsNullOrEmpty(objectId))
        throw new SpeckleException("referencedObject not found, check stream's commits.");

      var variablesList = new List<object>();
      foreach (var queryParam in queryParams)
      {
        variablesList.Add(
           new
           {
             field = queryParam.Key.Item1,
             value = queryParam.Key.Item2,
             Operator = queryParam.Value
           }
        );
      }

      var request = new GraphQLRequest
      {
        Query = @"
query ($streamID: String!, $objectID: String!, $paramsQr: [JSONObject!]) {
  stream(id: $streamID) {
    name
    object (id: $objectID) {
      children(query: $paramsQr) {
        result_list: objects {
          result_data: data
        }
      }
    }
  }
}
          ",
        Variables = new
        {
          streamID = $"{stream.StreamId}",
          objectID = $"{objectId}",
          paramsQr = variablesList
        }
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

        var resJson = JObject.Parse(res.Data.ToString()).SelectToken("$..result_list");

        var objRefs = new Dictionary<ObjectReference, string>();
        var resData = resJson.SelectTokens("$..result_data");
        foreach (var objData in resData)
        {
          var objRef = objData.ToObject<ObjectReference>();
          objRef.referencedId = objData.SelectToken(".id").ToString();
          objRefs[objRef] = objData.ToString();
        }

        return objRefs;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }
  }
}

