using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Specialized;
using System.Runtime.Serialization.Formatters.Soap;
using System.Runtime.Serialization;
using System.Linq;
using System.Data;
using System.Collections.Generic;
using System;

// Settings Approach Explained
// There are four different types of settings "bags", which are accessed & persisted thusly:
//
// 1) "Trivial" *User* specific settings - used for persisting various UI preferences, accessed via "Properties.Settings.Default.{PropertyName}", persisted in the users local AppData folder (C:\Users\{username}\AppData\Local\iTRAACv2), good example: the users preferred zoom level (aka font size)
//
// 2) Database settings - persisted in the iTRAACv2.dbo.Settings, futher broken down between...
//    a) *global* settings (e.g. MaxClass1FormsCount) accessed via Settings.Global["key"] and 
//    b) local *office* specific settings (e.g. the matrix printer mappings) accessed via Settings.Local["key"]
//
//    this Settubgs database table will be replicated up to the central server all the global settings will be kept consistent across offices (e.g. updating the admin override password)
//    (the office specific settings won't be changing often enough to worry about an inordinate amount of replication chatter)

namespace iTRAACv2
{
  public class SettingsModel
  {
    static public int MaxClass1FormsCount { get { return (System.Convert.ToInt16(Global["MaxClass1FormsCount"])); } }

    //make sure that OfficeCode remains something that is set on a client connection by connection basis
    //the point being, leave open the possibility of connecting folks directly into the central DB while still maintaining their individual office context
    static public string TaxOfficeCode { get { return (Properties.Settings.Default.TaxOfficeCode); } }

    static public int TaxOfficeId { get; private set; }
    static public double RoughUSDToEuroRate { get { return (System.Convert.ToDouble(Global["RoughUSDToEuroRate"])); } }
    static public double TaxFormTotalCostMin { get { return (System.Convert.ToDouble(Global["TaxFormTotalCostMin"])); } }

    static public SettingsModel Global { get; private set; }
    static public SettingsModel Local { get; private set; }

    static SettingsModel()
    {
      if (WPFHelpers.DesignMode) return;

      //load db settings...
      //realized this should parameterized on OfficeCode so that we leave the option open to connect directly to the central DB, so using "iTRAACProc" which supplies OfficeCode, vs generic "Proc"
      using (Proc Settings_s = new Proc("Settings_s"))
      {
        Settings_s["@TaxOfficeCode"] = TaxOfficeCode;
        Settings_s.ExecuteDataSet();

        Global = new SettingsModel(Settings_s.Tables[0]);
        Local = new SettingsModel(Settings_s.Tables[1]);

        TaxOfficeId = (int)Settings_s["@TaxOfficeId"];
      }
    }

    //ensure this class is only called via static members
    private SettingsModel() { }

    private SettingsModel(DataTable table)
    {
      this.table = table;
      this.table.PrimaryKey = new DataColumn[] { this.table.Columns["Name"] };
    } 

    private DataTable table;

    public string this[string name]
    {
      get
      {
        DataRow r = table.Rows.Find(name);
        return (r != null ? r["Value"].ToString() : null);
      }
      set
      {
        DataRow r = table.Rows.Find(name);
        if (r == null) 
        {
          r = table.NewRow();
          r.ItemArray = new object[] { name, value };
          table.Rows.Add(r); //i always forget this!!!
        }
        else r["Value"] = value;
      }
    }

    static public void SaveLocalSettings(bool DisplaySuccess = false)
    {
      using (Proc Settings_u = new iTRAACProc("Settings_u"))
      {
        Settings_u["@Settings"] = Local.table;
        Settings_u.ExecuteNonQuery("Settings - ", DisplaySuccess);
      }
    }

  }

  public static class SettingsModelExtensions
  {
    public static T Field<T>(this SettingsModel settings, string FieldName)
    {
      return UnboxT<T>.Unbox(settings[FieldName]);
    }
  }

}


namespace iTRAACv2.Properties
{
  public partial class Settings
  {
    //declare destructor on Dynamic Properties to ensure those are saved when app shuts down
    ~Settings()
    {
      Save(); 
    }
  }

}

