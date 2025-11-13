using CodeBlockEndTag.Model;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace CodeBlockEndTag.OptionPage;

public partial class CBEOptionPageControl : UserControl
{
    private const string GitHubUrl = "https://github.com/KhaosCoders/VSCodeBlockEndTag";
    private const string ProStoreUrl = "https://khaoscoders.onfastspring.com/cbe-1y";

    public CBEOptionPageControl()
    {
        InitializeComponent();
    }

    internal CBEOptionPage optionsPage;

    public void Initialize()
    {
        // Track options page opened
        try
        {
            bool hasLicense = Services.LicenseService.HasValidProLicense();
            Telemetry.TelemetryEvents.TrackOptionsPageOpened(hasLicense);
        }
        catch
        {
            // Telemetry should never break functionality
        }

        LoadOptionsToUI();
        LoadSupportedLanguagesToUI();
        LoadLicenseStatusToUI();
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
        chkTelemetryEnabled.Checked = optionsPage.TelemetryEnabled;
    }

    private void LoadSupportedLanguagesToUI()
    {
        lviLanguages.Items.Clear();
        bool hasProLicense = Services.LicenseService.HasValidProLicense();

        string[] langs = optionsPage.SupportedLangDisplayNames;
        var supportedLangs = optionsPage.GetSupportedLanguages();

        for (int i = 0; i < langs.Length; i++)
        {
            bool isCSharp = supportedLangs[i].Name.Equals(Languages.CSharp, StringComparison.OrdinalIgnoreCase);

            string displayName = langs[i];
            if (!isCSharp && !hasProLicense)
            {
                displayName += " 🔒 [PRO]"; // Add lock indicator
            }

            lviLanguages.Items.Add(displayName);
            if (optionsPage.SupportedLangActive[i])
            {
                lviLanguages.SetItemChecked(i, true);
            }
        }
    }

    private void LoadLicenseStatusToUI()
    {
        bool hasLicense = Services.LicenseService.HasValidProLicense();

        if (hasLicense)
        {
            var expDate = Services.LicenseService.GetLicenseExpirationDate();
            lblLicenseStatus.Text = $"✓ PRO License Active (Expires: {expDate?.ToShortDateString() ?? "unknown"})";
            lblLicenseStatus.ForeColor = System.Drawing.Color.Green;
            lblProInfo.Text = "All features unlocked!";
        }
        else
        {
            lblLicenseStatus.Text = "No PRO License - C# only";
            lblLicenseStatus.ForeColor = System.Drawing.Color.Gray;
            lblProInfo.Text = "Unlock all features with PRO!";
        }
    }

    /// <summary>
    /// Enables all supported languages (called after license activation)
    /// </summary>
    private void EnableAllLanguages()
    {
        var supportedLangs = optionsPage.GetSupportedLanguages();
        for (int i = 0; i < supportedLangs.Length; i++)
        {
            optionsPage.SetSupportedLangActive(i, true);
        }

        // Update the ListView to reflect the changes
        for (int i = 0; i < lviLanguages.Items.Count; i++)
        {
            lviLanguages.SetItemChecked(i, true);
        }
    }

    private void LnkGitHub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(GitHubUrl));
    }

    private void LnkBuyPro_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        string email = Services.LicenseService.GetVisualStudioEmail();
        if (string.IsNullOrEmpty(email))
        {
            MessageBox.Show("Couldn't read your Microsoft email address from Visual Studio settings. This is required for license activation after you bought a key. Please open an issue on GitHub.", "Activation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // Track store page opened
        try
        {
            Telemetry.TelemetryEvents.TrackStorePageOpened(!string.IsNullOrEmpty(email));
        }
        catch
        {
            // Telemetry should never break functionality
        }

        Process.Start(new ProcessStartInfo(ProStoreUrl));
    }

    private async void BtnActivateLicense_Click(object sender, EventArgs e)
    {
        string key = txtLicenseKey.Text.Trim();
        if (string.IsNullOrEmpty(key))
        {
            MessageBox.Show("Please enter a license key.", "Activation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnActivateLicense.Enabled = false;
            btnActivateLicense.Text = "Activating...";

            string email = Services.LicenseService.GetVisualStudioEmail();
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Email address is required for license activation.", "Activation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Try re-aquiring before activated token
            string jwt = await Services.LicenseService.RequireActivatedTokenAsync(key, email);

            if (string.IsNullOrWhiteSpace(jwt))
            {
                // Ask user to confirm binding the license to their email
                DialogResult result = MessageBox.Show(
                $"This will bind the license key to your email address:\n\n{email}\n\nDo you want to continue?",
                "Confirm License Activation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    return;
                }
                jwt = await Services.LicenseService.ActivateLicenseAsync(key, email);
            }

            optionsPage.LicenseToken = jwt;

            // Enable all languages after successful activation
            EnableAllLanguages();

            optionsPage.SaveSettingsToStorage();

            MessageBox.Show("License activated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            LoadLicenseStatusToUI();
            LoadSupportedLanguagesToUI(); // Refresh language list to remove locks
            txtLicenseKey.Text = ""; // Clear the input
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Activation failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnActivateLicense.Enabled = true;
            btnActivateLicense.Text = "Activate";
        }
    }

    private void TxtLicenseKey_TextChanged(object sender, EventArgs e)
    {
        // Enable activate button only if key is entered
        btnActivateLicense.Enabled = !string.IsNullOrWhiteSpace(txtLicenseKey.Text);
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
        var supportedLangs = optionsPage.GetSupportedLanguages();
        string langName = supportedLangs[e.Index].Name;
        bool isCSharp = langName.Equals(Languages.CSharp, StringComparison.OrdinalIgnoreCase);

        // Prevent enabling non-C# languages without PRO license
        if (!isCSharp && !Services.LicenseService.HasValidProLicense() && e.NewValue == CheckState.Checked)
        {
            e.NewValue = CheckState.Unchecked;
            MessageBox.Show(
              "This language requires a PRO license.\n\nC# is free to use, but all other languages require a PRO license.\n\nClick 'Buy Pro' to unlock all languages!",
                       "PRO Feature Required",
               MessageBoxButtons.OK,
           MessageBoxIcon.Information);
            return;
        }

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

    private void ChkTelemetryEnabled_CheckedChanged(object sender, EventArgs e)
    {
        optionsPage.TelemetryEnabled = chkTelemetryEnabled.Checked;
    }
}
