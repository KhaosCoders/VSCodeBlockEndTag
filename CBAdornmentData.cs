using System.Windows;

namespace CodeBlockEndTag
{
    struct CBAdornmentData
    {
        internal readonly int StartPosition;
        internal readonly int EndPosition;

        internal readonly UIElement Adornment;


        internal CBAdornmentData(int start, int end, UIElement adornment)
        {
            StartPosition = start;
            EndPosition = end;
            Adornment = adornment;
        }

    }
}
