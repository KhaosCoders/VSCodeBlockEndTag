using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using System.Windows;

namespace CodeBlockEndTag
{
    internal class CBRegion
    {
        public Span Span { get; set; }

        public string Header { get; set; }

        public CBETagControl Adornment { get; set; }

        public bool IsDisplayed { get; set; }

        public ImageMoniker IconMoniker { get; set; }

        public CBRegion(string header, Span span, ImageMoniker iconMoniker)
        {
            Span = span;
            Header = header;
            IconMoniker = iconMoniker;
        }

    }
}
