using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

//copied from here: http://support.microsoft.com/kb/322091
//note, for this to work, you must use the "Generic / Text Only" driver for the printer
//edit: actually, i found that it also works with the full fledged Epson FX-890 driver as well
//see comment by Greg Young on Jun 28 '06 here: http://bytes.com/topic/c-sharp/answers/506479-dotmatrix-printer-again
//another good reference: http://stackoverflow.com/questions/449777/print-on-dot-matrix-printer-in-net
public class RawPrinterHelper
{
  // Structure and API declarions:
  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
  public class DOCINFOA
  {
    [MarshalAs(UnmanagedType.LPStr)]
    public string pDocName;
    [MarshalAs(UnmanagedType.LPStr)]
    public string pOutputFile;
    [MarshalAs(UnmanagedType.LPStr)]
    public string pDataType;
  }
  [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
  public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

  [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
  public static extern bool ClosePrinter(IntPtr hPrinter);

  [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
  public static extern bool StartDocPrinter(IntPtr hPrinter, Int32 level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

  [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
  public static extern bool EndDocPrinter(IntPtr hPrinter);

  [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
  public static extern bool StartPagePrinter(IntPtr hPrinter);

  [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
  public static extern bool EndPagePrinter(IntPtr hPrinter);

  [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
  public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, Int32 dwCount, out Int32 dwWritten);

  // SendBytesToPrinter()
  // When the function is given a printer name and an unmanaged array
  // of bytes, the function sends those bytes to the print queue.
  // Returns true on success, false on failure.
  public static bool SendBytesToPrinter(string szPrinterName, IntPtr pBytes, Int32 dwCount)
  {
    Int32 dwError = 0, dwWritten = 0;
    IntPtr hPrinter = new IntPtr(0);
    DOCINFOA di = new DOCINFOA();
    bool bSuccess = false; // Assume failure unless you specifically succeed.

    di.pDocName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + " Document";
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
      dwError = Marshal.GetLastWin32Error();
    }
    return bSuccess;
  }


  public static bool SendManagedBytesToPrinter(string szPrinterName, byte[] bytes)
  {
    // Your unmanaged pointer.
    IntPtr pUnmanagedBytes = new IntPtr(0);
    // Allocate some unmanaged memory for those bytes.
    pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
    // Copy the managed byte array into the unmanaged array.
    Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);
    // Send the unmanaged bytes to the printer.
    bool bSuccess = SendBytesToPrinter(szPrinterName, pUnmanagedBytes, bytes.Length);
    // Free the unmanaged memory allocated earlier.
    Marshal.FreeCoTaskMem(pUnmanagedBytes);
    return bSuccess;
  }


  public static bool SendFileToPrinter(string szPrinterName, string szFileName)
  {
    return(SendStreamToPrinter(szPrinterName, new FileStream(szFileName, FileMode.Open)));
  }

  public static bool SendStreamToPrinter(string szPrinterName, Stream binStream)
  {
    int nLength;
    // Your unmanaged pointer.
    IntPtr pUnmanagedBytes = new IntPtr(0);
    bool bSuccess = false;
    // Dim an array of bytes big enough to hold the file's contents.
    Byte[] bytes = new Byte[binStream.Length];

    using (binStream)
    using (BinaryReader br = new BinaryReader(binStream)) // Create a BinaryReader on the stream.
    {
      nLength = Convert.ToInt32(binStream.Length);
      // Read the contents of the file into the array.
      bytes = br.ReadBytes(nLength);
      binStream.Close();
    }

    // Allocate some unmanaged memory for those bytes.
    pUnmanagedBytes = Marshal.AllocCoTaskMem(nLength);
    try
    {
      // Copy the managed byte array into the unmanaged array.
      Marshal.Copy(bytes, 0, pUnmanagedBytes, nLength);
      // Send the unmanaged bytes to the printer.
      bSuccess = SendBytesToPrinter(szPrinterName, pUnmanagedBytes, nLength);
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
    IntPtr pBytes;
    Int32 dwCount;
    // How many characters are in the string?
    dwCount = szString.Length;
    // Assume that the printer is expecting ANSI text, and then convert
    // the string to ANSI text.
    pBytes = Marshal.StringToCoTaskMemAnsi(szString);
    // Send the converted ANSI string to the printer.
    SendBytesToPrinter(szPrinterName, pBytes, dwCount);
    Marshal.FreeCoTaskMem(pBytes);
    return true;
  }

}

public class RawCharacterPage
{
  public int PageWidth {get; private set;} 
  public int PageHeight {get; private set;}

  private string initcodes;
  private byte[] pagebytes;
  

  public bool SendToPrinter(string PrinterName)
  {
    return(RawPrinterHelper.SendManagedBytesToPrinter(PrinterName, pagebytes));
  }

  public RawCharacterPage(string InitCodes, int Width_chars, int Height_chars)
  {
    PageWidth = Width_chars;
    PageHeight = Height_chars;
    initcodes = InitCodes;

    // using a simple "flat 2D array" approach ala: http://www.dotnetperls.com/flatten-array
    // logicalrow = row * pagewidth
    pagebytes = new byte[InitCodes.Length + ((PageWidth + 1 /*LF*/) * PageHeight) + 1 /*FF*/];

    //put the printer init codes at the beginning of the page buffer
    Buffer.BlockCopy(CP437GetBytes(initcodes), 0, pagebytes, 0, InitCodes.Length);

    //put a form feed (FF) at the very end of the page buffer
    pagebytes[pagebytes.Length - 1] = 0x0C;

    //initialize the page with all spaces ...
    byte[] rowbytes = CP437GetBytes(new string(' ', PageWidth + 1 /*LF*/));
    //plus a line feed at the end of every line
    rowbytes[PageWidth] = 0x0A; // linefeed (LF)
    //apparently simple for-loop is the recommended best practice for this drudgery
    //http://stackoverflow.com/questions/1897555/what-is-the-equivalent-of-memset-in-c
    //http://stackoverflow.com/questions/6150097/initialize-a-byte-array-to-a-certain-value-other-than-the-default-null
    for (int row = 0; row < PageHeight; row++)
    {
      PrintBytesAtPos(rowbytes, 0, row);
    }
  }

  public void PrintTestRulers()
  {
    string column_ruler1 = ""; //first row ruler = 0123456789 over and over
    string column_ruler2 = ""; //second row ruler = the 10 marker, 20 marker, etc.
    int i;
    for (i = 1; i * 10 < PageWidth; i++)
    {
      column_ruler1 += "0123456789";
      column_ruler2 += (((i == 1 ? 0 : -1) + i).ToString() + "         ").Left(10);
    }
    column_ruler1 += "0123456789".Left(PageWidth - column_ruler1.Length);
    column_ruler2 += ((i - 1).ToString() + "         ").Left(PageWidth - column_ruler2.Length);

    PrintStringAtPos(column_ruler1, 0, 0);
    PrintStringAtPos(column_ruler2, 0, 1);
    for (int row = 2; row < PageHeight; row++)
    {
      PrintStringAtPos(row.ToString(), 0, row);
    }
  }

  public RawCharacterPage Clone()
  {
    RawCharacterPage pr = new RawCharacterPage(initcodes, PageWidth, PageHeight);
    pr.pagebytes = pagebytes;
    return (pr);
  }

  public void PrintStringAtPos(string str, int col, int row, int maxlength = 100, int maxrows = 1)
  {
    if (String.IsNullOrWhiteSpace(str)) return;

    // replace Euro with "{" {backspace} "=" as the best approximation i've come up with...
    // unforunately the Euro currency came along after the printer's ROM... "{=" seemed to look the best out of various potential combinations, e.g. (=, C=, C:
    str = str.Replace("€", "{\x08=");

    /* learned more about code pages and accomplished this more elegantly via Encoding.GetEncoding(437).GetBytes()
    Replace("ä", "\x84").Replace("Ä", "\x8e").
    Replace("ë", "\x89").Replace("Ë", "\x89"). //no cap version
    Replace("ï", "\x8b").Replace("Ï", "\x8b"). //no cap
    Replace("ö", "\x94").Replace("Ö", "\x99").
    Replace("ü", "\x81").Replace("Ü", "\x9a").
    Replace("ÿ", "\x98").Replace("Ÿ", "\x98"). //no cap
    Replace("ß", "\xe1").
    */
      
    //wordwrap logic...
    if (str.Length > maxlength && Math.Abs(maxrows) > 1)
    {
      //regex from here: http://regexlib.com/%28A%28EUjmatMAbqeJAVntKDWeVsP5A_5Xg2Q0u9xwCAHWWstjGAsCMb9Bvt6wVbYw4T-qTSvJU4RyVpn50P4Nci6N1vvxBArnwgO_k8F_3qJa5EdPPrrHHjp02XUS30FiGt4NNIFU8ZEynWmDksDVFHsuXNjDJ4ZS2saCasmnMl03WWfEW6uh7-BNkiTj0JD4Jypu0%29%29/REDetails.aspx?regexp_id=470
      //grabs the 'maxlength' of chars it can, breaking on whitespace, period or dash
      Regex WordWrapRegex = new Regex(@"^[\s\S]{1," + maxlength.ToString() + @"}([\s\.\-]|$)", RegexOptions.Singleline | RegexOptions.ExplicitCapture);
      System.Collections.Specialized.StringCollection lines = new System.Collections.Specialized.StringCollection();
      while(lines.Count < Math.Abs(maxrows))
      {
        Match result = WordWrapRegex.Match(str);
        if (!result.Success) break;
        lines.Add(result.Value.TrimEnd());
        str = str.Left(-result.Value.Length);//negative Left "slices" x chars off the left and returns the remainder
      }
      //if we bailed out because we couldn't wordwrap anything in, just include the chopped text so at least it's visible
      if (lines.Count < Math.Abs(maxrows) && str.Length > 0) lines.Add(str.Left(maxlength));

      //negative maxrows means word wrap from the top of the block defined by row-lines.count
      //e.g. when you're printing an NF1, Field 11 (the NF2 value box) has a "DO NOT GO OVER 2500" phrase that wraps within that space,
      //so you want to "back up" to the top of the allocated text space and wrap down from there... 
      //conversely, when printing the flat NF2 dollar value, you want it to go right on the specified row at the bottom of the space
      if (maxrows < 0) row = row - lines.Count + 1;
      foreach(string line in lines)
        PrintBytesAtPos(CP437GetBytes(line), col, row++);
    }
    else
      //otherwise just print the maxlength of single line
      PrintBytesAtPos(CP437GetBytes(str.Left(maxlength)), col, row);
  }

  static public byte[] CP437GetBytes(string str)
  {
    //nugget: the Epson FX-890 dot matrix printer only supports code page 437
    //nugget: 437 is the original DOS character codepage which includes Extended ASCII chars (unlike Encoding.ASCII which is 7 bit only)
    //nugget: to print german umlaut characters like ü we have to send the byte codes lined up with the printer's CP437
    return (Encoding.GetEncoding(437).GetBytes(str));  //nugget: 
  }

  private void PrintBytesAtPos(byte[] bytes, int col, int row)
  {
    Assert.Check(row < PageHeight && row >= 0, "Printing attempted to an invalid ROW: " + row.ToString());
    Assert.Check(col < PageWidth && col >= 0, "Printing attempted to an invalid COL: " + col.ToString());

    Buffer.BlockCopy(bytes, 0, pagebytes, initcodes.Length + (row * (PageWidth + 1 /*LF*/)) + col, bytes.Length);
  }

}

