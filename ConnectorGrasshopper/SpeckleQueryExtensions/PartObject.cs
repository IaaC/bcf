using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Models;

namespace SpeckleQueryExtensions.Models
{
  public class PartObject : Base
  {
    public override string speckle_type => "BCF_Part";
    public string part_name { get; set; }
    public string project_code { get; set; }
    public string project_name { get; set; }
    public string project_deadline { get; set; }
    public string material { get; set; }
    public string thickness { get; set; }
    public double part_area { get; set; }

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
    public string sheet_name { get; set; }
    public string deadline { get; set; }
    public string material { get; set; }
    public string thickness { get; set; }
    public double efficiency { get; set; }
    public double spacing { get; set; }

    [DetachProperty]
    public Base outline { get; set; }

    [DetachProperty]
    public List<Base> parts { get; set; }

    public SheetObject() { }
  }

  public class StatusObject : Base
  {
    public override string speckle_type => "BCF_Status";

    public string part_status { get; set; }

    [DetachProperty]
    public object partObj { get; set; }

    public StatusObject() { }
  }
}
