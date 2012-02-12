using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Data;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using System.Security.Principal;
using System.Configuration;

namespace WebDash.Web
{
  //nugget: [ServiceBehavior(IncludeExceptionDetailInFaults = true)] attribute defined on WCF web service class allows full exception detail to propagate to client (e.g. silverlight)
  // note: should be disabled in production for security (once everything has stabilized)
  [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
  public class DashWCF : IDashWCF
  {

    static DashWCF()
    {
      //nugget: if you get exceptions in the web service constructor you're not going to get any exception detail back
      // it will only say "The type initializer for 'WebDash.Web.DashWCF' threw an exception."
      // so the best way to debug is to move the code out of the constructor and down to the "SimpleTest" method
      // which will return full exception info at that point.
    }

    public string SimpleTest()
    {
      return ("does this work!?!?");
    }

    public IEnumerable<Dictionary<string, object>> ProcCall(string ProcName, params object[] parms)
    {
      try
      {
        if (Proc.ConnectionString == null)
        {
          Proc.ConnectionString = ConfigurationManager.ConnectionStrings["iTRAACv2ConnectionString"].ConnectionString;
          Proc.ConnectionString += WebDash.Web.Properties.Settings.Default.iTRAACv2ConnectionString;
        }

        using (Proc Proc1 = new Proc(ProcName))
        {
          if (parms != null) for (int i = 0; i < parms.Length; i += 2) Proc1[parms[i].ToString()] = parms[i + 1];
          return (Proc1.ExecuteDataSet(false).Table0.ToSimpleTable());
        }
      }
      catch (Exception ex)
      {
        throw new Exception(
          "** Exception occurred***\r\rRunning under account: " + WindowsIdentity.GetCurrent().Name + 
          "\r\rMessage:\r" + ex.Message +
          ((ex.Message.Contains("RSA"))?
            "\r\rTip: If you get the error \"The RSA key container could not be opened.\"," +
              "\rthen you need to run the \"RSAKey_Manager.cmd\" on the web server." : "") +
          "\r\rStack: \r" + ex.StackTrace);
      }

    }

  }
}
