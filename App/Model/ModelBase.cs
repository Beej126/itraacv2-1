using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;
using System.Linq;
using System.Windows.Data;
using System.Text.RegularExpressions;

namespace iTRAACv2
{
  //always stick to the basic rule of thumb: 
  //  for any given model, the GUI should only be referencing _generic_collections_of_fields_
  //  that could be readily replaced with another collection like Dictionary, etc w/o having to change the client code
  //  (-or- simple flat properties are OK of course)

  //provide wrapper methods for anything requiring any "data navigation"
  //  so the GUI is always coding to a generic model interface that could be implemented otherwise
  //as long as we don't use anything beyond DataRowView's indexers and flat properties to pull values, 
  //  the GUI stays clean of knowing anything too specific about the next layer down

  // These BO's are akin to the Microsoft Typed DataSet concept.
  // rationales for not going that route explicitly, include:
  // . Typed DataSets inherit from DataSet, presents the challenge of how to incorporate the benefits of a custom base class
  // . the desire to make BO's more of a first class citizen wrapped around DataTables versus the more 'secondary' nature of table classes in typed datasets
  // . concern of overly constraining the rapid evolution of add/removing fields during development due to the overhead of slugging thru Designer-regen process every time something wiggles a little on the database side
  // . how to gen the typed dataset in the first place (probably over a contrived sql view of the desired fields would've been most straightforward)
  // . there's always some unexpected limitation that you have to jump through with these gen'd interfaces
  // . the various dataset child object wrappers herein do take advantage of the typed dataset approach w/o being constricted by it
  // . it seems to hold up quite well that to have a model to hang all the fancy OO stuff that also publishes a DataRowView for raw field databinding (see 'Fields' in the public members)

  // concerning Typed DataSets:
  // . http://szymonrozga.net/blog/?p=228

  //WPF and ADO.Net objects... 
  //  specifically, WPF doesn't "fully" support IBindingList collections like DataTable/DataView
  //  not quite sure EXACTLY what we're missing... 
  // . [2010.08] http://forums.lhotka.net/forums/p/9406/44603.aspx
  //  he mentions "automatic sorting", but the DataView bound DataGrids seem to sort just fine:
  // . [2009.01] http://forums.lhotka.net/forums/t/6218.aspx?PageIndex=1
  // what i have found is that DataTable.Merge does not fire INotifyPropertyChanged.PropertyChanged or IBindingList.ListChanged

  public abstract class ModelBase : INotifyPropertyChanged
  {
    /// <summary>
    /// Leave constructors empty
    /// LoadFields() is really where all the constructor stuff should go, this frees us to create lightweight model instances w/o firing all the load logic
    /// </summary>
    protected ModelBase() { } 

    /// <summary>
    /// Each model subclass is responsible for its own load/save logic
    /// </summary>
    protected abstract void LoadSubClass();

    protected virtual string AltLookup(string Id) { return (Id); }

    private void LoadFields()
    {
      UnLoad();
      LoadSubClass();
      Assert.Check(Fields != null, String.Format("Invalid State: Fields == null after LoadFields() [{0} - GUID:{1}]", this.GetType(), GUID));
    }

    //keep this the way it is as a simple public method basically wrapped around LoadFields... it helps provide a clean consistent public interface and hide the plumbing of LoadFields behind the curtain
    public void ReLoad()
    {
      //keep the model instance around and just swap out the raw DataRowView bag-o-fields
      LoadFields();
      OnPropertyChanged("Fields"); //nugget: this is awesome that this automatically refreshes all connected GUI elements in one fell swoop!! too cool, good job Microsoft
    }

    public bool Save()
    {
      //removing this now that i prefer the subclass to walk through it's objects and make sure more than this simple property:
      //if (!IsModified) return (true);

      bool success = SaveSubClass();
      if (success)
      {
        Fields.AcceptChanges();
        IsModified = false;
      }
      return (success);
    }

    protected abstract bool SaveSubClass();

    public bool SaveUnload()
    {
      if (Save())
      {
        UnLoad();
        return (true);
      }
      return (false);
    }

    public void UnLoad()
    {
      UnLoadSubClass();

      ModelCache.Remove(GUID);

      //*** very crucial to eliminate this reference from a table object to an event handler on each corresponding model instance (otherwise it's a potential mem leak)
      //*** we want everything disconnected from an unloaded instance so it can be garbage collected
      if (_fields != null)
      {
        _fields.Row.Table.RowChanged -= FieldsTable_RowChanged;
        _fields.DetachRow();
        _fields = null;
      }
    }

