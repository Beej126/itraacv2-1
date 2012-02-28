using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Threading;
using System.Reflection;

//http://stackoverflow.com/questions/5655073/wpf-datagrid-and-the-tab-key
public class TabOutDataGrid : DataGrid
{
  public TabOutDataGrid()
  {
    Style = new Style(GetType(), FindResource(typeof(DataGrid)) as Style); //nugget: inherit whatever Style base class already has
  }

  void TablessDataGrid_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
  {
    throw new NotImplementedException();
  }
  
  protected override void OnKeyDown(KeyEventArgs e)
  {
    //let enter key bubble up... so it hits a default button on this form or something
    if (e.Key == Key.Return) e.Handled = false;
    else if (e.Key == Key.Tab) //make Shift/Tab pop out of the datagrid rather than navigating amongst cells
    {
      if (Keyboard.Modifiers == ModifierKeys.Shift)
        MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous)); //somehow this works just fine, but the "Next" version goes into the cell navigation
      else
        this.Parent.FocusNext(this); //the grid typically has a sub element with keyboard focus, so next from there represents somewhere else inside the grid
                                     //therefore we need to specificy "next" relative to the grid itself by passing that in to override the default behavior

      e.Handled = true;
    }
    else base.OnKeyDown(e);
  }

  protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
  {
    if (!(e.OldFocus is DataGridCell)) //only reset focus if we're coming from outside the datagrid, otherwise you get into a recursive loop of setting focus
      //&& (lastDataGridFocus != null))
    {
      Keyboard.Focus(lastDataGridFocus);
      if (SelectedIndex == -1 && Items.Count > 0) SelectedIndex = 0;
      e.Handled = true;
      return;
    }
  }

  private IInputElement lastDataGridFocus = null;
  protected override void OnPreviewLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
  {
    base.OnPreviewLostKeyboardFocus(e);
    lastDataGridFocus = Keyboard.FocusedElement;
  }

}

public static class WPFHelpers
{
  static private MethodInfo GetNextTab;
  static private object KeyboredNavigation;
  public static void FocusNext(this DependencyObject Container, IInputElement RelativeElement = null)
  {
    //FocusNavigationDirection.Next just doesn't work: MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)); 
    //it tabs around the DataGrid cells by default
    //so had to do the following hack... fortunately there is a hidden method KeyboardNavigation.GetNextTab() that we can use (for now, until MS tweaks the framework :(
    if (GetNextTab == null)
    {
      GetNextTab = typeof(KeyboardNavigation).GetMethod("GetNextTab", BindingFlags.NonPublic | BindingFlags.Instance);
      KeyboredNavigation = typeof(FrameworkElement).GetProperty("KeyboardNavigation", BindingFlags.NonPublic | BindingFlags.Static).GetValue(Keyboard.FocusedElement, null);
    }
    object NextControl = GetNextTab.Invoke(KeyboredNavigation, new object[] { RelativeElement ?? Keyboard.FocusedElement, Container, false }); //pretty cool this works exactly how what we need, get the next tab sibling given me and my parent
    Keyboard.Focus(NextControl as IInputElement);
  }

  //public static void UIThreadSafe(Delegate method)
  //{
  //  if (!Application.Current.Dispatcher.CheckAccess())
  //    Application.Current.Dispatcher.Invoke(method);
  //  else
  //    method.DynamicInvoke(null);
  //}

  static public Thread LightBackGroundWorker(Action work, int SleepMillisecs = 0)
  {
    Thread t = new System.Threading.Thread(delegate()
    {
      try
      {
        if (work != null)
        {
          Thread.Sleep(SleepMillisecs);
          work();
        }
      }
      catch (Exception ex) //this exception handler must be included as part of this pattern wherever else it's implemented
      {
        Application.Current.Dispatcher.Invoke((Action)delegate() { throw ex; }, null); //toss any exceptions over to the main UI thread, per MSDN direction: http://msdn.microsoft.com/en-us/library/system.windows.application.dispatcherunhandledexception.aspx
      }
    });

    t.Start();

    return (t);
  }



