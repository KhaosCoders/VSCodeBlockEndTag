using System;
using System.Collections.Generic;

namespace CodeBlockEndTag.OptionPage
{
    internal static class ContentTypeDisplayNameMapper
    {
        private static readonly Dictionary<string, string> _displayNameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Basic", "Basic" },
            { "CSharp", "C#" },
            { "C/C++", "C/C++" },
            { "FSharp", "F#" },
            { "VisualBasic", "Visual Basic" },
            { "ClangFormat", "Clang Format" },
            { "CMake", "CMake" },
            { "CMakePresets", "CMake Presets" },
            { "CMakeSettings", "CMake Settings" },
            { "code", "Code" },
            { "code++", "Code++" },
            { "code-languagesserver-base", "Code Language Server" },
            { "code-languagesserver-preview", "Code Language Server (Preview)" },
            { "Command", "Command" },
            { "CppProperties", "C/C++ Properties" },
            { "css", "CSS" },
            { "css.extensions", "CSS (extensions)" },
            { "cssLSPClient", "CSS (LSP)" },
            { "EmbeddedCodeContentType", "Embedded Code" },
            { "FSharpInteractive", "F# Interactive" },
            { "handlebars", "Handlebars" },
            { "HLSL", "HLSL" },
            { "htc", "HTC" },
            { "HTML", "HTML" },
            { "html-delegation", "HTML (delegation)" },
            { "htmlLSPClient", "HTML (LSP)" },
            { "Immediate", "Immediate Window" },
            { "Interactive Command", "Interactive Command" },
            { "JSON", "JSON" },
            { "LegacyRazor", "Razor (Legacy)" },
            { "LegacyRazorCoreCSharp", "Razor (Legacy C#)" },
            { "LegacyRazorCSharp", "Razor (Legacy C#)" },
            { "LegacyRazorVisualBasic", "Razor (Legacy VB)" },
            { "LESS", "LESS" },
            { "McpJson", "MCP JSON" },
            { "mustache", "Mustache" },
            { "Register", "Register" },
            { "Rest", "REST" },
            { "Roslyn Languages", "Roslyn" },
            { "SCSS", "SCSS" },
            { "SemanticSearch-CSharp", "C# (Semantic Search)" },
            { "Specialized CSharp and VB Interactive Command", "C# & VB Interactive" },
            { "srf", "SRF" },
            { "T-SQL90", "T-SQL" },
            { "TypeScript", "TypeScript" },
            { "TypeScript.Pug", "TypeScript (Pug)" },
            { "VB_LSP", "Visual Basic (LSP)" },
            { "vbscript", "VBScript" },
            { "WebForms", "ASP.NET Web Forms" },
            { "wsh", "WSH" },
            { "XAML", "XAML" },
            { "XML", "XML" },
            { "yaml", "YAML" }
        };

        public static string GetDisplayName(string contentTypeName)
        {
            if (string.IsNullOrEmpty(contentTypeName))
                return contentTypeName;

            if (_displayNameMap.TryGetValue(contentTypeName, out var display))
                return display;

            var fallback = contentTypeName.Replace('-', ' ').Replace('.', ' ');
            if (fallback.Length > 0)
                return char.ToUpperInvariant(fallback[0]) + fallback.Substring(1);

            return contentTypeName;
        }
    }
}