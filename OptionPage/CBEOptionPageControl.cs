using System;
using System.Diagnostics;
using System.Windows.Forms;
using CodeBlockEndTag.Model;
using Microsoft.Internal.VisualStudio.Shell.Interop;

namespace CodeBlockEndTag.OptionPage;

public partial class CBEOptionPageControl : UserControl
{
    private const string DonateUrl = "https://www.paypal.com/donate?hosted_button_id=37PBGZPHXY8EC";
    private const string GitHubUrl = "https://github.com/KhaosCoders/VSCodeBlockEndTag";

    public CBEOptionPageControl()
    {
        InitializeComponent();
    }

    internal CBEOptionPage optionsPage;

    public void Initialize()
    {
        LoadOptionsToUI();
        LoadSupportedLanguagesToUI();
    }

    private void LoadOptionsToUI()
    {
        chkCBETaggerEnabled.Checked = optionsPage.CBETaggerEnabled;
        rdbAlways.Checked = optionsPage.CBEVisibilityMode == (int)VisibilityModes.Always;
        rdbHeaderInvisible.Checked = optionsPage.CBEVisibilityMode == (int)VisibilityModes.HeaderNotVisible;

        rdbSingleClick.Checked = optionsPage.CBEClickMode == (int)ClickMode.SingleClick;
        rdbDoubleClick.Checked = optionsPage.CBEClickMode == (int)ClickMode.DoubleClick;
        rdbCtrlClick.Checked = optionsPage.CBEClickMode == (int)ClickMode.CtrlClick;

        rdbIconAndText.Checked = optionsPage.CBEDisplayMode == (int)DisplayModes.IconAndText;
        rdbIconOnly.Checked = optionsPage.CBEDisplayMode == (int)DisplayModes.Icon;
        rdbTextOnly.Checked = optionsPage.CBEDisplayMode == (int)DisplayModes.Text;

        txtMargin.Text = optionsPage.CBEMargin.ToString();
    }

    private void LoadSupportedLanguagesToUI()
    {
        lviLanguages.Items.Clear();
        string[] langs = optionsPage.SupportedLangDisplayNames;
        for (int i = 0; i < langs.Length; i++)
        {
            lviLanguages.Items.Add(langs[i]);
            if (optionsPage.SupportedLangActive[i])
            {
                lviLanguages.SetItemChecked(i, true);
            }
        }
    }

    private void LblLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(DonateUrl));
    }

    private void LnkGitHub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(GitHubUrl));
    }

    private void ChkCBETaggerEnabled_CheckedChanged(object sender, EventArgs e)
    {
        optionsPage.CBETaggerEnabled = chkCBETaggerEnabled.Checked;
    }

    private void RdbAlways_CheckedChanged(object sender, EventArgs e)
    {
        if (rdbAlways.Checked)
        {
            optionsPage.CBEVisibilityMode = (int)VisibilityModes.Always;
        }
    }

    private void RdbHeaderInvisible_CheckedChanged(object sender, EventArgs e)
    {
        if (rdbHeaderInvisible.Checked)
        {
            optionsPage.CBEVisibilityMode = (int)VisibilityModes.HeaderNotVisible;
        }
    }

    private void RdbSingleClick_CheckedChanged(object sender, EventArgs e)
    {
        if (rdbSingleClick.Checked)
        {
            optionsPage.CBEClickMode = (int)ClickMode.SingleClick;
        }
    }

    private void RdbDoubleClick_CheckedChanged(object sender, EventArgs e)
    {
        if (rdbDoubleClick.Checked)
        {
            optionsPage.CBEClickMode = (int)ClickMode.DoubleClick;
        }
    }

    private void RdbCtrlClick_CheckedChanged(object sender, EventArgs e)
    {
        if (rdbCtrlClick.Checked)
        {
            optionsPage.CBEClickMode = (int)ClickMode.CtrlClick;
        }
    }

    private void RdbIconAndText_CheckedChanged(object sender, EventArgs e)
    {
        if (rdbIconAndText.Checked)
        {
            optionsPage.CBEDisplayMode = (int)DisplayModes.IconAndText;
        }
    }

    private void RdbIconOnly_CheckedChanged(object sender, EventArgs e)
    {
        if (rdbIconOnly.Checked)
        {
            optionsPage.CBEDisplayMode = (int)DisplayModes.Icon;
        }
    }

    private void RdbTextOnly_CheckedChanged(object sender, EventArgs e)
    {
        if (rdbTextOnly.Checked)
        {
            optionsPage.CBEDisplayMode = (int)DisplayModes.Text;
        }
    }

    private void LviLanguages_ItemCheck(object sender, ItemCheckEventArgs e)
    {
        optionsPage.SetSupportedLangActive(e.Index, e.NewValue == CheckState.Checked);
    }

    private void lblFont_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        CBETagPackage.Instance.ShowOptionPage(typeof(FontAndColorsOptionPageDummy));
    }

    private void txtMargin_TextChanged(object sender, EventArgs e)
    {
        if (txtMargin.Text.Length > 0 && int.TryParse(txtMargin.Text, out int margin))
        {
            optionsPage.CBEMargin = margin;
        }
        else
        {
            optionsPage.CBEMargin = 0;
        }
    }
}
