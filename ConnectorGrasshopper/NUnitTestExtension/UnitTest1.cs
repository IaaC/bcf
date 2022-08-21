using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Serialisation;
using Speckle.Core.Transports;
using SpeckleQueryExtensions;
namespace NUnitTestExtension
{
  public class Tests
  {
    public string streamId = "473ca261d5";
    public StreamWrapper stream = new StreamWrapper("http://bettercncfactory.iaac.net/streams/8163da49cd/commits/215e302e78");

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void QueryObjects_ByType_Count()
    {
      var acc = AccountManager.GetDefaultAccount();
      var client = new QueryExtensions(acc);

      var qList2 = new Dictionary<(string, dynamic), string>();
      qList2[("speckle_type", "PartObject")] = "=";
      var uploadedStats = client.ObjectsFilter(stream, qList2).Result;

      Assert.Greater(uploadedStats.Count, 0);
    }

    [Test]
    public void ReplaceObjectWithRef_Base_EqualID()
    {
      //var serializer = new BaseObjectSerializerV2();
      var transport = new MemoryTransport { TransportName = "MT" };
      //serializer.WriteTransports.Add(transport1);

      var objChild = new Base();
      objChild["sample_field"] = "Test Value";
      var idChild = objChild.GetId();

      var objMain = new Base();
      objMain["@child"] = objChild;


      ObjectReference objRef = new ObjectReference() { referencedId = idChild };
      var objWRef = new Base();
      objWRef["@child"] = objRef;

      var idMain = Operations.Send(
            objMain,
            CancellationToken.None,
            new List<ITransport> { transport },
            useDefaultCache: false,
            onProgressAction: y => { },
            onErrorAction: (x, z) => { },
            disposeTransports: false).Result;

      var idWRef = Operations.Send(
            objWRef,
            CancellationToken.None,
            new List<ITransport> { transport },
            useDefaultCache: false,
            onProgressAction: y => { },
            onErrorAction: (x, z) => { },
            disposeTransports: true).Result;

      Assert.AreEqual(idWRef, idMain);
    }

    [Test]
    public void SerializationTest()
    {
      // Make some fake Base Objects
      var main = new Base();
      var childs = new List<Base>();
      for (int i = 0; i < 7; i++)
      {
        var obj = new Base();
        obj["SampleFiled"] = i;
        childs.Add(obj);
      }
      main["@Childs"] = childs;

      var transport = new MemoryTransport();
      transport.TransportName = "MT";

      var BaseId = Operations.Send(
            main,
            CancellationToken.None,
            new List<ITransport> { transport },
            useDefaultCache: false,
            onProgressAction: y => { },
            onErrorAction: (x, z) => { },
            disposeTransports: true).Result;

      Assert.IsNotNull(BaseId);
      Assert.AreNotEqual(BaseId, "");
      Assert.Pass();
    }

  }
}
