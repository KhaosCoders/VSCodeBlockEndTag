﻿using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Size = System.Windows.Size;

namespace CodeBlockEndTag
{
    /// <summary>
    /// Interaction logic for CBETagControl.xaml
    /// </summary>
    public partial class CBETagControl : UserControl
    {
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(CBETagControl), new PropertyMetadata("Unknown"));

        public object TextColor
        {
            get => (object)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }
        public static readonly DependencyProperty TextColorProperty = DependencyProperty.Register(
            "TextColor", typeof(object), typeof(CBETagControl), new PropertyMetadata(Colors.Black));

        public ImageMoniker IconMoniker
        {
            get => (ImageMoniker)GetValue(IconMonikerProperty);
            set => SetValue(IconMonikerProperty, value);
        }
        public static readonly DependencyProperty IconMonikerProperty =
            DependencyProperty.Register("IconMoniker", typeof(ImageMoniker), typeof(CBETagControl), new PropertyMetadata(KnownMonikers.QuestionMark));

        public int DisplayMode
        {
            get => (int)GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }
        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register("DisplayMode", typeof(int), typeof(CBETagControl), new PropertyMetadata(0));

        public double LineHeight
        {
            get => (double)GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
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

            TextBlock tb = btnTag.Template.FindName("txtTag", btnTag) as TextBlock;
            return new Size(8 + LineHeight + (tb?.ActualWidth ?? 0) + paddingRight, LineHeight);
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
            int neededClickCount = CBETagPackage.CBEClickMode switch
            {
                (int)CBEOptionPage.ClickMode.SingleClick or (int)CBEOptionPage.ClickMode.CtrlClick => 1,
                (int)CBEOptionPage.ClickMode.DoubleClick => 2,
                _ => 0,
            };
            if (AdornmentData != null)
            {
                bool jumpToHead = (clickCount >= neededClickCount) && buttonModifiersPressed;
                TagClicked?.Invoke(AdornmentData, jumpToHead);
            }
        }

        private bool CheckModifiers()
        {
            return CBETagPackage.CBEClickMode != (int)CBEOptionPage.ClickMode.CtrlClick
                || Keyboard.IsKeyDown(Key.LeftCtrl)
                || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        private void TxtTag_OnInitialized(object sender, EventArgs e)
        {
            this.InvalidateMeasure();

            TextBlock tb = btnTag.Template.FindName("txtTag", btnTag) as TextBlock;
        }
    }
}
