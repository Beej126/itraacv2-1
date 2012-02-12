using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Data;

namespace iTRAACv2
{
  public class ucToggleButton_Lock : ucToggleButton
  {
    public ucToggleButton_Lock()
    {
      InitializeComponent();

      //had to use the "pack" resource URI format per this: (look for "ParserContext")
      //http://social.msdn.microsoft.com/Forums/en-US/wpf/thread/10f84d55-0670-44c5-9670-8262d3719db4
      UpImage = new ImageSourceConverter().ConvertFromString("pack://application:,,,/Assets/Glyphs/locked.png") as ImageSource;
      DownImage = new ImageSourceConverter().ConvertFromString("pack://application:,,,/Assets/Glyphs/unlocked.png") as ImageSource;
    }

  }
}
