using System;

public static class Assert
{
  static public void Check(Boolean expression, string errormsg, params object[] parms)
  {
    if (!expression) throw (new Exception(String.Format(errormsg, parms)));
  }
}
