namespace EXONSYSTEM.Layout
{
    partial class frmViewExamHistory
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.mtcHistory = new MetroFramework.Controls.MetroTabControl();
            this.mtpAnswerHistory = new MetroFramework.Controls.MetroTabPage();
            this.pnlAnswerHistory = new System.Windows.Forms.Panel();
            this.mtcHistory.SuspendLayout();
            this.mtpAnswerHistory.SuspendLayout();
            this.SuspendLayout();
            // 
            // mtcHistory
            // 
            this.mtcHistory.Controls.Add(this.mtpAnswerHistory);
            this.mtcHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mtcHistory.Location = new System.Drawing.Point(20, 60);
            this.mtcHistory.Name = "mtcHistory";
            this.mtcHistory.SelectedIndex = 0;
            this.mtcHistory.Size = new System.Drawing.Size(760, 460);
            this.mtcHistory.TabIndex = 0;
            this.mtcHistory.UseSelectable = true;
            // 
            // mtpAnswerHistory
            // 
            this.mtpAnswerHistory.Controls.Add(this.pnlAnswerHistory);
            this.mtpAnswerHistory.HorizontalScrollbarBarColor = true;
            this.mtpAnswerHistory.HorizontalScrollbarHighlightOnWheel = false;
            this.mtpAnswerHistory.HorizontalScrollbarSize = 10;
            this.mtpAnswerHistory.Location = new System.Drawing.Point(4, 38);
            this.mtpAnswerHistory.Name = "mtpAnswerHistory";
            this.mtpAnswerHistory.Size = new System.Drawing.Size(752, 418);
            this.mtpAnswerHistory.TabIndex = 0;
            this.mtpAnswerHistory.Text = "Lịch sử đáp án";
            this.mtpAnswerHistory.VerticalScrollbarBarColor = true;
            this.mtpAnswerHistory.VerticalScrollbarHighlightOnWheel = false;
            this.mtpAnswerHistory.VerticalScrollbarSize = 10;
            // 
            // pnlAnswerHistory
            // 
            this.pnlAnswerHistory.AutoScroll = true;
            this.pnlAnswerHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlAnswerHistory.Location = new System.Drawing.Point(0, 0);
            this.pnlAnswerHistory.Name = "pnlAnswerHistory";
            this.pnlAnswerHistory.Size = new System.Drawing.Size(752, 418);
            this.pnlAnswerHistory.TabIndex = 0;
            // 
            // frmViewExamHistory
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 540);
            this.Controls.Add(this.mtcHistory);
            this.Movable = false;
            this.Name = "frmViewExamHistory";
            this.Text = "Lịch sử thi";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.frmViewExamHistory_Load);
            this.mtcHistory.ResumeLayout(false);
            this.mtpAnswerHistory.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private MetroTabControl mtcHistory;
        private MetroTabPage mtpAnswerHistory;
        private System.Windows.Forms.Panel pnlAnswerHistory;
    }
}
