using System;

// ReSharper disable CheckNamespace
public static class Assert
// ReSharper restore CheckNamespace
{
  static public void Check(Boolean expression, string errormsg, params object[] parms)
  {
    if (!expression) throw (new Exception(String.Format(errormsg, parms)));
  }
}
