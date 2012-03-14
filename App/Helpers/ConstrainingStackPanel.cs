using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

/// <summary>
/// This panel stacks children vertically and tries to constrain children so that
/// the panel fits within the available size given by the parent. Only children 
/// which have the attached property 'Constrain' set to true are constrained.
/// </summary>
// ReSharper disable CheckNamespace
public class ConstrainingStackPanel : Panel
// ReSharper restore CheckNamespace
{
    private readonly List<UIElement> _constrainableChildren = new List<UIElement>();

    public static bool GetConstrain(DependencyObject obj)
    {
        return (bool)obj.GetValue(ConstrainProperty);
    }
     
    public static void SetConstrain(DependencyObject obj, bool value)
    {
        obj.SetValue(ConstrainProperty, value);
    }

    // Using a DependencyProperty as the backing store for Constrain.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ConstrainProperty =
        DependencyProperty.RegisterAttached("Constrain", typeof(bool), typeof(ConstrainingStackPanel), new FrameworkPropertyMetadata(false, 
                                                                                      FrameworkPropertyMetadataOptions.AffectsParentMeasure ));

        

    protected override Size MeasureOverride(Size availableSize)
    {
        // Desired size for this panel to return to the parent
        double desiredHeight = 0;
        double desiredWidth = 0;

        // Desired heights the two 'types' of children
        double desiredHeightConstrainableChildren = 0;
        double desiredHeightRegularChildren = 0;            
            
        _constrainableChildren.Clear();

        foreach (UIElement child in InternalChildren)
        {
            // Let child figure out how much space it needs
            child.Measure(availableSize);

            if (GetConstrain(child))
            {
                // Deal with  constrainable children later once we know if they
                // need to be constrained or not
                _constrainableChildren.Add(child);
                desiredHeightConstrainableChildren += child.DesiredSize.Height;
            }
            else
            {
                desiredHeightRegularChildren += child.DesiredSize.Height;
                desiredHeight += child.DesiredSize.Height;
                desiredWidth = Math.Max(desiredWidth, child.DesiredSize.Width);
            }
        }
            
        // If the desired height of all children exceeds the available height, set the 
        // constrain flag to true
        double desiredHeightAllChildren = desiredHeightConstrainableChildren + desiredHeightRegularChildren;
        bool constrain = desiredHeightAllChildren > availableSize.Height;

        // Holds the space available for the constrainable children to share
        double availableVerticalSpace = Math.Max(availableSize.Height - desiredHeightRegularChildren, 0);

        // Re-measure these children and contrain them proportionally, if necessary, so the
        // largest child gets the largest portion of the vertical space available
        foreach (UIElement child in _constrainableChildren)
        {
            if (constrain)
            {
                double percent = child.DesiredSize.Height / desiredHeightConstrainableChildren;
                double verticalSpace = percent * availableVerticalSpace;
                child.Measure(new Size(availableSize.Width, verticalSpace));
            }
            desiredHeight += child.DesiredSize.Height;
            desiredWidth = Math.Max(desiredWidth, child.DesiredSize.Width);
        }
            
        return new Size(desiredWidth, desiredHeight);
    }


    protected override Size ArrangeOverride(Size finalSize)
    {
        double yPosition = 0;
        foreach (UIElement child in InternalChildren)
        {
            child.Arrange(new Rect(0, yPosition, finalSize.Width, child.DesiredSize.Height));
            yPosition += child.DesiredSize.Height;
        }
        return finalSize;
    }
}
