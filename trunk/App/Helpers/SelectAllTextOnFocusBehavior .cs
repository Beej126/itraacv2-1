using System; //Guid
using System.Diagnostics; //Debug class
using System.Windows.Interactivity; //Behavior
using System.Windows.Input; //MouseButtonEventArgs
using System.Windows; //DependencyObject, UIElement
using System.Windows.Controls; //TextBox
using System.Windows.Media; //VisualTreeHelper

//nugget: magic sauce that allows for a "Behavior" to be attached via XAML Style syntax
//nugget: from here: http://www.livingagile.com/Blog.aspx?tagid=70
namespace LivingAgile.Common.WPF
{
  public class StylizedBehaviorCollection : FreezableCollection<Behavior>
  {
    #region Methods (protected)
    protected override Freezable CreateInstanceCore()
    {
      return new StylizedBehaviorCollection();
    }
    #endregion
  }

  public class StylizedBehaviors
  {
    #region Fields (private)
    private static readonly DependencyProperty BehaviorIdProperty =
        DependencyProperty.RegisterAttached(
            @"BehaviorId", typeof(Guid), typeof(StylizedBehaviors), new UIPropertyMetadata(Guid.Empty));
    #endregion

    #region Fields (public)
    public static readonly DependencyProperty BehaviorsProperty = DependencyProperty.RegisterAttached(
        @"Behaviors",
        typeof(StylizedBehaviorCollection),
        typeof(StylizedBehaviors),
        new FrameworkPropertyMetadata(null, OnPropertyChanged));
    #endregion

    #region Static Methods (public)
    public static StylizedBehaviorCollection GetBehaviors(DependencyObject uie)
    {
      return (StylizedBehaviorCollection)uie.GetValue(BehaviorsProperty);
    }

    public static void SetBehaviors(DependencyObject uie, StylizedBehaviorCollection value)
    {
      uie.SetValue(BehaviorsProperty, value);
    }
    #endregion

    #region Static Methods (private)
    private static Guid GetBehaviorId(DependencyObject obj)
    {
      return (Guid)obj.GetValue(BehaviorIdProperty);
    }

    private static int GetIndexOf(BehaviorCollection itemBehaviors, Behavior behavior)
    {
      int index = -1;

      Guid behaviorId = GetBehaviorId(behavior);

      for (int i = 0; i < itemBehaviors.Count; i++)
      {
        Behavior currentBehavior = itemBehaviors[i];

        if (currentBehavior == behavior)
        {
          index = i;
          break;
        }

        Guid cloneId = GetBehaviorId(currentBehavior);

        if (cloneId == behaviorId)
        {
          index = i;
          break;
        }
      }

      return index;
    }

    private static void OnPropertyChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs e)
    {
      var uie = dpo as UIElement;

      if (uie == null)
      {
        return;
      }

      BehaviorCollection itemBehaviors = System.Windows.Interactivity.Interaction.GetBehaviors(uie);

      var newBehaviors = e.NewValue as StylizedBehaviorCollection;
      var oldBehaviors = e.OldValue as StylizedBehaviorCollection;

      if (newBehaviors == oldBehaviors)
      {
        return;
      }

      if (oldBehaviors != null)
      {
        foreach (var behavior in oldBehaviors)
        {
          int index = GetIndexOf(itemBehaviors, behavior);

          if (index >= 0)
          {
            itemBehaviors.RemoveAt(index);
          }
        }
      }

      if (newBehaviors != null)
      {
        foreach (var behavior in newBehaviors)
        {
          Guid behaviorId = GetBehaviorId(behavior);

          int index = GetIndexOf(itemBehaviors, behavior);

          if (index < 0)
          {
            var clone = (Behavior)behavior.Clone();
            if (GetBehaviorId(clone) == Guid.Empty) SetBehaviorId(clone, Guid.NewGuid());

            itemBehaviors.Add(clone);
          }
        }
      }
    }

    private static void SetBehaviorId(DependencyObject obj, Guid value)
    {
      obj.SetValue(BehaviorIdProperty, value);
    }
    #endregion
  }
}


// from here: http://stackoverflow.com/questions/660554/how-to-automatically-select-all-text-on-focus-in-wpf-textbox
public sealed class SelectAllTextOnFocusBehavior : Behavior<TextBox>
{
  protected override void OnAttached()
  {
    base.OnAttached();
    AssociatedObject.PreviewMouseLeftButtonDown += AssociatedObject_SelectivelyIgnoreMouseButton;
    AssociatedObject.GotKeyboardFocus += SelectAllText;
    AssociatedObject.MouseDoubleClick += SelectAllText;
  }

  protected override void OnDetaching()
  {
    base.OnDetaching();
    AssociatedObject.PreviewMouseLeftButtonDown -= AssociatedObject_SelectivelyIgnoreMouseButton;
    AssociatedObject.GotKeyboardFocus -= SelectAllText;
    AssociatedObject.MouseDoubleClick -= SelectAllText;
  }

  private void AssociatedObject_SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
  {
    // Find the TextBox
    DependencyObject parent = e.OriginalSource as UIElement;
    while (parent != null && !(parent is TextBox))
      parent = VisualTreeHelper.GetParent(parent);

    if (parent != null)
    {
      var textBox = (TextBox)parent;
      if (!textBox.IsKeyboardFocusWithin)
      {
        // If the text box is not yet focussed, give it the focus and
        // stop further processing of this click event.
        textBox.Focus();
        e.Handled = true;
      }
    }
  }

  private static void SelectAllText(object sender, RoutedEventArgs e)
  {
    var textBox = e.OriginalSource as TextBox;
    if (textBox != null)
      textBox.SelectAll();
  }

}
