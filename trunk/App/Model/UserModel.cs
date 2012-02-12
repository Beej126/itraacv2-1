using System;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Data;

using System.ComponentModel;
using System.Threading;

namespace iTRAACv2
{
  class UserModel
  {
    static private UserModel _current = null; 
    static public UserModel Current
    {
      get
      {
        if (_current == null && !WPFHelpers.DesignMode)
        {
          _current = new UserModel();
          using (Proc User_Login = new Proc("User_Login"))
          {
            User_Login["@TaxOfficeId"] = SettingsModel.TaxOfficeId;
            User_Login["@LoginName"] = _current.LoginName;
            User_Login["@TaxOfficeId"] = SettingsModel.TaxOfficeId;
            if (User_Login.ExecuteDataSet().Row0 == null)
            {
              System.Windows.MessageBox.Show(String.Format("Login '{0}' not registered in database", _current.LoginName));
              App.Current.Shutdown();
              return (null);
            }

            _current.GUID = (Guid)User_Login.Row0["UserGUID"];
            _current.Access = UserAccess.LoadBO(User_Login.Row0);
            _current.Name = User_Login.Row0["Name"].ToString();
          }

        }
        return (_current);
      }
    }

    private UserModel(){}

    public Guid GUID { get; private set; }
    public string LoginName { get { return (SecurityHelpers.CurrentWindowsLoginName_SansDomain); } }
    public string Name { get; private set; }

    public UserAccess Access { get; private set; }
    public class UserAccess : INotifyPropertyChanged
    {
      /// <summary>
      /// tie most (all?) properties to IsAdmin as an ultimate flag no matter what else is currently set
      /// </summary>

      private bool _HasUnlockForm = false;
      public bool HasUnlockForm
      { 
        private set
        {
          _HasUnlockForm = value;
          OnPropertyChanged("HasUnlockForm");
        }
        get
        {
          return (IsAdmin || _HasUnlockForm);
        }
      }

      #region Admin Override Logic
      public bool IsAdmin { private set { _IsAdmin = value; OnPropertyChanged("IsAdmin"); OnPropertyChanged("HasUnlockForm"); } get { return (_IsAdmin); } } private bool _IsAdmin = false;
      public DateTime AdminOverrideRemaining { private set { _AdminOverrideRemaining = value; OnPropertyChanged("AdminOverrideRemaining"); OnPropertyChanged("IsAdminOverride"); } get { return (_AdminOverrideRemaining); } } private DateTime _AdminOverrideRemaining = DateTime.Parse("00:00:00");
      public bool IsAdminOverride { get { return (AdminOverrideRemaining.Minute > 0 || AdminOverrideRemaining.Second > 0); } }

      private const int _AdminOverrideTimerPeriod = 5000; //ticks every 5 seconds, rather than every 1 second just so we're not overly chatty to our listeners
      private Timer _AdminOverrideTimer = null;
      public bool AdminOverride(string AdminPassword, int RollbackMinutes)
      {
        using (Proc User_AdminOverride = new Proc("User_AdminOverride"))
        {
          User_AdminOverride["@Password"] = AdminPassword;
          IsAdmin = bool.Parse(User_AdminOverride.ExecuteNonQuery()["@Result"].ToString());
        }

        if (IsAdmin)
        {
          AdminOverrideRemaining = DateTime.Parse("00:" + RollbackMinutes.ToString() + ":00");

          if (_AdminOverrideTimer == null) _AdminOverrideTimer = new Timer(new TimerCallback(AdminOverrideTimerTick), state: null, dueTime: 0 /*start immediately*/, period: _AdminOverrideTimerPeriod);
          else _AdminOverrideTimer.Change(dueTime: 0, period: _AdminOverrideTimerPeriod);
        }

        return (IsAdmin);
      }

      //brent:everything works fine and dandy, we've got a temporary admin mode, with a visual countdown timer 
      //the one thing that buggers me is that we're a bit chatty with our listeners ... 
      //must always remember that it's not simply the observers of the one changed propertyName that are notified, 
      //rather, it is everything that is observing this instance that is getting an update message every tick
      //INotifyPropertyChanged is executing at the class/object level, not at the granular propertyName level
      //sooo... what would be preferable ways to implement??
      //the challenge to me at the moment is that the controller of the countdown should be this centralized class
      //so i'm going to change the period to every 5 seconds for now and leave it be
      private void AdminOverrideTimerTick(object state)
      {
        AdminOverrideRemaining = AdminOverrideRemaining.AddSeconds(-1 * _AdminOverrideTimerPeriod / 1000);
        if (AdminOverrideRemaining.Minute == 0 && AdminOverrideRemaining.Second == 0)
          AdminOverrideCancel();
      }

      public void AdminOverrideCancel()
      {
        if (_AdminOverrideTimer != null) _AdminOverrideTimer.Change(Timeout.Infinite, period: _AdminOverrideTimerPeriod); //stop the ticks
        AdminOverrideRemaining = DateTime.Parse("00:00:00");
        IsAdmin = false; //revert to non-admin status
      }

      #endregion

      public event PropertyChangedEventHandler PropertyChanged;
      protected void OnPropertyChanged(string property)
      {
        if (PropertyChanged != null)
          PropertyChanged(this, new PropertyChangedEventArgs(property));
      }

      static public UserAccess LoadBO(DataRow r) 
      {
        if (r == null) throw (new Exception("UserAccess.LoadBO - supplied DataRow is null")); 

        UserAccess ua = new UserAccess();

        ua.IsAdmin = Convert.ToBoolean(r["IsAdmin"]);
        ua.HasUnlockForm = Convert.ToBoolean(r["HasUnlockForm"]);

        return (ua);
      }

      private UserAccess() {}

    }

  }

}
