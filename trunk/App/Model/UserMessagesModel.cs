using System;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace iTRAACv2
{
  public class UserMessagesModel
  {
    private UserMessagesModel() {}

    static public void Add(String text)
    {
      List.Add(new UserMessagesModel()
      {
        Date = DateTime.Now,
        Text = text,
      });
    }

    public DateTime Date { get; private set; }
    public String Text { get; private set; }

    static UserMessagesModel() //nugget: C# supports a static constructor (class initializer)... how sweet
    {
      //List = new ObservableCollection<UserMessages>();
      List_BindingWrapper = new CollectionViewSource() { Source = List }; //nugget: default sort on a model list
      List_BindingWrapper.SortDescriptions.Add(new System.ComponentModel.SortDescription("Date", System.ComponentModel.ListSortDirection.Descending)); //nugget:
    }

    static private ObservableCollection<UserMessagesModel> List = new ObservableCollection<UserMessagesModel>();
    static public CollectionViewSource List_BindingWrapper { get; private set; }

  }
}
