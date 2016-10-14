using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CodeBlockEndTag
{

    [Guid("B009CDB7-6900-47DC-8403-285191252811")]
    public class CBEOptionPage : DialogPage
    {
        public delegate void OptionChangedHandler(object sender);
        public event OptionChangedHandler OptionChanged;

        /// <summary>
        /// Gets or sets the supported VS content types for the extension
        /// </summary>
        public string CBEContentTypes
        {
            get { return cbeContentTypes; }
            set
            {
                if (cbeContentTypes != value)
                {
                    cbeContentTypes = value;
                }
            }
        }
        private string cbeContentTypes = "CSharp";


        /// <summary>
        /// Gets or set the option: Enable CodeBlock End Tagger
        /// </summary>
        public bool CBETaggerEnabled
        {
            get { return cbeTaggerEnabled; }
            set
            {
                if (cbeTaggerEnabled != value)
                {
                    cbeTaggerEnabled = value;
                    OptionChanged?.Invoke(this);
                }
            }
        }
        private bool cbeTaggerEnabled = true;

        /// <summary>
        /// Gets or sets the option: Display Mode (Icon&Text / Icon / Text)
        /// </summary>
        public int CBEDisplayMode
        {
            get { return cbeDisplayMode; }
            set
            {
                if (cbeDisplayMode != value)
                {
                    cbeDisplayMode = value;
                    OptionChanged?.Invoke(this);
                }
            }
        }
        private int cbeDisplayMode = (int)DisplayModes.IconAndText;

        public enum DisplayModes : int
        {
            Text = 1,
            Icon = 2,
            IconAndText = 3
        }

        /// <summary>
        /// Gets or sets the option: Navigate on (Single-Click / Double-Click)
        /// </summary>
        public int CBEClickCount
        {
            get { return cbeClickCount; }
            set
            {
                if (cbeClickCount != value)
                {
                    cbeClickCount = value;
                    OptionChanged?.Invoke(this);
                }
            }
        }
        private int cbeClickCount = 1;

        /// <summary>
        /// Gets or sets the option: Show Tags when (Always / Header not visible)
        /// </summary>
        public int CBEVisibilityMode
        {
            get { return cbeVisibilityMode; }
            set
            {
                if (cbeVisibilityMode != value)
                {
                    cbeVisibilityMode = value;
                    OptionChanged?.Invoke(this);
                }
            }
        }
        private int cbeVisibilityMode = (int)VisibilityModes.HeaderNotVisible;

        public enum VisibilityModes : int
        {
            Always = 1,
            HeaderNotVisible = 2
        }


        protected override IWin32Window Window
        {
            get
            {
                CBEOptionPageControl page = new CBEOptionPageControl();
                page.optionsPage = this;
                page.Initialize();
                return page;
            }
        }


    }
}
