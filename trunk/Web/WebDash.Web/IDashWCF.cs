using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Data;

namespace WebDash.Web
{
  // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IDashWCF" in both code and config file together.
  [ServiceContract]
  public interface IDashWCF
  {
    [OperationContract]
    string SimpleTest();
    
    [OperationContract]
    IEnumerable<Dictionary<string, object>> ProcCall(string ProcName, params object[] parms);
  }
}