    protected abstract void UnLoadSubClass();

    /// <summary>
    /// Primary key on all model entities
    /// </summary>
    public string GUID { get; protected set; }

    /// <summary>
    /// DataBinding Path syntax for the BO's primary display title
    /// This is currently what gets displayed in the tab header area
    /// </summary>
    public abstract BindingBase TitleBinding { get; }

    /// <summary>
    /// Container of the flat data fields for this model (assigned in subclass LoadFields())
    /// </summary>
    public DataRowView Fields
    {
      get { return (_fields); }
      set
      {
        _fields = value;
        //TODO:this became a bug for new sponsor scenario, not sure why i put there in the first place: if (GUID == Guid.Empty.ToString()) return;

        if (value == null )
        {
          string errmsg = String.Format("{0}[GUID: {1}].Fields is null (check for inner join disconnects in {0}_s sproc).", GetType().Name, GUID);
          ShowUserMessage(errmsg);
          throw(new Exception(errmsg)); 
        }

        _fields.Row.Table.RowChanged += FieldsTable_RowChanged; //nugget: Table.RowChanged fires for Row.AcceptChanges in addition to individual field hits so it's better than DataRowView.PropertyChanged
      }
    }

    private DataRowView _fields = null;

    /// <summary>
    /// Classic 'dirty flag' we all know and love
    /// </summary>
    // override in subclass if there's to be more to it than that (e.g. TaxForm forms a parent-child with another resultset for extended fields by TransactionType that needs it's dirty flag aggregated)
    private bool _IsModified = false;
    public bool IsModified { get { return (_IsModified); } protected set { if (value != _IsModified) { _IsModified = value; OnPropertyChanged("IsModified"); } } } //{ get { return (Fields.IsDirty()); } }
    public void SetIsModified() { IsModified = true; }

    public abstract string WhatIsModified { get; }

    #region Field Validation stuff
    protected bool Validate_Generic(ref bool IsValid, string FieldName, string Expression = "'?' != ''", string Message = "Required")
    {
      return (Validate_Generic(Fields, ref IsValid, FieldName, true, Expression, Message));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="drv"></param>
    /// <param name="IsValid"></param>
    /// <param name="FieldName">to reference another field to be invalidated as well, comma delimit with no spaces (yeah i know it's hacky)</param>
    /// <param name="Expression"></param>
    /// <param name="Message"></param>
    /// <returns></returns>
    protected bool Validate_Generic(DataRowView drv, ref bool IsValid, string FieldName, bool DoValidation = true, string Expression = "'?' != ''", string Message = "Required")
    {
      //if (!drv.IsDirty()) return (true);

      string[] flds = FieldName.Split(',');
      if (flds.Length == 2) FieldName = flds[0];

      bool isvalid = true;
      if (DoValidation) isvalid = StringEvaluator.EvalToBool(Expression.Replace("?", drv[FieldName].ToString()));
      IsValid = IsValid && isvalid;

      //if error, set error and notify UI, otherwise clear any existing errors
      if (!isvalid)
      {
        if (flds.Length == 2) drv.SetColumnError(flds[1], Message);
        drv.SetColumnError(FieldName, Message); //nugget: setting errors on ADO.Net DataRowView fields which propogate all the way back up to little red boxes & tooltips on the corresponding UI widgets
      }
      else
      {
        if (flds.Length == 2) drv.ClearColumnError(flds[1]);
        drv.ClearColumnError(FieldName);
      }

      return (isvalid);
    }

    #endregion

    #region UserMessageCallback stuff
    /// <summary>
    /// allow the models to broadcast human readable notifications where beneficial
    /// implementing as a callback avoids undesirable coupling from BO's up to UI
    /// </summary>
    /// <param name="Text"></param>
    public delegate void UserMessageDelegate(string Text);
    static private UserMessageDelegate _UserMessageCallback;
    static public void SetUserMessageCallback(UserMessageDelegate CallBack)
    {
      _UserMessageCallback = CallBack;
    }
    static public void ShowUserMessage(string Text)
    {
      if (_UserMessageCallback != null) _UserMessageCallback(Text);
    }
    #endregion

    #region INotifyPropertyChanged stuff
    //Table.RowChanged fires for Row.AcceptChanges in addition to individual field hits so it supercedes DataRowView.PropertyChanged
    void FieldsTable_RowChanged(object sender, DataRowChangeEventArgs e)
    {
      if (e.Row == _fields.Row)
      {
        e.Row.ClearErrors();
        IsModified = true;
      }
    }

    //implement propertyName changed to be handy for all model subclasses
    public event PropertyChangedEventHandler PropertyChanged;
    public virtual void OnPropertyChanged(string property) //had to make this public for TransactionList to access it :(  sure wish C# had C++ style "friends"
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(property));
    }
    #endregion

