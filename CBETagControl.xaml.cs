using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CodeBlockEndTag
{
    /// <summary>
    /// Interaction logic for CBETagControl.xaml
    /// </summary>
    public partial class CBETagControl : UserControl
    {

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(CBETagControl), new PropertyMetadata("Unkown"));


        public ImageMoniker IconMoniker
        {
            get { return (ImageMoniker)GetValue(IconMonikerProperty); }
            set { SetValue(IconMonikerProperty, value); }
        }
        public static readonly DependencyProperty IconMonikerProperty =
            DependencyProperty.Register("IconMoniker", typeof(ImageMoniker), typeof(CBETagControl), new PropertyMetadata(KnownMonikers.QuestionMark));


        public int DisplayMode
        {
            get { return (int)GetValue(DisplayModeProperty); }
            set { SetValue(DisplayModeProperty, value); }
        }
        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register("DisplayMode", typeof(int), typeof(CBETagControl), new PropertyMetadata(0));


        public double LineHeight
        {
            get { return (double)GetValue(LineHeightProperty); }
            set { SetValue(LineHeightProperty, value); }
        }
        public static readonly DependencyProperty LineHeightProperty =
            DependencyProperty.Register("LineHeight", typeof(double), typeof(CBETagControl), new PropertyMetadata(9d));


        internal CBAdornmentData AdornmentData { get; set; }

        internal delegate void TagClickHandler(CBAdornmentData adornment, bool jumpToHead);
        internal event TagClickHandler TagClicked;

        public CBETagControl()
        {
            DataContext = this;
            InitializeComponent();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var paddingRight = LineHeight / 2;
            if (DisplayMode == 2)
            {
                // No label
                return new Size(LineHeight + 4 + paddingRight, LineHeight);
            }
            else
            {
                TextBlock tb = btnTag.Template.FindName("txtTag", btnTag) as TextBlock;
                Size size = new Size(8 + LineHeight + (tb?.ActualWidth ?? 0) + paddingRight, LineHeight);
                return size;
            }
        }

        private DispatcherTimer buttonSingleClickTimeout;
        private bool buttonModifiersPressed;

        private void ButtonSingleClick(object sender, EventArgs e)
        {
            buttonSingleClickTimeout.Stop();
            ButtonClicked(1);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            buttonModifiersPressed = CheckModifiers();
            if (buttonSingleClickTimeout == null)
            {
                buttonSingleClickTimeout =
                new DispatcherTimer(
                    TimeSpan.FromSeconds(0.25),
                    DispatcherPriority.Background,
                    ButtonSingleClick,
                    Dispatcher.CurrentDispatcher);
            }

            buttonSingleClickTimeout.Start();
        }

        private void Button_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            buttonSingleClickTimeout.Stop();
            e.Handled = true;
            ButtonClicked(2);
        }

        private void ButtonClicked(int clickCount)
        {
            int neededClickCount = 0;
            switch (CBETagPackage.CBEClickMode)
            {
                case (int)CBEOptionPage.ClickMode.SingleClick:
                    neededClickCount = 1;
                    break;
                case (int)CBEOptionPage.ClickMode.CtrlClick:
                    neededClickCount = 1;
                    break;
                case (int)CBEOptionPage.ClickMode.DoubleClick:
                    neededClickCount = 2;
                    break;
            }

            if (AdornmentData != null)
            {
                bool jumpToHead = (clickCount >= neededClickCount) && buttonModifiersPressed;
                TagClicked?.Invoke(AdornmentData, jumpToHead);
            }
        }

        private bool CheckModifiers()
        {
            Key modifier = Key.None;
            Key altModifier = Key.None;
            switch (CBETagPackage.CBEClickMode)
            {
                case (int)CBEOptionPage.ClickMode.CtrlClick:
                    modifier = Key.LeftCtrl;
                    altModifier = Key.RightCtrl;
                    break;
            }
            return (modifier == Key.None || Keyboard.IsKeyDown(modifier) || (altModifier != Key.None && Keyboard.IsKeyDown(altModifier)));
        }

    }
}
