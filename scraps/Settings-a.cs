using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iTRAACv2
{
  class Settings
  {
    static readonly public Settings Current = new Settings();

    public string OfficeCode { get; set; }
    public string AdminPassword { get; set; }

    private Settings() {
      using (Proc Settings_s = new Proc("Settings_s"))
      {
        Settings_s.ExecuteDataTable();

        OfficeCode = Settings_s.Row0["OfficeCode"].ToString();
        AdminPassword = Settings_s.Row0["AdminPassword"].ToString();
      }
    }


  }
}
