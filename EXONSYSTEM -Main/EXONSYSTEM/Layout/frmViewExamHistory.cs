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

            AddOverviewRow("ContestantShiftID", _contestantInformation != null ? _contestantInformation.ContestantShiftID.ToString() : string.Empty);
            AddOverviewRow("Trạng thái", BuildStatusText());
            AddOverviewRow("TimeStarted", BuildTimeStartedText());
            AddOverviewRow("SubmitTime", BuildSubmitTimeText());
            AddOverviewRow("TimeWorked", BuildTimeWorkedText());
            AddOverviewRow("SubmitStatus", BuildSubmitStatusText());
            AddOverviewRow("TimeSource", BuildTimeSourceText());
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

            return GetStatusText(_contestantInformation.Status);
        }

        private string GetStatusText(int status)
        {
            if (status == Constant.STATUS_FINISHED)
            {
                return "Submitted";
            }

            if (status == Constant.STATUS_DOING)
            {
                return "Doing";
            }

            if (status == Constant.STATUS_DOING_BUT_INTERRUPT)
            {
                return "Interrupted";
            }

            if (status == Constant.STATUS_LOGGED)
            {
                return "Pending";
            }

            if (status == Constant.STATUS_LOGGED_DO_NOT_FINISH)
            {
                return "Pending";
            }

            if (status == Constant.STATUS_READY)
            {
                return "Pending";
            }

            if (status == Constant.STATUS_READY_TO_GET_TEST)
            {
                return "Pending";
            }

            if (status == Constant.STATUS_INITIALIZE)
            {
                return "Pending";
            }

            return "Pending";
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
                return "Success";
            }

            if (_contestantInformation.Status == Constant.STATUS_FINISHED || _contestantInformation.Status == Constant.STATUS_REJECT)
            {
                // Đã finished nhưng SubmitSuccess = false → lưu DB thất bại
                return "Failed";
            }

            return "Pending";
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
                dgvConnection.Rows.Add(entry.TimeText, NormalizeConnectionEvent(entry), NormalizeConnectionDetail(entry));
            }
        }

        private void RenderActionHistory()
        {
            dgvActions.Rows.Clear();
            for (int i = 0; i < AnswerSelectionLogger.Logs.Count; i++)
            {
                AnswerSelectionLogEntry log = AnswerSelectionLogger.Logs[i];
                string action = (i > 0 && AnswerSelectionLogger.Logs[i - 1].QuestionNo == log.QuestionNo) ? "CHANGE" : "SELECT";
                dgvActions.Rows.Add(ExtractTimeOnly(log.SelectedAtMsText), "Q" + log.QuestionNo, action, log.AnswerLabel);
            }

            if (dgvActions.Rows.Count == 0)
            {
                foreach (string[] entry in ReadActionLogEntries())
                {
                    dgvActions.Rows.Add(entry[0], entry[1], entry[2], entry[3]);
                }
            }
        }

        private void RenderAutosaveHistory()
        {
            dgvAutosave.Rows.Clear();
            if (dgvAutosave.Columns.Count >= 9)
            {
                dgvAutosave.Columns[0].HeaderText = "Time";
                dgvAutosave.Columns[1].HeaderText = "Event";
                dgvAutosave.Columns[2].HeaderText = "Status";
                dgvAutosave.Columns[0].Visible = true;
                dgvAutosave.Columns[1].Visible = true;
                dgvAutosave.Columns[2].Visible = true;
                dgvAutosave.Columns[0].Width = 180;
                dgvAutosave.Columns[1].Width = 120;
                dgvAutosave.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                for (int i = 3; i < dgvAutosave.Columns.Count; i++)
                {
                    dgvAutosave.Columns[i].Visible = false;
                }
            }

            foreach (ConnectionEvent ev in ConnectionLogger.Events)
            {
                if (ev.EventType == "SUBMIT" || ev.EventType == "AUTOSAVE")
                {
                    dgvAutosave.Rows.Add(ExtractTimeOnly(ev.TimeText), ev.EventType, NormalizeSubmitStatus(ev));
                }
            }

            if (dgvAutosave.Rows.Count == 0)
            {
                foreach (string path in GetSubmitLogFiles())
                {
                    Dictionary<string, string> data = ReadKeyValueFile(path);
                    string reason = GetValue(data, "Reason");
                    string loggedAt = GetValue(data, "LoggedAt");
                    dgvAutosave.Rows.Add(ExtractTimeOnly(loggedAt), NormalizeSubmitEvent(reason), "SUCCESS");
                }
            }
        }

        private string NormalizeConnectionEvent(ConnectionEvent entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.EventType))
            {
                return string.Empty;
            }

            if (entry.EventType == "LOGIN") return "LOGIN";
            if (entry.EventType == "DISCONNECT") return "DISCONNECT";
            if (entry.EventType == "RECONNECT") return "RECONNECT";
            if (entry.EventType == "SUBMIT") return "SUBMIT";
            return entry.EventType;
        }

        private string NormalizeConnectionDetail(ConnectionEvent entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Detail))
            {
                return string.Empty;
            }

            int computerIndex = entry.Detail.IndexOf("Computer=", StringComparison.OrdinalIgnoreCase);
            if (computerIndex >= 0)
            {
                return entry.Detail.Substring(computerIndex).Trim();
            }

            return string.Empty;
        }

        private string NormalizeSubmitStatus(ConnectionEvent entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            if (entry.EventType == "SUBMIT")
            {
                return "SUCCESS";
            }

            if (entry.Detail.IndexOf("FAILED", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "FAILED";
            }

            return "SUCCESS";
        }

        private string NormalizeSubmitEvent(string reason)
        {
            if (string.Equals(reason, "SUBMIT_CLICK", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(reason, "TIMEOUT", StringComparison.OrdinalIgnoreCase))
            {
                return "SUBMIT";
            }

            if (string.Equals(reason, "AUTOSAVE", StringComparison.OrdinalIgnoreCase))
            {
                return "AUTOSAVE";
            }

            return string.IsNullOrEmpty(reason) ? "SUBMIT" : reason;
        }

        private string ExtractTimeOnly(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            DateTime parsed;
            if (DateTime.TryParse(value, out parsed))
            {
                return parsed.ToString("HH:mm:ss.fff");
            }

            string[] parts = value.Split(' ');
            if (parts.Length > 1)
            {
                string timeText = parts[parts.Length - 1];
                int lastColon = timeText.LastIndexOf(':');
                if (lastColon >= 0)
                {
                    timeText = timeText.Substring(0, lastColon) + "." + timeText.Substring(lastColon + 1);
                }

                return timeText;
            }

            return value;
        }

        private IEnumerable<string[]> ReadActionLogEntries()
        {
            List<string[]> rows = new List<string[]>();
            string path = GetActionLogPath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return rows;
            }

            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 2; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string trimmed = line.Trim();
                    if (trimmed.Length < 30)
                    {
                        continue;
                    }

                    string timeText = trimmed.Substring(0, Math.Min(23, trimmed.Length)).Trim();
                    string remain = trimmed.Length > 23 ? trimmed.Substring(23).Trim() : string.Empty;
                    string[] parts = remain.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 3)
                    {
                        continue;
                    }

                    string question = parts[0];
                    string action = parts[1];
                    string answer = string.Join(" ", parts.Skip(2).ToArray());
                    rows.Add(new string[] { ExtractTimeOnly(timeText), question, action, answer });
                }
            }
            catch
            {
            }

            return rows;
        }

        private string GetActionLogPath()
        {
            if (_contestantInformation == null)
            {
                return string.Empty;
            }

            return Path.Combine(@"C:\ProgramData\EXON\Logs",
                string.Format("{0}_{1}_actions.log", _contestantInformation.ContestantCode, _contestantInformation.ContestantShiftID));
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
