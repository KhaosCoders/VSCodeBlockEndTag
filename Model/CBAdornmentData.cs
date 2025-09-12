using System.Windows;

namespace CodeBlockEndTag.Model;

internal struct CBAdornmentData(int startPosition, int endPosition, int headerStartPosition, UIElement adornment)
{
    public int StartPosition = startPosition;
    public int EndPosition = endPosition;
    public int HeaderStartPosition = headerStartPosition;
    public UIElement Adornment = adornment;

    public void Move(int offset)
    {
        StartPosition += offset;
        EndPosition += offset;
        HeaderStartPosition += offset;
    }
}