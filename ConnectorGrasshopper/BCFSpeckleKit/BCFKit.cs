using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Kits;
using BCFSpeckleKit.Models;

namespace BCFSpeckleKit
{
  public class BCFKit : ISpeckleKit
  {
    public IEnumerable<Type> Types => new Type[] { typeof(PartObject), typeof(SheetObject) };
    public IEnumerable<string> Converters => new string[] { }; // just return an empty list

    public string Description => "Adding Parts and Sheets objects for the BCF project to the Speckle culture.";

    public string Name => "BCF Kit";

    public string Author => "IAAC AAG (Ardeshir Talaei)";

    public string WebsiteOrEmail => "ardeshir.talaei@iaac.net";

    public ISpeckleConverter LoadConverter(string app)
    {
      return null;
    }
  }
}
