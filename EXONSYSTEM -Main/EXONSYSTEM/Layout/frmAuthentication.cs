using BUS;
using DAO.DataProvider;
using EXONSYSTEM.Common;
using MetroFramework;
using MetroFramework.Controls;
using MetroFramework.Forms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EXONSYSTEM.Layout
{
    public partial class frmAuthentication : MetroForm
    {
        private int loginCount = 3;
        private int LoginType = Constant.LOGIN_WITH_CONTESTANT_CODE;
        private Contest rC = null;
        private ConfigApplication ca = new ConfigApplication();
        public static string logFile;
     

        private UserHelper.UserTransformation UT = new UserHelper.UserTransformation();

        public frmAuthentication()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }

        public delegate void SendInformationToMainForm(ContestantInformation CI, Contest C, SqlConnection sql);

        public void HandelInformationFromFrmConfig(ConfigApplication _ca)
        {
            this.ca = _ca;
        }

        private void frmAuthentication_Load(object sender, EventArgs e)
        {
            //File.SetAttributes(frmAuthentication.logFile, FileAttributes.Normal);

            ErrorController rEC = new ErrorController();
            //Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "AUTHENTICATION | frmAuthentication_Load | DecryptConfigFile", Controllers.Instance.HandleStringErrorController(rEC));


            //Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "AUTHENTICATION | frmAuthentication_Load | GetConfigFile", Controllers.Instance.HandleStringErrorController(rEC));
            //   AppConfig app = new AppConfig();
            //app.SaveConnectionString(app.AnalyzeConnectionString(ca), out rEC);
            //   if (rEC.ErrorCode == Constant.STATUS_OK)
            //  {
            //if (Controllers.Instance.HandleCheckDB())
            //{
            // check thời gian shift ( ca thi lớn)
            ContestBUS.Instance.GetContestByShiftTime(Dns.GetHostName(), out rC, out rEC);
            if (rEC.ErrorCode == Constant.STATUS_OK)
            {
                this.Text = rC.ContestName.ToUpper();
                lbLine.BackColor = Constant.BACKCOLOR_PANEL_WRAPPER_CONTENT;
                lbLine.Height = 2;
                lbLine.Width = this.Width * 8 / 10;
                lbLine.Location = new Point((this.Width - lbLine.Width) / 2, 70);

                btnSubmit.ForeColor = Constant.FORCECOLOR_BUTTON_SUBMIT;
                btnSubmit.BackColor = Constant.BACKCOLOR_BUTTON_CONTROLLER;

                //mrbtnIdentityCardName.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
                mrbtnContestantCode.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
                //mrbtnStudentCode.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
                //mrbtnIdentityCardName.Text = HandleGetStringLoginType(Constant.LOGIN_WITH_IDENTITY_CARD_NAME).ToUpper();
                mrbtnContestantCode.Text = HandleGetStringLoginType(Constant.LOGIN_WITH_CONTESTANT_CODE).ToUpper();
                //mrbtnStudentCode.Text = HandleGetStringLoginType(Constant.LOGIN_WITH_STUDENT_CODE).ToUpper();
                btnSubmit.Text = Properties.Resources.MSG_GUI_0016;

                mrbtnContestantCode.Location = new Point(this.Width/4, 0);
                //mrbtnStudentCode.Location = new Point(mrbtnContestantCode.Right, mrbtnContestantCode.Top);
                //mrbtnIdentityCardName.Location = new Point(mrbtnStudentCode.Right, mrbtnContestantCode.Top);

                //pAuthen.Width = mrbtnIdentityCardName.Right;
                pAuthen.Location = new Point((this.Width - pAuthen.Width) / 2, lbLine.Bottom + 20);

                txtUsername.Width = btnSubmit.Width = pAuthen.Width - 40;
                txtUsername.Left = btnSubmit.Left = 20;
                this.Height = pAuthen.Bottom + 10;
            }
            else
            {
                //Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_FATAL, Controllers.Instance.HandleStringErrorController(rEC));
                Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_INFO, Controllers.Instance.HandleStringErrorController(rEC), this);
                Controllers.Instance.ExitApplicationFromNotificationForm(this);
            }
            // }
            //else
            //{
            //    Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_FATAL, Properties.Resources.MSG_GUI_0061);
            //    Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Properties.Resources.MSG_GUI_0061, this);
            //    Controllers.Instance.ExitApplicationFromNotificationForm(this);
            //}
            //}
            //else
            //{
            //    Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_FATAL, Controllers.Instance.HandleStringErrorController(rEC));
            //    Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), this);
            //    Controllers.Instance.ExitApplicationFromNotificationForm(this);
            //}

        }
        string filename;
        int timeError = 0;
        List<string> logError = new List<string>();

        private void ShowServerErrorAndRestoreLogin()
        {
            MetroMessageBox.Show(this, "Lỗi server, vui lòng thử lại hoặc liên hệ giám thị", Properties.Resources.MSG_MESS_0001, MessageBoxButtons.OK, MessageBoxIcon.Error, 100);
            HandleEnableControl(true);
            txtUsername.Focus();
        }

        private void ShowGenericErrorAndRestoreLogin()
        {
            MetroMessageBox.Show(this, "Có lỗi xảy ra, vui lòng thử lại hoặc liên hệ giám thị", Properties.Resources.MSG_MESS_0001, MessageBoxButtons.OK, MessageBoxIcon.Error, 100);
            HandleEnableControl(true);
            txtUsername.Focus();
        }

        private void SafeSetLogFileAttributes(FileAttributes attributes)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(frmAuthentication.logFile) && File.Exists(frmAuthentication.logFile))
                {
                    File.SetAttributes(frmAuthentication.logFile, attributes);
                }
            }
            catch
            {
            }
        }

        private void SafeCloseConnection(SqlConnection sql)
        {
            try
            {
                if (sql != null && sql.State != ConnectionState.Closed)
                {
                    sql.Close();
                }
            }
            catch
            {
            }
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            SqlConnection sql = null;
            bool keepConnectionForMainForm = false;
            try
            {
                HandleEnableControl(false);
                //Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "AUTHENTICATION", Properties.Resources.MSG_MESS_0008);
                if (string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    //Log.Instance.WriteLog(Properties.Resources.MSG_LOG_ERROR, "AUTHENTICATION", string.Format(Properties.Resources.MSG_MESS_0002, HandleGetStringLoginType(LoginType)));
                    DialogResult dResult = MetroMessageBox.Show(this, string.Format(Properties.Resources.MSG_MESS_0002, HandleGetStringLoginType(LoginType)), Properties.Resources.MSG_MESS_0001, MessageBoxButtons.OK, MessageBoxIcon.Error, 100);
                    if (dResult.Equals(DialogResult.OK))
                    {
                        HandleEnableControl(true);
                        txtUsername.Focus();
                    }
                }
                else
                {

                    ContestantInformation rCI = null;
                    ErrorController rEC = new ErrorController();
                    sql = new SqlConnection();
                    string connectString = DAO.Common.GetConnectString("EXON_SYSTEM_TESTEntities");
                    sql.ConnectionString = connectString;
                    sql.Open();
                    // kiểm tra đăng nhập của thí sinh
                    ContestantBUS.Instance.Authen(rC, txtUsername.Text, Dns.GetHostName(), LoginType, out rCI, out rEC,sql);
                    if (rEC.ErrorCode == Constant.STATUS_OK)
                    {
                        timeError = 0;
                        string examDate = Controllers.Instance.ConvertUnixToDateTime(rC.ShiftDate).ToString(Constant.FORMAT_DATE_DEFAULT);
                        string startShift = Controllers.Instance.ConvertUnixToDateTime(rC.StartTime).ToString(Constant.FORMAT_TIME_DEFAULT);
                        string endShift = Controllers.Instance.ConvertUnixToDateTime(rC.EndTime).ToString(Constant.FORMAT_TIME_DEFAULT);
                        string examShift = startShift.Replace(":", ".") + "-" + endShift.Replace(":", ".");
                        string fullName = Controllers.Instance.CapitalizeString(rCI.Fullname);
                        filename = rCI.ContestantCode + "_" + fullName + "_" + examDate + "_" + examShift + "_" + rCI.SubjectName.Split(' ')[0] + "_" + rC.RoomName + ".txt";
                        //filename = rCI.ContestantCode + "_" + fullName + "_" + examDate + "_" + examShift + "_" + rC.RoomName + ".txt";
                        Log.fileName = filename;
                        string pathLog = @"C:\ProgramData\EXON\" + filename;
                        logFile = pathLog;
                        //Dieu huong sang file can ghi
                        /*XmlConfigurator.Configure();
                        log4net.Repository.Hierarchy.Hierarchy h =
                        (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
                        foreach (var a in h.Root.Appenders)
                        {
                            if (a is log4net.Appender.FileAppender)
                            {
                                if (a.Name.Equals("CoreAppender"))
                                {
                                    log4net.Appender.FileAppender fa = (log4net.Appender.FileAppender)a;
                                    string logFileLocation = pathLog;
                                    fa.File = logFileLocation;
                                    fa.ActivateOptions();
                                }
                            }
                        }*/

                        //Log dang nhap thanh cong
                        foreach (var error in logError)
                        {
                            Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, "AUTHENTICATION | LOGIN FAIL", error);
                        }
                        Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "AUTHENTICATION", Properties.Resources.MSG_MESS_0007);
                        #region Check Status

                        if (rCI.Status == Constant.STATUS_REJECT)
                        {
                            //Vi pham quy che thi 3 lần. Bài thi bi huy
                            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "AUTHENTICATION", Properties.Resources.MSG_MESS_0045);
                            Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_INFO, Properties.Resources.MSG_GUI_0055, this);
                            if (DTO.NotificationForm.DialogResult == DialogResult.OK)
                            {
                                this.Close();
                            }
                        }
                        else if (rCI.Status == Constant.STATUS_INITIALIZE || rCI.Status == Constant.STATUS_LOGIN_FAIL || rCI.Status == Constant.STATUS_READY_TO_GET_TEST)
                        {
                            // Trạng thái thi mới
                            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "AUTHENTICATION | CHECK_STATUS | Authen | STATUS_INITIALIZE", Constant.STR_STATUS_INITIALIZE_CONTESTANT);

                            //Send message to supervisor LOGGED
                            UT.UserID = rCI.ContestantID;
                            UT.Status = Constant.STATUS_LOGGED;
                            UT.Content = "LOGIN SUCCESSFULLY";


                            // Đổi trạng thái thí sinh sang LOGGED 3000
                            rCI.Status = Constant.STATUS_LOGGED;
                            //rCI.IsNewStarted = true;
                            //rCI.TimeStarted  = Controllers.Instance.ConvertDateTimeToUnix(DAO.DAO.ConvertDateTime.GetDateTimeServer());

                            ContestantBUS.Instance.ChangeStatusContestant(rCI, rC, out rEC, sql);
                            if (rEC.ErrorCode == Constant.STATUS_OK)
                            {

                                Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "AUTHENTICATION | CHANGE_STATUS_CONTESTANT_LOGIN | STATUS_LOGGED", Controllers.Instance.HandleStringErrorController(rEC));

                                SendInformationToMainForm sitm = new SendInformationToMainForm(DTO.MainForm.HandleInformationFromAuthenForm);
                                sitm(rCI, rC, sql);
                                keepConnectionForMainForm = true;
                                this.Hide();
                                DTO.MainForm.Show();

                            }
                            else
                            {
                                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));
                            }
                        }
                        else if (rCI.Status == Constant.STATUS_FINISHED)
                        {
                            // Trạng thái thi xong rồi
                            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "AUTHENTICATION | CHECK_STATUS | Authen | STATUS_FINISHED", Constant.STR_STATUS_FINISHED);

                            Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_INFO, Constant.STR_STATUS_FINISHED, this);
                            if (DTO.NotificationForm.DialogResult == DialogResult.OK)
                            {
                                this.Close();
                            }

                        }
                        else if (rCI.Status == Constant.STATUS_DOING_BUT_INTERRUPT /*|| rCI.Status == Constant.STATUS_LOGGED_DO_NOT_FINISH */|| rCI.Status == Constant.STATUS_DOING)
                        {
                            // Trạng thái đang thi nhưng bị gián đoạn
                            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "AUTHENTICATION | CHECK_STATUS | Authen | STATUS_DOING_BUT_INTERRUPT", Constant.STR_STATUS_DOING_BUT_INTERRUPT);

                            rCI.Status = Constant.STATUS_LOGGED_DO_NOT_FINISH;
                            rCI.IsNewStarted = false;

                            ContestantBUS.Instance.ChangeStatusContestant(rCI, rC, out rEC, sql);
                            if (rEC.ErrorCode == Constant.STATUS_OK)
                            {
                                Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "AUTHENTICATION | CHANGE_STATUS_CONTESTANT_LOGIN | STATUS_LOGGED_DO_NOT_FINISH", Controllers.Instance.HandleStringErrorController(rEC));

                                SendInformationToMainForm sitm = new SendInformationToMainForm(DTO.MainForm.HandleInformationFromAuthenForm);
                                sitm(rCI, rC, sql);
                                keepConnectionForMainForm = true;
                                this.Hide();
                                DTO.MainForm.Show();

                            }
                            else
                            {
                                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));
                            }
                        }
                        else if (rCI.Status == Constant.STATUS_LOGGED)
                        {
                            // Trạng thái tài khoản đang được sử dụng
                            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "AUTHENTICATION | CHECK_STATUS | Authen | STATUS_DOING||STATUS_LOGGED_DO_NOT_FINISH||STATUS_READY||STATUS_LOGGED", Constant.STR_STATUS_DOING);
                            // Send message to supervisor LOGGED WITH STATUS DOING

                            Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Constant.STR_STATUS_DOING, this);
                            if (DTO.NotificationForm.DialogResult == DialogResult.OK)
                            {
                                this.Close();
                            }
                        }
                        else if (rCI.Status == Constant.STATUS_READY)
                        {
                            // Thí sinh đã sẵn sàng để thi (Load đề thi thành công)
                            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "AUTHENTICATION | CHECK_STATUS | Authen | STATUS_DOING||STATUS_LOGGED_DO_NOT_FINISH||STATUS_READY||STATUS_LOGGED", Constant.STR_STATUS_READY);
                                SendInformationToMainForm sitm = new SendInformationToMainForm(DTO.MainForm.HandleInformationFromAuthenForm);
                                sitm(rCI, rC, sql);
                                keepConnectionForMainForm = true;
                                this.Hide();
                                DTO.MainForm.Show();

                        }

                        else
                        {
                            DialogResult dEResult = MetroMessageBox.Show(this, "Thí sinh chưa xác thực!", Properties.Resources.MSG_MESS_0001, MessageBoxButtons.OK, MessageBoxIcon.Error, 100);
                            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "AUTHENTICATION | CHECK_STATUS | Authen | STATUS_DOING||STATUS_LOGGED_DO_NOT_FINISH||STATUS_READY||STATUS_LOGGED", "Thí sinh chưa xác thực!");
                            if (dEResult.Equals(DialogResult.OK))
                            {
                                this.Close();
                            }
                        }

                        #endregion Check Status
                    }
                    else
                    {
                        timeError += 1;
                        //LogError.Instance.WriteLogError(Properties.Resources.MSG_LOG_ERROR, "AUTHENTICATION | LOGIN FAIL", string.Format("{0}: {1}", string.Format(Properties.Resources.MSG_GUI_0044, HandleGetStringLoginType(LoginType)), txtUsername.Text)); 
                        logError.Add(string.Format("{0}: {1}", string.Format(Properties.Resources.MSG_GUI_0044, HandleGetStringLoginType(LoginType)), txtUsername.Text));
                        if (loginCount == 0)
                        {
                            DialogResult dEResult = MetroMessageBox.Show(this, Properties.Resources.MSG_GUI_0035, Properties.Resources.MSG_MESS_0001, MessageBoxButtons.OK, MessageBoxIcon.Error, 100);
                            if (dEResult.Equals(DialogResult.OK))
                            {
                                this.Close();
                            }
                        }
                        else
                        {
                            if (rEC.ErrorCode == Constant.STATUS_lOGIN_OUTTIME)
                            {

                                DialogResult dResult = MetroMessageBox.Show(this, "Bạn đã hết thời gian để làm bài. Bạn cần xem lại bài viết?", Properties.Resources.MSG_MESS_0001, MessageBoxButtons.YesNo, MessageBoxIcon.Question, 100);
                                if (dResult == DialogResult.Yes)
                                {
                                    string examDate = Controllers.Instance.ConvertUnixToDateTime(rC.ShiftDate).ToString(Constant.FORMAT_DATE_DEFAULT);
                                    string startShift = Controllers.Instance.ConvertUnixToDateTime(rC.StartTime).ToString(Constant.FORMAT_TIME_DEFAULT);
                                    string endShift = Controllers.Instance.ConvertUnixToDateTime(rC.EndTime).ToString(Constant.FORMAT_TIME_DEFAULT);
                                    string examShift = startShift.Replace(":", ".") + "-" + endShift.Replace(":", ".");
                                    string fullName = Controllers.Instance.CapitalizeString(rCI.Fullname);
                               
                                    filename = rCI.ContestantCode + "_" + fullName + "_" + examDate + "_" + examShift + "_" + rCI.SubjectName.Split(' ')[0] + "_" + rC.RoomName + ".txt";
                                    //filename = rCI.ContestantCode + "_" + fullName + "_" + examDate + "_" + examShift + "_" + rC.RoomName + ".txt";
                                    Log.fileName = filename;
                                    string pathLog = @"C:\ProgramData\EXON\" + filename;
                                    logFile = pathLog;
                                    SendInformationToMainForm sitm = new SendInformationToMainForm(DTO.MainForm.HandleInformationFromAuthenForm);
                                    sitm(rCI, rC, sql);
                                    this.Hide();
                                    DTO.MainForm.Show();
                                }
                                else
                                {
                                    this.Close();
                                }


                            }
                            else if (rEC.ErrorCode == Constant.STATUS_SIGNED)
                            {
                                DialogResult dResult = MetroMessageBox.Show(this, "Thí sinh đã ký nộp bài!", Properties.Resources.MSG_MESS_0001, MessageBoxButtons.OK, MessageBoxIcon.Error, 100);
                                this.Close();
                            }
                            else if (rEC.ErrorCode == Constant.STATUS_BAN)
                            {
                                DialogResult dResult = MetroMessageBox.Show(this, "Thí sinh bị cấm thi!", Properties.Resources.MSG_MESS_0001, MessageBoxButtons.OK, MessageBoxIcon.Error, 100);
                                this.Close();
                            }
                            else if (rEC.ErrorCode == Constant.STATUS_PAUSE)
                            {
                                DialogResult dResult = MetroMessageBox.Show(this, "Thí sinh đang được tạm dừng! Vui lòng chờ.", Properties.Resources.MSG_MESS_0001, MessageBoxButtons.OK, MessageBoxIcon.Error, 100);
                                this.Close();
                            }
                            else if (rEC.ErrorCode == Constant.STATUS_DUPLICATE_NAMECOMPUTER)
                            {
                                DialogResult dResult = MetroMessageBox.Show(this, "Bạn đăng nhập sai phòng thi", Properties.Resources.MSG_MESS_0001, MessageBoxButtons.OK, MessageBoxIcon.Error, 100);
                                if (dResult.Equals(DialogResult.OK))
                                {
                                    HandleEnableControl(true);
                                    loginCount--;
                                    txtUsername.Focus();
                                    //LogError.Instance.WriteLogError(Properties.Resources.MSG_LOG_ERROR, "AUTHENTICATION | LOGINFAIL", string.Format(Properties.Resources.MSG_MESS_0037, 3 - loginCount));
                                    logError.Add(string.Format(Properties.Resources.MSG_MESS_0037, 3 - loginCount));
                                }

                            }
                            else if (rEC.ErrorCode == Constant.STATUS_DOING)
                            {
                                DialogResult dResult = MetroMessageBox.Show(this, "Thí Sinh đang thi", Properties.Resources.MSG_MESS_0001, MessageBoxButtons.OK, MessageBoxIcon.Error, 100);
                                if (dResult.Equals(DialogResult.OK))
                                {
                                    HandleEnableControl(true);
                                    loginCount--;
                                    txtUsername.Focus();
                                    //LogError.Instance.WriteLogError(Properties.Resources.MSG_LOG_ERROR, "AUTHENTICATION | LOGINFAIL", string.Format(Properties.Resources.MSG_MESS_0037, 3 - loginCount));
                                    logError.Add(string.Format(Properties.Resources.MSG_MESS_0037, 3 - loginCount));
                                }
                            }
                            else if (rEC.ErrorCode == Constant.STATUS_FINISHED)
                            {
                                DialogResult dResult = MetroMessageBox.Show(this, "Thí sinh đã hoàn thành thi, vui lòng đợi hết thời gian.", Properties.Resources.MSG_MESS_0001, MessageBoxButtons.OK, MessageBoxIcon.Information, 100);
                                if (dResult.Equals(DialogResult.OK))
                                {
                                    HandleEnableControl(true);
                                    loginCount--;
                                    txtUsername.Focus();
                                    //LogError.Instance.WriteLogError(Properties.Resources.MSG_LOG_ERROR, "AUTHENTICATION | LOGINFAIL", string.Format(Properties.Resources.MSG_MESS_0037, 3 - loginCount));
                                    logError.Add(string.Format(Properties.Resources.MSG_MESS_0037, 3 - loginCount));
                                }
                            }
                            else if (rEC.ErrorCode == Constant.STATUS_EXIST_CONTESTANT)
                            {
                                DialogResult dResult = MetroMessageBox.Show(this, "Đã có thí sinh ngồi vị trí này!", Properties.Resources.MSG_MESS_0001, MessageBoxButtons.OK, MessageBoxIcon.Error, 100);
                                if (dResult.Equals(DialogResult.OK))
                                {
                                    HandleEnableControl(true);
                                    loginCount--;
                                    txtUsername.Focus();
                                    //LogError.Instance.WriteLogError(Properties.Resources.MSG_LOG_ERROR, "AUTHENTICATION | LOGINFAIL", string.Format(Properties.Resources.MSG_MESS_0037, 3 - loginCount));
                                    logError.Add(string.Format(Properties.Resources.MSG_MESS_0037, 3 - loginCount));
                                }
                            }
                            else
                            {
                                DialogResult dResult = MetroMessageBox.Show(this, rEC.ErrorCode == Constant.STATUS_WRONG_COMPUTTER ? Properties.Resources.MSG_GUI_0052 : string.Format(Properties.Resources.MSG_GUI_0044, HandleGetStringLoginType(LoginType)), Properties.Resources.MSG_MESS_0001, MessageBoxButtons.OK, MessageBoxIcon.Error, 100);
                                if (dResult.Equals(DialogResult.OK))
                                {
                                    HandleEnableControl(true);
                                    loginCount--;
                                    txtUsername.Focus();
                                    //LogError.Instance.WriteLogError(Properties.Resources.MSG_LOG_ERROR, "AUTHENTICATION | LOGINFAIL", string.Format(Properties.Resources.MSG_MESS_0037, 3 - loginCount));
                                    logError.Add(string.Format(Properties.Resources.MSG_MESS_0037, 3 - loginCount));
                                }
                            }
                            }
                                //}
                        }
                }
                if(timeError != 0)
                {
                    logError.Add(Properties.Resources.MSG_MESS_0009);
                }
                else
                {
                    Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "AUTHENTICATION", Properties.Resources.MSG_MESS_0009);
                }
            }
            catch (SqlException ex)
            {
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, "AUTHENTICATION | SERVER_ERROR | " + ex.Message);
                ShowServerErrorAndRestoreLogin();
            }
            catch (InvalidOperationException ex)
            {
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, "AUTHENTICATION | CONNECTION_ERROR | " + ex.Message);
                ShowServerErrorAndRestoreLogin();
            }
            catch (Exception ex)
            {
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, "AUTHENTICATION | UNHANDLED_ERROR | " + ex.Message);
                ShowGenericErrorAndRestoreLogin();
            }
            finally
            {
                if (!keepConnectionForMainForm)
                {
                    SafeCloseConnection(sql);
                    SafeSetLogFileAttributes(FileAttributes.Normal);
                }
            }

        }

        private void MetroRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            MetroRadioButton mrb = sender as MetroRadioButton;
            switch (mrb.Name)
            {
                case "mrbtnContestantCode":
                    LoginType = Constant.LOGIN_WITH_CONTESTANT_CODE;
                    break;

                //case "mrbtnIdentityCardName":
                //    LoginType = Constant.LOGIN_WITH_IDENTITY_CARD_NAME;
                //    break;

                //case "mrbtnStudentCode":
                //    LoginType = Constant.LOGIN_WITH_STUDENT_CODE;
                //    break;
            }
            txtUsername.Clear();
            txtUsername.Focus();
        }

        private string HandleGetStringLoginType(int loginType)
        {
            switch (loginType)
            {
                // LOGIN_WITH_CONTESTANT_CODE
                case 5000:
                    return Properties.Resources.MSG_GUI_0041;
                // LOGIN_WITH_STUDENT_CODE
                case 5001:
                    return Properties.Resources.MSG_GUI_0042;
                // LOGIN_WITH_IDENTITY_CARD_NAME
                case 5002:
                    return Properties.Resources.MSG_GUI_0043;

                default:
                    return string.Empty;
            }
        }

        private void HandleEnableControl(bool flag)
        {
            mrbtnContestantCode.Enabled =flag;
            pAuthen.Enabled = flag;
        }



        private void ClosingSocket()
        {
            Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_INFO, Properties.Resources.MSG_CON_0002, this);
            if (DTO.NotificationForm.DialogResult == DialogResult.OK)
            {
                this.Close();
                Application.Exit();
            }
        }

        private void frmAuthentication_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.ExitThread();
            Environment.Exit(0);
        }

        private void txtUsername_Click(object sender, EventArgs e)
        {
        }
    }
}
