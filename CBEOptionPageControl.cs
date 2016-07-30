using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace CodeBlockEndTag
{
    public partial class CBEOptionPageControl : UserControl
    {
        private const string DonateUrl = @"https://www.paypal.com/us/cgi-bin/webscr?cmd=_flow&SESSION=zy33NAY9x6TPiFGk26vXckTW9Nf1ffD_E4RDdAq3kXHpzeFaSsPIkFbZv9y&dispatch=5885d80a13c0db1f8e263663d3faee8d64813b57e559a2578463e58274899069";


        public CBEOptionPageControl()
        {
            InitializeComponent();
        }


        internal CBEOptionPage optionsPage;

        public void Initialize()
        {
            chkCBETaggerEnabled.Checked = optionsPage.CBETaggerEnabled;
            rdbAlways.Checked = (optionsPage.CBEVisibilityMode == (int)CBEOptionPage.VisibilityModes.Always);
            rdbHeaderInvisible.Checked = (optionsPage.CBEVisibilityMode == (int)CBEOptionPage.VisibilityModes.HeaderNotVisible);

            rdbSingleClick.Checked = (optionsPage.CBEClickCount == 1);
            rdbDoubleClick.Checked = (optionsPage.CBEClickCount == 2);

            rdbIconAndText.Checked = (optionsPage.CBEDisplayMode == (int)CBEOptionPage.DisplayModes.IconAndText);
            rdbIconOnly.Checked = (optionsPage.CBEDisplayMode == (int)CBEOptionPage.DisplayModes.Icon);
            rdbTextOnly.Checked = (optionsPage.CBEDisplayMode == (int)CBEOptionPage.DisplayModes.Text);
        }

        private void lblLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ProcessStartInfo sInfo = new ProcessStartInfo(DonateUrl);
            Process.Start(sInfo);
        }

        private void chkCBETaggerEnabled_CheckedChanged(object sender, EventArgs e)
        {
            optionsPage.CBETaggerEnabled = chkCBETaggerEnabled.Checked;
        }

        private void rdbAlways_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbAlways.Checked)
                optionsPage.CBEVisibilityMode = (int)CBEOptionPage.VisibilityModes.Always;
        }

        private void rdbHeaderInvisible_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbHeaderInvisible.Checked)
                optionsPage.CBEVisibilityMode = (int)CBEOptionPage.VisibilityModes.HeaderNotVisible;
        }

        private void rdbSingleClick_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbSingleClick.Checked)
                optionsPage.CBEClickCount = 1;
        }

        private void rdbDoubleClick_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbDoubleClick.Checked)
                optionsPage.CBEClickCount = 2;
        }

        private void rdbIconAndText_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbIconAndText.Checked)
                optionsPage.CBEDisplayMode = (int)CBEOptionPage.DisplayModes.IconAndText;
        }

        private void rdbIconOnly_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbIconOnly.Checked)
                optionsPage.CBEDisplayMode = (int)CBEOptionPage.DisplayModes.Icon;
        }

        private void rdbTextOnly_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbTextOnly.Checked)
                optionsPage.CBEDisplayMode = (int)CBEOptionPage.DisplayModes.Text;
        }
        
    }
}
