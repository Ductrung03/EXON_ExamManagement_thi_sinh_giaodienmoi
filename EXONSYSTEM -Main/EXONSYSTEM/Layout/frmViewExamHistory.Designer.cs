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
            this.tabOverview = new MetroFramework.Controls.MetroTabPage();
            this.pnlOverview = new System.Windows.Forms.Panel();
            this.tlpOverview = new System.Windows.Forms.TableLayoutPanel();
            this.tabConnection = new MetroFramework.Controls.MetroTabPage();
            this.dgvConnection = new System.Windows.Forms.DataGridView();
            this.colConnectionTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colConnectionEvent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colConnectionDetail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabActions = new MetroFramework.Controls.MetroTabPage();
            this.dgvActions = new System.Windows.Forms.DataGridView();
            this.colActionTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colQuestion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAction = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAnswer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabAutosave = new MetroFramework.Controls.MetroTabPage();
            this.dgvAutosave = new System.Windows.Forms.DataGridView();
            this.colLoggedAt = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSubmitTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWorked = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSource = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMaxScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colReason = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colComputer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFile = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.mtcHistory.SuspendLayout();
            this.tabOverview.SuspendLayout();
            this.pnlOverview.SuspendLayout();
            this.tabConnection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvConnection)).BeginInit();
            this.tabActions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvActions)).BeginInit();
            this.tabAutosave.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAutosave)).BeginInit();
            this.SuspendLayout();
            // 
            // mtcHistory
            // 
            this.mtcHistory.Controls.Add(this.tabOverview);
            this.mtcHistory.Controls.Add(this.tabConnection);
            this.mtcHistory.Controls.Add(this.tabActions);
            this.mtcHistory.Controls.Add(this.tabAutosave);
            this.mtcHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mtcHistory.Location = new System.Drawing.Point(20, 60);
            this.mtcHistory.Name = "mtcHistory";
            this.mtcHistory.SelectedIndex = 0;
            this.mtcHistory.Size = new System.Drawing.Size(960, 560);
            this.mtcHistory.TabIndex = 0;
            this.mtcHistory.UseSelectable = true;
            // 
            // tabOverview
            // 
            this.tabOverview.Controls.Add(this.pnlOverview);
            this.tabOverview.HorizontalScrollbarBarColor = true;
            this.tabOverview.HorizontalScrollbarHighlightOnWheel = false;
            this.tabOverview.HorizontalScrollbarSize = 10;
            this.tabOverview.Location = new System.Drawing.Point(4, 38);
            this.tabOverview.Name = "tabOverview";
            this.tabOverview.Size = new System.Drawing.Size(952, 518);
            this.tabOverview.TabIndex = 0;
            this.tabOverview.Text = "Tổng quan";
            this.tabOverview.VerticalScrollbarBarColor = true;
            this.tabOverview.VerticalScrollbarHighlightOnWheel = false;
            this.tabOverview.VerticalScrollbarSize = 10;
            // 
            // pnlOverview
            // 
            this.pnlOverview.AutoScroll = true;
            this.pnlOverview.Controls.Add(this.tlpOverview);
            this.pnlOverview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlOverview.Location = new System.Drawing.Point(0, 0);
            this.pnlOverview.Name = "pnlOverview";
            this.pnlOverview.Size = new System.Drawing.Size(952, 518);
            this.pnlOverview.TabIndex = 0;
            // 
            // tlpOverview
            // 
            this.tlpOverview.AutoSize = true;
            this.tlpOverview.ColumnCount = 2;
            this.tlpOverview.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tlpOverview.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpOverview.Dock = System.Windows.Forms.DockStyle.Top;
            this.tlpOverview.Location = new System.Drawing.Point(0, 0);
            this.tlpOverview.Name = "tlpOverview";
            this.tlpOverview.RowCount = 1;
            this.tlpOverview.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpOverview.Size = new System.Drawing.Size(952, 0);
            this.tlpOverview.TabIndex = 0;
            // 
            // tabConnection
            // 
            this.tabConnection.Controls.Add(this.dgvConnection);
            this.tabConnection.HorizontalScrollbarBarColor = true;
            this.tabConnection.HorizontalScrollbarHighlightOnWheel = false;
            this.tabConnection.HorizontalScrollbarSize = 10;
            this.tabConnection.Location = new System.Drawing.Point(4, 38);
            this.tabConnection.Name = "tabConnection";
            this.tabConnection.Size = new System.Drawing.Size(952, 518);
            this.tabConnection.TabIndex = 1;
            this.tabConnection.Text = "Lịch sử kết nối";
            this.tabConnection.VerticalScrollbarBarColor = true;
            this.tabConnection.VerticalScrollbarHighlightOnWheel = false;
            this.tabConnection.VerticalScrollbarSize = 10;
            // 
            // dgvConnection
            // 
            this.dgvConnection.AllowUserToAddRows = false;
            this.dgvConnection.AllowUserToDeleteRows = false;
            this.dgvConnection.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            this.dgvConnection.BackgroundColor = System.Drawing.Color.White;
            this.dgvConnection.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvConnection.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colConnectionTime,
            this.colConnectionEvent,
            this.colConnectionDetail});
            this.dgvConnection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvConnection.Location = new System.Drawing.Point(0, 0);
            this.dgvConnection.Name = "dgvConnection";
            this.dgvConnection.ReadOnly = true;
            this.dgvConnection.RowHeadersVisible = false;
            this.dgvConnection.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvConnection.Size = new System.Drawing.Size(952, 518);
            this.dgvConnection.TabIndex = 0;
            // 
            // colConnectionTime
            // 
            this.colConnectionTime.HeaderText = "Time";
            this.colConnectionTime.Name = "colConnectionTime";
            this.colConnectionTime.ReadOnly = true;
            this.colConnectionTime.Width = 200;
            // 
            // colConnectionEvent
            // 
            this.colConnectionEvent.HeaderText = "Event";
            this.colConnectionEvent.Name = "colConnectionEvent";
            this.colConnectionEvent.ReadOnly = true;
            this.colConnectionEvent.Width = 110;
            // 
            // colConnectionDetail
            // 
            this.colConnectionDetail.HeaderText = "Detail";
            this.colConnectionDetail.Name = "colConnectionDetail";
            this.colConnectionDetail.ReadOnly = true;
            this.colConnectionDetail.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            // 
            // tabActions
            // 
            this.tabActions.Controls.Add(this.dgvActions);
            this.tabActions.HorizontalScrollbarBarColor = true;
            this.tabActions.HorizontalScrollbarHighlightOnWheel = false;
            this.tabActions.HorizontalScrollbarSize = 10;
            this.tabActions.Location = new System.Drawing.Point(4, 38);
            this.tabActions.Name = "tabActions";
            this.tabActions.Size = new System.Drawing.Size(952, 518);
            this.tabActions.TabIndex = 2;
            this.tabActions.Text = "Thao tác làm bài";
            this.tabActions.VerticalScrollbarBarColor = true;
            this.tabActions.VerticalScrollbarHighlightOnWheel = false;
            this.tabActions.VerticalScrollbarSize = 10;
            // 
            // dgvActions
            // 
            this.dgvActions.AllowUserToAddRows = false;
            this.dgvActions.AllowUserToDeleteRows = false;
            this.dgvActions.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            this.dgvActions.BackgroundColor = System.Drawing.Color.White;
            this.dgvActions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvActions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colActionTime,
            this.colQuestion,
            this.colAction,
            this.colAnswer});
            this.dgvActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvActions.Location = new System.Drawing.Point(0, 0);
            this.dgvActions.Name = "dgvActions";
            this.dgvActions.ReadOnly = true;
            this.dgvActions.RowHeadersVisible = false;
            this.dgvActions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvActions.Size = new System.Drawing.Size(952, 518);
            this.dgvActions.TabIndex = 0;
            // 
            // colActionTime
            // 
            this.colActionTime.HeaderText = "Time";
            this.colActionTime.Name = "colActionTime";
            this.colActionTime.ReadOnly = true;
            this.colActionTime.Width = 200;
            // 
            // colQuestion
            // 
            this.colQuestion.HeaderText = "Q";
            this.colQuestion.Name = "colQuestion";
            this.colQuestion.ReadOnly = true;
            this.colQuestion.Width = 60;
            // 
            // colAction
            // 
            this.colAction.HeaderText = "Action";
            this.colAction.Name = "colAction";
            this.colAction.ReadOnly = true;
            this.colAction.Width = 90;
            // 
            // colAnswer
            // 
            this.colAnswer.HeaderText = "Answer";
            this.colAnswer.Name = "colAnswer";
            this.colAnswer.ReadOnly = true;
            this.colAnswer.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            // 
            // tabAutosave
            // 
            this.tabAutosave.Controls.Add(this.dgvAutosave);
            this.tabAutosave.HorizontalScrollbarBarColor = true;
            this.tabAutosave.HorizontalScrollbarHighlightOnWheel = false;
            this.tabAutosave.HorizontalScrollbarSize = 10;
            this.tabAutosave.Location = new System.Drawing.Point(4, 38);
            this.tabAutosave.Name = "tabAutosave";
            this.tabAutosave.Size = new System.Drawing.Size(952, 518);
            this.tabAutosave.TabIndex = 3;
            this.tabAutosave.Text = "Autosave/Submit";
            this.tabAutosave.VerticalScrollbarBarColor = true;
            this.tabAutosave.VerticalScrollbarHighlightOnWheel = false;
            this.tabAutosave.VerticalScrollbarSize = 10;
            // 
            // dgvAutosave
            // 
            this.dgvAutosave.AllowUserToAddRows = false;
            this.dgvAutosave.AllowUserToDeleteRows = false;
            this.dgvAutosave.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvAutosave.BackgroundColor = System.Drawing.Color.White;
            this.dgvAutosave.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAutosave.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colLoggedAt,
            this.colSubmitTime,
            this.colWorked,
            this.colSource,
            this.colScore,
            this.colMaxScore,
            this.colReason,
            this.colComputer,
            this.colFile});
            this.dgvAutosave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvAutosave.Location = new System.Drawing.Point(0, 0);
            this.dgvAutosave.Name = "dgvAutosave";
            this.dgvAutosave.ReadOnly = true;
            this.dgvAutosave.RowHeadersVisible = false;
            this.dgvAutosave.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvAutosave.Size = new System.Drawing.Size(952, 518);
            this.dgvAutosave.TabIndex = 0;
            // 
            // colLoggedAt
            // 
            this.colLoggedAt.HeaderText = "Time";
            this.colLoggedAt.Name = "colLoggedAt";
            this.colLoggedAt.ReadOnly = true;
            // 
            // colSubmitTime
            // 
            this.colSubmitTime.HeaderText = "Event";
            this.colSubmitTime.Name = "colSubmitTime";
            this.colSubmitTime.ReadOnly = true;
            // 
            // colWorked
            // 
            this.colWorked.HeaderText = "Status";
            this.colWorked.Name = "colWorked";
            this.colWorked.ReadOnly = true;
            // 
            // colSource
            // 
            this.colSource.HeaderText = "TimeSource";
            this.colSource.Name = "colSource";
            this.colSource.ReadOnly = true;
            // 
            // colScore
            // 
            this.colScore.HeaderText = "Score";
            this.colScore.Name = "colScore";
            this.colScore.ReadOnly = true;
            // 
            // colMaxScore
            // 
            this.colMaxScore.HeaderText = "MaxScore";
            this.colMaxScore.Name = "colMaxScore";
            this.colMaxScore.ReadOnly = true;
            // 
            // colReason
            // 
            this.colReason.HeaderText = "Reason";
            this.colReason.Name = "colReason";
            this.colReason.ReadOnly = true;
            // 
            // colComputer
            // 
            this.colComputer.HeaderText = "ComputerName";
            this.colComputer.Name = "colComputer";
            this.colComputer.ReadOnly = true;
            // 
            // colFile
            // 
            this.colFile.HeaderText = "File";
            this.colFile.Name = "colFile";
            this.colFile.ReadOnly = true;
            // 
            // frmViewExamHistory
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 640);
            this.Controls.Add(this.mtcHistory);
            this.Movable = false;
            this.Name = "frmViewExamHistory";
            this.Text = "Lịch sử thi";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.frmViewExamHistory_Load);
            this.mtcHistory.ResumeLayout(false);
            this.tabOverview.ResumeLayout(false);
            this.pnlOverview.ResumeLayout(false);
            this.pnlOverview.PerformLayout();
            this.tabConnection.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvConnection)).EndInit();
            this.tabActions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvActions)).EndInit();
            this.tabAutosave.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvAutosave)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private MetroFramework.Controls.MetroTabControl mtcHistory;
        private MetroFramework.Controls.MetroTabPage tabOverview;
        private MetroFramework.Controls.MetroTabPage tabConnection;
        private MetroFramework.Controls.MetroTabPage tabActions;
        private MetroFramework.Controls.MetroTabPage tabAutosave;
        private System.Windows.Forms.Panel pnlOverview;
        private System.Windows.Forms.TableLayoutPanel tlpOverview;
        private System.Windows.Forms.DataGridView dgvConnection;
        private System.Windows.Forms.DataGridView dgvActions;
        private System.Windows.Forms.DataGridView dgvAutosave;
        private System.Windows.Forms.DataGridViewTextBoxColumn colConnectionTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colConnectionEvent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colConnectionDetail;
        private System.Windows.Forms.DataGridViewTextBoxColumn colActionTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colQuestion;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAction;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAnswer;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLoggedAt;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSubmitTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWorked;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScore;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxScore;
        private System.Windows.Forms.DataGridViewTextBoxColumn colReason;
        private System.Windows.Forms.DataGridViewTextBoxColumn colComputer;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFile;
    }
}
