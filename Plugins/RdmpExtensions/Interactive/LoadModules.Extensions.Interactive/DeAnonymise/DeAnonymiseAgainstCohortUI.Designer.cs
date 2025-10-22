using Rdmp.UI.ChecksUI;

namespace LoadModules.Extensions.Interactive.DeAnonymise
{
    partial class DeAnonymiseAgainstCohortUI
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DeAnonymiseAgainstCohortUI));
            this.checksUI1 = new Rdmp.UI.ChecksUI.ChecksUI();
            this.btnChooseCohort = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.cbOverrideReleaseIdentifierColumn = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.lblExpectedReleaseIdentifierColumn = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // checksUI1
            // 
            this.checksUI1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checksUI1.Location = new System.Drawing.Point(-1, 257);
            this.checksUI1.Name = "checksUI1";
            this.checksUI1.Size = new System.Drawing.Size(889, 206);
            this.checksUI1.TabIndex = 0;
            // 
            // btnChooseCohort
            // 
            this.btnChooseCohort.Location = new System.Drawing.Point(15, 102);
            this.btnChooseCohort.Name = "btnChooseCohort";
            this.btnChooseCohort.Size = new System.Drawing.Size(122, 23);
            this.btnChooseCohort.TabIndex = 1;
            this.btnChooseCohort.Text = "Choose Cohort...";
            this.btnChooseCohort.UseVisualStyleBackColor = true;
            this.btnChooseCohort.Click += new System.EventHandler(this.btnChooseCohort_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(876, 90);
            this.label1.TabIndex = 2;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(306, 469);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(124, 23);
            this.btnOk.TabIndex = 3;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(436, 469);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(124, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "Cancel";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.Enabled = false;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(15, 185);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(862, 48);
            this.flowLayoutPanel1.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(15, 132);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(862, 37);
            this.label2.TabIndex = 5;
            this.label2.Text = resources.GetString("label2.Text");
            // 
            // cbOverrideReleaseIdentifierColumn
            // 
            this.cbOverrideReleaseIdentifierColumn.AutoSize = true;
            this.cbOverrideReleaseIdentifierColumn.Location = new System.Drawing.Point(18, 162);
            this.cbOverrideReleaseIdentifierColumn.Name = "cbOverrideReleaseIdentifierColumn";
            this.cbOverrideReleaseIdentifierColumn.Size = new System.Drawing.Size(268, 17);
            this.cbOverrideReleaseIdentifierColumn.TabIndex = 6;
            this.cbOverrideReleaseIdentifierColumn.Text = "Use Mispelled Column As Release Identifier Column";
            this.cbOverrideReleaseIdentifierColumn.UseVisualStyleBackColor = true;
            this.cbOverrideReleaseIdentifierColumn.CheckedChanged += new System.EventHandler(this.cbOverrideReleaseIdentifierColumn_CheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 240);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(178, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Expected Release Identifier Column:";
            // 
            // lblExpectedReleaseIdentifierColumn
            // 
            this.lblExpectedReleaseIdentifierColumn.AutoSize = true;
            this.lblExpectedReleaseIdentifierColumn.Location = new System.Drawing.Point(199, 240);
            this.lblExpectedReleaseIdentifierColumn.Name = "lblExpectedReleaseIdentifierColumn";
            this.lblExpectedReleaseIdentifierColumn.Size = new System.Drawing.Size(0, 13);
            this.lblExpectedReleaseIdentifierColumn.TabIndex = 7;
            // 
            // DeAnonymiseAgainstCohortUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(889, 504);
            this.Controls.Add(this.lblExpectedReleaseIdentifierColumn);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cbOverrideReleaseIdentifierColumn);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnChooseCohort);
            this.Controls.Add(this.checksUI1);
            this.Name = "DeAnonymiseAgainstCohortUI";
            this.Text = "DeAnonymiseAgainstCohort";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Rdmp.UI.ChecksUI.ChecksUI checksUI1;
        private System.Windows.Forms.Button btnChooseCohort;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox cbOverrideReleaseIdentifierColumn;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lblExpectedReleaseIdentifierColumn;
    }
}