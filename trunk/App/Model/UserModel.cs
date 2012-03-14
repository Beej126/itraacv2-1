using System;
using System.Data;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace iTRAACv2.Model
{
  class UserModel
  {
    static private UserModel _current; 
    static public UserModel Current
    {
      get
      {
        if (_current == null && !WPFHelpers.DesignMode)
        {
          _current = new UserModel();
// ReSharper disable InconsistentNaming
          using (var User_Login = new Proc("User_Login"))
// ReSharper restore InconsistentNaming
          {
            User_Login["@TaxOfficeId"] = SettingsModel.TaxOfficeId;
            User_Login["@LoginName"] = _current.LoginName;
            User_Login["@TaxOfficeId"] = SettingsModel.TaxOfficeId;
            if (User_Login.ExecuteDataSet().Row0 == null)
            {
              MessageBox.Show(String.Format("Login '{0}' not registered in database", _current.LoginName));
              Application.Current.Shutdown();
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
    public string LoginName { get { return (SecurityHelpers.CurrentWindowsLoginNameSansDomain); } }
    public string Name { get; private set; }

    public UserAccess Access { get; private set; }
    public class UserAccess : INotifyPropertyChanged
    {
      /// <summary>
      /// tie most (all?) properties to IsAdmin as an ultimate flag no matter what else is currently set
      /// </summary>

      private bool _hasUnlockForm;
      public bool HasUnlockForm
      { 
        private set
        {
          _hasUnlockForm = value;
          OnPropertyChanged("HasUnlockForm");
        }
        get
        {
          return (IsAdmin || _hasUnlockForm);
        }
      }

      #region Admin Override Logic
      public bool IsAdmin { private set { _isAdmin = value; OnPropertyChanged("IsAdmin"); OnPropertyChanged("HasUnlockForm"); } get { return (_isAdmin); } } private bool _isAdmin;
      public DateTime AdminOverrideRemaining { private set { _adminOverrideRemaining = value; OnPropertyChanged("AdminOverrideRemaining"); OnPropertyChanged("IsAdminOverride"); } get { return (_adminOverrideRemaining); } } private DateTime _adminOverrideRemaining = DateTime.Parse("00:00:00");
      public bool IsAdminOverride { get { return (AdminOverrideRemaining.Minute > 0 || AdminOverrideRemaining.Second > 0); } }

      private const int AdminOverrideTimerPeriod = 5000; //ticks every 5 seconds, rather than every 1 second just so we're not overly chatty to our listeners
      private Timer _adminOverrideTimer;
      public bool AdminOverride(string adminPassword, int rollbackMinutes)
      {
        using (var userAdminOverride = new Proc("User_AdminOverride"))
        {
          userAdminOverride["@Password"] = adminPassword;
          IsAdmin = bool.Parse(userAdminOverride.ExecuteNonQuery()["@Result"].ToString());
        }

        if (IsAdmin)
        {
          AdminOverrideRemaining = DateTime.Parse("00:" + rollbackMinutes.ToString(CultureInfo.InvariantCulture) + ":00");

          if (_adminOverrideTimer == null) _adminOverrideTimer =
            // ReSharper disable RedundantArgumentName
            new Timer(AdminOverrideTimerTick, 
                      state: null, 
                      dueTime: 0 /*start immediately*/,
                      period: AdminOverrideTimerPeriod);
          else _adminOverrideTimer.Change(dueTime: 0, period: AdminOverrideTimerPeriod);
          // ReSharper restore RedundantArgumentName
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
        AdminOverrideRemaining = AdminOverrideRemaining.AddSeconds(-1 * AdminOverrideTimerPeriod / 1000);
        if (AdminOverrideRemaining.Minute == 0 && AdminOverrideRemaining.Second == 0)
          AdminOverrideCancel();
      }

      public void AdminOverrideCancel()
      {
        // ReSharper disable RedundantArgumentName
        if (_adminOverrideTimer != null) _adminOverrideTimer.Change(Timeout.Infinite, period: AdminOverrideTimerPeriod); //stop the ticks
        // ReSharper restore RedundantArgumentName
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

        var ua = new UserAccess
                   {IsAdmin = Convert.ToBoolean(r["IsAdmin"]), HasUnlockForm = Convert.ToBoolean(r["HasUnlockForm"])};


        return (ua);
      }

      private UserAccess() {}

    }

  }

}
