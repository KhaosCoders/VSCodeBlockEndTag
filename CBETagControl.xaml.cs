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



        internal CBAdornmentData AdornmentData { get; set; }

        internal delegate void TagClickHandler(CBAdornmentData adornment, bool jumpToHead);
        internal event TagClickHandler TagClicked;

        public CBETagControl()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            int clickCount = 0;
            Key modifier = Key.None;
            Key altModifier = Key.None;

            switch (CBETagPackage.CBEClickMode)
            {
                case (int)CBEOptionPage.ClickMode.SingleClick:
                    clickCount = 1;
                    break;
                case (int)CBEOptionPage.ClickMode.CtrlClick:
                    clickCount = 1;
                    modifier = Key.LeftCtrl;
                    altModifier = Key.RightCtrl;
                    break;
                case (int)CBEOptionPage.ClickMode.DoubleClick:
                    clickCount = 2;
                    break;
            }

            if (AdornmentData != null && e.ClickCount >= clickCount)
            {
                bool jumpToHead = (e.LeftButton == MouseButtonState.Pressed &&
                    (modifier == Key.None || Keyboard.IsKeyDown(modifier) || (altModifier != Key.None && Keyboard.IsKeyDown(altModifier))));
                TagClicked?.Invoke(AdornmentData, jumpToHead);
            }
        }
    }
}
