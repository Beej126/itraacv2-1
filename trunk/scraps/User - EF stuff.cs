using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections.Specialized;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;


namespace iTRAACv2
{
  class User : DbContext
  {

    /*
    private NameValueCollection _access = null;
    static public NameValueCollection Access
    {
      get { return ((Application.Current as App)._access); }
    }

        using (Proc UserAccess_s = new Proc("UserAccess_s"))
        {
          UserAccess_s["@UserGUID"] = "00000000-0000-0000-0000-000000000000";
          _access = UserAccess_s.ExecuteNameValueCollection();
        }
    */

    public static string LoginNameSansDomain
    {
      get
      { 
        string[] login = WindowsIdentity.GetCurrent().Name.Split('\\');
        return (login[login.Length-1]); 
      }
    }

    private Guid _UserGUID;
    public Guid UserGUID { get { return (_UserGUID); } }

    private static User _Current = null;
    public static User Current
    {
      get
      {
        if (_Current == null)
        {
          using(Proc User_ByLogin = new Proc("User_ByLogin"))
          {
            User_ByLogin["@LoginName"] = LoginNameSansDomain;
            User_ByLogin.ExecuteDataTable();
            _Current = new User() { _UserGUID = new Guid(User_ByLogin.Row0["UserGUID"].ToString()) };
          }
        }
        return(_Current);
      }
    }

    public DbSet<UserAccess> UserAccess { get; set; } //DBSet provides automatic mapping to table named same as variable
    private UserAccess _Access = null;
    public UserAccess Access
    {
      get
      {
        if (_Access == null)
        {
          _Access = UserAccess.Single(a => a.UserGUID == UserGUID);
        }
        return (_Access);
      }
    }


  }

  public class UserAccess
  {
    public Guid UserAccessGUID { get; set; }
    [Key]
    public Guid UserGUID { get; set; }
    public bool UnlockForm { get; set; }
  }

}
