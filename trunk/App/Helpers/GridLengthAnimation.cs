using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;

/// <summary>
/// Animates a grid length value just like the DoubleAnimation animates a double value
/// </summary>
// ReSharper disable CheckNamespace
public class GridLengthAnimation : AnimationTimeline
// ReSharper restore CheckNamespace
{
  private bool _isCompleted;

  /// <summary>
  /// Marks the animation as completed
  /// </summary>
  public bool IsCompleted
  {
    get { return _isCompleted; }
    set { _isCompleted = value; }
  }

  /// <summary>
  /// Sets the reverse value for the second animation
  /// </summary>
  public double ReverseValue
  {
    get { return (double)GetValue(ReverseValueProperty); }
    set { SetValue(ReverseValueProperty, value); }
  }

  /// <summary>
  /// Dependency property. Sets the reverse value for the second animation
  /// </summary>
  public static readonly DependencyProperty ReverseValueProperty =
  DependencyProperty.Register("ReverseValue", typeof(double), typeof(GridLengthAnimation), new UIPropertyMetadata(0.0));

  /// <summary>
  /// Returns the type of object to animate
  /// </summary>
  public override Type TargetPropertyType
  {
    get
    {
      return typeof(GridLength);
    }
  }

  /// <summary>
  /// Creates an instance of the animation object
  /// </summary>
  /// <returns>Returns the instance of the GridLengthAnimation</returns>
  protected override Freezable CreateInstanceCore()
  {
    return new GridLengthAnimation();
  }

  /// <summary>
  /// Dependency property for the From property
  /// </summary>
  public static readonly DependencyProperty FromProperty = DependencyProperty.Register("From", typeof(GridLength), typeof(GridLengthAnimation));

  /// <summary>
  /// CLR Wrapper for the From depenendency property
  /// </summary>
  public GridLength From
  {
    get
    {
      return (GridLength)GetValue(FromProperty);
    }
    set
    {
      SetValue(FromProperty, value);
    }
  }

  /// <summary>
  /// Dependency property for the To property
  /// </summary>
  public static readonly DependencyProperty ToProperty = DependencyProperty.Register("To", typeof(GridLength), typeof(GridLengthAnimation));

  /// <summary>
  /// CLR Wrapper for the To property
  /// </summary>
  public GridLength To
  {
    get
    {
      return (GridLength)GetValue(ToProperty);
    }
    set
    {
      SetValue(ToProperty, value);
    }
  }
  
  AnimationClock _clock;

  /// <summary>
  /// registers to the completed event of the animation clock
  /// </summary>
  /// <param name="clock">the animation clock to notify completion status</param>
  void VerifyAnimationCompletedStatus(AnimationClock clock)
  {
    if (_clock != null) return;
    _clock = clock;
    _clock.Completed += delegate { _isCompleted = true; };
  }

  /// <summary>
  /// Animates the grid let set
  /// </summary>
  /// <param name="defaultOriginValue">The original value to animate</param>
  /// <param name="defaultDestinationValue">The final value</param>
  /// <param name="animationClock">The animation clock (timer)</param>
  /// <returns>Returns the new grid length to set</returns>
  public override object GetCurrentValue(object defaultOriginValue,
  object defaultDestinationValue, AnimationClock animationClock)
  {
    //check the animation clock event
    VerifyAnimationCompletedStatus(animationClock);
    
    //check if the animation was completed
    if (_isCompleted)
      return (GridLength)defaultDestinationValue;
    
    //if not then create the value to animate
    var fromVal = From.Value;
    var toVal = To.Value;
    
    //check if the value is already collapsed
    if (Math.Abs(((GridLength)defaultOriginValue).Value - toVal) < 0.1)
    {
      fromVal = toVal;
      toVal = ReverseValue;
    }
    else
      //check to see if this is the last tick of the animation clock.
    {
      Debug.Assert(animationClock.CurrentProgress != null, "animationClock.CurrentProgress != null");
      if (Math.Abs(animationClock.CurrentProgress.Value - 1.0) < 0.1)
        return To;
    }

    EasingFunctionBase easing = new ElasticEase {Oscillations = 2, EasingMode = EasingMode.EaseOut, Springiness = 10};
    Debug.Assert(animationClock.CurrentProgress != null, "animationClock.CurrentProgress != null");
  
    if (fromVal > toVal)
    {
      return new GridLength((1 - easing.Ease(animationClock.CurrentProgress.Value)) * (fromVal - toVal) + toVal,
                            From.IsStar ? GridUnitType.Star : GridUnitType.Pixel);
    }
    return new GridLength(easing.Ease(animationClock.CurrentProgress.Value) * (toVal - fromVal) + fromVal, 
                          From.IsStar ? GridUnitType.Star : GridUnitType.Pixel);
  }

  /// <summary>
  /// Animates a double value
  /// </summary>
  public class ExpanderDoubleAnimation : DoubleAnimationBase
  {

    /// <summary>
    /// Dependency property for the From property
    /// </summary>
    //public static readonly DependencyProperty FromProperty = DependencyProperty.Register("From", typeof(double?),
    //typeof(ExpanderDoubleAnimation));
    
    /// <summary>
    /// CLR Wrapper for the From depenendency property
    /// </summary>
    public double? From
    {
      get
      {
        return (double?)GetValue(FromProperty);
      }
      set
      {
        SetValue(FromProperty, value);
      }
    }
    
    /// <summary>
    /// Dependency property for the To property
    /// </summary>
    //public static readonly DependencyProperty ToProperty = DependencyProperty.Register("To", typeof(double?),
    //typeof(ExpanderDoubleAnimation));
    
    /// <summary>
    /// CLR Wrapper for the To property
    /// </summary>
    public double? To
    {
      get
      {
        return (double?)GetValue(ToProperty);
      }
      set
      {
        SetValue(ToProperty, value);
      }
    }
    
    /// <summary>
    /// Sets the reverse value for the second animation
    /// </summary>
    public double? ReverseValue
    {
      get { return (double)GetValue(ReverseValueProperty); }
      set { SetValue(ReverseValueProperty, value); }
    }
    
    /// <summary>
    /// Sets the reverse value for the second animation
    /// </summary>
    //public static readonly DependencyProperty ReverseValueProperty =
    //DependencyProperty.Register("ReverseValue", typeof(double?), typeof(ExpanderDoubleAnimation), new UIPropertyMetadata(0.0));

    /// <summary>
    /// Creates an instance of the animation
    /// </summary>
    /// <returns></returns>
    protected override Freezable CreateInstanceCore()
    {
      return new ExpanderDoubleAnimation();
    }
    
    /// <summary>
    /// Animates the double value
    /// </summary>
    /// <param name="defaultOriginValue">The original value to animate</param>
    /// <param name="defaultDestinationValue">The final value</param>
    /// <param name="animationClock">The animation clock (timer)</param>
    /// <returns>Returns the new double to set</returns>
    protected override double GetCurrentValueCore(double defaultOriginValue, double defaultDestinationValue, AnimationClock animationClock)
    {
      Debug.Assert(From != null, "From != null");
      Debug.Assert(To != null, "To != null");
      Debug.Assert(ReverseValue != null, "ReverseValue != null");
      Debug.Assert(animationClock.CurrentProgress != null, "animationClock.CurrentProgress != null");

      var fromVal = From.Value;
      var toVal = To.Value;

      if (Math.Abs(defaultOriginValue - toVal) < 0.1)
      {
        fromVal = toVal;
        toVal = ReverseValue.Value;
      }

      if (fromVal > toVal)
      {
        return (1 - animationClock.CurrentProgress.Value) * (fromVal - toVal) + toVal;
      }
      return (animationClock.CurrentProgress.Value *  (toVal - fromVal) + fromVal);
    }

  }
}