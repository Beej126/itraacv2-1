using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;


public partial class StoredProcedures {
  [Microsoft.SqlServer.Server.SqlProcedure]
  public static void OpenQueryCLR(string ConnStr, string Query) {

    try {
      PrintOutput = null;

      using (SqlConnection connection = new SqlConnection(ConnStr))
      {
        connection.Open();
        connection.InfoMessage += new SqlInfoMessageEventHandler(connection_InfoMessage);

        using (IDbCommand qry = new SqlCommand())
        {
          qry.Connection = connection;
          qry.CommandText = Query;
          qry.CommandType = CommandType.Text;

          //execute the proc and get a reader back
          using (IDataReader rdr = qry.ExecuteReader(CommandBehavior.CloseConnection))
          {

            //use the returned columns to build a sql resultset
            using (DataTable columns = rdr.GetSchemaTable())
            {

              //only return a resultset if we actually got one to return, many /400 procs are just parm outputs
              if ((columns != null) && (columns.Rows.Count > 0))
              {
                SqlMetaData[] md = new SqlMetaData[columns.Rows.Count];

                for (int c = 0; c < columns.Rows.Count; c++)
                {
                  md[c] = new SqlMetaData(columns.Rows[c]["ColumnName"].ToString(), SqlDbType.VarChar, -1);
                }

                SqlDataRecord record = new SqlDataRecord(md);
                object[] vals = new object[columns.Rows.Count];
                SqlContext.Pipe.SendResultsStart(record);
                int rowcount = 0;
                while (rdr.Read())
                {
                  rowcount++;
                  rdr.GetValues(vals);
                  for (int i = 0; i < vals.Length; i++) vals[i] = vals[i].ToString().Trim();
                  record.SetValues(vals);
                  SqlContext.Pipe.SendResultsRow(record);
                }
                SqlContext.Pipe.SendResultsEnd();
              }

            }

          }

          //connection.Close(); // this happens automatically as a result of "CommandBehavior.CloseConnection" passed in to the qry.ExecuteReader
        }

      }
      if (PrintOutput != null) SqlContext.Pipe.Send(PrintOutput);
    }
    catch (Exception ex) {
      if (SqlContext.Pipe.IsSendingResults) SqlContext.Pipe.SendResultsEnd();
      SqlContext.Pipe.Send("Exception: " + ex.Message + "\r\n" + ex.StackTrace);
    }

  }

  static string PrintOutput = null;
  static void connection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
  {
    PrintOutput += e.Message + "\n";
  }


};
