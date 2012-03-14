using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Threading;
using System.Linq;

// ReSharper disable CheckNamespace
public class TrulyObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged
// ReSharper restore CheckNamespace
{
  public TrulyObservableCollection()
  {
// ReSharper disable DoNotCallOverridableMethodsInConstructor
    CollectionChanged += TrulyObservableCollectionCollectionChanged;
// ReSharper restore DoNotCallOverridableMethodsInConstructor
  }

  public override event NotifyCollectionChangedEventHandler CollectionChanged;
  protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
  {
    var eh = CollectionChanged;
    if (eh == null) return;
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
  
  void TrulyObservableCollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
  {
    if (e.NewItems != null)
    {
      foreach (var item in e.NewItems)
      {
        ((INotifyPropertyChanged) item).PropertyChanged += ItemPropertyChanged;
      }
    }
    
    if (e.OldItems != null)
    {
      foreach (var item in e.OldItems)
      {
        ((INotifyPropertyChanged) item).PropertyChanged -= ItemPropertyChanged;
      }
    }
  }
  
  void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
  {
    var a = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset); OnCollectionChanged(a);
  }

}