    #region ModelCache stuff
    static protected Dictionary<string, ModelBase> ModelCache = new Dictionary<string, ModelBase>(StringComparer.CurrentCultureIgnoreCase);

    static public T Lookup<T>(string GUID, bool LoadIfNotLoaded = true)
      where T : ModelBase, new() //too bad C# (4.0) doesn't support *parameterized* constructor constraints on generic types, then we could require a constructor signature with a string argument, like: new(string GUID) 
    {
      ModelBase model, altmodel;
      if (ModelCache.TryGetValue(GUID, out model)) return (model as T);
      if (!LoadIfNotLoaded) return (null);

      model = new T();
      string altid = (model as T).AltLookup(GUID);
      if (altid != null && ModelCache.TryGetValue(altid, out altmodel)) return (altmodel as T); //slid this in to support pulling up TaxForm's by OrderNumber... there's a little sliver of this at the top of TaxForm.LoadSubClass as well... overall it's a little hacky but not too bad (yet)
      else
      {
        model.GUID = GUID;
        model.LoadFields();
        model.GUID = model.Fields["RowGUID"].ToString(); //for the scenario where we sent a '0000-00000' GUID to the database to request a new entity 
        ModelCache[model.GUID] = model;
        return (model as T);
      }
    }


    #endregion

    #region TableCache stuff

    static public DataSet dsCache { get; private set; }

    static ModelBase()
    {
      dsCache = new DataSet();
      dsCache.EnforceConstraints = false; //couldn't get the TaxOffice <-> User stuff to pass DataSet constraints inspection no matter how valid the keys lined up in SQL Server constraints
    }

    //the pattern of assumptions for model retrieval sprocs:
    //- all take a standard @GUID UNIQUEIDENTIFIER parameter as a consistent primary key (also required by replication)
    //- can return multiple resultsets in order to bulk load in one proc call
    //- if multiple resultsets, then must also return a standard @TableNames varchar(1000) OUT parameter with a comma list of names to cache each recordset

    /// <summary>
    /// Lookup entity by 'RowGUID'=GUID in TableName, if doesn't exist, retrieve via specified Proc
    /// </summary>
    /// <param name="GUID"></param>
    /// <param name="EntityTableName"></param>
    /// <param name="ProcName"></param>
    /// <param name="Refresh"></param>
    /// <returns></returns>
    static protected DataRowView EntityLookup(string GUID, string EntityTableName, string ProcName, bool Refresh = false)
    {
      using (iTRAACProc Entity_s = new iTRAACProc(ProcName))
      {
        return (EntityLookup(GUID, EntityTableName, Entity_s, Refresh));
      }
    }

    static private DataRowView EntityLookup(string GUID, string EntityTableName, Proc proc, bool Refresh = false)
    {
      EntityTableName = EntityTableName.ToLower();

      DataRowView row = null;
      if (!Refresh) row = RowLookup(EntityTableName, GUID);
      if (row != null) return (row);

      //if the GUID parm is blank, fill it with what's specified
      if (proc.Parameters.Contains("@GUID") && Convert.ToString(proc["@GUID"]) == "") proc["@GUID"] = GUID;

      CacheTables(proc);

      if (proc.Parameters.Contains("@GUID")) GUID = proc["@GUID"].ToString(); //for the case where TaxForm_s can lookup on OrderNumber as well as GUID

      //this logic has been moved down into Proc: if (proc.Parameters.Contains("@TableNames")) TableName = proc["@TableNames"].ToString(); //this kicks the potential comma LIST of tables back out to the calling context

      return (RowLookup(EntityTableName, GUID));
    }

    /// <summary>
    /// To be used by update sprocs that are only concerned with returning a batch of records to keep the DataTable cache in sync with database state
    /// </summary>
    /// <param name="ProcName"></param>
    /// <returns>Array of tablenames that were returned</returns>
    static protected string[] CacheTables(string ProcName)
    {
      using (iTRAACProc Entity_s = new iTRAACProc(ProcName))
      {
        CacheTables(Entity_s);
        return (Entity_s.TableNames);
      }
    }

