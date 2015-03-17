using System;
using System.Runtime.InteropServices;

namespace Evel.share {
    public class Utilities {
        // Contains information that the SHFileOperation function uses to perform 
        // file operations. 

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEOPSTRUCT {
            public IntPtr hwnd;   // Window handle to the dialog box to display 
            // information about the status of the file 
            // operation. 

            public UInt32 wFunc;   // Value that indicates which operation to 
            // perform.

            public IntPtr pFrom;   // Address of a buffer to specify one or more 
            // source file names. These names must be 
            // fully qualified paths. Standard Microsoft®   
            // MS-DOS® wild cards, such as "*", are 
            // permitted in the file-name position. 
            // Although this member is declared as a 
            // null-terminated string, it is used as a 
            // buffer to hold multiple file names. Each 

            // file name must be terminated by a single 
            // NULL character. An additional NULL 
            // character must be appended to the end of 
            // the final name to indicate the end of pFrom. 

            public IntPtr pTo;   // Address of a buffer to contain the name of 
            // the destination file or directory. This 
            // parameter must be set to NULL if it is not 
            // used. Like pFrom, the pTo member is also a 
            // double-null terminated string and is handled 
            // in much the same way. 

            public UInt16 fFlags;   // Flags that control the file operation. 


            public Int32 fAnyOperationsAborted;
            // Value that receives TRUE if the user aborted 
            // any file operations before they were 
            // completed, or FALSE otherwise. 


            public IntPtr hNameMappings;
            // A handle to a name mapping object containing 
            // the old and new names of the renamed files. 
            // This member is used only if the 

            // fFlags member includes the 
            // FOF_WANTMAPPINGHANDLE flag.


            [MarshalAs(UnmanagedType.LPWStr)]
            public String lpszProgressTitle;
            // Address of a string to use as the title of 
            // a progress dialog box. This member is used 
            // only if fFlags includes the 
            // FOF_SIMPLEPROGRESS flag.

        }


        private const UInt32 FO_MOVE = 0x1;
        private const UInt32 FO_COPY = 0x2;
        private const UInt32 FO_DELETE = 0x3;
        private const UInt32 FO_RENAME = 0x4;

        private const UInt16 FOF_SILENT = 0x4;
        private const UInt16 FOF_RENAMEONCOLLISION = 0x8;
        private const UInt16 FOF_NOCONFIRMATION = 0x10;
        private const UInt16 FOF_SIMPLEPROGRESS = 0x100;
        private const UInt16 FOF_ALLOWUNDO = 0x40;


        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern Int32 SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        public static void CopyDirectory(string source, string destination) {
            SHFILEOPSTRUCT plFileOp = new SHFILEOPSTRUCT();
            plFileOp.fAnyOperationsAborted = 0;
            plFileOp.hNameMappings = IntPtr.Zero;
            plFileOp.pFrom = Marshal.StringToHGlobalUni(source + "\0\0");
            plFileOp.pTo = Marshal.StringToHGlobalUni(destination + "\0\0");
            plFileOp.fFlags = FOF_NOCONFIRMATION | FOF_SILENT | FOF_SIMPLEPROGRESS;
            plFileOp.wFunc = FO_COPY;
            plFileOp.hwnd = IntPtr.Zero;
            SHFileOperation(ref plFileOp);
        }

        public static Exception findException(Exception exception) {
            while (exception.InnerException != null)
                exception = exception.InnerException;
            return exception;
        }

            //SHFILEOPSTRUCT FileOpStruct = new SHFILEOPSTRUCT();

            //FileOpStruct.hwnd = OwnerWindow;

            //FileOpStruct.wFunc = (uint)Operation;

            ////String multiSource = StringArrayToMultiString(SourceFiles);
            ////String multiDest = StringArrayToMultiString(DestFiles);
            //FileOpStruct.pFrom = Marshal.StringToHGlobalUni(source);
            //FileOpStruct.pTo = Marshal.StringToHGlobalUni(destination);

            //FileOpStruct.fFlags = (ushort)OperationFlags;
            //FileOpStruct.lpszProgressTitle = ProgressTitle;
            //FileOpStruct.fAnyOperationsAborted = 0;
            //FileOpStruct.hNameMappings = IntPtr.Zero;

            //int RetVal;
            //RetVal = ShellApi.SHFileOperation(ref FileOpStruct);

            //ShellApi.SHChangeNotify(
            //    (uint)ShellChangeNotificationEvents.SHCNE_ALLEVENTS,
            //    (uint)ShellChangeNotificationFlags.SHCNF_DWORD,
            //    IntPtr.Zero,
            //    IntPtr.Zero);

            //if (RetVal != 0)
            //    return false;

            //if (FileOpStruct.fAnyOperationsAborted != 0)
            //    return false;

            //return true;


    }
}
