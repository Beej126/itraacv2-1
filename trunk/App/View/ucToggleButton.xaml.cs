using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace iTRAACv2.View
{

  public partial class UcToggleButton
  {
    public ImageSource UpImage { get { return(_upImage) ;} set { _upImage = value; SetImageSource(); } } //having this propertyName implemented (rather than auto) fires the image setter so that it shows up in design mode which provides a better visual for layout
    private ImageSource _upImage;
    //public ImageSource UpImage { get; set; }

    /// <summary>
    /// UpImage is used for DownImage if none specified
    /// </summary>
    public ImageSource DownImage { get; set; }

    //public Style ToggleButtonStyle { get { return (btnToggle.Style); } set { btnToggle.Style = value; } }
    public Style ImageStyle { get { return (imgGlyph.Style); } set { imgGlyph.Style = value; } }

    public Style ToggleButtonStyle
    {
      get { return (Style)GetValue(ToggleButtonStyleProperty); }
      set { SetValue(ToggleButtonStyleProperty, value); }
    }

    public static readonly DependencyProperty ToggleButtonStyleProperty =
      DependencyProperty.Register("ToggleButtonStyle", typeof(Style), typeof(UcToggleButton),
        new PropertyMetadata((obj, args) =>
          { ((UcToggleButton) obj).btnToggle.Style = args.NewValue as Style; })); 


    public bool IsChecked
    {
      get { return ((bool)GetValue(IsCheckedProperty)); }
      set { SetValue(IsCheckedProperty, value); }
    }
    public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
      "IsChecked", typeof(bool), typeof(UcToggleButton),
        new PropertyMetadata(false, (obj, args) => 
          ((UcToggleButton) obj).CheckedChanged((bool)args.NewValue)));

    public event RoutedEventHandler IsCheckedChanged;
    private void CheckedChanged(bool newValue)
    {
      btnToggle.IsChecked = newValue;
      SetImageSource();
      if (IsCheckedChanged != null) IsCheckedChanged(this, new RoutedEventArgs());
    }

    public event RoutedEventHandler Click;
    private void BtnToggleClick(object sender, RoutedEventArgs e)
    {
      if (Click != null) Click(this, e);
    }

    private void SetImageSource()
    {
      Debug.Assert(btnToggle.IsChecked != null, "btnToggle.IsChecked != null");
      imgGlyph.Source = btnToggle.IsChecked.Value ? DownImage ?? UpImage : UpImage;
    }

    public string Text
    {
      get { return (GetValue(TextProperty) as string); }
      set { SetValue(TextProperty, value); }
    }
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
      "Text", typeof(string), typeof(UcToggleButton),
        new PropertyMetadata((obj, args) => 
          ((UcToggleButton) obj).TextChanged((string)args.NewValue)));

    private void TextChanged(string newValue)
    {
      lblText.Text = newValue;
      lblText.Visibility = (lblText.Text.Length > 0) ? Visibility.Visible : Visibility.Collapsed;
      double top;
      var left = top = (UpImage != null) ? 3 : 0;
      if (TextOrientation == Orientation.Horizontal) top = 0; else left = 0;
      lblText.Margin = new Thickness(left, top, 0, 0);
    }

    public Orientation TextOrientation
    {
      get { return (pnlStack.Orientation); }
      set { pnlStack.Orientation = value; }
    }

    //set the stretch mode only if the image is bound by specific height or width parameters
    public double ImageHeight { get { return (imgGlyph.Height); } set { imgGlyph.Height = value; /*imgGlyph.Stretch = Stretch.Uniform;*/ } }
    public double ImageWidth { get { return (imgGlyph.Width); } set { imgGlyph.Width = value; /*imgGlyph.Stretch = Stretch.Uniform;*/ } }

    public new bool IsVisible
    {
      get { return (btnToggle.Visibility == Visibility.Visible); }
      set { btnToggle.Visibility = (value ? Visibility.Visible : Visibility.Collapsed); }
    }

    //public new HorizontalAlignment HorizontalAlignment
    //{
    //  get { return (btnToggle.HorizontalAlignment); }
    //  set { btnToggle.HorizontalAlignment = value; }
    //}

    public UcToggleButton()
    {
      InitializeComponent();
      btnToggle.Checked += BtnToggleChecked;
      btnToggle.Unchecked += BtnToggleUnchecked;
    }

    /// <summary>
    /// Basically the normal Padding setting but republished under different name because Padding wouldn't override properly (yes even tried the "new" propertyName syntax)
    /// </summary>
    public Thickness ButtonPadding
    {
      get { return (btnToggle.Padding); }
      set { btnToggle.Padding = value; }
    }

    public double ButtonHeight
    {
      get { return (btnToggle.Height); }
      set { btnToggle.Height = value; }
    }

    private void BtnToggleChecked(object sender, RoutedEventArgs e)
    {
      IsChecked = true;
    }

    private void BtnToggleUnchecked(object sender, RoutedEventArgs e)
    {
      IsChecked = false;
    }

    protected override void UserControlLoaded(object sender, RoutedEventArgs e)
    {
      SetImageSource(); //couldn't figure out any other way to set the initial image... if the IsChecked boolean's default value is the same as the initial value the IsChecked setter doesn't fire on it's own
      TextChanged(Text);
      imgGlyph.Visibility = (UpImage == null) ? Visibility.Collapsed : Visibility.Visible;
    }

  }

}
