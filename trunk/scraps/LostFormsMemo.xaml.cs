using System.Windows.Documents;
using System.IO;
using System.IO.Packaging;
using System;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace iTRAACv2
{
  public partial class LostFormsMemo 
  {
    public LostFormsMemo()
    {
    }

    static public void Display()
    {
      FixedPage fpage = App.LoadComponent(new Uri("Views/LostFormsMemo.xaml", UriKind.Relative)) as FixedPage;
      PageContent pagewrapper = new PageContent();
      (pagewrapper as IAddChild).AddChild(fpage);
      FixedDocument doc = new FixedDocument();
      doc.Pages.Add(pagewrapper);
      DocumentViewer docviewer = new DocumentViewer();
      docviewer.Document = doc;
      docviewer.Zoom = 75;
      
      Window win = new Window();
      win.Content = docviewer;
      win.Owner = App.Current.MainWindow;
      win.Width = (fpage.ActualWidth + 50) * 0.75;// docviewer.ExtentWidth + 50;
      //win.Height = fpage.ActualHeight;
      win.Show();
    }
  }
}
