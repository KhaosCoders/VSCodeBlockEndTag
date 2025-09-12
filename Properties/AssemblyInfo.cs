using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using CodeBlockEndTag;


[assembly: AssemblyTitle(Vsix.Name)]
[assembly: AssemblyDescription(Vsix.Description)]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(Vsix.Author)]
[assembly: AssemblyProduct(Vsix.Name)]
[assembly: AssemblyCopyright(Vsix.Author)]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: AssemblyVersion(Vsix.Version)]
[assembly: AssemblyFileVersion(Vsix.Version)]

// Ensure WPF looks for default styles in Themes/Generic.xaml
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
)]

namespace System.Runtime.CompilerServices
{
    public class IsExternalInit { }
}