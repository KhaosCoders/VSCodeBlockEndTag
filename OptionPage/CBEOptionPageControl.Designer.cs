namespace CodeBlockEndTag.OptionPage
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
            this.rdbCtrlClick = new System.Windows.Forms.RadioButton();
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
            this.lblFont = new System.Windows.Forms.LinkLabel();
            this.cntTagPos = new System.Windows.Forms.GroupBox();
            this.txtMargin = new System.Windows.Forms.TextBox();
            this.lblMargin = new System.Windows.Forms.Label();
            this.cntVisibilityMode.SuspendLayout();
            this.cntNavigateMode.SuspendLayout();
            this.cntDisplayMode.SuspendLayout();
            this.cntInfo.SuspendLayout();
            this.cntLanguages.SuspendLayout();
            this.cntTagPos.SuspendLayout();
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
            this.chkCBETaggerEnabled.CheckedChanged += new System.EventHandler(this.ChkCBETaggerEnabled_CheckedChanged);
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
            this.rdbHeaderInvisible.CheckedChanged += new System.EventHandler(this.RdbHeaderInvisible_CheckedChanged);
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
            this.rdbAlways.CheckedChanged += new System.EventHandler(this.RdbAlways_CheckedChanged);
            // 
            // cntNavigateMode
            // 
            this.cntNavigateMode.Controls.Add(this.rdbCtrlClick);
            this.cntNavigateMode.Controls.Add(this.rdbDoubleClick);
            this.cntNavigateMode.Controls.Add(this.rdbSingleClick);
            this.cntNavigateMode.Location = new System.Drawing.Point(24, 97);
            this.cntNavigateMode.Name = "cntNavigateMode";
            this.cntNavigateMode.Size = new System.Drawing.Size(200, 64);
            this.cntNavigateMode.TabIndex = 8;
            this.cntNavigateMode.TabStop = false;
            this.cntNavigateMode.Text = "Navigate on";
            // 
            // rdbCtrlClick
            // 
            this.rdbCtrlClick.AutoSize = true;
            this.rdbCtrlClick.Location = new System.Drawing.Point(109, 19);
            this.rdbCtrlClick.Name = "rdbCtrlClick";
            this.rdbCtrlClick.Size = new System.Drawing.Size(82, 17);
            this.rdbCtrlClick.TabIndex = 8;
            this.rdbCtrlClick.TabStop = true;
            this.rdbCtrlClick.Text = "CTRL+Click";
            this.rdbCtrlClick.UseVisualStyleBackColor = true;
            this.rdbCtrlClick.CheckedChanged += new System.EventHandler(this.RdbCtrlClick_CheckedChanged);
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
            this.rdbDoubleClick.CheckedChanged += new System.EventHandler(this.RdbDoubleClick_CheckedChanged);
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
            this.rdbSingleClick.CheckedChanged += new System.EventHandler(this.RdbSingleClick_CheckedChanged);
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
            this.rdbTextOnly.CheckedChanged += new System.EventHandler(this.RdbTextOnly_CheckedChanged);
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
            this.rdbIconOnly.CheckedChanged += new System.EventHandler(this.RdbIconOnly_CheckedChanged);
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
            this.rdbIconAndText.CheckedChanged += new System.EventHandler(this.RdbIconAndText_CheckedChanged);
            // 
            // cntInfo
            // 
            this.cntInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cntInfo.Controls.Add(this.lblLink);
            this.cntInfo.Controls.Add(this.lblInfo);
            this.cntInfo.Location = new System.Drawing.Point(0, 386);
            this.cntInfo.Name = "cntInfo";
            this.cntInfo.Size = new System.Drawing.Size(430, 54);
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
            this.lblLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LblLink_LinkClicked);
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
            this.cntLanguages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cntLanguages.Controls.Add(this.lviLanguages);
            this.cntLanguages.Controls.Add(this.lnkGitHub);
            this.cntLanguages.Controls.Add(this.lblSuggestMore);
            this.cntLanguages.Location = new System.Drawing.Point(233, 61);
            this.cntLanguages.Name = "cntLanguages";
            this.cntLanguages.Size = new System.Drawing.Size(188, 319);
            this.cntLanguages.TabIndex = 10;
            this.cntLanguages.TabStop = false;
            this.cntLanguages.Text = "Enable for code type";
            // 
            // lviLanguages
            // 
            this.lviLanguages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lviLanguages.FormattingEnabled = true;
            this.lviLanguages.Items.AddRange(new object[] {
            "Dummy"});
            this.lviLanguages.Location = new System.Drawing.Point(6, 19);
            this.lviLanguages.Name = "lviLanguages";
            this.lviLanguages.Size = new System.Drawing.Size(176, 244);
            this.lviLanguages.TabIndex = 11;
            this.lviLanguages.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.LviLanguages_ItemCheck);
            // 
            // lnkGitHub
            // 
            this.lnkGitHub.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lnkGitHub.AutoSize = true;
            this.lnkGitHub.LinkBehavior = System.Windows.Forms.LinkBehavior.AlwaysUnderline;
            this.lnkGitHub.Location = new System.Drawing.Point(6, 300);
            this.lnkGitHub.Name = "lnkGitHub";
            this.lnkGitHub.Size = new System.Drawing.Size(77, 13);
            this.lnkGitHub.TabIndex = 12;
            this.lnkGitHub.TabStop = true;
            this.lnkGitHub.Text = "Visit on GitHub";
            this.lnkGitHub.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkGitHub_LinkClicked);
            // 
            // lblSuggestMore
            // 
            this.lblSuggestMore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblSuggestMore.AutoSize = true;
            this.lblSuggestMore.Location = new System.Drawing.Point(4, 281);
            this.lblSuggestMore.Name = "lblSuggestMore";
            this.lblSuggestMore.Size = new System.Drawing.Size(126, 13);
            this.lblSuggestMore.TabIndex = 11;
            this.lblSuggestMore.Text = "Feel free to suggest more";
            // 
            // lblFont
            // 
            this.lblFont.AutoSize = true;
            this.lblFont.LinkBehavior = System.Windows.Forms.LinkBehavior.AlwaysUnderline;
            this.lblFont.Location = new System.Drawing.Point(230, 27);
            this.lblFont.Name = "lblFont";
            this.lblFont.Size = new System.Drawing.Size(127, 13);
            this.lblFont.TabIndex = 13;
            this.lblFont.TabStop = true;
            this.lblFont.Text = "Change font, size or color";
            this.lblFont.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblFont_LinkClicked);
            // 
            // cntTagPos
            // 
            this.cntTagPos.Controls.Add(this.txtMargin);
            this.cntTagPos.Controls.Add(this.lblMargin);
            this.cntTagPos.Location = new System.Drawing.Point(24, 261);
            this.cntTagPos.Name = "cntTagPos";
            this.cntTagPos.Size = new System.Drawing.Size(200, 46);
            this.cntTagPos.TabIndex = 14;
            this.cntTagPos.TabStop = false;
            this.cntTagPos.Text = "Tag Positioning";
            // 
            // txtMargin
            // 
            this.txtMargin.Location = new System.Drawing.Point(109, 17);
            this.txtMargin.Name = "txtMargin";
            this.txtMargin.Size = new System.Drawing.Size(85, 20);
            this.txtMargin.TabIndex = 1;
            this.txtMargin.TextChanged += new System.EventHandler(this.txtMargin_TextChanged);
            // 
            // lblMargin
            // 
            this.lblMargin.AutoSize = true;
            this.lblMargin.Location = new System.Drawing.Point(16, 20);
            this.lblMargin.Name = "lblMargin";
            this.lblMargin.Size = new System.Drawing.Size(39, 13);
            this.lblMargin.TabIndex = 0;
            this.lblMargin.Text = "Margin";
            // 
            // CBEOptionPageControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cntLanguages);
            this.Controls.Add(this.cntInfo);
            this.Controls.Add(this.cntTagPos);
            this.Controls.Add(this.lblFont);
            this.Controls.Add(this.cntDisplayMode);
            this.Controls.Add(this.cntNavigateMode);
            this.Controls.Add(this.cntVisibilityMode);
            this.Controls.Add(this.chkCBETaggerEnabled);
            this.MinimumSize = new System.Drawing.Size(430, 440);
            this.Name = "CBEOptionPageControl";
            this.Size = new System.Drawing.Size(430, 440);
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
            this.cntTagPos.ResumeLayout(false);
            this.cntTagPos.PerformLayout();
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
        private System.Windows.Forms.RadioButton rdbCtrlClick;
        private System.Windows.Forms.LinkLabel lblFont;
        private System.Windows.Forms.GroupBox cntTagPos;
        private System.Windows.Forms.TextBox txtMargin;
        private System.Windows.Forms.Label lblMargin;
    }
}
