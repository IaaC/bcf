using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Models;

namespace BCFSpeckleKit.Models
{
  public class PartObject : Base
  {
    public override string speckle_type => "BCF_Part";
#pragma warning disable IDE1006 // Naming Styles
    public string part_name { get; set; }
    public string project_code { get; set; }
    public string project_name { get; set; }
    public string project_deadline { get; set; }
    public string material { get; set; }
    public string thickness { get; set; }
    public double part_area { get; set; }
    public string client { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    //public object __geometry = null;
    //public double[][] transformation { get; set; } = null;

    [DetachProperty]
    public object original { get; set; } = null;

    [DetachProperty]
    public Base part_geometry { get; set; }

    public PartObject() { }
  }

  public class SheetObject : Base
  {
    public override string speckle_type => "BCF_Sheet";
#pragma warning disable IDE1006 // Naming Styles
    public string sheet_name { get; set; }
    public string deadline { get; set; }
    public string material { get; set; }
    public string thickness { get; set; }
    public double efficiency { get; set; }
    public double spacing { get; set; }

    public Base outline { get; set; }

    [DetachProperty]
    public List<Base> parts { get; set; }
#pragma warning restore IDE1006 // Naming Styles

    public SheetObject() { }
  }
}