    /// <summary>
    /// To be used by update sprocs that are only concerned with returning a batch of records to keep dsCache in sync with the current database state
    /// Takes in a DataSet and a list of TableNames and merges them into the main dsCache
    /// </summary>
    /// <param name="proc"></param>
    /// <param name="TableNames"></param>
    static public void CacheTables(Proc proc, params string[] OnlyTableNames)
    {
      //if the outer scope hasn't already executed and populated the corresponding DataSet, go ahead and execute now
      if (proc.dataSet == null) proc.ExecuteDataSet();

      //determine the list of tables we want to process
      OnlyTableNames = proc.MatchingTableNames(OnlyTableNames);

      for (int i=0; i<OnlyTableNames.Length; i++)
      {
        //need to handle table.1, table.2 type results where we return multiple resultsets intended to be merged into the same cached table
        string cacheTableName = multitables.Match(OnlyTableNames[i]).Groups[1].Value;

        DataTable incomingTable = proc.dataSet.Tables[OnlyTableNames[i]];
        string incomingName = incomingTable.TableName;
        incomingTable.TableName = cacheTableName; //needs to be the same so merge works, this table is renamed back at the end to prevent collision with any subsequent

        //*** DataSet.Merge requires PK's to be assigned on both incoming and existing tables in order to merge records appropriately
        //sometimes PrimaryKey info is missing from the ADO.Net schema metadata that comes back from a stored proc call
        //it appears that selecting from a sql view is one reason this can happen
        //so, this logic defines a PK if one isn't present
        if (incomingTable.PrimaryKey.Length == 0)
        {
          try
          {
            string PKColName = null;
            if (incomingTable.Columns.Contains("RowGUID")) PKColName = "RowGUID";
            else if (incomingTable.Columns.Contains(cacheTableName + "ID")) PKColName = cacheTableName + "ID";
            else if (incomingTable.Columns[0].ColumnName.Contains("GUID")) PKColName = incomingTable.Columns[0].ColumnName;  //initially necessary for Sponsor_TaxForms.TaxFormGUID

            if (PKColName != null)
              incomingTable.PrimaryKey = new DataColumn[] { incomingTable.Columns[PKColName] };
          }
          catch (Exception ex)
          {
            if (ex.Message != "These columns don't currently have unique values.") throw (ex); //opening a little wiggle room here for situations like the User table which gets dual keyed off TaxOfficeId and User RowGUID, in this case, the PK will be properly set in the calling context
          }
        }

        //nugget: DataSet.Merge(DataTable) has become a real linchpin in the whole data roundtrip approach
        //nugget: in a nutshell, update sprocs return a bare minimum of updated fields in a return resultset along with a corresponding CSV list of @TableNames
        DataTable cachedTable = dsCache.Tables[cacheTableName];
        dsCache.Merge(incomingTable, 
          preserveChanges: false, //preserveChanges pretty much has to be false in order to count on what comes back getting slammed in
          missingSchemaAction: (cachedTable == null) ? MissingSchemaAction.AddWithKey : //if this table hasn't been cached yet, go with most robust MissingSchemaAction so we get the first one populated with good metadata
            MissingSchemaAction.Ignore); //but be as relaxed as possible with any subsequent data headed for this table once it's been established... set any other way, constraints would just blow up like crazy on me and they were too tough to identify what was specifically wrong... almost to the point that logic is more paranoid than it should be

        incomingTable.TableName = incomingName; //to prevent name collision as we loop through

        //now that it's been cached...
        cachedTable = dsCache.Tables[cacheTableName];
        //make the DefaultView.Sort the same as the PK so that we can consistently use Table.DefaultView.FindRows() & Table.Find() interchangeably
        if (cachedTable.DefaultView.Sort == "" && cachedTable.PrimaryKey.Length > 0)
          cachedTable.DefaultView.Sort = cachedTable.PrimaryKey[0].ColumnName;
      }
      
    }
    static private Regex multitables = new Regex(@"(.*?)(\.[0-9]+)?$");

