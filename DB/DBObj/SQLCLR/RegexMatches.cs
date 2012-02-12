using System;
using System.Collections;
using System.Data.SqlTypes;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Server;

public class RegexMatches {
  [SqlFunction(FillRowMethodName = "FillRow", TableDefinition = "Match nvarchar(4000)", Name = "RegexMatches")]

  public static IEnumerable InitMethod(String Input, String Pattern) {
    return Regex.Matches(Input, Pattern, RegexOptions.IgnoreCase);
  }

  public static void FillRow(Object obj, out SqlString Match) {
    Match = ((Match)obj).Value;
  }
}