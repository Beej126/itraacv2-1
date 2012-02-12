using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace iTRAACv2
{
  class RoutedCommands
  {
    public static readonly RoutedCommand OpenTaxForm = new RoutedCommand();
    public static readonly RoutedCommand OpenSponsor = new RoutedCommand();
    public static readonly RoutedCommand NewSponsor = new RoutedCommand();
  }
}
