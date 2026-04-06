using EXONSYSTEM.Common;
using MetroFramework.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EXONSYSTEM.Layout
{
    public partial class frmViewExamHistory : MetroFramework.Forms.MetroForm
    {
        private readonly int _contestantShiftId;

        public frmViewExamHistory(int contestantShiftId)
        {
            InitializeComponent();
            _contestantShiftId = contestantShiftId;
        }

        private void frmViewExamHistory_Load(object sender, EventArgs e)
        {
            RenderAnswerHistory();
        }

        private void RenderAnswerHistory()
        {
            pnlAnswerHistory.Controls.Clear();

            List<ExamAnswerHistoryEntry> entries = ExamAnswerHistoryStore.GetEntries(_contestantShiftId);
            if (entries.Count == 0)
            {
                pnlAnswerHistory.Controls.Add(CreateLabel("Không có lịch sử chọn đáp án.", true));
                return;
            }

            int top = 10;
            foreach (IGrouping<int, ExamAnswerHistoryEntry> group in entries.GroupBy(x => x.QuestionNo).OrderBy(x => x.Key))
            {
                Label lblQuestion = CreateLabel("Câu " + group.Key, true);
                lblQuestion.Location = new Point(10, top);
                pnlAnswerHistory.Controls.Add(lblQuestion);
                top = lblQuestion.Bottom + 6;

                foreach (ExamAnswerHistoryEntry entry in group.OrderBy(x => x.SelectedAt))
                {
                    Label lblEntry = CreateLabel(string.Format("{0} - {1}", entry.SelectedAtText, entry.AnswerText), false);
                    lblEntry.MaximumSize = new Size(pnlAnswerHistory.Width - 40, 0);
                    lblEntry.Location = new Point(28, top);
                    pnlAnswerHistory.Controls.Add(lblEntry);
                    top = lblEntry.Bottom + 4;
                }

                top += 10;
            }
        }

        private Label CreateLabel(string text, bool isHeader)
        {
            return new Label
            {
                AutoSize = true,
                ForeColor = Constant.COLOR_BLACK,
                BackColor = Color.Transparent,
                Font = new Font(Constant.FONT_FAMILY_DEFAULT, isHeader ? 14 : Constant.FONT_SIZE_DEFAULT, isHeader ? FontStyle.Bold : FontStyle.Regular),
                Text = text
            };
        }
    }
}
