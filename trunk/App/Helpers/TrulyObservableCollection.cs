using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Threading;
using System.Linq;

public class TrulyObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged
{
  public TrulyObservableCollection() : base()
  {
    CollectionChanged += new NotifyCollectionChangedEventHandler(TrulyObservableCollection_CollectionChanged);
  }

  public override event NotifyCollectionChangedEventHandler CollectionChanged;
  protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
  {
    var eh = CollectionChanged;
    if (eh != null)
    {
      Dispatcher dispatcher = (from NotifyCollectionChangedEventHandler nh in eh.GetInvocationList()
                               let dpo = nh.Target as DispatcherObject
                               where dpo != null
                               select dpo.Dispatcher).FirstOrDefault();

      if (dispatcher != null && dispatcher.CheckAccess() == false)
      {
        dispatcher.Invoke(DispatcherPriority.DataBind, (Action)(() => OnCollectionChanged(e)));
      }
      else
      {
        foreach (NotifyCollectionChangedEventHandler nh in eh.GetInvocationList())
          nh.Invoke(this, e);
      }
    }
  }
  
  void TrulyObservableCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
  {
    if (e.NewItems != null)
    {
      foreach (Object item in e.NewItems)
      {
        (item as INotifyPropertyChanged).PropertyChanged += new PropertyChangedEventHandler(item_PropertyChanged);
      }
    }
    
    if (e.OldItems != null)
    {
      foreach (Object item in e.OldItems)
      {
        (item as INotifyPropertyChanged).PropertyChanged -= new PropertyChangedEventHandler(item_PropertyChanged);
      }
    }
  }
  
  void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
  {
    NotifyCollectionChangedEventArgs a = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset); OnCollectionChanged(a);
  }

}