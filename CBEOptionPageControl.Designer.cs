namespace CodeBlockEndTag
{
    partial class CBEOptionPageControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.chkCBETaggerEnabled = new System.Windows.Forms.CheckBox();
            this.cntVisibilityMode = new System.Windows.Forms.GroupBox();
            this.rdbHeaderInvisible = new System.Windows.Forms.RadioButton();
            this.rdbAlways = new System.Windows.Forms.RadioButton();
            this.cntNavigateMode = new System.Windows.Forms.GroupBox();
            this.rdbDoubleClick = new System.Windows.Forms.RadioButton();
            this.rdbSingleClick = new System.Windows.Forms.RadioButton();
            this.cntDisplayMode = new System.Windows.Forms.GroupBox();
            this.rdbTextOnly = new System.Windows.Forms.RadioButton();
            this.rdbIconOnly = new System.Windows.Forms.RadioButton();
            this.rdbIconAndText = new System.Windows.Forms.RadioButton();
            this.cntInfo = new System.Windows.Forms.GroupBox();
            this.lblLink = new System.Windows.Forms.LinkLabel();
            this.lblInfo = new System.Windows.Forms.Label();
            this.cntLanguages = new System.Windows.Forms.GroupBox();
            this.lviLanguages = new System.Windows.Forms.CheckedListBox();
            this.lnkGitHub = new System.Windows.Forms.LinkLabel();
            this.lblSuggestMore = new System.Windows.Forms.Label();
            this.cntVisibilityMode.SuspendLayout();
            this.cntNavigateMode.SuspendLayout();
            this.cntDisplayMode.SuspendLayout();
            this.cntInfo.SuspendLayout();
            this.cntLanguages.SuspendLayout();
            this.SuspendLayout();
            // 
            // chkCBETaggerEnabled
            // 
            this.chkCBETaggerEnabled.AutoSize = true;
            this.chkCBETaggerEnabled.Location = new System.Drawing.Point(6, 4);
            this.chkCBETaggerEnabled.Name = "chkCBETaggerEnabled";
            this.chkCBETaggerEnabled.Size = new System.Drawing.Size(172, 17);
            this.chkCBETaggerEnabled.TabIndex = 1;
            this.chkCBETaggerEnabled.Text = "enable CodeBlock End Tagger";
            this.chkCBETaggerEnabled.UseVisualStyleBackColor = true;
            this.chkCBETaggerEnabled.CheckedChanged += new System.EventHandler(this.chkCBETaggerEnabled_CheckedChanged);
            // 
            // cntVisibilityMode
            // 
            this.cntVisibilityMode.Controls.Add(this.rdbHeaderInvisible);
            this.cntVisibilityMode.Controls.Add(this.rdbAlways);
            this.cntVisibilityMode.Location = new System.Drawing.Point(24, 27);
            this.cntVisibilityMode.Name = "cntVisibilityMode";
            this.cntVisibilityMode.Size = new System.Drawing.Size(200, 64);
            this.cntVisibilityMode.TabIndex = 7;
            this.cntVisibilityMode.TabStop = false;
            this.cntVisibilityMode.Text = "Tags visible";
            // 
            // rdbHeaderInvisible
            // 
            this.rdbHeaderInvisible.AutoSize = true;
            this.rdbHeaderInvisible.Location = new System.Drawing.Point(16, 42);
            this.rdbHeaderInvisible.Name = "rdbHeaderInvisible";
            this.rdbHeaderInvisible.Size = new System.Drawing.Size(140, 17);
            this.rdbHeaderInvisible.TabIndex = 9;
            this.rdbHeaderInvisible.TabStop = true;
            this.rdbHeaderInvisible.Text = "When header not visible";
            this.rdbHeaderInvisible.UseVisualStyleBackColor = true;
            this.rdbHeaderInvisible.CheckedChanged += new System.EventHandler(this.rdbHeaderInvisible_CheckedChanged);
            // 
            // rdbAlways
            // 
            this.rdbAlways.AutoSize = true;
            this.rdbAlways.Location = new System.Drawing.Point(16, 19);
            this.rdbAlways.Name = "rdbAlways";
            this.rdbAlways.Size = new System.Drawing.Size(58, 17);
            this.rdbAlways.TabIndex = 8;
            this.rdbAlways.TabStop = true;
            this.rdbAlways.Text = "Always";
            this.rdbAlways.UseVisualStyleBackColor = true;
            this.rdbAlways.CheckedChanged += new System.EventHandler(this.rdbAlways_CheckedChanged);
            // 
            // cntNavigateMode
            // 
            this.cntNavigateMode.Controls.Add(this.rdbDoubleClick);
            this.cntNavigateMode.Controls.Add(this.rdbSingleClick);
            this.cntNavigateMode.Location = new System.Drawing.Point(24, 97);
            this.cntNavigateMode.Name = "cntNavigateMode";
            this.cntNavigateMode.Size = new System.Drawing.Size(200, 64);
            this.cntNavigateMode.TabIndex = 8;
            this.cntNavigateMode.TabStop = false;
            this.cntNavigateMode.Text = "Navigate on";
            // 
            // rdbDoubleClick
            // 
            this.rdbDoubleClick.AutoSize = true;
            this.rdbDoubleClick.Location = new System.Drawing.Point(16, 42);
            this.rdbDoubleClick.Name = "rdbDoubleClick";
            this.rdbDoubleClick.Size = new System.Drawing.Size(85, 17);
            this.rdbDoubleClick.TabIndex = 7;
            this.rdbDoubleClick.TabStop = true;
            this.rdbDoubleClick.Text = "Double-Click";
            this.rdbDoubleClick.UseVisualStyleBackColor = true;
            this.rdbDoubleClick.CheckedChanged += new System.EventHandler(this.rdbDoubleClick_CheckedChanged);
            // 
            // rdbSingleClick
            // 
            this.rdbSingleClick.AutoSize = true;
            this.rdbSingleClick.Location = new System.Drawing.Point(16, 19);
            this.rdbSingleClick.Name = "rdbSingleClick";
            this.rdbSingleClick.Size = new System.Drawing.Size(80, 17);
            this.rdbSingleClick.TabIndex = 6;
            this.rdbSingleClick.TabStop = true;
            this.rdbSingleClick.Text = "Single-Click";
            this.rdbSingleClick.UseVisualStyleBackColor = true;
            this.rdbSingleClick.CheckedChanged += new System.EventHandler(this.rdbSingleClick_CheckedChanged);
            // 
            // cntDisplayMode
            // 
            this.cntDisplayMode.Controls.Add(this.rdbTextOnly);
            this.cntDisplayMode.Controls.Add(this.rdbIconOnly);
            this.cntDisplayMode.Controls.Add(this.rdbIconAndText);
            this.cntDisplayMode.Location = new System.Drawing.Point(24, 167);
            this.cntDisplayMode.Name = "cntDisplayMode";
            this.cntDisplayMode.Size = new System.Drawing.Size(200, 88);
            this.cntDisplayMode.TabIndex = 9;
            this.cntDisplayMode.TabStop = false;
            this.cntDisplayMode.Text = "Display tag as";
            // 
            // rdbTextOnly
            // 
            this.rdbTextOnly.AutoSize = true;
            this.rdbTextOnly.Location = new System.Drawing.Point(16, 65);
            this.rdbTextOnly.Name = "rdbTextOnly";
            this.rdbTextOnly.Size = new System.Drawing.Size(68, 17);
            this.rdbTextOnly.TabIndex = 9;
            this.rdbTextOnly.TabStop = true;
            this.rdbTextOnly.Text = "Text only";
            this.rdbTextOnly.UseVisualStyleBackColor = true;
            this.rdbTextOnly.CheckedChanged += new System.EventHandler(this.rdbTextOnly_CheckedChanged);
            // 
            // rdbIconOnly
            // 
            this.rdbIconOnly.AutoSize = true;
            this.rdbIconOnly.Location = new System.Drawing.Point(16, 42);
            this.rdbIconOnly.Name = "rdbIconOnly";
            this.rdbIconOnly.Size = new System.Drawing.Size(68, 17);
            this.rdbIconOnly.TabIndex = 8;
            this.rdbIconOnly.TabStop = true;
            this.rdbIconOnly.Text = "Icon only";
            this.rdbIconOnly.UseVisualStyleBackColor = true;
            this.rdbIconOnly.CheckedChanged += new System.EventHandler(this.rdbIconOnly_CheckedChanged);
            // 
            // rdbIconAndText
            // 
            this.rdbIconAndText.AutoSize = true;
            this.rdbIconAndText.Location = new System.Drawing.Point(16, 19);
            this.rdbIconAndText.Name = "rdbIconAndText";
            this.rdbIconAndText.Size = new System.Drawing.Size(91, 17);
            this.rdbIconAndText.TabIndex = 7;
            this.rdbIconAndText.TabStop = true;
            this.rdbIconAndText.Text = "Icon and Text";
            this.rdbIconAndText.UseVisualStyleBackColor = true;
            this.rdbIconAndText.CheckedChanged += new System.EventHandler(this.rdbIconAndText_CheckedChanged);
            // 
            // cntInfo
            // 
            this.cntInfo.Controls.Add(this.lblLink);
            this.cntInfo.Controls.Add(this.lblInfo);
            this.cntInfo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cntInfo.Location = new System.Drawing.Point(0, 386);
            this.cntInfo.Name = "cntInfo";
            this.cntInfo.Size = new System.Drawing.Size(440, 54);
            this.cntInfo.TabIndex = 10;
            this.cntInfo.TabStop = false;
            this.cntInfo.Text = "Info";
            // 
            // lblLink
            // 
            this.lblLink.AutoSize = true;
            this.lblLink.LinkBehavior = System.Windows.Forms.LinkBehavior.AlwaysUnderline;
            this.lblLink.Location = new System.Drawing.Point(10, 34);
            this.lblLink.Name = "lblLink";
            this.lblLink.Size = new System.Drawing.Size(86, 13);
            this.lblLink.TabIndex = 1;
            this.lblLink.TabStop = true;
            this.lblLink.Text = "PayPal Donation";
            this.lblLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblLink_LinkClicked);
            // 
            // lblInfo
            // 
            this.lblInfo.AutoSize = true;
            this.lblInfo.Location = new System.Drawing.Point(7, 17);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(350, 13);
            this.lblInfo.TabIndex = 0;
            this.lblInfo.Text = "This extension is 100% free to use. But you might buy me a drink or two ;)";
            // 
            // cntLanguages
            // 
            this.cntLanguages.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.cntLanguages.Controls.Add(this.lviLanguages);
            this.cntLanguages.Controls.Add(this.lnkGitHub);
            this.cntLanguages.Controls.Add(this.lblSuggestMore);
            this.cntLanguages.Location = new System.Drawing.Point(237, 27);
            this.cntLanguages.Name = "cntLanguages";
            this.cntLanguages.Size = new System.Drawing.Size(188, 353);
            this.cntLanguages.TabIndex = 10;
            this.cntLanguages.TabStop = false;
            this.cntLanguages.Text = "Enable for code type";
            // 
            // lviLanguages
            // 
            this.lviLanguages.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lviLanguages.FormattingEnabled = true;
            this.lviLanguages.Items.AddRange(new object[] {
            "Dummy"});
            this.lviLanguages.Location = new System.Drawing.Point(6, 19);
            this.lviLanguages.Name = "lviLanguages";
            this.lviLanguages.Size = new System.Drawing.Size(176, 289);
            this.lviLanguages.TabIndex = 11;
            this.lviLanguages.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.lviLanguages_ItemCheck);
            // 
            // lnkGitHub
            // 
            this.lnkGitHub.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lnkGitHub.AutoSize = true;
            this.lnkGitHub.LinkBehavior = System.Windows.Forms.LinkBehavior.AlwaysUnderline;
            this.lnkGitHub.Location = new System.Drawing.Point(6, 334);
            this.lnkGitHub.Name = "lnkGitHub";
            this.lnkGitHub.Size = new System.Drawing.Size(77, 13);
            this.lnkGitHub.TabIndex = 12;
            this.lnkGitHub.TabStop = true;
            this.lnkGitHub.Text = "Visit on GitHub";
            this.lnkGitHub.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkGitHub_LinkClicked);
            // 
            // lblSuggestMore
            // 
            this.lblSuggestMore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblSuggestMore.AutoSize = true;
            this.lblSuggestMore.Location = new System.Drawing.Point(4, 315);
            this.lblSuggestMore.Name = "lblSuggestMore";
            this.lblSuggestMore.Size = new System.Drawing.Size(126, 13);
            this.lblSuggestMore.TabIndex = 11;
            this.lblSuggestMore.Text = "Feel free to suggest more";
            // 
            // CBEOptionPageControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cntLanguages);
            this.Controls.Add(this.cntInfo);
            this.Controls.Add(this.cntDisplayMode);
            this.Controls.Add(this.cntNavigateMode);
            this.Controls.Add(this.cntVisibilityMode);
            this.Controls.Add(this.chkCBETaggerEnabled);
            this.Name = "CBEOptionPageControl";
            this.Size = new System.Drawing.Size(440, 440);
            this.cntVisibilityMode.ResumeLayout(false);
            this.cntVisibilityMode.PerformLayout();
            this.cntNavigateMode.ResumeLayout(false);
            this.cntNavigateMode.PerformLayout();
            this.cntDisplayMode.ResumeLayout(false);
            this.cntDisplayMode.PerformLayout();
            this.cntInfo.ResumeLayout(false);
            this.cntInfo.PerformLayout();
            this.cntLanguages.ResumeLayout(false);
            this.cntLanguages.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkCBETaggerEnabled;
        private System.Windows.Forms.GroupBox cntVisibilityMode;
        private System.Windows.Forms.RadioButton rdbHeaderInvisible;
        private System.Windows.Forms.RadioButton rdbAlways;
        private System.Windows.Forms.GroupBox cntNavigateMode;
        private System.Windows.Forms.RadioButton rdbDoubleClick;
        private System.Windows.Forms.RadioButton rdbSingleClick;
        private System.Windows.Forms.GroupBox cntDisplayMode;
        private System.Windows.Forms.RadioButton rdbTextOnly;
        private System.Windows.Forms.RadioButton rdbIconOnly;
        private System.Windows.Forms.RadioButton rdbIconAndText;
        private System.Windows.Forms.GroupBox cntInfo;
        private System.Windows.Forms.LinkLabel lblLink;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.GroupBox cntLanguages;
        private System.Windows.Forms.LinkLabel lnkGitHub;
        private System.Windows.Forms.Label lblSuggestMore;
        private System.Windows.Forms.CheckedListBox lviLanguages;
    }
}
