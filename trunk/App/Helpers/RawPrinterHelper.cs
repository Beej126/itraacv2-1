using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

//copied from here: http://support.microsoft.com/kb/322091
//note, for this to work, you must use the "Generic / Text Only" driver for the printer
//edit: actually, i found that it also works with the full fledged Epson FX-890 driver as well
//see comment by Greg Young on Jun 28 '06 here: http://bytes.com/topic/c-sharp/answers/506479-dotmatrix-printer-again
//another good reference: http://stackoverflow.com/questions/449777/print-on-dot-matrix-printer-in-net
// ReSharper disable CheckNamespace
public class RawPrinterHelper
// ReSharper restore CheckNamespace
{
  // Structure and API declarions:

  [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi,
    ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
  public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter,
                                        IntPtr pd);

  [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true,
    CallingConvention = CallingConvention.StdCall)]
  public static extern bool ClosePrinter(IntPtr hPrinter);

  [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi,
    ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
  public static extern bool StartDocPrinter(IntPtr hPrinter, Int32 level,
                                            [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

  [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true,
    CallingConvention = CallingConvention.StdCall)]
  public static extern bool EndDocPrinter(IntPtr hPrinter);

  [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true,
    CallingConvention = CallingConvention.StdCall)]
  public static extern bool StartPagePrinter(IntPtr hPrinter);

  [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true,
    CallingConvention = CallingConvention.StdCall)]
  public static extern bool EndPagePrinter(IntPtr hPrinter);

  [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true,
    CallingConvention = CallingConvention.StdCall)]
  public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, Int32 dwCount, out Int32 dwWritten);

  // SendBytesToPrinter()
  // When the function is given a printer name and an unmanaged array
  // of bytes, the function sends those bytes to the print queue.
  // Returns true on success, false on failure.
  public static bool SendBytesToPrinter(string szPrinterName, IntPtr pBytes, Int32 dwCount)
  {
    IntPtr hPrinter;
    var di = new DOCINFOA();
    var bSuccess = false; // Assume failure unless you specifically succeed.

    di.pDocName = Assembly.GetExecutingAssembly().GetName().Name + " Document";
    //di.pDataType = "RAW"; //leaving this null means it can be specified at the printer definition...
    //this facilitates having RAW for the true Epson printer and TEXT for the Generic / Text Only printer
    //for whatever reason the Generic printer will only print to "FILE:" when the TEXT "print processor" has been chosen
    //(under Control Panel > Printers > your printer > Printer properties > Advanced > Print processor)
    //and print to file is simply a convenient way to check that the basic print logic is working w/o wasting paper
    //what's even more whacked out is that when you do print to file, on Win7, the "Interactive Services Detection" will popup
    //to provide the GUI for entering the filename to output to
    //references: http://blogs.msdn.com/b/patricka/archive/2010/04/27/what-is-interactive-services-detection-and-why-is-it-blinking-at-me.aspx
    //http://s309.codeinspot.com/q/1071364

    // Open the printer.
    if (OpenPrinter(szPrinterName.Normalize(), out hPrinter, IntPtr.Zero))
    {
      // Start a document.
      if (StartDocPrinter(hPrinter, 1, di))
      {
        // Start a page.
        if (StartPagePrinter(hPrinter))
        {
          // Write your bytes.
          int dwWritten;
          bSuccess = WritePrinter(hPrinter, pBytes, dwCount, out dwWritten);
          EndPagePrinter(hPrinter);
        }
        EndDocPrinter(hPrinter);
      }
      ClosePrinter(hPrinter);
    }
    // If you did not succeed, GetLastError may give more information
    // about why not.
    if (bSuccess == false)
    {
      Marshal.GetLastWin32Error();
    }
    return bSuccess;
  }


  public static bool SendManagedBytesToPrinter(string szPrinterName, byte[] bytes)
  {
    // Your unmanaged pointer.
    // Allocate some unmanaged memory for those bytes.
    var pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
    // Copy the managed byte array into the unmanaged array.
    Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);
    // Send the unmanaged bytes to the printer.
    var bSuccess = SendBytesToPrinter(szPrinterName, pUnmanagedBytes, bytes.Length);
    // Free the unmanaged memory allocated earlier.
    Marshal.FreeCoTaskMem(pUnmanagedBytes);
    return bSuccess;
  }


  public static bool SendFileToPrinter(string szPrinterName, string szFileName)
  {
    return (SendStreamToPrinter(szPrinterName, new FileStream(szFileName, FileMode.Open)));
  }

  public static bool SendStreamToPrinter(string szPrinterName, Stream binStream)
  {
    int nLength;
    // Your unmanaged pointer.
    // Dim an array of bytes big enough to hold the file's contents.
    byte[] bytes;

    using (binStream)
    using (var br = new BinaryReader(binStream)) // Create a BinaryReader on the stream.
    {
      nLength = Convert.ToInt32(binStream.Length);
      // Read the contents of the file into the array.
      bytes = br.ReadBytes(nLength);
      binStream.Close();
    }

    // Allocate some unmanaged memory for those bytes.
    var pUnmanagedBytes = Marshal.AllocCoTaskMem(nLength);
    try
    {
      // Copy the managed byte array into the unmanaged array.
      Marshal.Copy(bytes, 0, pUnmanagedBytes, nLength);
      // Send the unmanaged bytes to the printer.
      var bSuccess = SendBytesToPrinter(szPrinterName, pUnmanagedBytes, nLength);
      return bSuccess;
    }
    finally
    {
      // Free the unmanaged memory allocated earlier.
      Marshal.FreeCoTaskMem(pUnmanagedBytes);
    }
  }

  public static bool SendStringToPrinter(string szPrinterName, string szString)
  {
    // How many characters are in the string?
    var dwCount = szString.Length;
    // Assume that the printer is expecting ANSI text, and then convert
    // the string to ANSI text.
    var pBytes = Marshal.StringToCoTaskMemAnsi(szString);
    // Send the converted ANSI string to the printer.
    SendBytesToPrinter(szPrinterName, pBytes, dwCount);
    Marshal.FreeCoTaskMem(pBytes);
    return true;
  }

  #region Nested type: DOCINFOA

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
  public class DOCINFOA
  {
    [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
    [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
    [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
  }

  #endregion
}


public class RawPrinterJob
{
  public List<byte[]> Pages = new List<byte[]>();
  public int PageWidth { get; private set; }
  public int PageHeight { get; private set; }

  private readonly string _initCodes;

  public RawPrinterJob(string initCodes, int widthChars, int heightChars)
  {
    PageWidth = widthChars;
    PageHeight = heightChars;
    _initCodes = initCodes;
    AddPage();
  }

  private void AddPage()
  {
    var numberOfBytes = _initCodes.Length
                        + ((PageWidth + 1 /*LF*/)*PageHeight)
                        + 1 /*FF*/;

    // using a simple "flat 2D array" approach ala: http://www.dotnetperls.com/flatten-array
    // logicalrow = row * pagewidth
    Pages.Add(new byte[numberOfBytes]);

    //put the printer init codes at the beginning of the page buffer
    Buffer.BlockCopy(CP437GetBytes(_initCodes), 0, Pages[Pages.Count - 1], 0, _initCodes.Length);

    //put a form feed (FF) at the very end of the page buffer
    Pages[Pages.Count - 1][Pages[Pages.Count - 1].Length - 1] = 0x0C;

    //initialize the page with all spaces ...
    var rowbytes = CP437GetBytes(new string(' ', PageWidth + 1 /*LF*/));
    //plus a line feed at the end of every line
    rowbytes[PageWidth] = 0x0A; // linefeed (LF)
    //apparently our venerable friend the simple loop is the recommended best practice for this drudgery
    //http://stackoverflow.com/questions/1897555/what-is-the-equivalent-of-memset-in-c
    //http://stackoverflow.com/questions/6150097/initialize-a-byte-array-to-a-certain-value-other-than-the-default-null
    for (var row = 0; row < PageHeight; row++)
    {
      PrintBytesAtPos(rowbytes, Pages.Count - 1, 0, row);
    }
  }

  public bool SendToPrinter(string printerName)
  {
    var success = true;
    foreach(var page in Pages)
    {
      success = RawPrinterHelper.SendManagedBytesToPrinter(printerName, page);
      if (!success) break;
    }
    return (success);
  }

  public void PrintTestRulers()
  {
    var columnRuler1 = ""; //first row ruler = 0123456789 over and over
    var columnRuler2 = ""; //second row ruler = the 10 marker, 20 marker, etc.
    int i;
    for (i = 1; i*10 < PageWidth; i++)
    {
      columnRuler1 += "0123456789";
      columnRuler2 += (((i == 1 ? 0 : -1) + i).ToString(CultureInfo.InvariantCulture) + "         ").Left(10);
    }
    columnRuler1 += "0123456789".Left(PageWidth - columnRuler1.Length);
    columnRuler2 +=
      ((i - 1).ToString(CultureInfo.InvariantCulture) + "         ").Left(PageWidth - columnRuler2.Length);

    PrintStringAtPos(columnRuler1, 0, 0, 0);
    PrintStringAtPos(columnRuler2, 0, 0, 1);
    for (var row = 2; row < PageHeight; row++)
    {
      PrintStringAtPos(row.ToString(CultureInfo.InvariantCulture), 0, 0, row);
    }
  }

  public RawPrinterJob Clone()
  {
    return (new RawPrinterJob(_initCodes, PageWidth, PageHeight) {Pages = Pages});
  }

  public void PrintStringAtPos(string str, int pagenum, int col, int row, int maxlength = 100, int maxrows = 1)
  {
    if (String.IsNullOrWhiteSpace(str)) return;

    // replace Euro with "{" {backspace} "=" as the best approximation i've come up with...
    // unforunately the Euro currency came along after the printer's ROM... "{=" seemed to look the best out of various potential combinations, e.g. (=, C=, C:
    str = str.Replace("€", "{\x08=");

    //wordwrap logic...
    if (str.Length > maxlength && Math.Abs(maxrows) > 1)
    {
      var lines = new StringCollection();

      //first check if text already has hard coded carriage returns in it and just assume those have been formatted properly
      if (str.Contains("\n")) lines.AddRange(str.Split('\n'));
      else
      {
        //otherwise dive into word wrap logic...
        //regex from here: http://regexlib.com/%28A%28EUjmatMAbqeJAVntKDWeVsP5A_5Xg2Q0u9xwCAHWWstjGAsCMb9Bvt6wVbYw4T-qTSvJU4RyVpn50P4Nci6N1vvxBArnwgO_k8F_3qJa5EdPPrrHHjp02XUS30FiGt4NNIFU8ZEynWmDksDVFHsuXNjDJ4ZS2saCasmnMl03WWfEW6uh7-BNkiTj0JD4Jypu0%29%29/REDetails.aspx?regexp_id=470
        //grabs the 'maxlength' of chars it can, breaking on whitespace, period or dash
        var wordWrapRegex =
          new Regex(@"^[\s\S]{1," + maxlength.ToString(CultureInfo.InvariantCulture) + @"}([\s\.\-]|$)",
                    RegexOptions.Singleline | RegexOptions.ExplicitCapture);
        while (lines.Count < Math.Abs(maxrows))
        {
          Match result = wordWrapRegex.Match(str);
          if (!result.Success) break;
          lines.Add(result.Value.TrimEnd());
          str = str.Left(-result.Value.Length);
          //negative Left "slices" x chars off the left and returns the remainder
        }
        //if we bailed out because we couldn't wordwrap anything in, just include the chopped text so at least it's visible
        if (lines.Count < Math.Abs(maxrows) && str.Length > 0) lines.Add(str.Left(maxlength));
      }

      //negative maxrows means word wrap from the top of the block defined by row-lines.count
      //e.g. when you're printing an NF1, Field 11 (the NF2 value box) has a "DO NOT GO OVER 2500" phrase that wraps within that space,
      //so you want to "back up" to the top of the allocated text space and wrap down from there... 
      //conversely, when printing the flat NF2 dollar value, you want it to go right on the specified row at the bottom of the space
      if (maxrows < 0) row = row - lines.Count + 1;

      foreach (var line in lines)
        PrintBytesAtPos(CP437GetBytes(line), pagenum, col, row++);
    }
    else
      //otherwise just print the maxlength of single line
      PrintBytesAtPos(CP437GetBytes(str.Left(maxlength)), pagenum, col, row);
  }

  /// <summary>
  /// Get ASCII bytes corresponding to code page 437 for specified string.
  /// </summary>
  /// <param name="str"></param>
  /// <returns></returns>
  public static byte[] CP437GetBytes(string str)
  {
    //nugget: the Epson FX-890 dot matrix printer only supports code page 437
    //nugget: 437 is the original DOS character codepage which includes Extended ASCII chars (unlike Encoding.ASCII which is 7 bit only)
    //nugget: to print german umlaut characters like ü we have to send the byte codes lined up with the printer's CP437
    return (Encoding.GetEncoding(437).GetBytes(str)); //nugget: 
  }

  private void PrintBytesAtPos(byte[] bytes, int pagenum, int col, int row)
  {
    Assert.Check(row < PageHeight && row >= 0,
                 "Printing attempted to an invalid ROW: " + row.ToString(CultureInfo.InvariantCulture));
    Assert.Check(col < PageWidth && col >= 0,
                 "Printing attempted to an invalid COL: " + col.ToString(CultureInfo.InvariantCulture));

    if (pagenum == Pages.Count) AddPage();
    Buffer.BlockCopy(bytes, 0, Pages[pagenum], _initCodes.Length + (row*(PageWidth + 1 /*LF*/)) + col, bytes.Length);
  }
}