  static public bool IsValid(this DependencyObject obj)
  {
    // The dependency object is valid if it has no errors, 
    //and all of its children (that are dependency objects) are error-free.
    return !Validation.GetHasError(obj) &&
        LogicalTreeHelper.GetChildren(obj)
        .OfType<DependencyObject>()
        .All(child => IsValid(child));
  }


  static public DataGridCell GetCell(this DataGrid dgGrid, int nRow, int nCol)
  {
    DataGridRow dgRow = dgGrid.GetRow(nRow);

    if (dgRow == null) return(null);

    DataGridCellsPresenter presenter = dgRow.FindChild<DataGridCellsPresenter>();
    DataGridCell dgCell = presenter.ItemContainerGenerator.ContainerFromIndex(nCol) as DataGridCell;
    if (dgCell == null)
    {
      dgGrid.ScrollIntoView(dgRow, dgGrid.Columns[nCol]);
      dgCell = presenter.ItemContainerGenerator.ContainerFromIndex(nCol) as DataGridCell;
    }
    return(dgCell);
  }
   

  static public DataGridRow GetRow(this DataGrid grid, int index)
  {
    DataGridRow gridrow = ((DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index));
    grid.UpdateLayout(); //nugget: for DataGrid.ItemContainerGenerator.ContainerFromIndex() to work, sometimes you have to bring a "virtualized" DataGridRow into view
    grid.ScrollIntoView(grid.Items[index]);
    gridrow = ((DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index));
    return (gridrow);
  }


  //nugget: this the most generic way recordindex could figure this so far...still not satisfied... but there are absolutely *zero* *XAML* based examples for setting the default ElementStyle for *AutoGenerated* columns
  static public void DataGridRightAlignAutoGeneratedNumericColumns(object sender, DataGridAutoGeneratingColumnEventArgs e)
  {
    DataGridTextColumn c = (e.Column as DataGridTextColumn);
    if (c != null && e.PropertyType.IsNumeric())
    {
      if (c.ElementStyle.IsSealed) c.ElementStyle = new Style(c.ElementStyle.TargetType, c.ElementStyle.BasedOn);
      c.ElementStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
      c.ElementStyle.Seal();
      c.Binding.StringFormat = "{0:#,0}";
    }
  }


  public static Brush BeginBrushColorAnimation(this Brush brush, Color color, int seconds = 1)
  {
    Brush br = (brush as SolidColorBrush == null) ? new SolidColorBrush() : brush.Clone(); //otherwise the default brush is "frozen" and can't be animated
    br.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(color, TimeSpan.FromSeconds(seconds)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
    return (br);
  }

  public static void EndBrushColorAnimation(this Brush brush)
  {
    if (!brush.IsSealed) brush.BeginAnimation(SolidColorBrush.ColorProperty, null);
  }

  //dictionary the storyboards per each usage
  private static System.Collections.Generic.Dictionary<DefinitionBase, Storyboard> GridSplitterPositions = new System.Collections.Generic.Dictionary<DefinitionBase, Storyboard>();
  public static void GridSplitterOpeningBounce(this DefinitionBase RowColDefinition, bool Opening = false, int OpenToSize = 0, Action<bool> AfterCompleted = null)
  {
    if (RowColDefinition == null) return; //for when events fire before everything is initialized

    bool IsRow = (RowColDefinition.GetType() == typeof(RowDefinition));

    Storyboard story;
    if (!GridSplitterPositions.TryGetValue(RowColDefinition, out story))
    {
      GridLengthAnimation animation = new GridLengthAnimation();
      animation.To = new GridLength(OpenToSize);
      animation.Duration = new TimeSpan(0,0,1);

      Storyboard.SetTarget(animation, RowColDefinition);
      Storyboard.SetTargetProperty(animation, new PropertyPath(IsRow ? "Height" : "Width"));

      GridSplitterPositions[RowColDefinition] = story = new Storyboard();
      story.Children.Add(animation);
      if (AfterCompleted != null) story.Completed += (s,e) => AfterCompleted(Opening);
    }

    DependencyProperty CurrentPositionProperty = IsRow ? RowDefinition.HeightProperty : ColumnDefinition.WidthProperty;

    if (Opening)
    {
      //only bugger with popping open if not already opened by user
      if (((GridLength)RowColDefinition.GetValue(CurrentPositionProperty)).Value == 0)
        story.Begin();
    }
    else
    {
      story.Stop();

      //save the current position in the animation's "To" property so it opens back to where it was before we closed it
      GridLength current = (GridLength)RowColDefinition.GetValue(CurrentPositionProperty);
      if (current.GridUnitType != GridUnitType.Star && current.Value > 0) (story.Children[0] as GridLengthAnimation).To = current;

      RowColDefinition.SetValue(CurrentPositionProperty, new GridLength(0, GridUnitType.Pixel));
    }
  }

  //nugget: DoEvents() WPF equivalent: http://kentb.blogspot.com/2008/04/dispatcher-frames.html
  public static void DoEvents()
  {
    //Invoke won't return until all higher priority messages have been pumped from the queue
    //DispatcherPriority.Background is lower than DispatcherPriority.Input
    //http://msdn.microsoft.com/en-us/library/system.windows.threading.dispatcherpriority.aspx
    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new VoidHandler(() => { }));
  }
  private delegate void VoidHandler();

