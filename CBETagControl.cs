using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CodeBlockEndTag;

public class CBETagControl : ButtonBase
{
    private static readonly Type typeofThis = typeof(CBETagControl);

    static CBETagControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeofThis, new FrameworkPropertyMetadata(typeofThis));
    }

    public CBETagControl()
    {
        DataContext = this;
    }

    public double LineHeight
    {
        get { return (double)GetValue(LineHeightProperty); }
        set { SetValue(LineHeightProperty, value); }
    }
    public static readonly DependencyProperty LineHeightProperty =
        DependencyProperty.Register("LineHeight", typeof(double), typeofThis, new PropertyMetadata(16.0));

    public object IconMoniker
    {
        get { return GetValue(IconMonikerProperty); }
        set { SetValue(IconMonikerProperty, value); }
    }
    public static readonly DependencyProperty IconMonikerProperty =
        DependencyProperty.Register("IconMoniker", typeof(object), typeofThis, new PropertyMetadata(null));

    public int DisplayMode
    {
        get { return (int)GetValue(DisplayModeProperty); }
        set { SetValue(DisplayModeProperty, value); }
    }
    public static readonly DependencyProperty DisplayModeProperty =
        DependencyProperty.Register("DisplayMode", typeof(int), typeofThis, new PropertyMetadata(0));

    public string Text
    {
        get { return (string)GetValue(TextProperty); }
        set { SetValue(TextProperty, value); }
    }
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeofThis, new PropertyMetadata(string.Empty));

    public Brush TextColor
    {
        get { return (Brush)GetValue(TextColorProperty); }
        set { SetValue(TextColorProperty, value); }
    }
    public static readonly DependencyProperty TextColorProperty =
        DependencyProperty.Register("TextColor", typeof(Brush), typeofThis, new PropertyMetadata(Brushes.Black));

    // Custom event for tag click
    internal event Action<Model.CBAdornmentData, bool> TagClicked;
    internal Model.CBAdornmentData? AdornmentData { get; set; }

    private DispatcherTimer buttonSingleClickTimeout;
    private bool buttonModifiersPressed;

    private void ButtonSingleClick(object sender, EventArgs e)
    {
        buttonSingleClickTimeout?.Stop();
        ButtonClicked(1);
    }

    protected override void OnClick()
    {
        buttonModifiersPressed = CheckModifiers();
        buttonSingleClickTimeout ??= new DispatcherTimer(
                TimeSpan.FromSeconds(0.25),
                DispatcherPriority.Background,
                ButtonSingleClick,
                Dispatcher.CurrentDispatcher);
        buttonSingleClickTimeout.Start();
    }


    protected override void OnPreviewMouseDoubleClick(MouseButtonEventArgs e)
    {
        buttonSingleClickTimeout?.Stop();
        e.Handled = true;
        ButtonClicked(2);
    }

    private void ButtonClicked(int clickCount)
    {
        if (!AdornmentData.HasValue)
            return;

        int neededClickCount = CBETagPackage.CBEClickMode switch
        {
            (int)Model.ClickMode.SingleClick or (int)Model.ClickMode.CtrlClick => 1,
            (int)Model.ClickMode.DoubleClick => 2,
            _ => 0,
        };
        bool jumpToHead = (clickCount >= neededClickCount) && buttonModifiersPressed;
        TagClicked?.Invoke(AdornmentData.Value, jumpToHead);
    }

    private bool CheckModifiers() =>
        CBETagPackage.CBEClickMode != (int)Model.ClickMode.CtrlClick
            || Keyboard.IsKeyDown(Key.LeftCtrl)
            || Keyboard.IsKeyDown(Key.RightCtrl);
}
