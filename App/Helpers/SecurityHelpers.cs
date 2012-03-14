using System;
using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;

// ReSharper disable CheckNamespace
class SecurityHelpers
// ReSharper restore CheckNamespace
{
  static public string CurrentWindowsLoginNameSansDomain
  {
    get
    {
      var windowsIdentity = WindowsIdentity.GetCurrent();
      if (windowsIdentity == null) return (null);
      var login = windowsIdentity.Name.Split('\\');
      return (login[login.Length - 1]);
    }
  }

  static public bool IsUserInGroup(string userName, string groupName)
  {
    //nugget: unfortunately, adding the current Windows User to a new Group via the Windows CompMgmt.msc GUI does not register in the WindowsIdentity data structures until after a logout/login cycle
    //nugget: what i read was that the WindowsIdentity info is based on security tokens and the tokens are cached _ONLY_ at login time and is absolutely *not*refreshable* in any way other than logging out and back in
    //nugget: (e.g. WindowsIdentity.GetCurrent().Groups)
    //(from grp in WindowsIdentity.GetCurrent().Groups
    // where grp.Translate(typeof(NTAccount)).Value.ToLower().Contains("iTRAAC_Users".ToLower())
    // select 1).Count();

    //nugget: especially under the oppresive yoke of Vista, it is prohibitively annoying to force a user log out/in at applicaiton install time
    //nugget: and it's easily forgotten to do this up front
    //nugget: so ideally we support the scenario of launching CompMgmt.msc under temporary admin credentials and adding the user to this group on the fly
    //nugget: (while leaving the current user logged in to the desktop)
    //nugget: this is accomplished by using the ActiveDirectory API's to pull the list of fresh group members thus avoiding the above cached token issue
    string step = "";
    try
    {
      step = "1";
      //best to go after the local group and then check members so that we can handle a domain user being in our local group
      var group = new DirectoryEntry("WinNT://./" + groupName + ",group"); //nugget: 
      step = "2";
      var members = group.Invoke("members") as IEnumerable;
      step = "3";
      Debug.Assert(members != null, "members != null");
      return (members.Cast<object>().Any(g => new DirectoryEntry(g).Name == userName));

      //foreach (object group in groups) Console.WriteLine(new DirectoryEntry(group).Name);
      //DirectoryEntry iTRAAC_Users = new DirectoryEntry("WinNT://./" + GroupName + ",group"); //nugget: 
      //IEnumerable members = iTRAAC_Users.Invoke("members") as IEnumerable; //nugget: Group.Invoke("members") worked, Group.Children was always empty
      //string CurrentUserName = WindowsLoginNameSansDomain;
      //foreach (object member in members) if (new DirectoryEntry(member).Name == CurrentUserName) return (true);
    }
    catch (/*System.Runtime.InteropServices.COMException*/ Exception ex)
    {
      //nugget: on my dev Win7 box, when the iTRAAC_Users group was not yet created, i'd get a nice COMException with coherent message text
      //nugget: but on field Vista boxes i just get a "Uknown error (0x800708ac)" which seems to be correspond to the same thing given the Google hits
      if (ex.Message.Contains("The group name could not be found")
        || ex.Message.Contains("0x800708ac")) return (false);
      throw (new Exception(String.Format("Error occurred attempting to access local user/group info @step {0}\r\n\r\n{1}\r\n{2}", 
                                         step, ex.GetType(), ex.Message)));
    }
  }

  //nugget: good explanation of encrypting .config connection strings: http://msdn.microsoft.com/en-us/library/ms998283.aspx
  //nugget: see SecurityHelpers.cs, App.config & "RSAKey_Export.cmd" in the root application development folder as well
  //nugget: key point - "you must create a custom RSA encryption key container and deploy the same key container on all servers in your Web farm. This won't work by default because the default RSA encryption key, "NetFrameworkConfigurationKey", is different for each computer."
  static public void ToggleConnectionStringsEncryption()
  {
    // Takes the executable file name without the
    // .config extension.

    // Open the configuration file and retrieve the connectionStrings section.
    // WARNING: System.AppDomain.CurrentDomain.FriendlyName supposedly doesn't return what you'd expect under click-once deployment ("DefaultDomain" or something like that)
    var config = ConfigurationManager.OpenExeConfiguration(AppDomain.CurrentDomain.FriendlyName);

    var section = config.GetSection("connectionStrings") as ConnectionStringsSection;

    Debug.Assert(section != null, "section != null");
    if (section.SectionInformation.IsProtected)
    {
      section.SectionInformation.UnprotectSection();
    }
    else
    {
      section.SectionInformation.ProtectSection("ExportableRsaCryptoServiceProvider");
    }

    config.Save();
  }


}
