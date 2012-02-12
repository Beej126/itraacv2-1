using System;
using System.Reflection;
using PostSharp.Laos;

//nugget: this guy really narrowed down the otherwise overly boated PostSharp based code I was finding out there:
//nugget: http://www.lesnikowski.com/blog/index.php/inotifypropertychanged-with-postsharp/
//some more good comments: http://thetreeknowseverything.wordpress.com/2009/01/21/auto-implement-inotifypropertychanged-with-aspects/

//6 of one half dozen of the other... interface is obviously cleaner for compile time safety, passing a simple method name string works pretty good too ;)
//public interface IOnSetter
//{
//  void OnSetter(string PropertyName);
//}

[Serializable]  // required by PostSharp
public class SetterInjectAttribute : OnMethodBoundaryAspect
{
  private string oldVal = null;
  private PropertyInfo prop = null;
  private string _OnSetterMethodName = null;

  public SetterInjectAttribute(string OnSetterMethodName)
  {
    _OnSetterMethodName = OnSetterMethodName;
  }

  public override void OnEntry(MethodExecutionEventArgs eventArgs)
  {
    //save old value in order to check if setter actually changes value, only fire OnSetter if it does change
    if (prop == null) prop = eventArgs.Instance.GetType().GetProperty(eventArgs.Method.Name.Replace("set_", ""));
    if (prop != null)
    {
      object oldobj = prop.GetValue(eventArgs.Instance, null);
      if (oldobj != null) oldVal = oldobj.ToString();
    }
  }

  public override void OnSuccess(MethodExecutionEventArgs eventArgs)
  {
    if (prop != null)
    {
      object newobj = prop.GetValue(eventArgs.Instance, null);
      if ((newobj == null && !String.IsNullOrEmpty(oldVal))
        || (newobj != null && newobj.ToString() != oldVal))
      {
        eventArgs.Instance.GetType().InvokeMember(_OnSetterMethodName, BindingFlags.InvokeMethod, null, eventArgs.Instance, new object[] { prop.Name });
      }
    }
  }

  public override bool CompileTimeValidate(MethodBase method)
  {
    return (method.Name.StartsWith("set_")); //if it's a setter, we wanna know 'bout it!
  }

};
