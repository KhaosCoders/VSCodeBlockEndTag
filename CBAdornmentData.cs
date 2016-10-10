using System.Windows;

namespace CodeBlockEndTag
{
    internal class CBAdornmentData
    {
        internal int StartPosition;
        internal int EndPosition;
        internal int HeaderStartPosition;

        internal readonly UIElement Adornment;


        internal CBAdornmentData(int start, int end, int headerStart, UIElement adornment)
        {
            StartPosition = start;
            EndPosition = end;
            HeaderStartPosition = headerStart;
            Adornment = adornment;
        }

    }
}
