using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.VisualStudio.Shell.Interop;

[SuppressUnmanagedCodeSecurity]
internal static class SafeNativeMethods
{
    [DllImport("user32.dll")]
    internal static extern int GetSysColor(int nIndex);
}