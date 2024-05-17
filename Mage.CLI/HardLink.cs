using System.Runtime.InteropServices;

namespace Mage.IO;

public class FileExt {

    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode )]
    public static extern bool CreateHardLink(
        string lpFileName,
        string lpExistingFileName,
        IntPtr lpSecurityAttributes
    );

}