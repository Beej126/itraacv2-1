using System;
using System.Data.SqlTypes;
using System.Text.RegularExpressions;

public partial class UserDefinedFunctions {
  [Microsoft.SqlServer.Server.SqlFunction]
  public static SqlString RegexReplace(SqlString Input, SqlString Pattern, SqlString Replacement) {
    return new SqlString(Regex.Replace(Input.ToString(), Pattern.ToString(), Replacement.ToString(), RegexOptions.IgnoreCase));
  }
};

