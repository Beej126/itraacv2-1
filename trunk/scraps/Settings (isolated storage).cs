using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Specialized;
using System.Runtime.Serialization.Formatters.Soap;
using System.Runtime.Serialization;

namespace iTRAACv2
{
  class Settings
  {
    
    static public int MaxClass1FormsCount { get { return (System.Convert.ToInt16(_DBSettings["MaxClass1FormsCount"])); } }
    //make sure that OfficeCode remains something that is set on a client connection by connection basis
    //the point being, leave open the possibility of connecting folks directly into the central DB while still maintaining their individual office context
    static public string OfficeCode { get { return (Properties.Settings.Default.OfficeCode); } }

    static public NameValueCollection AppSettings { get; private set; }

    static private System.Collections.Specialized.NameValueCollection _DBSettings = null;
    static Settings()
    {
      //load global DB settings...
      using (Proc ControlCentral_s = new Proc("ControlCentral_s")) _DBSettings = ControlCentral_s.ExecuteNameValueCollection();
      //load local db settings...adding them to the existing NameValueCollection
      using (Proc LocalSettings_s = new Proc("LocalSettings_s")) LocalSettings_s.ExecuteNameValueCollection(_DBSettings);

      //load IsolatedStorage based AppSettings (e.g. matrix printer mappings)
      LoadAppSettings();
    }

    //private Settings() {}

    static private void LoadAppSettings()
    {
      if (WPFHelpers.DesignMode) return;

      IsolatedStorageFile store = IsolatedStorageFile.GetMachineStoreForApplication(); //nugget: for this to be happy: Project > Properties > Security > Enabled ClickOnce security settings
      using (Stream stream = new IsolatedStorageFileStream("AppSettings.xml", FileMode.OpenOrCreate, store))
      {
        if (stream.Length == 0) AppSettings = new NameValueCollection();
        else {
          IFormatter formatter = new SoapFormatter();
          AppSettings = (NameValueCollection)formatter.Deserialize(stream);
        }
      }
    }

    static public void SaveAppSettings()
    {
      IsolatedStorageFile store = IsolatedStorageFile.GetMachineStoreForApplication();
      using (Stream stream = new IsolatedStorageFileStream("AppSettings.xml", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, store))
      {
        IFormatter formatter = new SoapFormatter();
        formatter.Serialize(stream, AppSettings);
      }
    }

  }
}

namespace iTRAACv2.Properties
{
  public partial class Settings
  {
    ~Settings()
    {
      Save(); 
    }
  }

}