    static protected DataRowView RowLookup(string TableName, object GUID)
    {
      Guid theGUID;
      if (!Guid.TryParse(GUID.ToString(), out theGUID) ) return (null);

      TableName = TableName.ToLower();

      /*
      if (GUID.GetType() == typeof(Guid)) theGUID = (Guid)GUID;
      else if (GUID.GetType() == typeof(string)) theGUID = new Guid((string)GUID);
      else throw (new Exception("EntityCache.RowLookup : unexpected type supplied for GUID parameter."));
      */

      DataTable table = dsCache.Tables[TableName];
      if (table == null) return (null);
      DataRowView[] rows = table.DefaultView.FindRows(theGUID);
      if (rows.Length > 0) return (rows[0]);
      else return (null);
    }
    #endregion

  }

  #region TransactionList
  //nugget: ObservableCollection does NOT fire CollectionChanged event when an individual item property is twiddled
  //nugget: but BindingList<T> doesn't work so hot either
  //nugget: reference: http://stackoverflow.com/questions/1427471/c-observablecollection-not-noticing-when-item-in-it-changes-even-with-inotify
  public class TransactionList : TrulyObservableCollection<TransactionList.TransactionItem>
  {
    public class TransactionItem : INotifyPropertyChanged
    {
      public bool IsPending { get; protected set; }
      public string Description { get; protected set; }
      public decimal Price { get; protected set; }
      public string GUID { get; protected set; }
      protected TransactionList _ParentList { get; private set; }
      public string PendingAction { get; protected set; }

      public virtual void Execute() { throw new NotImplementedException(); }

      protected TransactionItem(TransactionList ParentList) 
      {
        _ParentList = ParentList;
        PendingAction = "Go";
      }

      public TransactionItem(TransactionList ParentList, string description, decimal price) : this(ParentList)
      {
        Description = description;
        Price = price;
        AfterConstructor();
      }

      /// <summary>
      /// this must be called by base class!!
      /// nugget: bummer, C# has no direct way to sequence base class code after subclass initialization (Delphi sure was awesome)
      /// </summary>
      protected void AfterConstructor()
      {
        _ParentList.AddTransaction(this);
      }

      public void Rollback()
      {
        _ParentList.RollbackTransaction(this);
      }

      public event PropertyChangedEventHandler PropertyChanged;
      protected virtual void NotifyPropertyChanged(string property) //had to make this public for TransactionList to access it :(  sure wish C# had C++ style "friends"
      {
        if (PropertyChanged != null)
          PropertyChanged(this, new PropertyChangedEventArgs(property));
      }

    }

    private TransactionList() { HasPending = false; }
    private ModelBase owner = null;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="ListPropertyName">This is the owner's PropertyName for the list so that the list can take care of publishing OnPropertyChanged notifications itself</param>
    public TransactionList(ModelBase owner, string ListPropertyName) : this() { this.owner = owner; _ListPropertyName = ListPropertyName; }
    private string _ListPropertyName = null;
    public decimal TotalPrice { get; private set; }
    public bool HasPending { get; private set; }

    public void ExecuteAllPending()
    {
      foreach (TransactionItem txn in Items) if (txn.IsPending) txn.Execute();
    }

    private void AddTransaction(TransactionItem item)
    {
      base.Add(item);
      TotalPrice = this.Sum(r => r.Price); //resum rather than simple add facilitates merging multiple pending NF1 packages into one bundle
      if (item.IsPending) HasPending = true;
      owner.SetIsModified();
      owner.OnPropertyChanged(_ListPropertyName);
    }

    private void RollbackTransaction(TransactionItem item)
    {
      Assert.Check(item.IsPending, "Can't roll back a committed transaction");

      base.Remove(item);
      TotalPrice = this.Sum(r => r.Price);
      if (HasPending && item.IsPending) HasPending = Items.Any(i => i.IsPending);
      owner.OnPropertyChanged("IsModified");
      owner.OnPropertyChanged(_ListPropertyName);
    }

    //new public void RemoveItem(int index)
    //{
    //  // if this list has Pending items, and we're removing one of them, check if there are any more and set status accordingly, otherwise, don't bother checking
    //  if (HasPending && Items[index].Pending) HasPending = (Items.Where(i => i.Pending).Count() > 1);
    //  base.RemoveItem(index);
    //  owner.SetIsModified();
    //  owner.OnPropertyChanged(_ListPropertyName);
    //}

    //new public void ClearItems()
    //{
    //  base.ClearItems();
    //  HasPending = false;
    //  owner.SetIsModified();
    //  owner.OnPropertyChanged(_ListPropertyName);
    //}
  }
  #endregion
  
}