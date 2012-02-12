using System;
using System.Data;

namespace iTRAACv2
{
  static public class EntityCache
  {
    static private DataSet dsCache = new DataSet();
    static public DataSet DS { get { return (dsCache); } }

    //the pattern of assumptions for Business Object procs:
    //- all take a standard @GUID UNIQUEIDENTIFIER parameter as a consistent primary key (also required by replication)
    //- can return multiple resultsets in order to bulk load in one proc call
    //- if multiple resultsets, then must also return a standard @TableNames varchar(1000) OUT parameter with a comma list of names to cache each recordset

    static public DataRowView TableCache(string GUID, string ProcName, string TableName)
    {
      string tbl = TableName;
      return(TableCache(GUID, ProcName, ref tbl));
    }

    /// <summary>
    /// </summary>
    /// <param name="TableCache">comma delimited list of tables if there are more than one returned from one proc</param>
    /// <param name="GUID"></param>
    /// <returns></returns>
    static public DataRowView TableCache(string GUID, string ProcName, ref string TableName)
    {
      //future: if we get to the point where we want to get more parms back from a proc
      //  then we could tweak TableCache() to return it to the outer context

      string MainTableName = TableName.ToLower(); //save the initial singular incoming table name so we can lookup on this cleanly versus the potential list of output tables

      DataRowView row = RowLookup(MainTableName, GUID);
      if (row != null) return (row);

      using (Proc Entity_s = new Proc(ProcName))
      {
        Entity_s["@GUID"] = GUID;
        DataSet ds = Entity_s.ExecuteDataSet();
        TableName = Entity_s["@TableNames"].ToString();//this kicks the potential comma LIST of tables back out to the calling context
        string[] tableNames = TableName.Split(',');

        bool IsNew = false;
        for (int i = 0; i < tableNames.Length; i++)
        {
          string tableName = tableNames[i].Trim().ToLower();
          ds.Tables[i].TableName = tableName;
          IsNew = !dsCache.Tables.Contains(tableName);
          //foreach (DataColumn col in ds.Tables[i].Columns)
          //{
          //  //col.ExtendedProperties["visible"] = !(col.ColumnName.Left(1) == "~");
          //  //col.ColumnName = col.ColumnName.Replace("~", "");
          //  col.ReadOnly = false;
          //}
        }

        dsCache.Merge(ds, true);

        //too bad Dataset.merge doesn't copy over all this metadata
        if (IsNew)
        {
          foreach (DataTable incomming in ds.Tables)
          {
            DataTable t = dsCache.Tables[incomming.TableName];
            t.PrimaryKey = new DataColumn[] { t.Columns["RowGUID"] };
            t.DefaultView.Sort = "RowGUID";

            //foreach (DataColumn col in t.Columns)
            //{
            //  col.ExtendedProperties["visible"] = ds.Tables[incomming.TableName].Columns[col.ColumnName].ExtendedProperties["visible"];
            //}
          }
        }
      }

      return (RowLookup(MainTableName, GUID));
    }

    static public DataRowView RowLookup(string TableName, object GUID)
    {
      TableName = TableName.ToLower();

      Guid theGUID = new Guid();
      if (GUID.GetType() == typeof(Guid)) theGUID = (Guid)GUID;
      else if (GUID.GetType() == typeof(string)) theGUID = new Guid((string)GUID);
      else throw (new Exception("EntityCache.RowLookup : unexpected type supplied for GUID parameter."));

      DataTable table = dsCache.Tables[TableName];
      if (table == null) return (null);
      DataRowView[] rows = table.DefaultView.FindRows(theGUID);
      if (rows.Length > 0) return (rows[0]);
      else return (null);
    }

  }

}