  static public void AutoTabTextBox_TextChanged(object sender, TextChangedEventArgs e)
  {
    TextBox txt = sender as TextBox;
    if (txt.Text.Length == txt.MaxLength)
    {
      //implement "auto-tab" effect
      (e.OriginalSource as UIElement).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
    }
  }

  static public void IntegerOnlyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
  {
    int dummy = 0;
    e.Handled = !(int.TryParse(e.Text, out dummy));
  }


  static public bool DesignMode { get { return (System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv"); } }

  static public void ComboBoxDataTable(ComboBox cbx, DataTable t, string TextColumnsName, string ValueColumnName, string DefaultText, string DefaultValue)
  {
    if (DefaultText != null)
      cbx.Items.Add(new ComboBoxItem() { Content = DefaultText, Tag = DefaultValue });

    foreach (DataRowView r in t.DefaultView)
    {
      cbx.Items.Add(new ComboBoxItem() { Content = r[TextColumnsName].ToString(), Tag = r[ValueColumnName].ToString() });
    }
  }

  static private DependencyObject _lastCell = null;
  static public void WPFDataGrid_MouseRightButtonUp_SaveCell(object sender, System.Windows.Input.MouseButtonEventArgs e)
  {
    DependencyObject dep = (DependencyObject)e.OriginalSource;

    // iteratively traverse the visual tree
    while ((dep != null) &&
            !(dep is DataGridCell) &&
            !(dep is DataGridColumnHeader) &&
            !(dep is System.Windows.Documents.Run))
    {
      dep = VisualTreeHelper.GetParent(dep);
    }

    if (dep == null) return;

    if (dep is DataGridColumnHeader)
      dep.FindParent<DataGrid>().ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
    else dep.FindParent<DataGrid>().ClipboardCopyMode = DataGridClipboardCopyMode.ExcludeHeader;

    if (dep is DataGridCell) _lastCell = dep;
  }

  static public void WPFDataGrid_CopyCell_Click(object sender, System.Windows.RoutedEventArgs e)
  {
    if (_lastCell == null) return;

    DataGridRow row = _lastCell.FindParent<DataGridRow>();

    // find the column that this cell belongs to
    DataGridBoundColumn col = (_lastCell as DataGridCell).Column as DataGridBoundColumn;

    // find the propertyName that this column is bound to
    Binding binding = col.Binding as Binding;
    string boundPropertyName = binding.Path.Path;

    // find the object that is related to this row
    object data = row.Item;

    // extract the propertyName value
    PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(data);

    PropertyDescriptor property = properties[boundPropertyName];
    object value = property.GetValue(data).ToString();

    Clipboard.SetText(value.ToString());
  }

  static public void GridSort(DataGrid Grid, string ColumnName, ListSortDirection Direction)
  {
    DataGridColumn col = Grid.Columns.FirstOrDefault(c => c.SortMemberPath == ColumnName);
    if (col != null)
    {
      Grid.Items.SortDescriptions.Add(new SortDescription(ColumnName, Direction)); 
      col.SortDirection = Direction;
    }
  }

  static public void GridSort(DataGrid Grid, string[] ColumnNames, ListSortDirection[] Directions)
  {
    for (int i = 0; i < ColumnNames.Length; i++) GridSort(Grid, ColumnNames[i], Directions[i]);
  }

}

public class IsEnabledWrapper : IDisposable
{
  private Control _ctrl = null;
  private WaitCursorWrapper _wc = null;

  public IsEnabledWrapper(Control ctrl) : this(ctrl, false) { }

  /// <summary>
  /// Utilize via a "using(new IsEnabledWrapper(btnReload, true)) {...}" around a block of code to disable/enable a control while background work is executing
  /// </summary>
  /// <param name="ctrl"></param>
  /// <param name="ShowWaitCursor"></param>
  public IsEnabledWrapper(Control ctrl, bool ShowWaitCursor)
  {
    _ctrl = ctrl;
    _ctrl.IsEnabled = false;
    _wc = new WaitCursorWrapper();
  }

  // IDisposable Members
  public void Dispose()
  {
    _ctrl.IsEnabled = true;
    _wc.Dispose();
  }
}

/// <summary>
/// Made this a Disposable object so that we can wrapper it in a using() {} block as a convenient way to automatically "turn off" the wait cursor
/// </summary>
public class WaitCursorWrapper : IDisposable
{
  //sure enough, others thought of the IDisposable trick: http://stackoverflow.com/questions/307004/changing-the-cursor-in-wpf-sometimes-works-sometimes-doesnt
  public WaitCursorWrapper()
  {
    //nugget: Application.Current.Dispatcher.CheckAccess() confirms whether the current thread is the UI thread before attempting to hit objects tied to the UI thread, like Application.Current.MainWindow
    //in the iTRAAC v2 application, this conflict happens when we use a BackgroundWorker to execute certain datalayer stuff off the UI thread, leaving the UI responsive for more input during the data access...
    //but the datalayer methods can run into this WaitCursor logic via a callback so we have to account for that scenario

    //no loss though, because the BackgroundWorkerEx class implements WaitCursor toggling on its own anyway
    //nugget: crazy, Dispatcher.CheckAccess() is hidden from intellisense on purpose!?!: http://arstechnica.com/phpbb/viewtopic.php?f=20&SelectedFoldersTable=103740

    //nugget: Mouse.OverrideCursor is much more effective than Application.Current.MainWindow.Cursor: http://stackoverflow.com/questions/307004/changing-the-cursor-in-wpf-sometimes-works-sometimes-doesnt
    if (Application.Current == null) return;
    if (Application.Current.Dispatcher.CheckAccess()) Mouse.OverrideCursor = Cursors.Wait; //if we're on the UI thread, make it a solid wait cursor
    else Application.Current.Dispatcher.Invoke((Action)delegate() { Mouse.OverrideCursor = Cursors.AppStarting; }); //otherwise make it a wait circle with an active pointer to let user know they can still do stuff
  }

  public void Dispose()
  {
    if (Application.Current != null)
      Application.Current.Dispatcher.Invoke((Action)delegate() { Mouse.OverrideCursor = null; });
  }

  /// <summary>
  /// Used to pass down to lower layers (e.g. SqlClientHelper) as a callback so they effect a visual delay w/o undesirable bottom-up coupling.
  /// </summary>
  /// <returns></returns>
  static public WaitCursorWrapper WaitCursorWrapperFactory()
  {
    return (new WaitCursorWrapper());
  }
}


