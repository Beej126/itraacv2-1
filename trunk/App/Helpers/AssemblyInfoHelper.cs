using System;
using System.Reflection;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.IO;

public static class AssemblyHelper
{
  static private NameValueCollection _Properties = new NameValueCollection(StringComparer.OrdinalIgnoreCase);
  static public NameValueCollection Properties { get { if (_Properties.Count == 0) Initialize(); return (_Properties); } }

  static private void Initialize()
  {
    foreach (object att in Assembly.GetExecutingAssembly().GetCustomAttributes(false))
    {
      PropertyDescriptorCollection props = TypeDescriptor.GetProperties(att);
      if (props.Count == 2) //the AssemblyInfo values of general interest all have one main propertyName[0] plus a TypeId propertyName[1]
        _Properties.Add(att.GetType().Name.ToString().Replace("Attribute", "").Replace("Assembly", ""), props[0].GetValue(att).ToString());
    }
  }

  /* clickonce versioning... until we can count on clickonce working under our oppressive group policies, this is pretty moot
  static public string CurrentDeployedVersion
  {
    get
    {
      return (System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString());
    }
  }*/

  /// <summary>
  /// this should always be wrapped in a "using() {}" block because the generated stream belongs to the caller
  /// </summary>
  /// <param name="ResourcePath"></param>
  /// <returns></returns>
  static public Stream GetEmbeddedResource(string ResourcePath)
  {
    Assembly assembly = Assembly.GetExecutingAssembly();
    string fullresname = assembly.GetManifestResourceNames().SingleOrDefault(s => s.Contains(ResourcePath));
    Assert.Check(fullresname != null, String.Format("{0} not found embedded in this assembly", ResourcePath));

    return (assembly.GetManifestResourceStream(fullresname));
  }

}
