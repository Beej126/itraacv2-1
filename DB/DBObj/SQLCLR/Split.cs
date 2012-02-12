using System;
using System.Collections;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

public class Split {
  [SqlFunction(FillRowMethodName = "FillRow", TableDefinition = "SeqNo int, [Value] nvarchar(4000)", Name = "Split")]
  public static IEnumerable InitMethod(String Source, String Delimiter) {

    /* bja: ok the reason i didn't just do String.Split and call it a day, is this...
     * i'm denormalizing a comma delimited list of addresscodes into Session.NearestStores (e.g. '403,430,603')
     * and naturally they need to be sorted by distance when the "nearest stores" list is displayed to the user.
     * so rather than carrying around the distance and complicating my storage of those values,
     * i wanted to simply depend on the original sort order of the stores determined by distance at the time they were calculated
     * the annoying thing is, sql server is non deterministic about the sequence of calling the FillRow method so that it has the option to parallelize the query if it wants
     * and in practice, i noticed the sort order of the results from this function as being in exactly the opposite order when returned (i.e. 603 was row 1, 430 row 2 & 403 row 3, rather than the other way around)
     */

    if (Source == null)
      return (new IndexedString[] { });

    string[] strings = Source.Split(new String[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);

    IndexedString[] indexedStrings = new IndexedString[strings.Length];
    for (int i = 0; i < strings.Length; i++) {
      indexedStrings[i].index = i;
      indexedStrings[i].str = strings[i].Trim();
    }

    return indexedStrings;
  }

  public static void FillRow(Object obj, out SqlInt32 SeqNo , out SqlString Value) {
    SeqNo = ((IndexedString)obj).index;
    Value = ((IndexedString)obj).str;
  }
}

public struct IndexedString {
  public int index;
  public string str;
}