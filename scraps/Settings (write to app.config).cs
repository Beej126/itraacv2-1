using System.Configuration;
using System.IO;
using System.IO.IsolatedStorage;

namespace iTRAACv2
{
  class Settings
  {
    static public int MaxClass1FormsCount { get { return (System.Convert.ToInt16(_DBSettings["MaxClass1FormsCount"])); } }
    //make sure that this property remains something that is set on a client connection by connection basis
    //the point being, leave open the possibility of connecting folks directly into the central DB while still maintaining their individual office context
    
    //[10 May 2011] centralizing is a recurring theme 
    //practically speaking, well into 2011 there continues to be a chronic amount of connectivity downtime out there in the field... check the "Downtime" database on 144.170.180.80 to see what I mean
    //similarly, rectrac has hit major downtime as a result of credit card processing network issues... credit card is simply not something that can run autonomously, so there's no choice there
    //but on systems like iTRAAC, where we do have a choice for offices to continue running on a local server for short durations, it seems strategic to preserve that benefit
    //however, it is worth at least considering migrating certain *very*stable* offices to depend on the full time connectivity of running directly off the central database
    static public string OfficeCode { get { return (Properties.Settings.Default.OfficeCode); } }

    static public SettingElementCollectionWrapper App { get; private set; } //e.g. App["key"].Value

    static public void Save()
    {
      //appConfig.Save();
      //IsolatedStorageFile appScope = IsolatedStorageFile.GetMachineStoreForApplication();
      //IsolatedStorageFileStream appsettings = new IsolatedStorageFileStream("AppSettings.config", FileMode.OpenOrCreate, appScope);
    }

    
    static private System.Collections.Specialized.NameValueCollection _DBSettings = null;
    static private Configuration appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
    static private ClientSettingsSection applicationSettingsSection = null;


    static Settings()
    {
      //load DB based settings...
      using (Proc ControlCentral_s = new Proc("ControlCentral_s")) _DBSettings = ControlCentral_s.ExecuteNameValueCollection();

      //load "app.config" file based settings...
      //it should be noted that *saving* *directly* to the app.config file is a well known no-no... from a security standpoing which isn't really elaborated on much that i can find
      //but that winds up meaning that the *Application* scoped settings accessible via {appname}.Properties.Settings.Default are code gen'd as read only properties.
      //there's lots of folks whining about wanting to *write* to these properties for various reasons out in the forums...
      //the main reason here is that we want the printer mappings to be settable by all users but at the application wide level not user based... 
      //i.e. we don't want every user to be required to select the same printers that everybody on this machine will be using
      //i feel like i basically get the security implications that the administrator/installer must open read/write ACL on the Program Files\{app} folder and app.config file
      //probalby because you can throw config info in those files which opens up even more access or something like that
      //anyway, i'm still going for it until i read more about it... found the following code here: http://social.msdn.microsoft.com/Forums/en-US/csharpgeneral/thread/13428050-4fde-4c34-90f8-5255f4123a20/
      
      ConfigurationSectionGroup applicationSectionGroup = appConfig.GetSectionGroup("applicationSettings");
      applicationSettingsSection = applicationSectionGroup.Sections[ReflectionHelpers.CurrentAppName + ".Properties.Settings"] as ClientSettingsSection;
      applicationSettingsSection.SectionInformation.ForceSave = true; //crucial, otherwise just doesn't save, even though documentation indicates that it supposedly means save even if there aren't changes
      App = new SettingElementCollectionWrapper(applicationSettingsSection.Settings);

    }

    public class SettingElementCollectionWrapper
    {
      private SettingElementCollection _settings = null;
      public SettingElementCollectionWrapper(SettingElementCollection settings)
      {
        _settings = settings;
      }

      public string this[string key]
      {
        get { return (WPFHelpers.DesignMode ? "" : _settings.Get(key).Value.ValueXml.InnerText); }
        set { _settings.Get(key).Value.ValueXml.InnerText = value; }
      }

    }

  }

}

/*
namespace iTRAACv2.Properties
{

  public partial class Settings
  {
    static Settings()
    {
      defaultInstance.SettingChanging += new SettingChangingEventHandler(defaultInstance_SettingChanging);
    }

    static void defaultInstance_SettingChanging(object sender, SettingChangingEventArgs e)
    {
    }
  }

}
*/

