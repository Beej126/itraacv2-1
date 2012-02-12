using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace iTRAACv2
{
	public partial class ucSpinningGears : ucBase
	{
		public ucSpinningGears()
		{
			this.InitializeComponent();
		}

    public PlacementMode Placement { get { return (popWaitAnimation.Placement); } set { popWaitAnimation.Placement = value; } }

    public UIElement PlacementTarget
    {
      get { return (UIElement)GetValue(PlacementTargetProperty); }
      set { SetValue(PlacementTargetProperty, value); }
    }

    // Using a DependencyProperty as the backing store for PlacementTarget.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PlacementTargetProperty =
        DependencyProperty.Register("PlacementTarget", typeof(UIElement), typeof(ucSpinningGears),
          new PropertyMetadata(propertyChangedCallback: (obj, args) => { (obj as ucSpinningGears).popWaitAnimation.PlacementTarget = (args.NewValue as UIElement); }));
    

    protected override void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      base.UserControl_Loaded(sender, e);
      (FindResource("spinner") as Storyboard).Stop();
    }

    public bool IsOpen { get { return (popWaitAnimation.IsOpen); } }

    public void Show()
    {
      popWaitAnimation.IsOpen = true;
      (FindResource("spinner") as Storyboard).Begin();
    }

    public void Hide()
    {
      popWaitAnimation.IsOpen = false;
      (FindResource("spinner") as Storyboard).Stop();
    }
	}
}