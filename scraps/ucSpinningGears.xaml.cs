using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace iTRAACv2.View
{
	public partial class UcSpinningGears
	{
		public UcSpinningGears()
		{
			InitializeComponent();
		}

    public PlacementMode Placement { get { return (popWaitAnimation.Placement); } set { popWaitAnimation.Placement = value; } }

    public UIElement PlacementTarget
    {
      get { return (UIElement)GetValue(PlacementTargetProperty); }
      set { SetValue(PlacementTargetProperty, value); }
    }

    // Using a DependencyProperty as the backing store for PlacementTarget.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PlacementTargetProperty =
        DependencyProperty.Register("PlacementTarget", typeof(UIElement), typeof(UcSpinningGears),
          new PropertyMetadata((obj, args) => { ((UcSpinningGears) obj).popWaitAnimation.PlacementTarget = (args.NewValue as UIElement); }));
    

    protected override void UserControlLoaded(object sender, RoutedEventArgs e)
    {
      base.UserControlLoaded(sender, e);
      ((Storyboard) FindResource("spinner")).Stop();
    }

    public bool IsOpen { get { return (popWaitAnimation.IsOpen); } }

    public void Show()
    {
      popWaitAnimation.IsOpen = true;
      ((Storyboard) FindResource("spinner")).Begin();
    }

    public void Hide()
    {
      popWaitAnimation.IsOpen = false;
      ((Storyboard) FindResource("spinner")).Stop();
    }
	}
}