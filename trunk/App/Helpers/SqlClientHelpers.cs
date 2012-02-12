using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.ComponentModel;
//make sure to keep this clean of any particular UI assembly dependencies so that it can be
//reused across ASP.Net, Windows.Forms and WPF projects

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// <summary>
/// Basically a wrapper around SqlCommand.
/// One benefit is .Parameters is pre-populated and conveniently exposed as Proc[string] indexer.
/// </summary>
public class Proc : IDisposable
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  #region Wait Cursor stuff
  public delegate IDisposable WaitObjectConstructor();
  static public WaitObjectConstructor NewWaitObject = DummyWaitObjectConstructor;
  static private IDisposable DummyWaitObjectConstructor()
  {
    return (new DummyDisposable());
  }
  private class DummyDisposable : IDisposable { public void Dispose() { } };
  #endregion

  public delegate void ExecuteMessageCallback(string Message);
  static public ExecuteMessageCallback MessageCallback = null;

  private void DoMessageCallback(string Message, string Prefix = null)
  {
    if (Message == null) return;

    if (MessageCallback != null) MessageCallback(Prefix + SqlClientHelpers.SqlErrorTextCleaner(Message));
    else throw (new Exception(Message));
  }

  private void DoMessageCallback(Exception ex, string Prefix = null)
  {
    if (ex == null) return;

    if (MessageCallback != null) DoMessageCallback(ex.Message, Prefix);
    else throw (Prefix == null ? ex : new Exception(Prefix + ex.Message)); //the context info provided by Prefix seems important enough to toss all the other exception info in the balance... we could probably get a little more fancy here if that info becomes crucial
  }

  public string PrintOutput { get; private set; }

  static public string ConnectionString = null;
  public bool TrimAndNull = true;
  private SqlCommand _cmd = null;
  private string _ProcName = null;
  private string _UserName = null;
  private DataSet _ds = null;

  private bool ProcOwnsDataSet = true;
  public DataSet dataSet { get { return (_ds); } set { _ds = value; ProcOwnsDataSet = false; } }

  public Proc(string ProcName) : this(ProcName, null) { }

  public bool IsDevMode { get { return (System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv"); } }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="ProcName"></param>
  /// <param name="UserName">pass this in to set "Workstation ID" which can be obtained via T-SQL's HOST_NAME() function... as a handy layup for a simple table audit framework :)</param>
  // I'm not entirely convinced this is the most elegant way to support this versus something more "automatic" in the background
  // the main challenge is maintaining a generically reusable Proc class that doesn't know whether it's running under ASP.Net or WPF
  // so rather than implementing a bunch of dynamic "drilling" to identify where you are and who the current username is
  // i'm thinking this is a nice "good enough" for now to simply pass it in from the outercontext
  public Proc(string ProcName, string UserName)
  {

    _ProcName = ProcName;
    _UserName = UserName;
    //scratch that idea... see "NOOP" comment further down: if (_ProcName == "NOOP") return; //this supports doing nothing if we're chaining AssignValues(DataRowView).ExecuteNonQuery() and the DataRowView.Row.RowState == UnChanged

    if (IsDevMode) return;

    Assert.Check(ConnectionString != null, "'Proc.ConnectionString' must be assigned prior to creating Proc instances, System.ComponentModel.LicenseManager.UsageMode: " + System.ComponentModel.LicenseManager.UsageMode.ToString() + ", System.Diagnostics.Process.GetCurrentProcess().ProcessName: " + System.Diagnostics.Process.GetCurrentProcess().ProcessName);

    if (_ProcName.Left(4).ToLower() != "dbo.") _ProcName = "dbo." + _ProcName;

    PopulateParameterCollection();
  }
  static private Regex UDTParamTypeNameFix = new Regex(@"(.*?)\.(.*?)\.(.*)", RegexOptions.Compiled);
  static private Dictionary<string, SqlCommand> _parmcache = new Dictionary<string, SqlCommand>(StringComparer.OrdinalIgnoreCase); 

  public SqlParameterCollection Parameters { get { return (_cmd.Parameters); } }

  public string[] TableNames { 
    get { Assert.Check(dataSet != null, "{}.TableNames pulled prior to populating its dataset", _ProcName); 
    return (dataSet.Tables.Cast<DataTable>().Select(t => t.TableName).ToArray()); } }

  public string[] MatchingTableNames(params string[] BaseTableNames)
  {
    if (BaseTableNames.Length == 0) return (TableNames); //if no table name filter specified, then we want all of them
    return(TableNames.Where(have => BaseTableNames.Any(want => //nugget: compare two lists with linq and take the intersection, 
      Regex.IsMatch(have, want + @"(\.[0-9]+)?$", RegexOptions.IgnoreCase))).ToArray()); //nugget: with a Regex for the comparison <nice>

    /* this was a fun learning exercise, but there's too much ambiguity involved with IEqualityComparer's dependence on having a rational GetHashCode() available
     * see this: http://stackoverflow.com/questions/98033/wrap-a-delegate-in-an-iequalitycomparer
     * so, sticking with the nested Linq approach above, works just fine and there's no screwy edge cases to worry about

     OnlyTableNames = OnlyTableNames.Intersect(proc.TableNames, //for an even more Linq'y approach, use built in .Intersect() ... 
        new LambdaComparer<string>((have, want) => //but we need a helper class to wrapper the required IEqualityComparer<T> ...
          Regex.IsMatch(have, want + @"(\.[0-9]+)?$", RegexOptions.IgnoreCase))).ToArray(); //around a comparison lambda */
  }

  private void PopulateParameterCollection(bool RefreshCache = false)
  {
    //pull cached parms if available
    string logicalConnectionString = ConnectionString + ((_UserName != null) ? ";Workstation ID=" + _UserName : "");
    string parmcachekey = logicalConnectionString + "~" + _ProcName;

    //this is used to facilitate an automatic parm load retry when we run into a "parameter missing" exception
    if (RefreshCache) { _parmcache.Remove(parmcachekey); return; }
        
    bool HasCachedParms = _parmcache.TryGetValue(parmcachekey, out _cmd);
    if (HasCachedParms) _cmd = _cmd.Clone();
    else
    {
      _cmd = new SqlCommand(_ProcName);
      _cmd.CommandType = CommandType.StoredProcedure;
    }
    _cmd.Connection = new SqlConnection(logicalConnectionString);
    if (_cmd.Connection.State != ConnectionState.Open) _cmd.Connection.Open();

    if (!HasCachedParms)
    {
      //i love this little gem, 
      //this allows us to skip all that noisy boiler plate proc parm definition code in the calling context, and simply assign parm names to values 
      SqlCommandBuilder.DeriveParameters(_cmd); //nugget: automatically assigns all the available parms to this SqlCommand object by querying SQL Server's proc definition metadata

      //strip the dbname off any UDT's... there appears to be a mismatch between the part of microsoft that wrote DeriveParameters and what SQL Server actually wants
      //otherwise you get this friendly error message:
      //The incoming tabular data stream (TDS) remote procedure call (RPC) protocol stream is incorrect. Table-valued parameter 1 ("@MyTable"), row 0, column 0: Data type 0xF3 (user-defined table type) has a non-zero length database name specified.  Database name is not allowed with a table-valued parameter, only schema name and type name are valid.
      foreach (SqlParameter p in _cmd.Parameters) if (p.TypeName != "")
        {
          Match m = UDTParamTypeNameFix.Match(p.TypeName);
          if (m.Success) p.TypeName = m.Groups[2] + "." + m.Groups[3];
        }

      _parmcache.Add(parmcachekey, _cmd.Clone()); //nugget: cache SqlCommand objects to avoid unnecessary SqlCommandBuilder.DeriveParameters() calls
    }
  }

  public void AssignValues(IOrderedDictionary values)
  {
    foreach (string key in values.Keys)
    {
      if (_cmd.Parameters.Contains("@"+key))
        this["@"+key] = values[key];
    }
  }

  public Proc AssignValues(DataRowView values) 
  {
    return (AssignValues(values.Row));
  }

  public Proc AssignValues(DataRow values) //nugget: giving the "fluent" API approach a shot: http://en.wikipedia.org/wiki/Fluent_interface, well ok, starting with simple method chaining then: http://martinfowler.com/bliki/FluentInterface.html
  {
    //don't do this, then we can't chain multiple things together where one may be dirty at the end!! if (values.Row.RowState == DataRowState.Unchanged) return new Proc("NOOP");

    foreach (DataColumn col in values.Table.Columns)
    {
      string colname = "@" + col.ColumnName;

      if (!_cmd.Parameters.Contains(colname))
        colname = "@" + col.ColumnName.Replace(" ", ""); //try mapping to columns by removing spaces in the name

      if (!_cmd.Parameters.Contains(colname)) continue; //otherwise it's not here, just move on, the approach is that this is not supposed to be a show stopper

      this[colname] = values[col.ColumnName];
    }
    return (this);
  }


  public Proc AssignValues(object[] values)
  {
    for(int i = 0; i < values.Length; i += 2)
    {
      if (_cmd.Parameters.Contains("@" + values[i]))
        this["@" + values[i]] = values[i+1];
    }
    return (this);
  }

  public void Dispose()
  {
    if (_cmd != null)
    {
      _cmd.Connection.InfoMessage -= Connection_InfoMessage;
      _cmd.Connection.Dispose();
      _cmd.Connection = null;
      _cmd.Dispose();
      _cmd = null;
    }

    if (_ds !=null && ProcOwnsDataSet) _ds.Dispose();
    _ds = null;
  }

  public Proc ExecuteDataSet()
  {
    if (IsDevMode) return (this); 
    //this doesn't work like it should: if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime) return(null);

    using (IDisposable obj = NewWaitObject())
    using (SqlDataAdapter da = new SqlDataAdapter(_cmd))
    {
      if (_ds == null) _ds = new DataSet();
      _ds.EnforceConstraints = false;
      if (_cmd.Connection.State != ConnectionState.Open) _cmd.Connection.Open();
      _cmd.Connection.InfoMessage += Connection_InfoMessage; //nugget: capture print/raiserror<11 output
      da.MissingSchemaAction = MissingSchemaAction.AddWithKey; //this magical line tells ADO.Net to go to the trouble of bringing back the schema info like DataColumn.MaxLength (which would otherwise always be -1!!)
      //da.FillSchema(_ds, SchemaType.Source);
      try
      {
        da.Fill(_ds);
        DoMessageCallback(PrintOutput);
        NameTables();
      }
      catch (Exception ex)
      {
        DoMessageCallback(ex);
      }
    }

    foreach (DataTable table in _ds.Tables) foreach (DataColumn column in table.Columns) column.ReadOnly = false;
    return (this); //trying some "Fluent API" type stuff: http://en.wikipedia.org/wiki/Fluent_interface
  }

  private void NameTables()
  {
    //establish a nice little convention here... sprocs can return a list of "friendly" names for each resultset...
    //(not required, just supported if the @TableNames output parm is present)
    //so, if the proc is telling us it's list of tables names, rename the generic Table0, Table1, etc to the corresponding names so caller can reference them more explicitly
    if (_cmd.Parameters.Contains("@TableNames"))
    {
      string[] tableNames = this["@TableNames"].ToString().ToLower().CleanCommas().Split(',')
        //we'd like the flexibility to send back multiple resultsets with the *SAME* name
        //this allows us the non trivial freedom to fire nested proc calls which return data headed into the same table without coupling those procs' internal logic with the need to do a union
        //but DataSet doesn't allow duplicate table names
        //so, .Uniquify() loops through the strings and renames them to table.1, table.2 etc
        //(the client will just have to be involved with anticipating this and consuming appropriately)
        .Uniquify().ToArray();

      Assert.Check(tableNames.Length == _ds.Tables.Count,
        String.Format("{0}[@TableNames] specifies {1} table(s), but proc returned {2} resultsets.",
        _ProcName, tableNames.Length, _ds.Tables.Count));

      //at this point it's just a matter of renaming the DataSet.Tables by ordinal mapping to our unique list of tablenames...
      for(int i=0; i<tableNames.Length; i++) _ds.Tables[i].TableName = tableNames[i];

      //and lastly, sync the @TableNames parm back up with whatever renaming we've done
      //just in case the caller depends on this being consistent, but preferrably callers will rely on the Proc.TableNames property for this
      this["@TableNames"] = TableNames.Join(","); 
    }
  }

  void Connection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
  {
    PrintOutput = e.Message;
  }

  public DataSet ExecuteDataSet(object label) //actually any object with a .Text propertyName will suffice :)
  {
    ReflectionHelpers.PropertySetter(label, "Success", true);

    try
    {
      return(ExecuteDataSet().dataSet);
    }
    catch(Exception ex) {
      ReflectionHelpers.PropertySetter(label, "Success", false);

      //if the caller has provided a way to display the error then do so
      if (ReflectionHelpers.PropertySetter(label, "Text", SqlClientHelpers.SqlErrorTextCleaner(ex.Message)))
        return (null);
      //otherwise rethrow so that we can see the bug that caused the exception and fix it
      else 
        throw (ex);
    }
  }

  public bool ExecuteDataSet(String ExecuteMessagePrefix, bool DisplaySuccess = false)
  {
    try
    {
      ExecuteDataSet();
      if (DisplaySuccess) DoMessageCallback(ExecuteMessagePrefix == null ? "Saved Successfully" : ExecuteMessagePrefix + "successful");
      return (true);
    }
    catch (Exception ex)
    {
      DoMessageCallback(ex, ExecuteMessagePrefix);
      return (false);
    }
  }

  public Proc ExecuteNonQuery(bool RetryContext = false)
  {
    if (_cmd == null) return (this);

    using (IDisposable obj = NewWaitObject())
    {
      try
      {
        _cmd.ExecuteNonQuery();
      }
      catch (Exception ex)
      {
        //nugget: automatic reload of the proc parms in case we're actively patching procs while running... it was easy to implement here, still need to investigate feasibility of implementing ExecuteDataSet() with this approach
        if (!RetryContext && ex.Message.Contains("expects parameter")) //nugget:
        {
          PopulateParameterCollection(RefreshCache: true); //nugget:
          ExecuteNonQuery(true); //nugget: retry 
        }
        else throw (ex); 
      }
      return (this);
    }
  }

  public bool ExecuteNonQuery(object label, bool DisplaySuccess)
  {
    try
    {
      ExecuteNonQuery();
      if (DisplaySuccess) ReflectionHelpers.PropertySetter(label, "Text", "Saved Successfully");
      return (true);
    }
    catch (Exception ex)
    {
      if (!ReflectionHelpers.PropertySetter(label, "Text", SqlClientHelpers.SqlErrorTextCleaner(ex.Message)))
        throw (ex);
      return (false);
    }
  }

  public void ExecuteNonQueryBackground(String ExecuteMessagePrefix, bool DisplaySuccess = false)
  {
    (new System.Threading.Thread(delegate()
    {
      try
      {
        ExecuteNonQuery(ExecuteMessagePrefix, DisplaySuccess, false);
      }
      catch (Exception ex) //this exception handler should be included as part of this threading pattern wherever else it's implemented so that we don't lose errors behind the scenes
      {
        DoMessageCallback(ex, ExecuteMessagePrefix);
      }
    })).Start();
  }

  public bool ExecuteNonQuery(String ExecuteMessagePrefix, bool DisplaySuccess = false, bool BackgroundThreadContext = false)
  {
    Assert.Check(MessageCallback != null, "SqlClientHelpers.cs : Proc.MessageCallback should not be null when calling this overload");

    try
    {
      ExecuteNonQuery();
      if (DisplaySuccess) DoMessageCallback(ExecuteMessagePrefix + "Saved Successfully");
      if (BackgroundThreadContext) Dispose();
      return (true);
    }
    catch (Exception ex)
    {
      if (BackgroundThreadContext) throw (ex);
      DoMessageCallback(ex, ExecuteMessagePrefix);
      return (false);
    }
  }

  public DataTableCollection Tables
  {
    get
    {
      Assert.Check(_ds != null, "must execute proc prior to retrieving tables");
      return (_ds.Tables);
    }
  }

  public DataTable Table0
  {
    get
    {
      if (IsDevMode) return(new DataTable());

      if (_ds == null) ExecuteDataSet();
      //Assert.Check(_ds != null, "must execute proc prior to retrieving tables");
      return (_ds.Tables.Count > 0 ? _ds.Tables[0] : null);
    }
  }

  public DataRow Row0
  {
    get
    {
      if (_ds == null) ExecuteDataSet();
      //Assert.Check(_ds != null, "must execute proc prior to retrieving tables");
      return (Table0.Rows.Count > 0 ? Table0.Rows[0] : null);
    }
  }

  public SqlDataReader ExecuteReader()
  {
    return (_cmd.ExecuteReader(CommandBehavior.SingleResult | CommandBehavior.CloseConnection));
  }


  public NameValueCollection ExecuteNameValueCollection(NameValueCollection vals = null)
  {
    using (DataSet ds = ExecuteDataSet().dataSet) return (DataTableToNameValueCollection(Table0, vals));
  }

  /// <summary>
  /// Add to an existing NameValueCollection
  /// </summary>
  static private NameValueCollection DataTableToNameValueCollection(DataTable table, NameValueCollection vals = null)
  {
    if ((table == null) || (table.Rows.Count == 0)) return (null);

    if (table.Columns[0].ColumnName.ToLower() == "name") //row based name-value pairs
    {
      if (vals == null) vals = new NameValueCollection(table.Rows.Count - 1);
      foreach (DataRow row in table.Rows) vals[row["name"].ToString()] = row["value"].ToString();
    }
    else //column based...
    {
      if (vals == null) vals = new NameValueCollection(table.Columns.Count - 1);
      foreach (DataColumn col in table.Columns) vals[col.ColumnName] = table.Rows[0][col.ColumnName].ToString();
    }

    return (vals);
  }



  public object this[string key]
  {
    //nulls can get a little tricky to look at here...
    //if TrimAndNull is on, then it'll truncate a blank string and convert it to DBNull

    //this getter here returns SqlValue not "Value" ... which translates SqlString parameters containing DBNull into C# nulls... 
    //this may or may not come in handy... we'll have to see and tweak accordingly
    get
    {
      return(_cmd.Parameters[key].Value);
    }
    set
    {
      if (!_cmd.Parameters.Contains(key)) return;

      if (TrimAndNull && !(value is DataTable) && (value != null) && (value != DBNull.Value)) {
        value = value.ToString().Trim();
        if ((string)value == "") value = null;
      }
      _cmd.Parameters[key].Value = (value == null || value == DBNull.Value) ? DBNull.Value : 
        (_cmd.Parameters[key].SqlDbType == SqlDbType.UniqueIdentifier) ? new Guid(value.ToString()) : value;
    }
  }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
public static class SqlClientHelpers
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{

  static public void SetReadonlyField(this DataRow row, string ColName, object Data)
  {
    bool current = row.Table.Columns[ColName].ReadOnly;
    row.Table.Columns[ColName].ReadOnly = false;
    row[ColName] = Data;
    row.Table.Columns[ColName].ReadOnly = current;
  }


  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  //DataRow/View extension methods - use this approach to create a wrapper around DataRowView
  //such that callers stay obvlivious to DataRowView specifics and therefore could be implemented by some other model field container w/o lots of rippling changes

  //static private FieldInfo DataView_OnListChanged = typeof(DataView).GetField("onListChanged", BindingFlags.NonPublic | BindingFlags.Instance);
  //static public void InvokeListChanged(this DataView v)
  //{
  //  ListChangedEventHandler OnListChanged = DataView_OnListChanged.GetValue(v) as ListChangedEventHandler;
  //  if (OnListChanged != null) OnListChanged(v, new ListChangedEventArgs(ListChangedType.Reset, 0));
  //}

  static public T Field<T>(this DataRowView drv, string FieldName)
  {
    return ((drv == null || drv[FieldName] is DBNull || drv.Row.RowState == DataRowState.Detached) ? default(T) : drv.Row.Field<T>(FieldName));
  }


  static public bool IsFieldsModified(this DataRow row, params string[] FieldNames)
  {
    foreach (string FieldName in FieldNames)
      if (row[FieldName, DataRowVersion.Original].ToString() != row[FieldName, DataRowVersion.Current].ToString())
        return (true);
    return (false);
  }

  static public void SetAllNonDBNullColumnsToEmptyString(this DataRow r, string EmptyPlaceholder = "")
  {
    if (r == null) return;
    foreach (DataColumn c in r.Table.Columns) if (!c.AllowDBNull && r[c.ColumnName] == DBNull.Value) r[c.ColumnName] = EmptyPlaceholder;
  }

  /// <summary>
  /// This is the 'undo' of SetAllNonDBNullColumnsToEmptyString()
  /// </summary>
  /// <param name="r"></param>
  /// <param name="EmptyPlaceholder"></param>
  static public void RemoveEmptyPlaceholder(this DataRow r, string EmptyPlaceholder)
  {
    if (r == null) return;
    foreach (DataColumn c in r.Table.Columns) if (!c.AllowDBNull && r[c.ColumnName].ToString() == EmptyPlaceholder) r[c.ColumnName] = DBNull.Value;
  }


  static private FieldInfo DataRowView_OnPropertyChanged = typeof(DataRowView).GetField("onPropertyChanged", BindingFlags.NonPublic | BindingFlags.Instance);
  static public void SetColumnError(this DataRowView drv, string FieldName, string Message)
  {
    drv.Row.SetColumnError(FieldName, Message);
    (DataRowView_OnPropertyChanged.GetValue(drv) as PropertyChangedEventHandler)(drv, new PropertyChangedEventArgs(FieldName));
  }

  static public void ClearColumnError(this DataRowView drv, string FieldName)
  {
    //if no current error, clear previous one (if there was one) and notify UI
    //... trying to minimize notifications as much as possible, rather than just firing notify either way
    if (drv.Row.GetColumnError(FieldName) != "")
    {
      drv.Row.SetColumnError(FieldName, "");
      (DataRowView_OnPropertyChanged.GetValue(drv) as PropertyChangedEventHandler)(drv, new PropertyChangedEventArgs(FieldName));
    }
  }

  
  public static bool ColumnsContain(this DataRowView drv, string ColumnName)
  {
    return(drv.Row.Table.Columns.Contains(ColumnName));
  }

  public static void AcceptChanges(this DataRowView drv)
  {
    drv.Row.AcceptChanges();
  }

  public static void AcceptChanges(this DataView v)
  {
    foreach(DataRowView r in v) r.Row.AcceptChanges();
  }

  public static bool IsDirty(this DataRowView drv)
  {
    return (drv == null ? false : !(drv.Row.RowState == DataRowState.Unchanged || drv.Row.RowState == DataRowState.Detached));
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="v"></param>
  /// <param name="RespectRowFilter">If true, does NOT clear RowFilter prior to dirty check.</param>
  /// <returns></returns>
  public static bool IsDirty(this DataView v, bool RespectRowFilter = true)
  {
    if (v == null) return (false);

    //nugget: this approach of clearing the rowfilter, and then restoring it created a bug.
    //        TextBox bound to this view would lose it's focus as you were entering text, not gonna work.
    //        the entry would fire the PropertyChanged -> IsModified -> IsDirty()
    //
    //        but the typical save logic really needs a way to know *everything* that should be saved, including Filtered rows
    //        so unfortunately this is a sharp edge that we have to keep around...
    //        namely, IsDirty() calls has been removed from the IsModified logic, IsModified is now a simple get/set property
    //        and IsDirty() should only be called for saves where the user has already changed focus to a Save button anyway

    //Assert.Check(v.RowFilter == "" || RespectRowFilter == true, "Checking dirty on a DataView with a RowFilter is debatable");

    string saveFilter = "";
    try
    {
      if (!RespectRowFilter & v.RowFilter != "")
      {
        saveFilter = v.RowFilter;
        v.RowFilter = "";
      }
      return (v.Cast<DataRowView>().Any(r => r.IsDirty()));
    }
    finally
    {
      if (saveFilter != "") v.RowFilter = saveFilter;
    }
  }

  static public void DetachRowsAndDispose(this DataView v, bool PreserveRowFilter = false)
  {
    if (v == null) return;
    if (!PreserveRowFilter) v.RowFilter = "";
    foreach (DataRowView r in v) r.DetachRow();
    v.Dispose();
  }

  static public void DetachRow(this DataRowView v)
  {
    if (v == null) return;
    if (v.Row.RowState != DataRowState.Detached)
    {
      v.Row.CancelEdit();
      v.Row.RejectChanges();
      if (v.Row.RowState != DataRowState.Detached)//i guess this happens when we reject a recently added row
        v.Row.Table.Rows.Remove(v.Row);
    }
  }


  static public void AddRelation(this DataSet ds, string Name, DataColumn Parent, DataColumn Child, bool EnforceConstraints = true)
  {
    if (!ds.Relations.Contains(Name))
    {
      ds.Relations.Add(Name, Parent, Child, EnforceConstraints);
    }
  }

  static public void AddColumn(this DataTable Table, string ColName, Type type, string Expression)
  {
    if (!Table.Columns.Contains(ColName))
      Table.Columns.Add(ColName, type, Expression);
  }

  static public IEnumerable<Dictionary<string, object>> ToSimpleTable(this DataTable t)
  {
      return t.AsEnumerable().Select(r => t.Columns.Cast<DataColumn>().Select(c =>
                           new { Column = c.ColumnName, Value = r[c] })
                       .ToDictionary(i => i.Column, i => i.Value != DBNull.Value ? i.Value : null));
  }

  static public DataRow Clone(this DataRow row)
  {
    return(row.Table.Rows.Add(row.ItemArray));
  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  private static string[] _SQLErrorTranslations = new string[]
  {
    //Cannot insert the value NULL into column 'CreateDate', table 'iTRAACv2.dbo.ClientPreviousSponsor'; column does not allow nulls. INSERT fails.
    @"Cannot insert the value NULL into column '(.+?)', table '(.+?)'",
      "Please fill out the {1} field ({2}).",
    @"The DELETE statement conflicted with the REFERENCE constraint.*table \""(?<object>.+?)\""",
      "You must remove the associated {1}s before you can delete this.",
    @"Cannot insert duplicate key row in object '(?<object>.+?)' with unique index '(.+?)'",
      "{1} already exists (key: {2})", //the assumption here is that unique indexes are named with an eye towards displaying to user when violated
    @"Violation of PRIMARY KEY constraint '(.+?)'. Cannot insert duplicate key in object '(?<object>.+?)'. The duplicate key value is \((.+?)\).",
      "{3} already exists (key: {1}, value: {2})"
  };

  /// <summary>
  /// Regular expression string for patterns to be deleted from designated SQL object name
  /// e.g. "(^tbl|^vw_|^dbo.)"
  /// </summary>
  public static string SQLError_ObjectRemoveRegex //
  {
    get
    {
      return (_SQLError_ObjectRemoveRegex.ToString());
    }
    set
    {
      _SQLError_ObjectRemoveRegex = new Regex(value, RegexOptions.Compiled);
    }
  }
  private static Regex _SQLError_ObjectRemoveRegex = null;

  static SqlClientHelpers()
  {
    SQLError_ObjectRemoveRegex = @"^dbo\.";
  }

  /// <summary>
  /// Formats raw SQL error text into more human readable wording
  /// e.g. given SQLError_ObjectRemoveRegex = "(^dbo.|^tbl|^vw_|es$|s$)", 
  ///   then "Violation of PRIMARY KEY constraint 'PK1_1'. Cannot insert duplicate key in object 'dbo.tblSponsors'. The duplicate key value is (370000683).\r\nThe statement has been terminated."
  ///   is translated to: "Sponsor already exists (key: PK1_1, value: 370000683)"
  /// </summary>
  /// <param name="message"></param>
  /// <returns></returns>
  public static string SqlErrorTextCleaner(string message)
  {
    for (int i = 0; i < _SQLErrorTranslations.Length-1; i += 2)
    {
      Regex regex = new Regex(_SQLErrorTranslations[i]);
      Match m = regex.Match(message);
      if (m.Success)
      {
        if (m.Groups["object"].Success)
        {
          string objectname = m.Groups["object"].Value;

          if (_SQLError_ObjectRemoveRegex != null)
          {
            objectname = _SQLError_ObjectRemoveRegex.Replace(objectname, ""); //for example, first time removes "^dbo." 
            objectname = _SQLError_ObjectRemoveRegex.Replace(objectname, ""); //and then second time removes "^tbl"
            //this two step allows the replacement patterns to be prefix targeted rather than openly replacing the pattern found anywhere in the string
          }

          m = regex.Match(message.Replace(m.Groups["object"].Value, objectname)); //repopulate the groups with the updated <object>
        }
        return (String.Format(_SQLErrorTranslations[i + 1], m.Groups[0], m.Groups[1], m.Groups[2], m.Groups[3], m.Groups[4], m.Groups[5]));
      }
    }
    return (message);
  }

  public static void AddInitialEmptyRows(DataSet ds, StringCollection rootTables)
  {
    //if root table is totally empty, create an initial row so that the grids show up and are ready to add the first entry
    foreach (string r in rootTables)
    {
      DataTable t = ds.Tables[r];
      if (t.Rows.Count == 0) AddNewRowWithPK(t);
    }

    //now walk the relationships and create initial CHILD rows where necessary
    foreach (DataRelation rel in ds.Relations)
    {
      foreach (DataRow ParentRow in rel.ParentTable.Rows)
      {
        if (ParentRow.GetChildRows(rel).Length == 0)
        {
          DataRow ChildRow = AddNewRowWithPK(rel.ChildTable);
          //fill out the foreign-key
          ChildRow[rel.ChildKeyConstraint.Columns[0].ColumnName] = ParentRow[rel.ChildKeyConstraint.RelatedColumns[0].ColumnName];
        }
      }
    }

  }

  public static DataRow AddNewRowWithPK(DataTable t)
  {
    DataRow r = t.NewRow();
    r[t.PrimaryKey[0].ColumnName] = System.Guid.NewGuid();
    t.Rows.Add(r);
    return (r);
  }

  public static DataRow AddNewNestedRow(DataTable t)
  {
    //create new row, assign PK
    DataRow r = AddNewRowWithPK(t);

    //fill this new row's foreign keys to its parents
    foreach (DataRelation rel in t.ParentRelations)
    {
      string col = rel.ChildColumns[0].ColumnName;
      r[col] = t.Rows[0][col];
    }

    //create new empty child rows with their FK's pointing to this new row so any related sub grids display
    foreach (DataRelation rel in t.ChildRelations)
    {
      string col = rel.ParentColumns[0].ColumnName;
      DataRow childrow = AddNewRowWithPK(rel.ChildTable);
      childrow[col] = r[col];
    }

    return (r);
  }
}

