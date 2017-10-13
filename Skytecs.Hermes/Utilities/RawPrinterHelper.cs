using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Printing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Utilities
{
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

        // Send()
        // When the function is given a printer name and an unmanaged array
        // of bytes, the function sends those bytes to the print queue.
        // Returns true on success, false on failure.
        public static bool Send(string printerName, string task)
        {
            if (String.IsNullOrWhiteSpace(printerName))
            {
                throw new ArgumentNullException("printerName");
            }

            if (String.IsNullOrWhiteSpace(task))
            {
                throw new ArgumentNullException("task");
            }

            var bytes = Encoding.GetEncoding(1251).GetBytes(task);

            Int32 dwError = 0;
            Int32 dwWritten = 0;
            IntPtr hPrinter = new IntPtr(0);
            DOCINFOA di = new DOCINFOA();
            bool bSuccess = false; // Assume failure unless you specifically succeed.

            var defaultPrinter = LocalPrintServer.GetDefaultPrintQueue();

            di.pDocName = "My C#.NET RAW Document";
            di.pDataType = "RAW";

            // Open the printer.
            if (OpenPrinter(printerName.Normalize(), out hPrinter, IntPtr.Zero))
            {
                // Start a document.
                if (StartDocPrinter(hPrinter, 1, di))
                {
                    // Start a page.
                    if (StartPagePrinter(hPrinter))
                    {
                        IntPtr pUnmanagedBytes = IntPtr.Zero;
                        try
                        {
                            var nLength = bytes.Length;
                            pUnmanagedBytes = Marshal.AllocCoTaskMem(nLength);
                            // Copy the managed byte array into the unmanaged array.
                            Marshal.Copy(bytes, 0, pUnmanagedBytes, nLength);
                            // Send the unmanaged bytes to the printer.
                            bSuccess = WritePrinter(hPrinter, pUnmanagedBytes, nLength, out dwWritten);
                        }
                        finally
                        {
                            EndPagePrinter(hPrinter);
                            if (pUnmanagedBytes != IntPtr.Zero)
                            {
                                Marshal.FreeCoTaskMem(pUnmanagedBytes);
                            }
                        }
                        // Write your bytes.
                    }
                    EndDocPrinter(hPrinter);
                }
                ClosePrinter(hPrinter);
            }

            var lps = new LocalPrintServer(PrintSystemDesiredAccess.AdministrateServer);
            lps.DefaultPrintQueue = defaultPrinter;
            lps.Commit();

            // If you did not succeed, GetLastError may give more information
            // about why not.
            if (!bSuccess)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "My custom error message.");
            }
            return bSuccess;
        }
    }
}
