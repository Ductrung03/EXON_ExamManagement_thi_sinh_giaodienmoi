using DAO.DataProvider;
using EXONSYSTEM.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace EXONSYSTEM.Layout
{
    public partial class frmViewExamHistory : MetroFramework.Forms.MetroForm
    {
        private readonly ContestantInformation _contestantInformation;

        public frmViewExamHistory(ContestantInformation contestantInformation)
        {
            InitializeComponent();
            _contestantInformation = contestantInformation;
        }

        private void frmViewExamHistory_Load(object sender, EventArgs e)
        {
            RenderOverview();
            RenderConnectionHistory();
            RenderActionHistory();
            RenderAutosaveHistory();
        }

        private void RenderOverview()
        {
            tlpOverview.Controls.Clear();
            tlpOverview.RowStyles.Clear();
            tlpOverview.RowCount = 0;

            AddOverviewRow("Mã ca thi (ShiftID)", _contestantInformation != null ? _contestantInformation.ContestantShiftID.ToString() : string.Empty);
            AddOverviewRow("Trạng thái ca thi", BuildStatusText());
            AddOverviewRow("Thời điểm bắt đầu", BuildTimeStartedText());
            AddOverviewRow("Thời điểm nộp bài", BuildSubmitTimeText());
            AddOverviewRow("Tổng thời gian làm", BuildTimeWorkedText());
            AddOverviewRow("Trạng thái nộp bài", BuildSubmitStatusText());
            AddOverviewRow("Nguồn giờ nộp bài", BuildTimeSourceText());
            AddOverviewRow("Số thao tác chọn đáp án", AnswerSelectionLogger.Logs.Count.ToString());
            AddOverviewRow("Số sự kiện kết nối", ConnectionLogger.Events.Count.ToString());
        }

        private void AddOverviewRow(string title, string value)
        {
            int rowIndex = tlpOverview.RowCount++;
            tlpOverview.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label lblTitle = new Label();
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font(Constant.FONT_FAMILY_DEFAULT, 11F, FontStyle.Bold);
            lblTitle.ForeColor = Constant.COLOR_BLACK;
            lblTitle.Margin = new Padding(6);
            lblTitle.Text = title;

            Label lblValue = new Label();
            lblValue.AutoSize = true;
            lblValue.Font = new Font(Constant.FONT_FAMILY_DEFAULT, 11F, FontStyle.Regular);
            lblValue.ForeColor = Constant.COLOR_BLACK;
            lblValue.Margin = new Padding(6);
            lblValue.MaximumSize = new Size(520, 0);
            lblValue.Text = string.IsNullOrEmpty(value) ? "(trống)" : value;

            tlpOverview.Controls.Add(lblTitle, 0, rowIndex);
            tlpOverview.Controls.Add(lblValue, 1, rowIndex);
        }

        private string BuildStatusText()
        {
            if (_contestantInformation == null)
            {
                return string.Empty;
            }

            return string.Format("{0} - {1}", _contestantInformation.Status, GetStatusText(_contestantInformation.Status));
        }

        private string GetStatusText(int status)
        {
            if (status == Constant.STATUS_FINISHED)
            {
                return "FINISHED";
            }

            if (status == Constant.STATUS_DOING)
            {
                return "DOING";
            }

            if (status == Constant.STATUS_DOING_BUT_INTERRUPT)
            {
                return "DOING_BUT_INTERRUPT";
            }

            if (status == Constant.STATUS_LOGGED)
            {
                return "LOGGED";
            }

            if (status == Constant.STATUS_LOGGED_DO_NOT_FINISH)
            {
                return "LOGGED_DO_NOT_FINISH";
            }

            if (status == Constant.STATUS_READY)
            {
                return "READY";
            }

            if (status == Constant.STATUS_READY_TO_GET_TEST)
            {
                return "READY_TO_GET_TEST";
            }

            if (status == Constant.STATUS_INITIALIZE)
            {
                return "INITIALIZE";
            }

            return "UNKNOWN";
        }

        private string BuildTimeStartedText()
        {
            if (_contestantInformation == null || _contestantInformation.TimeStarted <= 0)
            {
                return string.Empty;
            }

            return Controllers.Instance.ConvertUnixToDateTime(_contestantInformation.TimeStarted).ToString("dd-MM-yyyy HH:mm:ss");
        }

        private string BuildSubmitTimeText()
        {
            if (_contestantInformation == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(_contestantInformation.SubmitTimeText))
            {
                return _contestantInformation.SubmitTimeText;
            }

            ConnectionEvent submitEvent = ConnectionLogger.Events.LastOrDefault(x => x.EventType == "SUBMIT");
            if (submitEvent != null)
            {
                return submitEvent.TimeText;
            }

            return _contestantInformation.EndTimeMsText;
        }

        private string BuildTimeWorkedText()
        {
            if (_contestantInformation == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(_contestantInformation.TimeWorkedMsText))
            {
                return _contestantInformation.TimeWorkedMsText;
            }

            if (_contestantInformation.TimeWorkedMs > 0)
            {
                return TimeSpan.FromMilliseconds(_contestantInformation.TimeWorkedMs).ToString(@"hh\:mm\:ss\:fff");
            }

            return string.Empty;
        }

        private string BuildSubmitStatusText()
        {
            if (_contestantInformation == null)
            {
                return "Pending";
            }

            if (_contestantInformation.SubmitSuccess)
            {
                return "✓ Success – Đã lưu lên server";
            }

            if (_contestantInformation.Status == Constant.STATUS_FINISHED || _contestantInformation.Status == Constant.STATUS_REJECT)
            {
                // Đã finished nhưng SubmitSuccess = false → lưu DB thất bại
                return "✗ Failed – Lỗi khi lưu lên server";
            }

            return "Pending – Chưa nộp bài";
        }

        private string BuildTimeSourceText()
        {
            if (_contestantInformation != null && !string.IsNullOrEmpty(_contestantInformation.TimeSource))
            {
                return _contestantInformation.TimeSource;
            }

            ConnectionEvent submitEvent = ConnectionLogger.Events.LastOrDefault(x => x.EventType == "SUBMIT" && !string.IsNullOrEmpty(x.Detail));
            if (submitEvent != null)
            {
                int sourceIndex = submitEvent.Detail.IndexOf("Source=", StringComparison.OrdinalIgnoreCase);
                if (sourceIndex >= 0)
                {
                    return submitEvent.Detail.Substring(sourceIndex + 7).Trim();
                }
            }

            return string.Empty;
        }

        private void RenderConnectionHistory()
        {
            dgvConnection.Rows.Clear();
            foreach (ConnectionEvent entry in ConnectionLogger.Events)
            {
                dgvConnection.Rows.Add(entry.TimeText, entry.EventType, entry.Detail);
            }
        }

        private void RenderActionHistory()
        {
            dgvActions.Rows.Clear();
            for (int i = 0; i < AnswerSelectionLogger.Logs.Count; i++)
            {
                AnswerSelectionLogEntry log = AnswerSelectionLogger.Logs[i];
                string action = (i > 0 && AnswerSelectionLogger.Logs[i - 1].QuestionNo == log.QuestionNo) ? "CHANGE" : "SELECT";
                dgvActions.Rows.Add(log.SelectedAtMsText, "Q" + log.QuestionNo, action, log.AnswerLabel);
            }
        }

        private void RenderAutosaveHistory()
        {
            // Rebuild cột cho dgvAutosave: hiển thị ConnectionLogger events AUTOSAVE + SUBMIT
            dgvAutosave.Rows.Clear();
            dgvAutosave.Columns.Clear();
            dgvAutosave.Columns.Add("colTime", "Thời gian");
            dgvAutosave.Columns.Add("colType", "Loại");
            dgvAutosave.Columns.Add("colDetail", "Chi tiết");
            dgvAutosave.Columns["colTime"].Width = 200;
            dgvAutosave.Columns["colType"].Width = 100;
            dgvAutosave.Columns["colDetail"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Ưu tiên đọc từ ConnectionLogger (trong bộ nhớ – real-time)
            foreach (ConnectionEvent ev in ConnectionLogger.Events)
            {
                if (ev.EventType == "SUBMIT" || ev.EventType == "AUTOSAVE")
                {
                    dgvAutosave.Rows.Add(ev.TimeText, ev.EventType, ev.Detail);
                }
            }

            // Bổ sung từ file .submitlog (trường hợp đọc lại sau khi tắt mở lại)
            if (dgvAutosave.Rows.Count == 0)
            {
                foreach (string path in GetSubmitLogFiles())
                {
                    Dictionary<string, string> data = ReadKeyValueFile(path);
                    string reason = GetValue(data, "Reason");
                    string loggedAt = GetValue(data, "LoggedAt");
                    string detail = string.Format("Score={0}/{1} Source={2} Computer={3}",
                        GetValue(data, "Score"), GetValue(data, "MaxScore"),
                        GetValue(data, "TimeSource"), GetValue(data, "ComputerName"));
                    dgvAutosave.Rows.Add(loggedAt, reason, detail);
                }
            }
        }

        private IEnumerable<string> GetSubmitLogFiles()
        {
            if (_contestantInformation == null)
            {
                return Enumerable.Empty<string>();
            }

            List<string> files = new List<string>();
            string searchPattern = string.Format("{0}_{1}_*.submitlog",
                _contestantInformation.ContestantCode, _contestantInformation.ContestantShiftID);
            string[] folders = new string[]
            {
                @"C:\ProgramData\EXON\Logs",
                @"C:\ProgramData\EXON\SubmitLogs"
            };

            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder)) continue;
                files.AddRange(Directory.GetFiles(folder, searchPattern));
            }

            return files.OrderByDescending(x => x).ToList();
        }

        private Dictionary<string, string> ReadKeyValueFile(string path)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    int sep = line.IndexOf('=');
                    if (sep <= 0) continue;
                    values[line.Substring(0, sep).Trim()] = line.Substring(sep + 1).Trim();
                }
            }
            catch { }
            return values;
        }

        private string GetValue(Dictionary<string, string> data, string key)
        {
            string value;
            return data.TryGetValue(key, out value) ? value : string.Empty;
        }
    }
}
