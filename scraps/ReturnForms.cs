using System;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.ComponentModel;

namespace iTRAACv2
{
  public class ReturnForms
  {
    private ReturnForms() {}

    static public void Add(string TaxFormGUID, string OrderNumber, string CustomerName, string Message, bool Success)
    {
      List.Add(new ReturnForms()
      {
        Index = List.Count,
        RowGUID = TaxFormGUID,
        OrderNumber = OrderNumber,
        CustomerName = CustomerName,
        Message = Message,
        Success = Success
      });
    }

    [Browsable(false)]
    public string RowGUID { get; private set; }
    [DisplayName("Form #")]
    public string OrderNumber { get; private set; }
    [DisplayName("Customer Name")]
    public string CustomerName { get; private set; }
    public string Message { get; private set; }
    [Browsable(false)]
    public bool Success { get; private set; }
    public int Index { get; private set; }

    static ReturnForms()
    {
      List_BindingWrapper = new CollectionViewSource() { Source = List };
      List_BindingWrapper.SortDescriptions.Add(new System.ComponentModel.SortDescription("Index", System.ComponentModel.ListSortDirection.Ascending));

      //test data: 
      Add("C19A9062-1928-4B59-B65A-F4330B92D617", "NF1-HD-10-00001", "Test, Customer", "Already filed on 13 Oct 2010", false); 
    }

    static private  ObservableCollection<ReturnForms> List = new ObservableCollection<ReturnForms>();
    static public CollectionViewSource List_BindingWrapper { get; private set; }

  }
}
