using System;
using System.Globalization;
using System.Reflection;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.IO;

// ReSharper disable CheckNamespace
public static class AssemblyHelper
// ReSharper restore CheckNamespace
{
  static private readonly NameValueCollection PropertiesBacking = new NameValueCollection(StringComparer.OrdinalIgnoreCase);
  static public NameValueCollection Properties { get { if (PropertiesBacking.Count == 0) Initialize(); return (PropertiesBacking); } }

  static private void Initialize()
  {
    foreach (object att in Assembly.GetExecutingAssembly().GetCustomAttributes(false))
    {
      PropertyDescriptorCollection props = TypeDescriptor.GetProperties(att);
      if (props.Count == 2) //the AssemblyInfo values of general interest all have one main propertyName[0] plus a TypeId propertyName[1]
      {
        var value = props[0].GetValue(att);
        if (value != null)
          PropertiesBacking.Add(att.GetType().Name.ToString(CultureInfo.InvariantCulture).Replace("Attribute", "").Replace("Assembly", ""), value.ToString());
      }
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
  /// <param name="resourcePath"></param>
  /// <returns></returns>
  static public Stream GetEmbeddedResource(string resourcePath)
  {
    Assembly assembly = Assembly.GetExecutingAssembly();
    string fullresname = assembly.GetManifestResourceNames().SingleOrDefault(s => s.Contains(resourcePath));
    Assert.Check(fullresname != null, String.Format("{0} not found embedded in this assembly", resourcePath));

    return (assembly.GetManifestResourceStream(fullresname));
  }

}
