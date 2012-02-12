    public class Type
    {
      //FormTypeID	FormName	FormType	MaxCkOut	RecordSource	CodeName	Passive	Active
      public int FormTypeID { get; private set; }
      public string Description { get; private set; }
      public int Class { get; private set; }
      public int MaxCheckout { get; private set; }
      public string PrintDataProcName { get; private set; }
      public string Code { get; private set; }
      public bool UserSelectable { get; private set; }
      public bool Active { get; private set; }

      static Type()
      {
        using (Proc TaxFormTypes_s = new Proc("TaxFormTypes_s"))
        {
          // FormTypeID, FormName, FormType, MaxCkOut, RecordSource, CodeName, Passive, Active
          DataTable t = TaxFormTypes_s.Table0;
          foreach (DataRow r in t.Rows)
          {
            List.Add((string)r["CodeName"], new Type() { 
              Active = (bool)r["Active"], 
              Code = (string)r["CodeName"], 
              Description = (string)r["FormName"],
              FormTypeID = (int)r["FormTypeID"],
              Class = (int)r["FormType"],
              MaxCheckout = (int)r["MaxCkOut"],
              PrintDataProcName = (string)r["RecordSource"],
              UserSelectable = !(bool)r["Passive"]
            });
          }

        }
      }
      public static readonly System.Collections.Generic.Dictionary<string, Type> List = new System.Collections.Generic.Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

      /// <summary>
      /// This is the list of NF1/EF1 Form Types
      /// intended for GUI logic that attempts to be more "formtype agnostic"
      /// </summary>
      public static ObservableCollection<Type> UserSelectableClass1
      {
        get
        {
          if (_UserSelectableClass1 == null) _UserSelectableClass1 = new ObservableCollection<Type>(from Type t in List where (t.Active == true && t.Class == 1) == true select t);
          return (_UserSelectableClass1);
        }
      }
      static private ObservableCollection<Type> _UserSelectableClass1 = null;

      /// <summary>
      /// This is the list of NF2/EF2 Form Types
      /// </summary>
      public static ObservableCollection<Type> UserSelectableClass2
      {
        get
        {
          if (_UserSelectableClass2 == null) _UserSelectableClass2 = new ObservableCollection<Type>(from Type t in List where (t.Active == true && t.Class == 2) select t);
          return (_UserSelectableClass2);
        }
      }
      static private ObservableCollection<Type> _UserSelectableClass2 = null;


    }
  }
