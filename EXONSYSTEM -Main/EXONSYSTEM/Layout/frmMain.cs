using UserHelper;
using BUS;
using DAO.DataProvider;
using EXONSYSTEM.Common;
using EXONSYSTEM.Layout;
using MetroFramework;
using MetroFramework.Controls;
using MetroFramework.Drawing.Html;
using System;
using System.Collections.Generic;
using System.Data;

using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using System.Media;
using System.IO;
using EXONSYSTEM.Controls;
using MetroFramework.Forms;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Configuration;
namespace EXONSYSTEM
{
    public partial class frmMainForm : Form
    {
        #region Declare variables

        const string sound = "*.mp3 || *.wav";
        //ISoundEngine engine = new ISoundEngine();
        //ISound currentlyPlayingSound;
        string conn = ConfigurationManager.ConnectionStrings["EXON_SYSTEM_TESTEntities"].ConnectionString;
        private ContestantInformation CILogged;
        private Contest CLogged;

        private List<List<Questions>> lstLQuestion;
        private static List<ObjControl> lstObjControl;
        private static List<string> lstbtnQuestions;
        private static List<Control> lstControlQuestions;
        private static List<Control> lstControlBtnQuestions;
        private static SqlConnection Sql;
        SendWorking s;
        private List<PartOfTest> lstPartOfTest;
        //    private int StatusLogged;
        private int ContestantID;
        public delegate void SendQuestion(Questions q, int AnswerSheetID);
        public delegate void SendUCRowInfor(string strTitle, string strContent);
        public delegate void SendWorking(bool isProgress);
        private FlowLayoutPanel flpnListOfQuestions2 = new FlowLayoutPanel();
        private int PreSubQuestionID = Constant.STATUS_NORMAL; // Câu hõi đã được chọn trước.
        private bool IsContinute = false; // True: Nếu thí sinh gặp sự cố và thi tiếp || False: Thí sinh thi mới
        private int IsLoadingTest = Constant.STATUS_NORMAL; // True: Bắt đầu tải đề thi || False: Ngược lại
        private int WarningCount = 0; // Số lần bị cảnh cáo
        private UserHelper.UserTransformation UT;
        private int currentStatusContestant; // Trạng
        private bool checkGenBtnSubmit = false;
        private Answersheet rASH;
        // string _connectionString;
        private int maxTime = -1;
        private int ThoiGianBu = 0;
        private bool StartTest = false;// True: bắt đầu đếm ngược thời gian làm bài || False: ngược lại
                                       //private bool _isopenedQuestion = false;
        private Timer timerGetMaxTime;
        private Timer TimerSubmit;

        public int NumberOfOvertime = 0; //Số lần bù giờ cho thí sinh
        //Label display time saved writing question
        private Label lblTimeSavedWritingQuestion;

        // Fallback thời gian khi mất kết nối mạng
        // _serverTimeAtStart: giờ server tại thời điểm bật phần mềm (synced 1 lần)
        // _stopwatch: đếm thời gian đã trôi qua từ lúc sync
        private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        private DateTime _serverTimeAtStart = DateTime.MinValue;
        // True khi đồng hồ đếm ngược về 0 (hết giờ tự động)
        // Lúc đó thời gian làm bài = đúng bằng thời gian thi (TimeOfTest)
        private bool _isTimedOut = false;
        private string _currentSessionFolder;
        private string _currentSegmentGuid;
        private bool _hasShownServerDisconnectMessage;
        #endregion
        public frmMainForm()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            lstControlQuestions = new List<Control>();
            lstControlBtnQuestions = new List<Control>();
            flpnListOfButtonQuestions.Width = Constant.WIDTH_PANEL_INFORMATION;
            DTO.ViewRtfForm = new frmViewRtf();
            DTO.ViewTextControlFrom = new frmViewTextControl();

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

        }
        /// <summary>
        /// Xử lý dữ liệu từ form Authen
        /// </summary>
        public void HandleInformationFromAuthenForm(ContestantInformation CI, Contest C, SqlConnection sql)
        {

            Sql = sql;
            CILogged = CI;
            ExamAnswerHistoryStore.InitializeSession(CILogged.ContestantShiftID, CILogged.IsNewStarted || CILogged.Status == Constant.STATUS_LOGGED);

            CLogged = C;
            maxTime = 3600;
            WarningCount = CILogged.Warning;
            currentStatusContestant = CILogged.Status;
            ContestantID = CILogged.ContestantID;

            // Khởi tạo đồng hồ dự phòng: lấy giờ server 1 lần rồi đếm bằng Stopwatch
            // Nếu mất mạng sau đó, vẫn tính được thời gian chính xác
            try
            {
                _serverTimeAtStart = DAO.DAO.ConvertDateTime.GetDateTimeServer();
                _stopwatch.Restart();
                
            }
            catch
            {
                _serverTimeAtStart = DateTime.Now;
                _stopwatch.Restart();
                ShowRuntimeServerMessage("Mất kết nối server. Phần mềm sẽ dùng thời gian dự phòng, vui lòng liên hệ giám thị nếu sự cố kéo dài.");
            }

            AnswerSelectionLogger.Init(_serverTimeAtStart);
            // Nếu thí sinh đăng nhập lại sau gián đoạn → giữ log cũ, ghi RECONNECT
            // Nếu đăng nhập mới → xóa log cũ, ghi LOGIN
            bool isReconnect = CI.Status == Constant.STATUS_DOING_BUT_INTERRUPT;
            if (!isReconnect)
            {
                ConnectionLogger.Clear();
            }
            ConnectionLogger.Log(
                isReconnect ? "RECONNECT" : "LOGIN",
                string.Format("ContestantCode={0} ShiftID={1} Computer={2}", CI.ContestantCode, CI.ContestantShiftID, System.Net.Dns.GetHostName()));
            EnsureCurrentSessionFolderInitialized();
            WriteSessionInfoFile();
            WriteRuntimeStateFile();

            // Khởi tạo socket
            UT = new UserHelper.UserTransformation();
            UT.ComputerName = Dns.GetHostName();
            UT.UserCode = CILogged.ContestantCode;
            UT.UserID = CILogged.ContestantID;
            UT.ContestShiftID = CILogged.ContestantShiftID;

            // Render giao diện
            HandlePanelWelcome();
            HandlePanelInformation();
            HandlePanelListOfQuestions();
            HandleVisibleWelcome(true);
        }
        #region Handle Style panels
        /// <summary>
        /// Xử lý giao diện chờ ca thi tiếp theo
        /// </summary>

        #region Panel Welcome
        /// <summary>
        /// Xử lý giao diện Welcome
        /// </summary>
        private void HandlePanelWelcome()
        {
            MetroPanel mpnWelcome = new MetroPanel();
            mpnWelcome.Name = "mpnWelcome";
            Controllers.Instance.SetCanChangeMetroPanelColor(mpnWelcome);
            mpnWelcome.Location = new Point(0, 0);
            mpnWelcome.Width = Constant.WIDTH_SCREEN;
            mpnWelcome.Height = Constant.HEIGHT_SCREEN;
            mpnWelcome.BackColor = Constant.BACKCOLOR_PANEL_WELCOME;
            mpnWelcome.BringToFront();
            this.Controls.Add(mpnWelcome);

            MetroPanel mpnWelcomeWraper = new MetroPanel();
            Controllers.Instance.SetCanChangeMetroPanelColor(mpnWelcomeWraper);
            mpnWelcomeWraper.BackColor = Constant.COLOR_TRANSPARENT;
            mpnWelcome.Controls.Add(mpnWelcomeWraper);
            mpnWelcomeWraper.AutoSize = true;

            MetroPanel mpnWelcomeContent = new MetroPanel();
            Controllers.Instance.SetCanChangeMetroPanelColor(mpnWelcomeContent);
            mpnWelcomeContent.BackColor = Constant.COLOR_TRANSPARENT;
            mpnWelcomeWraper.Controls.Add(mpnWelcomeContent);
            mpnWelcomeContent.AutoSize = true;

            //Tên kỳ thi
            Label lbContestName = new Label();
            lbContestName.AutoSize = true;
            lbContestName.Text = CLogged.ContestName.ToUpper();
            lbContestName.Font = new Font(Constant.FONT_FAMILY_DEFAULT, 30, FontStyle.Bold);
            lbContestName.BackColor = Constant.COLOR_TRANSPARENT;
            lbContestName.ForeColor = Constant.FORCECOLOR_LABEL_CONTEST_NAME;
            mpnWelcomeWraper.Controls.Add(lbContestName);
            mpnWelcomeWraper.Width = lbContestName.Width;

            // Ngày diễn ra kỳ thi
            ucRowInfor ucContestDate = new ucRowInfor();
            SendUCRowInfor sucContestDate = new SendUCRowInfor(ucContestDate.HandleUC1);
            sucContestDate(Properties.Resources.MSG_GUI_0007, Controllers.Instance.ConvertUnixToDateTime(CLogged.ShiftDate).ToString(Constant.FORMAT_DATE_DEFAULT));
            mpnWelcomeContent.Controls.Add(ucContestDate);

             // Ca thi
             ucRowInfor ucContestShift = new ucRowInfor();
            SendUCRowInfor sucContestShift = new SendUCRowInfor(ucContestShift.HandleUC1);
            sucContestShift(Properties.Resources.MSG_GUI_0049, string.Format("{0} - {1}", Controllers.Instance.ConvertUnixToDateTime(CLogged.StartTime).ToString(Constant.FORMAT_TIME_DEFAULT), Controllers.Instance.ConvertUnixToDateTime(CLogged.EndTime).ToString(Constant.FORMAT_TIME_DEFAULT)));
            ucContestShift.Location = new Point(0, ucContestDate.Bottom);
            mpnWelcomeContent.Controls.Add(ucContestShift);

            // Tên môn thi
            ucRowInfor ucContestSubject = new ucRowInfor();
            SendUCRowInfor sucContestSubject = new SendUCRowInfor(ucContestSubject.HandleUC1);
            sucContestSubject(Properties.Resources.MSG_GUI_0008, CILogged.SubjectName);
            mpnWelcomeContent.Controls.Add(ucContestSubject);
            ucContestSubject.Location = new Point(0, ucContestShift.Bottom);


            // Thời gian thi
            ucRowInfor ucDuration = new ucRowInfor();
            SendUCRowInfor sucDuration = new SendUCRowInfor(ucDuration.HandleUC1);
            sucDuration(Properties.Resources.MSG_GUI_0014, string.Format(Properties.Resources.MSG_GUI_0048, Controllers.Instance.HandleDuration(CILogged.TimeOfTest)));
            mpnWelcomeContent.Controls.Add(ucDuration);
            ucDuration.Location = new Point(0, ucContestSubject.Bottom);

            // Tên phòng thi
            ucRowInfor ucContestRoom = new ucRowInfor();
            SendUCRowInfor sucContestRoom = new SendUCRowInfor(ucContestRoom.HandleUC1);
            sucContestRoom(Properties.Resources.MSG_GUI_0036, CLogged.RoomName);
            ucContestRoom.Location = new Point(0, ucDuration.Bottom);
            mpnWelcomeContent.Controls.Add(ucContestRoom);

            // Tên máy thi
            ucRowInfor ucContestComputer = new ucRowInfor();
            SendUCRowInfor sucContestComputer = new SendUCRowInfor(ucContestComputer.HandleUC1);
            sucContestComputer(Properties.Resources.MSG_GUI_0037, Dns.GetHostName());
            ucContestComputer.Location = new Point(0, ucContestRoom.Bottom);
            mpnWelcomeContent.Controls.Add(ucContestComputer);

            // Họ tên thí sinh
            ucRowInfor ucContestantName = new ucRowInfor();
            SendUCRowInfor sucContestantName = new SendUCRowInfor(ucContestantName.HandleUC1);
            sucContestantName(Properties.Resources.MSG_GUI_0001, Controllers.Instance.CapitalizeString(CILogged.Fullname));
            mpnWelcomeContent.Controls.Add(ucContestantName);
            ucContestantName.Location = new Point(0, ucContestComputer.Bottom + 20);

            // Số báo danh
            ucRowInfor ucContestantCode = new ucRowInfor();
            SendUCRowInfor sucContestantGender = new SendUCRowInfor(ucContestantCode.HandleUC1);
            sucContestantGender(Properties.Resources.MSG_GUI_0041, CILogged.ContestantCode);
            mpnWelcomeContent.Controls.Add(ucContestantCode);
            ucContestantCode.Location = new Point(0, ucContestantName.Bottom);

            // Ngày sinh
            ucRowInfor ucDOB = new ucRowInfor();
            SendUCRowInfor sucDOB = new SendUCRowInfor(ucDOB.HandleUC1);
            sucDOB(Properties.Resources.MSG_GUI_0002, Controllers.Instance.ConvertUnixToDateTime(CILogged.DOB).ToString(Constant.FORMAT_DATE_DEFAULT));
            mpnWelcomeContent.Controls.Add(ucDOB);
            ucDOB.Location = new Point(0, ucContestantCode.Bottom);

            // Số CMND
            //ucRowInfor ucStudentIdentify = new ucRowInfor();
            //SendUCRowInfor sucStudentIdentify = new SendUCRowInfor(ucStudentIdentify.HandleUC1);
            //sucStudentIdentify(Properties.Resources.MSG_GUI_0003, CILogged.IdentityCardName);
            //mpnWelcomeContent.Controls.Add(ucStudentIdentify);
            //ucStudentIdentify.Location = new Point(0, ucDOB.Bottom);

            // Số lần gián đoán
            //ucRowInfor ucAddTime = new ucRowInfor();
            //SendUCRowInfor sucAddTime = new SendUCRowInfor(ucAddTime.HandleUC1);
            //string AddTime = Controllers.Instance.HandleCountDown(CILogged.ThoiGianBu);
            //sucAddTime(Properties.Resources.MSG_GUI_0065, AddTime + " Phút");
            //mpnWelcomeContent.Controls.Add(ucAddTime);
            //ucAddTime.Location = new Point(0, ucStudentIdentify.Bottom);

            // Buttom cancel
            MetroButton mbtnCancel = new MetroButton();
            Controllers.Instance.SetCanChangeMetroButtonColor(mbtnCancel);
            mbtnCancel.Text = Properties.Resources.MSG_GUI_0070;
            mbtnCancel.BackColor = Constant.COLOR_WHITE;
            mbtnCancel.ForeColor = Constant.FORCECOLOR_BUTTON_CONFIRM;
            mbtnCancel.Size = new Size(130, 40);



            mbtnCancel.Click += new EventHandler(Close_Click);
            mpnWelcomeContent.Controls.Add(mbtnCancel);
            mbtnCancel.Location = new Point(170, ucDOB.Bottom + 30);
            this.AcceptButton = mbtnCancel;

            //Button confirm
            MetroButton mbtnConfirm = new MetroButton();
            Controllers.Instance.SetCanChangeMetroButtonColor(mbtnConfirm);
            mbtnConfirm.Text = Properties.Resources.MSG_GUI_0015;
            mbtnConfirm.BackColor = Constant.COLOR_WHITE;
            mbtnConfirm.ForeColor = Constant.FORCECOLOR_BUTTON_CONFIRM;
            mbtnConfirm.Size = new Size(130, 40);

            mbtnConfirm.Click += new System.EventHandler(this.mbtnConfirm_Click);
            mpnWelcomeContent.Controls.Add(mbtnConfirm);
            mbtnConfirm.Location = new Point(0, ucDOB.Bottom + 30);
            this.AcceptButton = mbtnConfirm;

            

            int widthWrapper = mpnWelcomeWraper.Width > mpnWelcomeContent.Width ? mpnWelcomeWraper.Width : mpnWelcomeContent.Width;
            lbContestName.Location = new Point(Convert.ToInt32((widthWrapper - lbContestName.Width) / 2));
            if (lbContestName.Width < mpnWelcomeContent.Width)
            {
                mpnWelcomeContent.Location = new Point(0, lbContestName.Bottom + 50);
            }
            else
            {
                mpnWelcomeContent.Location = new Point(Convert.ToInt32((widthWrapper - mpnWelcomeContent.Width) / 2), lbContestName.Bottom + 50);
            }
            mpnWelcomeWraper.Location = new Point(Convert.ToInt32((Constant.WIDTH_SCREEN - widthWrapper) / 2), Convert.ToInt32((Constant.HEIGHT_SCREEN - mpnWelcomeWraper.Height) / 2));
        }
        private void Close_Click(object sender, EventArgs e)
        {
            
            ErrorController rEC = new ErrorController();
            // Change status contestant to STATUS_FINISHED
            CILogged.Status = Constant.STATUS_LOGGED_DO_NOT_FINISH;
            //CILogged.IsDisconnected = true;

            ContestantBUS.Instance.ChangeStatusContestant(CILogged, CLogged, out rEC, Sql);
            this.Close(); //all your choice to close it or remove this line
        }
        private void mbtnConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(frmAuthentication.logFile) && File.Exists(frmAuthentication.logFile))
                {
                    File.SetAttributes(frmAuthentication.logFile, FileAttributes.Normal);
                }
            }
            catch
            {
            }

            //Lấy thông tin cho vào chuỗi Json
            UserLoginComputerDifferent ULCD = new UserLoginComputerDifferent();
            ULCD.ContestantCode = CILogged.ContestantCode;
            ULCD.ContestantName = CILogged.Fullname;
            ULCD.ContestShift = string.Format("{0} - {1}", Controllers.Instance.ConvertUnixToDateTime(CLogged.StartTime).ToString(Constant.FORMAT_TIME_DEFAULT), Controllers.Instance.ConvertUnixToDateTime(CLogged.EndTime).ToString(Constant.FORMAT_TIME_DEFAULT));
            ULCD.ContestSubject = CILogged.SubjectName;
            ULCD.RoomTest = CLogged.RoomName;
            ULCD.TimeChange = Controllers.Instance.ConvertDateTimeToUnix(GetCurrentTimeWithFallback());
            ULCD.JsonDescription = Ultis.FromObjectToJSON3(ULCD);

            ContestantBUS.Instance.ChangeContestantCode(ULCD, CILogged, Sql);
            NumberOfOvertime = ContestantBUS.Instance.GetNumberOfOvertime(CILogged, Sql);

            // kill process nghe
            foreach (Process process in Process.GetProcessesByName("WMPLib"))
            {

                process.Kill();

            }
            // Nếu là thí sinh đăng nhập lần đầu sẽ phải đợi hiệu lệnh của giám thị mới được thi
            // Nếu là thí sinh đang thi nhưng đang gặp sự cố sẽ được vào làm bài luôn.
            // Trạng thái ca thi

            int statusDivisionShift = ContestBUS.Instance.GetStatusDivisionShift(CILogged.DivisionShiftID, Sql);
            if (CILogged.Status == Constant.STATUS_LOGGED || statusDivisionShift != Constant.STATUS_START_TEST)
            {
                // Gửi tin nhắn thông báo rằng sẵn sàng nhận đề bài
                //UT.Status = Constant.STATUS_READY_TO_GET_TEST;
                //UT.Content = "READY TO GET TEST";

                ErrorController rEC = new ErrorController();
                CILogged.Status = Constant.STATUS_READY_TO_GET_TEST;
                ContestantBUS.Instance.ChangeStatusContestant(CILogged, CLogged, out rEC, Sql);
                if (rEC.ErrorCode == Constant.STATUS_OK)
                {
                    Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | CHANGE_STATUS_CONTESTANT_RADIO_PERFORMCLICK | STATUS_READY | ChangeStatusContestant", Controllers.Instance.HandleStringErrorController(rEC));
                }
                else
                {

                }

                HandleVisibleWelcome(false);
                HandleContentPanelLoading();
                this.Update();
                timerReadyToLoadTest.Start();
                timerReadyToLoadTest.Tick += TimerReadyToLoadTest_Tick;
                // return;
            }

            //trang thái lúc thí sinh tắt chương trình hoặc trong quá trình thi bị tắt điện hoặc là trường hợp chưa hoàn thành muốn xem lại bài viết
            else if (CILogged.Status == Constant.STATUS_LOGGED_DO_NOT_FINISH || CILogged.Status == Constant.STATUS_DOING_BUT_INTERRUPT || CILogged.Status == Constant.STATUS_FINISHED /*|| statusDivisionShift == Constant.STATUS_START_TEST*/)
            {
                HandleVisibleWelcome(false);
                // trường hợp login lúc chưa hoàn thành bài thi cho phép xem lại 
                if (CILogged.Status == Constant.STATUS_DOING_BUT_INTERRUPT || CILogged.Status == Constant.STATUS_FINISHED)
                {
                    timeCountDown.Stop();
                    GetPanelLoading().Visible = false;
                    GetPanelInformationWrapper().Visible = false;
                    currentStatusContestant = Constant.STATUS_LOGGED_DO_NOT_FINISH;
                    this.Update();
                    GernerateObjControl();
                    HandleGenerateButtonSubmitForComplete();

                    //flpnListOfQuestions.Visible = true;
                    foreach (Control c in flpnListOfQuestions.Controls)
                    {
                        if (c.GetType().ToString().EndsWith(Constant.STRING_FLOWLAYOUTPANEL))
                        {
                            c.Enabled = false;
                        }

                    }

                }
                else
                {
                    this.Update();
                    GernerateObjControl();
                    StartTest = true;
                    HandleStartDoingTest();
                    TimerSubmit = new Timer();
                    TimerSubmit.Interval = 5132;
                    TimerSubmit.Tick += new EventHandler(TimerSubmit_Tick);
                    TimerSubmit.Start();
                }


                //  return;
            }


        }

        private void TimerReadyToLoadTest_Tick(object sender, EventArgs e)
        {

            int status = ContestBUS.Instance.GetStatusDivisionShift(CILogged.DivisionShiftID, Sql);
            if (status == Constant.STATUS_DIVISION_TEST)
            {

                timerReadyToLoadTest.Stop();
                GernerateObjControl();
                Panel pnTimerMask = (Panel)pnInformation.Controls["pnTimerMask"];
                pnTimerMask.Visible = false;
                lbTimer.ForeColor = Constant.FORCECOLOR_LABEL_TIMER;
                lbTimer.Text = Controllers.Instance.HandleCountDown(CILogged.TimeOfTest);
                timerReadyToTest.Start();
                timerReadyToTest.Tick += TimerReadyToTest_Tick;
            }
            else if (status == Constant.STATUS_START_TEST)
            {
                timerReadyToLoadTest.Stop();
                GernerateObjControl();
                HandleStartDoingTest();
                StartTest = true;
                TimerSubmit = new Timer();
                TimerSubmit.Interval = 5132;
                TimerSubmit.Tick += new EventHandler(TimerSubmit_Tick);
                TimerSubmit.Start();

                //  IsLoadingTest = Constant.WAITING_BY_LOAD_TEST;

            }
            else if (status == -1)
            {
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, "Có lỗi lúc timer chờ lệnh phát đề giám sát");
                timerReadyToLoadTest.Stop();
                ShowRuntimeServerMessage("Mất kết nối server, vui lòng thử lại hoặc liên hệ giám thị.");
            }
        }

        private void TimerReadyToTest_Tick(object sender, EventArgs e)
        {
            int status = ContestBUS.Instance.GetStatusDivisionShift(CILogged.DivisionShiftID, Sql);
            if (status == Constant.STATUS_START_TEST)
            {
                HandleStartDoingTest();
                StartTest = true;
                timerReadyToTest.Stop();
                TimerSubmit = new Timer();
                TimerSubmit.Interval = 5132;
                TimerSubmit.Tick += new EventHandler(TimerSubmit_Tick);
                TimerSubmit.Start();
            }
            else if (status == -1)
            {
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_FATAL, "Có lỗi lúc timer chờ lệnh phát đề giám sát");
                timerReadyToTest.Stop();
                ShowRuntimeServerMessage("Mất kết nối server, vui lòng thử lại hoặc liên hệ giám thị.");
            }
        }

        private void TimerSubmit_Tick(object sender, EventArgs e)
        {
            try
            {
                HandlePushAnswerSheet();
                int TimeNow = Controllers.Instance.ConvertDateTimeToUnix(DAO.DAO.ConvertDateTime.GetDateTimeServer());
                BUS.ContestantBUS.Instance.UpdateLastimeConnect(CILogged.ContestantShiftID, TimeNow, Sql);

                //Nếu thực hiện bù giờ cho thí sinh:
                if (NumberOfOvertime != BUS.ContestantBUS.Instance.GetNumberOfOvertime(CILogged, Sql))
                {

                    ThoiGianBu = BUS.ContestantBUS.Instance.GetThoiGianBu(CILogged.ContestantShiftID);
                    NumberOfOvertime += 1; //Phải tăng lên 1 nếu ko nó sẽ tự động bù giờ liên tục
                                           //Panel pnTimerMask = (Panel)pnInformation.Controls["pnTimerMask"];
                    int TimeStart = BUS.ContestantBUS.Instance.GetTimeStartFromContestant(CILogged.ContestantShiftID);
                    TimeNow = Controllers.Instance.ConvertDateTimeToUnix(DAO.DAO.ConvertDateTime.GetDateTimeServer());
                    //   this.maxTime = currentStatusContestant == Constant.STATUS_LOGGED_DO_NOT_FINISH ? CILogged.TimeRemained : CILogged.TimeOfTest;

                    /// 1p Lấy thời gian bài làm từ server một lần
                    //timerGetMaxTime = new Timer();
                    //timerGetMaxTime.Interval = 62122;
                    //timerGetMaxTime.Interval = 100;
                    //timerGetMaxTime.Start();
                    //timerGetMaxTime.Tick += TimerGetMaxTime_Tick;
                    this.maxTime = CILogged.TimeOfTest - (TimeNow - TimeStart) + ThoiGianBu;
                    //pnTimerMask.Visible = false;
                    //lbTimer.ForeColor = Constant.FORCECOLOR_LABEL_TIMER;
                    //lbTimer.Text = Controllers.Instance.HandleCountDown(maxTime);
                    lbTimer.Text = Controllers.Instance.HandleCountDown(maxTime);
                    lbTimer.Update();
                }
            }
            catch
            {
                HandleRuntimeServerDisconnect();
            }
        }

        private MetroPanel GetMpnWelcome()
        {
            return (MetroPanel)this.Controls["mpnWelcome"];
        }
        private void HandleVisibleWelcome(bool flag)
        {
            GetMpnWelcome().Visible = flag;
            flpnListOfQuestions.Visible = pnInformation.Visible = GetPanelLoading().Visible = !flag;
        }
        private void HandleGetTestInformation()
        {
            //Debug.WriteLine(DateTime.Now + "Start");
            ErrorController rEC = new ErrorController();
            this.Cursor = Cursors.WaitCursor;
            if (currentStatusContestant == Constant.STATUS_LOGGED)
            {
                // Lấy TestID + ContestantTestID
                ContestantBUS.Instance.GetContestantTestInformation(CILogged, out CILogged, out rEC);
                if (rEC.ErrorCode == Constant.STATUS_OK)
                {
                    // Thêm mới 1 bài thi (AnswerSheet)
                    rASH = new Answersheet();
                    rASH.ContestantTestID = CILogged.ContestantTestID;
                    AnswersheetBUS.Instance.PushAnswerSheet(rASH, out rEC, Sql);
                    if (rEC.ErrorCode == Constant.STATUS_OK)
                    {
                        Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | PUSHANSWERSHEET | STATUS_LOGGED | HandleGetTestInformation | PushAnswerSheet", Controllers.Instance.HandleStringErrorController(rEC));
                        // Lấy mã bài thi (AnswerSheet)
                        AnswersheetBUS.Instance.GetAnswerSheetByContestantID(CILogged, out rASH, out rEC);
                        if (rEC.ErrorCode == Constant.STATUS_OK)
                        {
                            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | GETANSWERSHEETBYCONTESTANT | STATUS_LOGGED | HandleGetTestInformation | GetAnswerSheetByContestantID", Controllers.Instance.HandleStringErrorController(rEC));
                            CILogged.AnswerSheetID = rASH.AnswerSheetID;
                            // Lấy danh sách câu hỏi
                            int NumberQuestionsOfTest = 0;
                            TestBUS.Instance.GetListLQuestionByContestantInformation(CILogged, out lstLQuestion, out lstPartOfTest, out IsContinute, out NumberQuestionsOfTest, out rEC, Sql);
                            if (rEC.ErrorCode == Constant.STATUS_OK)
                            {
                                CLogged.NumberQuestionOfTest = NumberQuestionsOfTest;
                                Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | GETLISTLQUESTIONBYCONTESTANT | STATUS_LOGGED | HandleGetTestInformation | GetListLQuestionByContestantInformation", Controllers.Instance.HandleStringErrorController(rEC));
                            }
                            else
                            {
                                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_FATAL, Controllers.Instance.HandleStringErrorController(rEC));
                                Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), this);
                                Controllers.Instance.ExitApplicationFromNotificationForm(this);
                            }
                        }
                        else
                        {
                            Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_FATAL, Controllers.Instance.HandleStringErrorController(rEC));
                            Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), this);
                            Controllers.Instance.ExitApplicationFromNotificationForm(this);
                        }
                    }
                    else
                    {
                        Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_FATAL, Controllers.Instance.HandleStringErrorController(rEC));
                        Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), this);
                        Controllers.Instance.ExitApplicationFromNotificationForm(this);
                    }
                }
                else
                {
                    Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_FATAL, Controllers.Instance.HandleStringErrorController(rEC));
                    Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), this);
                    Controllers.Instance.ExitApplicationFromNotificationForm(this);
                    this.Cursor = Cursors.Arrow;
                    return;
                }
            }
            else if (currentStatusContestant == Constant.STATUS_LOGGED_DO_NOT_FINISH)
            {
                rASH = new Answersheet();
                // Lấy mã bài thi (AnswerSheet)
                AnswersheetBUS.Instance.GetAnswerSheetByContestantID(CILogged, out rASH, out rEC);
                if (rEC.ErrorCode != Constant.STATUS_OK)
                {
                    // Thêm mới 1 bài thi (AnswerSheet)
                    rASH = new Answersheet();
                    rASH.ContestantTestID = CILogged.ContestantTestID;
                    AnswersheetBUS.Instance.PushAnswerSheet(rASH, out rEC, Sql);
                    AnswersheetBUS.Instance.GetAnswerSheetByContestantID(CILogged, out rASH, out rEC);
                }
                if (rEC.ErrorCode == Constant.STATUS_OK)
                {
                    Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | GETANSWERSHEETBYCONTESTANT | STATUS_LOGGED_DO_NOT_FINISH | HandleGetTestInformation | GetAnswerSheetByContestantID", Controllers.Instance.HandleStringErrorController(rEC));
                    CILogged.AnswerSheetID = rASH.AnswerSheetID;
                    // Lấy danh sách câu hỏi
                    int NumberQuestionsOfTest = 0;
                    TestBUS.Instance.GetListLQuestionByContestantInformation(CILogged, out lstLQuestion, out lstPartOfTest, out IsContinute, out NumberQuestionsOfTest, out rEC, Sql);
                    if (rEC.ErrorCode == Constant.STATUS_OK)
                    {
                        CLogged.NumberQuestionOfTest = NumberQuestionsOfTest;
                        Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | GETLISTLQUESTIONBYCONTESTANT | STATUS_LOGGED_DO_NOT_FINISH | HandleGetTestInformation | GetAnswerSheetByContestantID", Controllers.Instance.HandleStringErrorController(rEC));
                    }
                    else
                    {
                        Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_FATAL, Controllers.Instance.HandleStringErrorController(rEC));
                        Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), this);
                        Controllers.Instance.ExitApplicationFromNotificationForm(this);
                    }
                }
                else
                {
                    Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_FATAL, Controllers.Instance.HandleStringErrorController(rEC));
                    Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), this);
                    Controllers.Instance.ExitApplicationFromNotificationForm(this);
                }
            }
            this.Cursor = Cursors.Arrow;
        }
        #endregion

        #region Information panel

        /// <summary>
        /// Giao diện thông tin kỳ thi
        /// </summary>
        private void HandlePanelInformation()
        {
            #region Panel Information
            pnInformation.Dock = DockStyle.Left;
            pnInformation.Location = new Point(0, 0);
            pnInformation.BackColor = Constant.BACKCOLOR_PANEL_INFORMATION;
            pnInformation.Width = Constant.WIDTH_PANEL_INFORMATION;
            pnInformation.Height = Constant.HEIGHT_SCREEN;

            Panel pnContestInformation = new Panel();
            pnContestInformation.Width = Constant.WIDTH_PANEL_INFORMATION;
            pnContestInformation.Location = new Point(0, 0);
            pnContestInformation.Name = "pnContestInformation";

            // Tên kỳ thi
            string s = CLogged.ContestName;
            List<string> lsts = s.Split(' ').ToList();
            string name = "";
            for (int i = 0; i < lsts.Count - 2; i++)
            {
                name += lsts[i] + " ";
            }
            Label lbContestName = new Label();
            lbContestName.AutoSize = true;

            lbContestName.Text = "KÌ THI " + name.ToUpper();
            lbContestName.TextAlign = ContentAlignment.MiddleCenter;

            lbContestName.Font = new Font(Constant.FONT_FAMILY_DEFAULT, 16, FontStyle.Bold);
            lbContestName.BackColor = Constant.COLOR_TRANSPARENT;
            lbContestName.ForeColor = Constant.BACKCOLOR_PANEL_WRAPPER_CONTENT;
            lbContestName.Location = new Point(40, 10);
            lbContestName.Width = pnContestInformation.Width;
            pnContestInformation.Controls.Add(lbContestName);

            //Lưu thời gian làm câu hỏi viết
            lblTimeSavedWritingQuestion = new Label();
            lblTimeSavedWritingQuestion.AutoSize = true;

            lblTimeSavedWritingQuestion.Text = "";
            lblTimeSavedWritingQuestion.TextAlign = ContentAlignment.MiddleCenter;

            lblTimeSavedWritingQuestion.Font = new Font(Constant.FONT_FAMILY_DEFAULT, 12, FontStyle.Bold);
            lblTimeSavedWritingQuestion.BackColor = Constant.COLOR_Yellow;
            lblTimeSavedWritingQuestion.ForeColor = Constant.BACKCOLOR_PANEL_WRAPPER_CONTENT;
            lblTimeSavedWritingQuestion.Location = new Point(lbContestName.Location.X - 30, lbContestName.Location.Y + 35);
            lblTimeSavedWritingQuestion.Width = pnContestInformation.Width;
            pnContestInformation.Controls.Add(lblTimeSavedWritingQuestion);


            // Môn thi
            Label lbContestSubject = new Label();
            lbContestSubject.AutoSize = true;
            lbContestSubject.Text = CLogged.Subject;
            lbContestSubject.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
            lbContestSubject.BackColor = Constant.COLOR_TRANSPARENT;
            lbContestSubject.ForeColor = Constant.BACKCOLOR_PANEL_WRAPPER_CONTENT;
            lbContestSubject.Location = new Point(5, lbContestName.Bottom + 5);
            pnContestInformation.Controls.Add(lbContestSubject);


            pnContestInformation.Height = lbContestSubject.Bottom + 20;
            pnInformation.Controls.Add(pnContestInformation);

            #region Timer
            // giao diện thời gian thi
            Panel pnTimerMask = new Panel();
            pnTimerMask.Name = "pnTimerMask";
            pnInformation.Controls.Add(pnTimerMask);

            lbTimer.Font = new Font(Constant.FONT_FAMILY_DEFAULT, 42, FontStyle.Bold);
            lbTimer.ForeColor = Constant.BACKCOLOR_PANEL_INFORMATION;
            lbTimer.TextAlign = ContentAlignment.MiddleCenter;
            lbTimer.AutoSize = true;
            lbTimer.Text = Controllers.Instance.HandleCountDown(CILogged.TimeOfTest);

            pnTimerMask.Size = lbTimer.Size;
            pnTimerMask.BringToFront();
            pnTimerMask.BackColor = Constant.BACKCOLOR_PANEL_INFORMATION;
            lbTimer.Location = new Point(Convert.ToInt32((Constant.WIDTH_PANEL_INFORMATION - lbTimer.Width) / 2), pnContestInformation.Bottom);
            pnTimerMask.Location = lbTimer.Location;
            timeCountDown.Start();
            #endregion

            HandleStylePanelController();
            #endregion
            //  HandleInitListenWarning();
        }



        private void HandleTimer()
        {
            Panel pnTimerMask = (Panel)pnInformation.Controls["pnTimerMask"];
            int TimeStart = BUS.ContestantBUS.Instance.GetTimeStartFromContestant(CILogged.ContestantShiftID);
            int TimeNow = Controllers.Instance.ConvertDateTimeToUnix(DAO.DAO.ConvertDateTime.GetDateTimeServer());
            //   this.maxTime = currentStatusContestant == Constant.STATUS_LOGGED_DO_NOT_FINISH ? CILogged.TimeRemained : CILogged.TimeOfTest;

            /// 1p Lấy thời gian bài làm từ server một lần
            timerGetMaxTime = new Timer();
            timerGetMaxTime.Interval = 62122;
            timerGetMaxTime.Start();
            timerGetMaxTime.Tick += TimerGetMaxTime_Tick;
            this.maxTime = CILogged.TimeOfTest - (TimeNow - TimeStart) + ThoiGianBu;
            pnTimerMask.Visible = false;
            lbTimer.ForeColor = Constant.FORCECOLOR_LABEL_TIMER;
            lbTimer.Text = Controllers.Instance.HandleCountDown(maxTime);
        }
        /// <summary>
        ///  1p Lấy thời gian bài làm từ server một lần
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerGetMaxTime_Tick(object sender, EventArgs e)
        {
            try
            {
                int TimeStart = 0;
                int TimeNow = 0;
                for (int i = 0; i < 3; i++)
                {
                    TimeStart = BUS.ContestantBUS.Instance.GetTimeStartFromContestant(CILogged.ContestantShiftID);
                    TimeNow = Controllers.Instance.ConvertDateTimeToUnix(DAO.DAO.ConvertDateTime.GetDateTimeServer());

                }
                if (TimeStart > 0 && TimeNow > 0)

                {
                    // ThoiGianBu = BUS.ContestantBUS.Instance.GetThoiGianBu(CILogged.ContestantShiftID);
                    this.maxTime = CILogged.TimeOfTest - (TimeNow - TimeStart) + ThoiGianBu;

                }

            }
            catch
            {
                HandleRuntimeServerDisconnect();
            }
        }

        private void timeCountDown_Tick(object sender, EventArgs e)
        {
            try
            {
                if (maxTime < 300)
                {
                    lbTimer.ForeColor = Constant.FORCECOLOR_LABEL_TIMER_5_MINUTES;
                }
                else if (maxTime < 60)
                {
                    lbTimer.ForeColor = Constant.FORCECOLOR_LABEL_TIMER_1_MINUTE;
                }
                Application.DoEvents();
                if (maxTime > 0 && StartTest)
                {
                    maxTime--;
                    lbTimer.Text = Controllers.Instance.HandleCountDown(maxTime);

                    if (maxTime <= 0 /*&& CILogged.Status == Constant.STATUS_DOING*/)
                    {
                        timerGetMaxTime.Stop();
                        timeCountDown.Stop();
                        TimerSubmit.Stop();
                        lbTimer.Text = "00:00";
                        this.lbTimer.Update();

                        _isTimedOut = true; // Đánh dấu hết giờ → TimeWorked = TimeOfTest

                        flpnListOfQuestions.Visible = false;
                        MetroPanel mpnController = (MetroPanel)pnInformation.Controls["mpnController"];
                        MetroButton mbtnSubmit = (MetroButton)mpnController.Controls["mbtnSubmit"];
                        mbtnSubmit.Text = Properties.Resources.MSG_GUI_0069;
                        mbtnSubmit.Visible = true;
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(200);

                        //    HandleSubmitTest();
                        Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | FINISHED | HandleSubmitTest", Properties.Resources.MSG_MESS_0028);
                    }
                 
                    // Hiển thị nút nộp bài
                    if (!checkGenBtnSubmit)
                    {

                        HandleGenerateButtonSubmit();
                        checkGenBtnSubmit = true;
                    }
                    if (conn.Contains("MTAQuizDGNL")&& maxTime>0)
                    {
                        MetroPanel mpnController = (MetroPanel)pnInformation.Controls["mpnController"];
                        MetroButton mbtnSubmit = (MetroButton)mpnController.Controls["mbtnSubmit"];
                        //mbtnSubmit.Visible = false;
                    }


                }
                else if (maxTime < 0)
                {
                    timerGetMaxTime.Stop();
                    timeCountDown.Stop();
                    TimerSubmit.Stop();
                    lbTimer.Text = "00:00";
                    flpnListOfQuestions.Visible = false;
                    MetroPanel mpnController = (MetroPanel)pnInformation.Controls["mpnController"];
                    MetroButton mbtnSubmit = (MetroButton)mpnController.Controls["mbtnSubmit"];
                    mbtnSubmit.Text = Properties.Resources.MSG_GUI_0069;
                    mbtnSubmit.Visible = true;
                    System.Threading.Thread.Sleep(200);
                    Application.DoEvents();
                    //  HandleSubmitTest();
                    Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | FINISHED | HandleSubmitTest", Properties.Resources.MSG_MESS_0028);

                }
            }
            catch
            {
                Log.Instance.WriteLog(Properties.Resources.MSG_LOG_ERROR, "MAIN | TIMER | GET TIME FAIL", Properties.Resources.MSG_MESS_0028);

            }

        }
        /// <summary>
        /// Giao diện nút báo lỗi + nộp bài
        /// </summary>
        private void HandleStylePanelController()
        {
            Label lbTimer = (Label)pnInformation.Controls["lbTimer"];

            MetroPanel mpnController = new MetroPanel();
            Controllers.Instance.SetCanChangeMetroPanelColor(mpnController);
            mpnController.Name = "mpnController";
            mpnController.AutoSize = true;
            mpnController.AutoScroll = false;

            MetroButton mbtnReportError = new MetroButton();
            Controllers.Instance.SetCanChangeMetroButtonColor(mbtnReportError);
            mbtnReportError.Name = "mbtnReportError";
            mbtnReportError.ForeColor = Constant.FORCECOLOR_BUTTON_REPORTERROR;
            mbtnReportError.BackColor = Constant.BACKCOLOR_BUTTON_CONTROLLER;
            mbtnReportError.Text = Properties.Resources.MSG_GUI_0009;
            mbtnReportError.Size = Constant.SIZE_BUTTON_DEFAULT;
            mbtnReportError.Click += new System.EventHandler(this.mbtnReportError_Click);
            mpnController.Controls.Add(mbtnReportError);
            mpnController.Height = mbtnReportError.Height + 20;
            mpnController.Width = mbtnReportError.Width;
            mbtnReportError.Location = new Point(0, Convert.ToInt32((mpnController.Height - mbtnReportError.Height) / 2));

            //THONGBAO LOI
            mbtnReportError.Visible = false;
            mpnController.Location = new Point(Convert.ToInt32((Constant.WIDTH_PANEL_INFORMATION - mpnController.Width) / 2), lbTimer.Bottom);
            pnInformation.Controls.Add(mpnController);



            MetroPanel mpnInformationWrapper = new MetroPanel();
            mpnInformationWrapper.Name = "mpnInformationWrapper";
            Controllers.Instance.SetCanChangeMetroPanelColor(mpnInformationWrapper);
            mpnInformationWrapper.Location = new Point(0, mpnController.Bounds.Bottom + 10);
            mpnInformationWrapper.BackColor = Constant.BACKCOLOR_PANEL_INFORMATION;
            pnInformation.Controls.Add(mpnInformationWrapper);

            //   Controllers.Instance.SetCanChangeMetroPanelColor(mpnInformationWrapper1);
            mpnInformationWrapper1.Location = new Point(0, mpnController.Bounds.Bottom + 10);
            mpnInformationWrapper1.BackColor = Constant.BACKCOLOR_PANEL_INFORMATION;

            mpnInformationWrapper1.SendToBack();
            flpnListOfButtonQuestions.Location = new Point(0, mpnController.Bounds.Bottom + 215);
            flpnListOfButtonQuestions.Height = Constant.HEIGHT_SCREEN - flpnListOfButtonQuestions.Top - 120;
            mpnInformationWrapper.Size = new Size(Constant.WIDTH_PANEL_INFORMATION, flpnListOfButtonQuestions.Bottom + 20);
            flpnListOfButtonQuestions.SendToBack();
            HandleStyleButtonReadTimePause();
            //HandleStyleWarningCount();
            HandleStyleInformationOfContest();
        }
        MetroButton mbtnPrevious = new MetroButton();
        MetroButton mbtnNext = new MetroButton();
        MetroButton mbtnViewExamHistory = new MetroButton();
        private void HandleStyleButtonReadTimePause()
        {

            MetroButton mbtnReadTimePause = new MetroButton();
            Controllers.Instance.SetCanChangeMetroButtonColor(mbtnReadTimePause);
            mbtnReadTimePause.Name = "mbtnReadTimePause";
            mbtnReadTimePause.Font = new Font(Constant.FONT_FAMILY_DEFAULT, 16, FontStyle.Bold);
            mbtnReadTimePause.ForeColor = Constant.FORCECOLOR_BUTTON_SUBMIT;
            mbtnReadTimePause.BackColor = Constant.BACKCOLOR_BUTTON_CONTROLLER;
            mbtnReadTimePause.Text = Properties.Resources.MSG_GUI_0066;
            mbtnReadTimePause.Size = new Size(90, 42);
            mbtnReadTimePause.Click += new System.EventHandler(this.mbtnReadTimePause_Click);
            mbtnReadTimePause.Location = new Point(0, flpnListOfButtonQuestions.Bottom + 20);

            mbtnReadTimePause.Width = Constant.WIDTH_PANEL_INFORMATION;
            pnInformation.Controls.Add(mbtnReadTimePause);

            Controllers.Instance.SetCanChangeMetroButtonColor(mbtnViewExamHistory);
            mbtnViewExamHistory.Name = "mbtnViewExamHistory";
            mbtnViewExamHistory.Font = new Font(Constant.FONT_FAMILY_DEFAULT, 16, FontStyle.Bold);
            mbtnViewExamHistory.ForeColor = Constant.FORCECOLOR_BUTTON_SUBMIT;
            mbtnViewExamHistory.BackColor = Constant.BACKCOLOR_BUTTON_CONTROLLER;
            mbtnViewExamHistory.Text = "Xem lịch sử thi";
            mbtnViewExamHistory.Size = new Size(Constant.WIDTH_PANEL_INFORMATION, 42);
            mbtnViewExamHistory.Location = new Point(0, mbtnReadTimePause.Bottom + 10);
            mbtnViewExamHistory.Click += new System.EventHandler(this.mbtnViewExamHistory_Click);
            mbtnViewExamHistory.Enabled = CanViewExamHistory();
            pnInformation.Controls.Add(mbtnViewExamHistory);
            //// Phân trang
            Panel pnlBottom = new Panel();
            pnlBottom.Height = 100;
            pnlBottom.Width = Constant.WIDTH_PANEL_INFORMATION;
            pnlBottom.Location = new Point(0, mbtnViewExamHistory.Bottom + 10);

            //MetroComboBox cbPage = new MetroComboBox();
            //cbPage.Items.Add(10);
            //cbPage.Items.Add(20);
            //cbPage.Items.Add(40);
            //cbPage.SelectedValueChanged += new EventHandler(this.CbPage_SelectedValueChanged);
            //cbPage.Location = new Point(0, 0);
            //pnlBottom.Controls.Add(cbPage);

            
            mbtnPrevious.Text = "Trang trước";
            mbtnPrevious.Click += new EventHandler(this.MbtnPrevious_Click);
            mbtnPrevious.Location = new Point(100, 0);
            mbtnPrevious.Size = Constant.SIZE_BUTTON_DEFAULT;
            pnlBottom.Controls.Add(mbtnPrevious);
            
            mbtnNext.Text = "Trang Tiếp";
            mbtnNext.Click += new EventHandler(this.MbtnNext_Click);
            mbtnNext.Location = new Point(mbtnPrevious.Right + 10, 0);
            mbtnNext.Size = Constant.SIZE_BUTTON_DEFAULT;
            
            pnlBottom.Controls.Add(mbtnNext);
            pnInformation.Controls.Add(pnlBottom);


            mbtnReadTimePause.SendToBack();
        }
        // xử lý việc cảnh cáo trong quá trình làm bài
        private void HandleStyleWarningCount()
        {
            Label lbWarningCount = new Label();
            lbWarningCount.Name = "lbWarningCount";
            pnInformation.Controls.Add(lbWarningCount);
            lbWarningCount.Location = new Point(30, flpnListOfButtonQuestions.Bottom + 20);
            lbWarningCount.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
            lbWarningCount.ForeColor = WarningCount > 0 ? Constant.FORCECOLOR_LABEL_HEADER_CONTENT : Constant.BACKCOLOR_PANEL_INFORMATION;
            lbWarningCount.Text = WarningCount > 0 ? string.Format(Properties.Resources.MSG_GUI_0053, WarningCount, GetValueOfWarningCount(WarningCount)) : string.Empty;
            lbWarningCount.Width = Constant.WIDTH_PANEL_INFORMATION;
            lbWarningCount.SendToBack();

        }
        /// <summary>
        /// Thêm nút nộp bài nếu kỳ thi có cho phép nộp bài trước thời gian hết giờ
        /// </summary>
        private void HandleGenerateButtonSubmit()
        {


            MetroPanel mpnController = (MetroPanel)pnInformation.Controls["mpnController"];
            Label mlblCode = (Label)mpnController.Controls["mlblCode"];

            MetroButton mbtnSubmit = new MetroButton();
            Controllers.Instance.SetCanChangeMetroButtonColor(mbtnSubmit);

            mbtnSubmit.Name = "mbtnSubmit";
            mbtnSubmit.Font = new Font(Constant.FONT_FAMILY_DEFAULT, 16, FontStyle.Bold);
            mbtnSubmit.ForeColor = Constant.FORCECOLOR_BUTTON_SUBMIT;
            mbtnSubmit.BackColor = Constant.BACKCOLOR_BUTTON_CONTROLLER;
            mbtnSubmit.Text = Properties.Resources.MSG_GUI_0011;
            mbtnSubmit.Size = new Size(90, 42);
            mbtnSubmit.Click += new System.EventHandler(this.mbtnSubmit_Click);
            mbtnSubmit.Location = new Point(mlblCode.Bounds.Width + 40, Convert.ToInt32((mpnController.Height - mlblCode.Height - 5) / 2));
            mpnController.Controls.Add(mbtnSubmit);
        }
        private void HandleGenerateButtonSubmitForComplete()
        {


            MetroPanel mpnController = (MetroPanel)pnInformation.Controls["mpnController"];
            Label mlblCode = (Label)mpnController.Controls["mlblCode"];

            MetroButton mbtnSubmit = new MetroButton();
            Controllers.Instance.SetCanChangeMetroButtonColor(mbtnSubmit);

            mbtnSubmit.Name = "mbtnSubmit";
            mbtnSubmit.Font = new Font(Constant.FONT_FAMILY_DEFAULT, 16, FontStyle.Bold);
            mbtnSubmit.ForeColor = Constant.FORCECOLOR_BUTTON_SUBMIT;
            mbtnSubmit.BackColor = Constant.BACKCOLOR_BUTTON_CONTROLLER;
            mbtnSubmit.Text = "Xem điểm";
            mbtnSubmit.Size = new Size(90, 42);
            mbtnSubmit.Click += new System.EventHandler(this.mbtnSubmitForFinish_Click);
            mbtnSubmit.Location = new Point(mlblCode.Bounds.Width + 40, Convert.ToInt32((mpnController.Height - mlblCode.Height - 5) / 2));
            mpnController.Controls.Add(mbtnSubmit);
        }
        private void HandleGenerateLabelCodeTest(string CodeTest)
        {
            MetroPanel mpnController = (MetroPanel)pnInformation.Controls["mpnController"];
            //mã đề

            Label mlblCode = new Label();
            mlblCode.Name = "mlblCode";
            mlblCode.ForeColor = Constant.COLOR_RED;
            mlblCode.AutoSize = true;
            mlblCode.Font = new Font(Constant.FONT_FAMILY_DEFAULT, 17, FontStyle.Bold);
            mlblCode.Text = "Mã đề: " + CodeTest;
            mlblCode.BackColor = Constant.COLOR_TRANSPARENT;

            mpnController.Controls.Add(mlblCode);

            mlblCode.Location = new Point(20, Convert.ToInt32((mpnController.Height - mlblCode.Height) / 2));
            mpnController.Width = mlblCode.Right + 100;
            mpnController.Location = new Point(Convert.ToInt32((Constant.WIDTH_PANEL_INFORMATION - mpnController.Width) / 2), lbTimer.Bottom + 10);
        }
        private MetroButton GetMetroButtonSubmit()
        {
            MetroPanel mpnController = (MetroPanel)pnInformation.Controls["mpnController"];
            return (MetroButton)mpnController.Controls["mbtnSubmit"];
        }
        /// <summary>
        /// Xử lý nộp bài
        /// </summary>
        private void HandleSubmitTest()
        {

            ErrorController rEC = new ErrorController();

            double result = 0;
            int count = 0;
            float rResult = 0;
            float sResult = 0;

            ///// 2025 add bonus variable
            float bonus = 0;
            float maxBonus = 0;
            Dictionary<int, int> numOfcorrectSubQues = new Dictionary<int, int>();

            //DTO.LoadingForm = new frmLoading();
            //DTO.LoadingForm.TotalProgress = 3;
            /////  DTO.LoadingForm.TotalProgress = 40;
            //s = new SendWorking(DTO.LoadingForm.HandleWorking);
            //DTO.LoadingForm.Owner = this;
            //DTO.LoadingForm.Show();
            //s(true);
            sResult = (float)Math.Round(TestBUS.Instance.SumScore(CILogged, Sql), 2);

            if (WarningCount >= 3)
            {
                rASH.TestScores = 0;
                rASH.Status = Constant.STATUS_WARNING;
            }
            else
            {
                HandlePushAnswerSheet();
                List<AnswersheetDetail> lstAHD = new List<AnswersheetDetail>();
                AnswersheetDetailBUS.Instance.GetListAnswerSheetDetail(CILogged, out lstAHD, Sql);

                foreach (AnswersheetDetail ad in lstAHD)
                {

                    rResult = TestBUS.Instance.CheckAnswers(ad, lstLQuestion, ref numOfcorrectSubQues, Sql);
                    if (rResult > 0)
                    {
                        result += rResult;
                        count++;
                    }
                }

                rASH.TestScores = (float)Math.Round(result, 2);

                //tinh bonus
                List<QuesIDwithBonus> LquesIDwithBonus = TestBUS.Instance.GetListQuestionWithBonusScore(Sql);

                if (LquesIDwithBonus != null && LquesIDwithBonus.Count > 0)
                {
                    foreach (var pair in numOfcorrectSubQues)
                    {

                        /// tim trong CTDL BONUS doan from, to ma co questionID = pair.Key va from<=pair.Value<to => testcore+=bonus
                        foreach (var quesIDwithBonus in LquesIDwithBonus)
                        {

                            if (pair.Key == quesIDwithBonus.QuestionID && quesIDwithBonus.From <= pair.Value && pair.Value <= quesIDwithBonus.To)
                            {

                                //rASH.TestScores +=  float.Parse(quesIDwithBonus.Bonus.ToString());

                                rASH.TestScores += (float)quesIDwithBonus.Bonus;

                                break;
                            }
                        }
                    }
                }
                //tinh maxbonus

                {
                    // this.CILogged.ScheduleID -> structureID -> from max theo tung structuredetail -> sum bonus
                    int ScheduleID = this.CILogged.ScheduleID;
                    List<StructureDetailIDwithMaxBonus> LstructureDetailIDwithMaxBonus = TestBUS.Instance.GetListMaxBonusWithStructureID(ScheduleID, Sql);

                    if (LstructureDetailIDwithMaxBonus != null && LstructureDetailIDwithMaxBonus.Count > 0)
                    {
                        foreach (var structureDetailIDwithMaxBonus in LstructureDetailIDwithMaxBonus)
                        {

                            sResult += (float)structureDetailIDwithMaxBonus.maxBonus * (int)structureDetailIDwithMaxBonus.NumOfQuestionInTest;

                        }
                    }

                }

                rASH.Status = Constant.STATUS_FINISHED;

                SafeSetAuthenticationLogFileAttributes(FileAttributes.ReadOnly);
                //  s(true);
            }

            // Tính thời gian nộp bài trước khi lưu DB (dùng fallback nếu mất mạng)
            string _timeSource;
            DateTime submitTime = GetCurrentTimeWithSource(out _timeSource);
            string endTimeMsText = submitTime.ToString("HH:mm:ss:fff");
            string submitTimeText = submitTime.ToString("dd-MM-yyyy HH:mm:ss.fff");
            long submitTimeUnixMs = (long)(submitTime - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds;
            string timeWorkedMsText = null;
            long timeWorkedMs = 0;
            try
            {
                if (_isTimedOut)
                {
                    TimeSpan totalExam = TimeSpan.FromSeconds(CILogged.TimeOfTest);
                    timeWorkedMsText = totalExam.ToString(@"hh\:mm\:ss\:fff");
                    timeWorkedMs = (long)totalExam.TotalMilliseconds;
                }
                else
                {
                    int timeStartUnix = BUS.ContestantBUS.Instance.GetTimeStartFromContestant(CILogged.ContestantShiftID);
                    if (timeStartUnix > 0)
                    {
                        long timeStartMs = (long)timeStartUnix * 1000L;
                        long thoiGianBuMs = (long)ThoiGianBu * 1000L;
                        timeWorkedMs = submitTimeUnixMs - timeStartMs - thoiGianBuMs;
                        if (timeWorkedMs < 0) timeWorkedMs = 0;
                        timeWorkedMsText = TimeSpan.FromMilliseconds(timeWorkedMs).ToString(@"hh\:mm\:ss\:fff");
                    }
                }
            }
            catch { }

            // Luôn ghi log local tại thời điểm click nộp bài (kể cả khi DB lưu thất bại)
            WriteSubmitLog(rASH.TestScores ?? 0f, sResult, submitTimeText, timeWorkedMsText, "SUBMIT_CLICK", _timeSource);
            ConnectionLogger.Log("SUBMIT", string.Format("Score={0} Source={1}", rASH.TestScores, _timeSource));
            ConnectionLogger.WriteToFile(CILogged.ContestantCode, CILogged.ContestantShiftID);
            AnswerSelectionLogger.WriteToFile(CILogged.ContestantCode, CILogged.ContestantShiftID);

            AnswersheetBUS.Instance.PushAnswerSheet(rASH, out rEC, Sql);
            if (rEC.ErrorCode == Constant.STATUS_OK)
            {
                CILogged.EndTimeMsText = endTimeMsText;
                CILogged.TimeWorkedMsText = timeWorkedMsText;
                CILogged.SubmitTimeUnixMs = submitTimeUnixMs;
                CILogged.SubmitTimeText = submitTimeText;
                CILogged.TimeWorkedMs = timeWorkedMs;
                CILogged.TimeSource = _timeSource;

                ChangeContestantStatusToFinished();

                string notifyContent = string.Format(Properties.Resources.MSG_GUI_0051, rASH.TestScores, sResult);
                if (!string.IsNullOrEmpty(timeWorkedMsText))
                    notifyContent += string.Format("\nThời gian làm bài: {0}. Thời điểm nộp bài: {1}", timeWorkedMsText, submitTimeText);
                UploadCurrentExamLogsToServer();
                Controllers.Instance.ShowNotificationFormResult(notifyContent, this, CILogged.DivisionShiftID, CILogged.ContestantShiftID, Sql);

                this.Controls.Remove(Controls.Owner);

                DTO.ViewRtfForm.Dispose();
                this.Hide();

                // //Ca thi tiếp theo
                frmWatingNextDivisionShift frm = new frmWatingNextDivisionShift(CLogged.ContestName, CILogged.ContestantID, Sql);
                frm.Show();
                this.Dispose();
            }
            else
            {
                // Mất mạng hoặc lỗi DB: log đã ghi ở trên, thông báo cho thí sinh
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_FATAL, Controllers.Instance.HandleStringErrorController(rEC));
                Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), this);
                Controllers.Instance.ExitApplicationFromNotificationForm(this);
            }
        }



        private double HandleWarning(double result)
        {
            switch (WarningCount)
            {
                case 1: return result * Properties.Settings.Default.Warning1 / 100;
                case 2: return result * Properties.Settings.Default.Warning2 / 100;
                case 3: return result;
                default: return 0;
            }
        }
        /// <summary>
        /// xử lý sự kiện đọc thời gian gián đoạn
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mbtnReadTimePause_Click(object sender, EventArgs e)
        {
            frmViewTimePause frm = new frmViewTimePause(CILogged.ContestantShiftID);
            frm.Show();

        }

        private void mbtnViewExamHistory_Click(object sender, EventArgs e)
        {
            if (!CanViewExamHistory())
            {
                return;
            }

            frmViewExamHistory frm = new frmViewExamHistory(CILogged);
            frm.Show();
        }
        /// <summary>
        /// Xử lý nút xem lại điểm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mbtnSubmitForFinish_Click(object sender, EventArgs e)
        {
            MetroButton mbtnSubmit = sender as MetroButton;
            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | SUBMITATION | mbtnSubmit_Click", Properties.Resources.MSG_MESS_0008);
            HandleSubmitTest();
            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | SUBMITATION | mbtnSubmit_Click", Properties.Resources.MSG_MESS_0009);



        }
        /// <summary>
        /// Xư3 lý sự kiện nút nộp bài
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mbtnSubmit_Click(object sender, EventArgs e)
        {
            MetroButton mbtnSubmit = sender as MetroButton;
            if (mbtnSubmit.Text == Properties.Resources.MSG_GUI_0011)
            {
                Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_YESNO, Properties.Resources.MSG_MESS_0040, this);
                if (DTO.NotificationForm.DialogResult == DialogResult.OK)
                {
                    flpnListOfQuestions.Visible = false;
                    timerGetMaxTime.Stop();
                    timeCountDown.Stop();
                    TimerSubmit.Stop();
                    StopAudio();
                    Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | SUBMITATION | mbtnSubmit_Click", Properties.Resources.MSG_MESS_0008);
                    HandleSubmitTest();
                    Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | SUBMITATION | mbtnSubmit_Click", Properties.Resources.MSG_MESS_0009);
                }
            }
            else
            {
                timerGetMaxTime.Stop();
                timeCountDown.Stop();
                TimerSubmit.Stop();
                StopAudio();
                Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | SUBMITATION | mbtnSubmit_Click", Properties.Resources.MSG_MESS_0008);
                HandleSubmitTest();
                Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | SUBMITATION | mbtnSubmit_Click", Properties.Resources.MSG_MESS_0009);
            }


        }
        /// <summary>
        /// Xử lý sự iện nút báo lỗi
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mbtnReportError_Click(object sender, EventArgs e)
        {
            Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_YESNO, Properties.Resources.MSG_GUI_0047, this);
            if (DTO.NotificationForm.DialogResult == DialogResult.OK)
            {

            }

        }
        /// <summary>
        /// Giao diện thẻ kỳ thi
        /// </summary>
        private void HandleStyleInformationOfContest()
        {
            MetroPanel mpnInformationWrapper = (MetroPanel)pnInformation.Controls["mpnInformationWrapper"];
            //MetroPanel mpnController = (MetroPanel)pnInformation.Controls["mpnController"];

            MetroPanel mpnContestWrapper = new MetroPanel();
            Controllers.Instance.SetCanChangeMetroPanelColor(mpnContestWrapper);
            mpnContestWrapper.Name = "mpnContestWrapper";
            mpnContestWrapper.BackColor = Constant.BACKCOLOR_PANEL_WRAPPER_CONTENT;
            mpnContestWrapper.Width = Constant.WIDTH_PANEL_INFORMATION;
            MetroPanel mpnContestContent = new MetroPanel();

            Controllers.Instance.SetCanChangeMetroPanelColor(mpnContestContent);
            mpnContestContent.Name = "mpnContestContent";

            Label lbHeaderContest = new Label();
            lbHeaderContest.Name = "lbHeaderContest";
            lbHeaderContest.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
            lbHeaderContest.ForeColor = Constant.FORCECOLOR_LABEL_HEADER_CONTENT;
            lbHeaderContest.BackColor = Constant.COLOR_TRANSPARENT;
            lbHeaderContest.AutoSize = true;
            lbHeaderContest.Text = Properties.Resources.MSG_GUI_0005;
            lbHeaderContest.Location = new Point(0, 0);
            mpnContestContent.BackColor = Constant.COLOR_TRANSPARENT;

            ////Contest's name
            //ucRowInfor ucContestName = new ucRowInfor();
            //SendUCRowInfor sucContestName = new SendUCRowInfor(ucContestName.HandleUC);
            //sucContestName(Properties.Resources.MSG_GUI_0006, CLogged.ContestName);
            //ucContestName.Location = new Point(0, lbHeaderContest.Bounds.Height);
            //mpnContestContent.Controls.Add(ucContestName);

            ////Contest's subject
            //ucRowInfor ucContestSubject = new ucRowInfor();
            //SendUCRowInfor sucContestSubject = new SendUCRowInfor(ucContestSubject.HandleUC);
            //sucContestSubject(Properties.Resources.MSG_GUI_0008, CLogged.Subject);
            //ucContestSubject.Location = new Point(0, ucContestName.Bottom);
            //mpnContestContent.Controls.Add(ucContestSubject);

            // Ngày thi
            ucRowInfor ucContestDate = new ucRowInfor();
            SendUCRowInfor sucContestDate = new SendUCRowInfor(ucContestDate.HandleUC);
            ucContestDate.BackColor = Constant.COLOR_TRANSPARENT;
            sucContestDate(Properties.Resources.MSG_GUI_0007, Controllers.Instance.ConvertUnixToDateTime(CLogged.ShiftDate).ToString(Constant.FORMAT_DATE_DEFAULT));
            ucContestDate.Location = new Point(0, lbHeaderContest.Bounds.Height);
            mpnContestContent.Controls.Add(ucContestDate);
            // Ca thi
            ucRowInfor ucContestShift = new ucRowInfor();
            SendUCRowInfor sucContestShift = new SendUCRowInfor(ucContestShift.HandleUC);
            sucContestShift(Properties.Resources.MSG_GUI_0049, string.Format("{0} - {1}", Controllers.Instance.ConvertUnixToDateTime(CLogged.StartTime).ToString(Constant.FORMAT_TIME_DEFAULT), Controllers.Instance.ConvertUnixToDateTime(CLogged.EndTime).ToString(Constant.FORMAT_TIME_DEFAULT)));
            ucContestShift.Location = new Point(0, ucContestDate.Bottom);
            mpnContestContent.Controls.Add(ucContestShift);

            // Phòng thi
            ucRowInfor ucContestRoom = new ucRowInfor();
            SendUCRowInfor sucContestRoom = new SendUCRowInfor(ucContestRoom.HandleUC);
            sucContestRoom(Properties.Resources.MSG_GUI_0036, CLogged.RoomName);
            ucContestRoom.Location = new Point(0, ucContestShift.Bottom);
            mpnContestContent.Controls.Add(ucContestRoom);

            // Máy thi
            ucRowInfor ucContestComputer = new ucRowInfor();
            SendUCRowInfor sucContestComputer = new SendUCRowInfor(ucContestComputer.HandleUC);
            sucContestComputer(Properties.Resources.MSG_GUI_0037, Dns.GetHostName());
            ucContestComputer.Location = new Point(0, ucContestRoom.Bottom);
            mpnContestContent.Controls.Add(ucContestComputer);

            mpnContestContent.AutoSize = true;
            mpnContestContent.AutoScroll = false;
            mpnContestContent.Height = ucContestComputer.Bottom;
            mpnContestWrapper.Location = new Point(0, lbHeaderContest.Bottom - lbHeaderContest.Height / 2);
            mpnContestContent.Location = new Point(40, 0);

            mpnContestWrapper.Height = mpnContestContent.Height + 20;
            mpnInformationWrapper.Controls.Add(lbHeaderContest);
            mpnContestWrapper.Controls.Add(mpnContestContent);
            mpnInformationWrapper.Controls.Add(mpnContestWrapper);

            HandleStyleInformationOfContestant();
        }
        /// <summary>
        /// Giao diện thông tin thí sinh
        /// </summary>
        private void HandleStyleInformationOfContestant()
        {
            MetroPanel mpnInformationWrapper = (MetroPanel)pnInformation.Controls["mpnInformationWrapper"];
            MetroPanel mpnContestWrapper = (MetroPanel)mpnInformationWrapper.Controls["mpnContestWrapper"];
            MetroPanel mpnContestantWrapper = new MetroPanel();
            Controllers.Instance.SetCanChangeMetroPanelColor(mpnContestantWrapper);
            mpnContestantWrapper.Name = "mpnContestantWrapper";
            mpnContestantWrapper.BackColor = Constant.BACKCOLOR_PANEL_WRAPPER_CONTENT;
            mpnContestantWrapper.Width = Constant.WIDTH_PANEL_INFORMATION;
            MetroPanel mpnContestantContent = new MetroPanel();

            Controllers.Instance.SetCanChangeMetroPanelColor(mpnContestantContent);
            mpnContestantContent.Name = "mpnContestantContent";

            Label lbHeaderIOC = new Label();
            lbHeaderIOC.Name = "lbHeaderIOC";
            lbHeaderIOC.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
            lbHeaderIOC.ForeColor = Constant.FORCECOLOR_LABEL_HEADER_CONTENT;
            lbHeaderIOC.BackColor = Constant.COLOR_TRANSPARENT;
            lbHeaderIOC.AutoSize = true;
            lbHeaderIOC.Text = Properties.Resources.MSG_GUI_0004;
            lbHeaderIOC.Location = new Point(0, mpnContestWrapper.Bounds.Bottom + 30);

            // Tên thí sinh
            ucRowInfor ucFullname = new ucRowInfor();
            SendUCRowInfor sucFullname = new SendUCRowInfor(ucFullname.HandleUC);
            sucFullname(Properties.Resources.MSG_GUI_0001, Controllers.Instance.CapitalizeString(CILogged.Fullname));
            ucFullname.Location = new Point(0, lbHeaderIOC.Bounds.Height);
            mpnContestantContent.Controls.Add(ucFullname);

            //Số báo danh 
            ucRowInfor ucContestantCode = new ucRowInfor();
            SendUCRowInfor sucContestantGender = new SendUCRowInfor(ucContestantCode.HandleUC);
            sucContestantGender("SBD", CILogged.ContestantCode);
            ucContestantCode.Location = new Point(0, ucFullname.Bottom);
            mpnContestantContent.Controls.Add(ucContestantCode);

            // Ngày sinh 
            ucRowInfor ucDOB = new ucRowInfor();
            SendUCRowInfor sucDOB = new SendUCRowInfor(ucDOB.HandleUC);
            sucDOB(Properties.Resources.MSG_GUI_0002, Controllers.Instance.ConvertUnixToDateTime(CILogged.DOB).ToString(Constant.FORMAT_DATE_DEFAULT));
            ucDOB.Location = new Point(0, ucContestantCode.Bottom);
            mpnContestantContent.Controls.Add(ucDOB);

            // Số CMND or lớp
            string s = Properties.Resources.MSG_GUI_0068;
            ucRowInfor ucStudentIdentify = new ucRowInfor();
            SendUCRowInfor sucStudentIdentify = new SendUCRowInfor(ucStudentIdentify.HandleUC);
            sucStudentIdentify(Properties.Resources.MSG_GUI_0068, CILogged.Unit);
            ucStudentIdentify.Location = new Point(0, ucDOB.Bottom);
            mpnContestantContent.Controls.Add(ucStudentIdentify);

            // Số lần gián đoán 


            mpnContestantContent.AutoSize = true;
            mpnContestantContent.AutoScroll = false;
            mpnContestantContent.Height = mpnContestantContent.Height + 40;
            mpnContestantWrapper.Location = new Point(0, lbHeaderIOC.Bottom - lbHeaderIOC.Height / 2);
            mpnContestantContent.Location = new Point(40, 0);

            mpnContestantWrapper.Height = mpnContestantContent.Height + 20;
            mpnInformationWrapper.Controls.Add(lbHeaderIOC);
            mpnContestantWrapper.Controls.Add(mpnContestantContent);
            mpnInformationWrapper.Controls.Add(mpnContestantWrapper);

            HandleStyleWarning();
        }
        private MetroPanel GetPanelInformationWrapper()
        {
            return (MetroPanel)pnInformation.Controls["mpnInformationWrapper"];
        }
        /// <summary>
        /// Giao diện chú ý lỗi
        /// </summary>
        private void HandleStyleWarning()
        {
            MetroPanel mpnInformationWrapper = (MetroPanel)pnInformation.Controls["mpnInformationWrapper"];
            MetroPanel mpnContestantWrapper = (MetroPanel)mpnInformationWrapper.Controls["mpnContestantWrapper"];

            MetroPanel mpnWarningWrapper = new MetroPanel();
            mpnWarningWrapper.Name = "mpnWarningWrapper";
            Controllers.Instance.SetCanChangeMetroPanelColor(mpnWarningWrapper);
            mpnWarningWrapper.BackColor = Constant.BACKCOLOR_PANEL_WRAPPER_CONTENT;
            mpnWarningWrapper.Width = Constant.WIDTH_PANEL_INFORMATION;
            mpnInformationWrapper.Controls.Add(mpnWarningWrapper);

            MetroPanel mpnWarningContent = new MetroPanel();
            Controllers.Instance.SetCanChangeMetroPanelColor(mpnWarningContent);
            mpnWarningContent.Name = "mpnWarningContent";
            mpnWarningContent.AutoSize = true;
            mpnWarningWrapper.Controls.Add(mpnWarningContent);

            Label lbHeaderWarning = new Label();
            lbHeaderWarning.Name = "lbHeaderWarning";
            lbHeaderWarning.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
            lbHeaderWarning.ForeColor = Constant.FORCECOLOR_LABEL_HEADER_CONTENT;
            lbHeaderWarning.BackColor = Constant.COLOR_TRANSPARENT;
            lbHeaderWarning.AutoSize = true;
            lbHeaderWarning.Text = Properties.Resources.MSG_MESS_0029;
            lbHeaderWarning.Location = new Point(0, mpnContestantWrapper.Bounds.Bottom + 30);
            mpnInformationWrapper.Controls.Add(lbHeaderWarning);

            Label lbWarning1 = new Label();
            lbWarning1.Name = "lbWarning1";
            lbWarning1.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
            lbWarning1.ForeColor = Constant.COLOR_WHITE;
            lbWarning1.BackColor = Constant.COLOR_TRANSPARENT;
            lbWarning1.AutoSize = true;
            lbWarning1.Text = string.Format(Properties.Resources.MSG_MESS_0030, 1, Properties.Settings.Default.Warning1);
            lbWarning1.Location = new Point(0, 0);
            mpnWarningContent.Controls.Add(lbWarning1);


            Label lbWarning2 = new Label();
            lbWarning2.Name = "lbWarning2";
            lbWarning2.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
            lbWarning2.ForeColor = Constant.COLOR_WHITE;
            lbWarning2.BackColor = Constant.COLOR_TRANSPARENT;
            lbWarning2.AutoSize = true;
            lbWarning2.Text = string.Format(Properties.Resources.MSG_MESS_0030, 2, Properties.Settings.Default.Warning2);
            lbWarning2.Location = new Point(0, lbWarning1.Bounds.Bottom + 10);
            mpnWarningContent.Controls.Add(lbWarning2);

            Label lbWarning3 = new Label();
            lbWarning3.Name = "lbWarning3";
            lbWarning3.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
            lbWarning3.ForeColor = Constant.COLOR_WHITE;
            lbWarning3.BackColor = Constant.COLOR_TRANSPARENT;
            lbWarning3.AutoSize = true;
            lbWarning3.Text = Properties.Resources.MSG_MESS_0031;
            lbWarning3.Location = new Point(0, lbWarning2.Bounds.Bottom + 10);
            mpnWarningContent.Controls.Add(lbWarning3);

            mpnWarningWrapper.Location = new Point(0, lbHeaderWarning.Bottom - lbHeaderWarning.Height / 2);
            mpnWarningContent.Height = lbWarning3.Bounds.Bottom;

            mpnWarningWrapper.Height = mpnWarningContent.Bottom + 30;
            mpnWarningContent.Location = new Point(30, lbHeaderWarning.Height);


            mpnInformationWrapper.Controls.Add(mpnWarningWrapper);
        }
        #endregion

        #region List of question panel
        /// <summary>
        ///Giao diện danh sách câu hỏi thi
        /// </summary>
        private void HandlePanelListOfQuestions()
        {
            HandlePanelLoading();
            #region Panel list of question
            flpnListOfQuestions.Dock = DockStyle.Right;
            flpnListOfQuestions.BackColor = Constant.BACKCOLOR_PANEL_INFORMATION;
            flpnListOfQuestions.Width = Constant.WIDTH_SCREEN - Constant.WIDTH_PANEL_INFORMATION;
            flpnListOfQuestions.Controls.Clear();
            #endregion
        }
        #endregion

        #region Panel loading
        private void HandlePanelLoading()
        {
            MetroPanel mpnLoading = new MetroPanel();
            mpnLoading.Name = "mpnLoading";
            this.Controls.Add(mpnLoading);
            Controllers.Instance.SetCanChangeMetroPanelColor(mpnLoading);
            mpnLoading.BackColor = Constant.BACKCOLOR_PANEL_INFORMATION;
            mpnLoading.Width = Constant.WIDTH_SCREEN - Constant.WIDTH_PANEL_INFORMATION;
            mpnLoading.Height = Constant.HEIGHT_SCREEN;
            mpnLoading.Location = new Point(Constant.WIDTH_PANEL_INFORMATION, 0);
            mpnLoading.BringToFront();
        }
        private MetroPanel GetPanelLoading()
        {
            return (MetroPanel)this.Controls["mpnLoading"];
        }
        /// <summary>
        /// Giao diện chờ làm bài thi
        /// </summary>
        private void HandleContentPanelLoading()
        {
            MetroPanel mpnLoadingWrapper = new MetroPanel();
            mpnLoadingWrapper.Name = "mpnLoadingWrapper";
            Controllers.Instance.SetCanChangeMetroPanelColor(mpnLoadingWrapper);
            GetPanelLoading().Controls.Add(mpnLoadingWrapper);
            mpnLoadingWrapper.BackColor = Constant.COLOR_TRANSPARENT;
            mpnLoadingWrapper.AutoSize = true;

            Label lbContent = new Label();
            lbContent.Name = "lbContent";
            mpnLoadingWrapper.Controls.Add(lbContent);
            lbContent.Font = new Font(Constant.FONT_FAMILY_DEFAULT, 20, FontStyle.Bold);
            lbContent.Text = Properties.Resources.MSG_GUI_0058;
            lbContent.AutoSize = true;

            //MetroButton mbtnStart = new MetroButton();
            //mbtnStart.Name = "mbtnStart";
            //Controllers.SetCanChangeMetroButtonColor(mbtnStart);
            //mbtnStart.Text = Properties.Resources.MSG_GUI_0010;
            //mbtnStart.ForeColor = Constant.FORCECOLOR_BUTTON_SUBMIT;
            //mbtnStart.BackColor = Constant.BACKCOLOR_BUTTON_CONTROLLER;
            //mbtnStart.Size = Constant.SIZE_BUTTON_DEFAULT;
            //mbtnStart.Click += new System.EventHandler(this.mbtnStart_Click);
            //mbtnStart.Location = new Point(Convert.ToInt32((mpnLoadingWrapper.Bounds.Width - mbtnStart.Width) / 2), lbContent.Bounds.Bottom + 20);

            //mpnLoadingWrapper.Controls.Add(mbtnStart);

            mpnLoadingWrapper.Location = new Point(Convert.ToInt32((GetPanelLoading().Bounds.Width - mpnLoadingWrapper.Width) / 2), Convert.ToInt32((GetPanelLoading().Bounds.Height - mpnLoadingWrapper.Height) / 2));
        }

        /// <summary>
        /// Hàm xử lý bắt đầu thi
        /// </summary>
        private void HandleStartDoingTest()
        {
            ErrorController rEC = new ErrorController();
            GetPanelLoading().Visible = false;
            GetPanelInformationWrapper().Visible = false;
            // cap nhap thoi gian sau khi gian doan
            ContestantBUS.Instance.UpdateForContestantPause(CILogged.ContestantShiftID, out rEC, out ThoiGianBu, Sql);
            if (rEC.ErrorCode == Constant.STATUS_OK)
            {
                Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | UPDATE_TIME_CONTESTANT | HandleStartDoingTest | update contestpause", Controllers.Instance.HandleStringErrorController(rEC));
            }
            else
            {
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));
                ShowRuntimeServerMessage("Mất kết nối server, vui lòng thử lại hoặc liên hệ giám thị.");
                return;
            }
            CILogged.Status = Constant.STATUS_DOING;
            if (currentStatusContestant == Constant.STATUS_LOGGED)
            {
                CILogged.IsNewStarted = true;
                // CILogged.TimeStarted = Controllers.Instance.ConvertDateTimeToUnix(DAO.DAO.ConvertDateTime.GetDateTimeServer());
            }
            ContestantBUS.Instance.ChangeStatusContestant(CILogged, CLogged, out rEC, Sql);

            if (rEC.ErrorCode == Constant.STATUS_OK)
            {
                Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | CHANGE_STATUS_CONTESTANT | STATUS_DOING | HandleStartDoingTest | ChangeStatusContestant", Controllers.Instance.HandleStringErrorController(rEC));

            }
            else
            {
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));
                ShowRuntimeServerMessage("Mất kết nối server, vui lòng thử lại hoặc liên hệ giám thị.");
                return;
            }

            HandleTimer();


        }
        #endregion
        #endregion

        #region Generate List control
        /// <summary>
        ///  Thực hiện tạo các object câu hỏi
        /// </summary>
        private void GernerateObjControl()
        {
            Application.DoEvents();
            int statusDivisionShift = ContestBUS.Instance.GetStatusDivisionShift(CILogged.DivisionShiftID, Sql);

            this.Cursor = Cursors.WaitCursor;
            HandleGetTestInformation();
            if (lstLQuestion == null || lstLQuestion.Count == 0)
            {
                this.Cursor = Cursors.Arrow;
                Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, "Không tải được đề thi hoặc danh sách câu hỏi đang rỗng.", this);
                return;
            }
            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | GENERATION_OBJ_CONTROL | GernerateObjControl | HandleGetTestInformation", Properties.Resources.MSG_MESS_0008);
            // Debug.WriteLine(IsContinute);
            DTO.LoadingForm = new frmLoading();
            DTO.LoadingForm.TotalProgress = IsContinute ? 5 : 4;
            ///  DTO.LoadingForm.TotalProgress = 40;
            s = new SendWorking(DTO.LoadingForm.HandleWorking);
            DTO.LoadingForm.Owner = this;
            DTO.LoadingForm.Show();
            // Khởi tạo tiến trình render giao diện trang làm bài
            lstObjControl = new List<ObjControl>();

            foreach (List<Questions> lstQ in lstLQuestion)
            {
                lstObjControl.Add(new ObjControl(lstQ, CILogged.AnswerSheetID, Constant.WIDTH_SCREEN - Constant.WIDTH_PANEL_INFORMATION - 50));
            }
            //Đếm số lượng câu hỏi
            //int countQuestions = 0;
            //for (int i = 0; i < lstLQuestion.Count; i++)
            //{
            //    if (lstLQuestion[i].Count == 1)
            //    {
            //        countQuestions += 1;
            //    }
            //    else
            //    {
            //        countQuestions += lstLQuestion[i][1].NumberQuestion;
            //    }

            //}
            //if (countQuestions < 50)
            //{
            //    mbtnPrevious.Visible = false;
            //    mbtnNext.Visible = false;
            //}
            GenerateControlQuestions();
            GenerateLayoutQuestions();
            GenerateControlBtnQuestions();
            GenerateLayoutButtonQuestions();
            HandleUCQuestionPerformClick();
            HandleGenerateLabelCodeTest(CILogged.TestID.ToString());
            ErrorController rEC = new ErrorController();
            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | RENDER_LAYOUT", Properties.Resources.MSG_MESS_0007);
            if (currentStatusContestant == Constant.STATUS_LOGGED)
            {
                Label lbContent = (Label)((MetroPanel)GetPanelLoading().Controls["mpnLoadingWrapper"]).Controls["lbContent"];
                lbContent.Text = Properties.Resources.MSG_GUI_0034;
            }
            // Change status to STATUS_READY
            //if (currentStatusContestant == Constant.STATUS_LOGGED_DO_NOT_FINISH && statusDivisionShift == UserHelper.Common.STATUS_STARTTEST)
            //{
            //    IsLoadingTest = Constant.LOAD_TEST_WITH_CONTESTANT_INTERRUPT;
            //}
            if (currentStatusContestant == Constant.STATUS_LOGGED)
            {
                CILogged.Status = Constant.STATUS_READY;
                ContestantBUS.Instance.ChangeStatusContestant(CILogged, CLogged, out rEC, Sql);
                if (rEC.ErrorCode == Constant.STATUS_OK)
                {
                    Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | CHANGE_STATUS_CONTESTANT_RADIO_PERFORMCLICK | STATUS_READY | ChangeStatusContestant", Controllers.Instance.HandleStringErrorController(rEC));

                    //if (currentStatusContestant == Constant.STATUS_LOGGED_DO_NOT_FINISH)
                    //{
                    //    IsLoadingTest = Constant.LOAD_TEST_WITH_CONTESTANT_INTERRUPT;
                    //}
                }
                else
                {
                    Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));
                    Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), this);
                    Controllers.Instance.ExitApplicationFromNotificationForm(this);

                }
            }
            this.Cursor = Cursors.Arrow;
        }
        /// <summary>
        /// Khởi tạo giao diện câu hỏi
        /// </summary>
        /// <returns></returns>
        private void GenerateControlQuestions()
        {
            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "GENERATION_QUESTIONS", Properties.Resources.MSG_MESS_0008);
            lstbtnQuestions = new List<string>();

            int next = 0;
            int nextd = 0;
            int XHeader = 0;
            int YHeader = 0;
            Application.DoEvents();
            foreach (ObjControl obj in lstObjControl)
            {

                if (obj.Question.Count > 1)
                {

                    FlowLayoutPanel flpnMultiQuestion = new FlowLayoutPanel();
                    flpnMultiQuestion.FlowDirection = FlowDirection.LeftToRight;
                    flpnMultiQuestion.Width = Constant.WIDTH_SCREEN - Constant.WIDTH_PANEL_INFORMATION;
                    flpnMultiQuestion.Name = "flpnMultiQuestion";
                    flpnMultiQuestion.BackColor = Constant.COLOR_WHITE;
                    flpnMultiQuestion.Width = obj.Width;

                    flpnMultiQuestion.AutoSize = true;

                    Questions qHeader = obj.Question[0];
                    obj.Question.RemoveAt(0);

                    if (lstPartOfTest != null)
                    {
                        foreach (PartOfTest pt in lstPartOfTest)
                        {
                            if (pt.Index == next + 1)
                            {
                                //seg1
                                TRichTextBox.AdvanRichTextBox rtbTitlePart = new TRichTextBox.AdvanRichTextBox();
                                rtbTitlePart.Rtf = pt.PartContent;
                                rtbTitlePart.Font = new Font(Constant.FONT_FAMILY_DEFAULT, 18, FontStyle.Bold);
                                rtbTitlePart.Width = flpnMultiQuestion.Width - 30;
                                //old  rtbTitlePart.Margin = new Padding(15, 5, 0, 20);
                                rtbTitlePart.Margin = new Padding(15, 5, 0, 40); // new

                                rtbTitlePart.Location = new Point(0, YHeader);

                                rtbTitlePart.ReadOnly = true;
                                rtbTitlePart.BackColor = SystemColors.Window;
                                rtbTitlePart.ForeColor = System.Drawing.Color.Red;




                                //Vì khi di chuyển chuột quan các tiêu đề các phần bị lỗi, chưa fix được
                                //Nguyễn Hữu Hải
                                //rtbTitlePart.MouseHover += RtbTitleOfQuestion_MouseHover;
                                flpnMultiQuestion.Controls.Add(rtbTitlePart);
                                flpnMultiQuestion.Height += rtbTitlePart.Height + 5;
                                YHeader = rtbTitlePart.Bottom + 5;

                                //Chỉnh titleOfPart ra giữa
                                rtbTitlePart.SelectAll();
                                rtbTitlePart.SelectionAlignment = TRichTextBox.AdvanRichTextBox.TextAlign.Center;
                                rtbTitlePart.DeselectAll();

                                break;
                            }

                        }
                    }

                    if (qHeader.Audio == null)
                    {
                        //TRichTextBox.AdvanRichTextBox temp = new TRichTextBox.AdvanRichTextBox();
                        //temp.ContentsResized += RtbTitleOfQuestion_ContentsResized;
                        //temp.Width = flpnMultiQuestion.Width - 30;
                        //temp.Rtf = qHeader.TitleOfQuestion;

                        TXTextControl.TextControl rtbTitleOfQuestion = new TXTextControl.TextControl();
                        // rtbTitleOfQuestion.ContentsResized += RtbTitleOfQuestion_ContentsResized;
                        rtbTitleOfQuestion.Width = flpnMultiQuestion.Width - 30;
                        Controllers.Instance.HandleRichTextBoxStyle(rtbTitleOfQuestion);

                        if (qHeader.HighToDisplayForQuestion > 0)
                        {
                            rtbTitleOfQuestion.Height = qHeader.HighToDisplayForQuestion;
                        }
                        //rtbTitleOfQuestion.Height = temp.Height;

                        rtbTitleOfQuestion.ScrollBars = ScrollBars.Vertical;
                        rtbTitleOfQuestion.Margin = new Padding(15, 5, 0, 5);
                        //  rtbTitleOfQuestion.ReadOnly = true;
                        rtbTitleOfQuestion.DoubleClick += RtbTitleOfQuestion_DoubleClick;
                        rtbTitleOfQuestion.MouseHover += RtbTitleOfQuestion_MouseHover;
                        rtbTitleOfQuestion.Location = new Point(0, YHeader);
                        rtbTitleOfQuestion.ViewMode = TXTextControl.ViewMode.SimpleControl;
                        rtbTitleOfQuestion.AutoControlSize.AutoExpand = TXTextControl.AutoSizeDirection.Both;
                        rtbTitleOfQuestion.MaximumSize = new Size(rtbTitleOfQuestion.Width, 500);
                        //TXTextControl.AutoSize auto_size = new TXTextControl.AutoSize();
                        //auto_size.AutoExpand = TXTextControl.AutoSizeDirection.Both;
                        //rtbTitleOfQuestion.AutoControlSize = auto_size;

                        flpnMultiQuestion.Controls.Add(rtbTitleOfQuestion);
                        flpnMultiQuestion.Height += rtbTitleOfQuestion.Height + 5;
                        XHeader = rtbTitleOfQuestion.Bottom + 5;
                        //rtbTitleOfQuestion.Load(qHeader.TitleOfQuestion, TXTextControl.StringStreamType.RichTextFormat);
                        rtbTitleOfQuestion.Text = qHeader.TitleOfQuestion;
                    }
                    else
                    {

                        byte[] buffer;
                        buffer = qHeader.Audio;

                        ucListenning ucAudio = new ucListenning(buffer, qHeader.FormatQuestion, qHeader.TestDetailID);
                        ucAudio.Width = flpnMultiQuestion.Width;
                        ucAudio.Location = new Point(0, YHeader);

                        qHeader = obj.Question[0];
                        TXTextControl.TextControl rtbTitleOfQuestion = new TXTextControl.TextControl();


                        //rtbTitleOfQuestion.Width = 800;
                        rtbTitleOfQuestion.Width = flpnMultiQuestion.Width - 30;
                        Controllers.Instance.HandleRichTextBoxStyle(rtbTitleOfQuestion);

                        if (qHeader.HighToDisplayForQuestion > 0)
                        {
                            rtbTitleOfQuestion.Height = qHeader.HighToDisplayForQuestion;
                        }

                        rtbTitleOfQuestion.Height += 20;
                        rtbTitleOfQuestion.Margin = new Padding(15, 5, 0, 5);
                        rtbTitleOfQuestion.Location = new Point(0, ucAudio.Bottom + 5);
                        rtbTitleOfQuestion.ViewMode = TXTextControl.ViewMode.SimpleControl;
                        rtbTitleOfQuestion.AutoControlSize.AutoExpand = TXTextControl.AutoSizeDirection.Both;
                        rtbTitleOfQuestion.MaximumSize = new Size(rtbTitleOfQuestion.Width, 500);
                        //TXTextControl.AutoSize auto_size = new TXTextControl.AutoSize();
                        //auto_size.AutoExpand = TXTextControl.AutoSizeDirection.Both;
                        //rtbTitleOfQuestion.AutoControlSize = auto_size;
                        //  rtbTitleOfQuestion.ReadOnly = true;
                        obj.Question.RemoveAt(0);

                        flpnMultiQuestion.Controls.Add(ucAudio);
                        flpnMultiQuestion.Controls.Add(rtbTitleOfQuestion);
                        flpnMultiQuestion.Height += rtbTitleOfQuestion.Height + 5;
                        XHeader = rtbTitleOfQuestion.Bottom + 5;
                        //rtbTitleOfQuestion.Load(qHeader.TitleOfQuestion, TXTextControl.StringStreamType.RichTextFormat);
                        rtbTitleOfQuestion.Text = qHeader.TitleOfQuestion;
                    }
                    foreach (Questions q in obj.Question)
                    {

                        if (q.IsQuestionContent && q.IsSingleChoice)
                        {
                            ucQuestionsRTF ucRTF = new ucQuestionsRTF(Sql);
                            ucRTF.Location = new Point(0, XHeader);
                            ucRTF.Width = obj.Width;
                            q.NO = (next + 1);
                            next++;
                            nextd = q.NO;
                            SendQuestion sq = new SendQuestion(ucRTF.HandleQuestion);
                            sq(q, obj.AnswerSheetID);
                            ucRTF.Tag = q.SubQuestionID;
                            ucRTF.SendWorking += UcRTF_SendWorking;
                            flpnMultiQuestion.Controls.Add(ucRTF);
                            flpnMultiQuestion.Height += ucRTF.Height + 5;
                        }
                        else if (!q.IsQuestionContent && q.IsSingleChoice)
                        {
                            ucQuestionsRTF ucRTF = new ucQuestionsRTF(Sql);
                            ucRTF.Location = new Point(0, XHeader);


                            ucRTF.Width = obj.Width;
                            q.NO = (next + 1);
                            next++;
                            nextd = q.NO;
                            SendQuestion sq = new SendQuestion(ucRTF.HandleQuestion);
                            sq(q, obj.AnswerSheetID);
                            ucRTF.Tag = q.SubQuestionID;
                            ucRTF.SendWorking += UcRTF_SendWorking;
                            flpnMultiQuestion.Controls.Add(ucRTF);
                            flpnMultiQuestion.Height += ucRTF.Height + 5;
                        }
                        else if (q.IsQuestionContent && !q.IsSingleChoice)
                        {
                            ucQuestionsRTF ucRTF = new ucQuestionsRTF(Sql);
                            ucRTF.Location = new Point(0, XHeader);
                            ucRTF.Width = obj.Width;
                            q.NO = (next + 1);
                            next++;
                            nextd = q.NO;
                            SendQuestion sq = new SendQuestion(ucRTF.HandleQuestion);
                            sq(q, obj.AnswerSheetID);
                            ucRTF.Tag = q.SubQuestionID;
                            ucRTF.SendWorking += UcRTF_SendWorking;
                            flpnMultiQuestion.Controls.Add(ucRTF);
                            flpnMultiQuestion.Height += ucRTF.Height + 5;
                        }
                        else
                        {
                            // TODO
                        }
                    }


                    // }
                    lstControlQuestions.Add(flpnMultiQuestion);
                }
                else
                {
                    FlowLayoutPanel flpnMultiQuestion = new FlowLayoutPanel();
                    flpnMultiQuestion.FlowDirection = FlowDirection.LeftToRight;

                    flpnMultiQuestion.BackColor = Constant.COLOR_WHITE;
                    flpnMultiQuestion.Width = obj.Width;

                    flpnMultiQuestion.AutoSize = true;
                    if (lstPartOfTest != null)
                    {
                        foreach (PartOfTest pt in lstPartOfTest)
                        {
                            if (pt.Index == next + 1)
                            {
                                //seg2
                                TRichTextBox.AdvanRichTextBox rtbTitlePart = new TRichTextBox.AdvanRichTextBox();

                                rtbTitlePart.Width = flpnMultiQuestion.Width - 30;
                                rtbTitlePart.Rtf = pt.PartContent;
                                rtbTitlePart.Margin = new Padding(15, 5, 0, 20);
                                rtbTitlePart.Location = new Point(0, XHeader);
                                //set back trắng cho phần title của part
                                rtbTitlePart.BackColor = System.Drawing.Color.White;
                                // đổi font cho title của part
                                rtbTitlePart.Font = new Font(Constant.FONT_FAMILY_DEFAULT, 18, FontStyle.Bold);
                                //đổi màu 
                                rtbTitlePart.ForeColor = System.Drawing.Color.Red;
                                //Vì khi di chuyển chuột quan các tiêu đề các phần bị lỗi, chưa fix được
                                //Nguyễn Hữu Hải
                                //rtbTitlePart.MouseHover += RtbTitleOfQuestion_MouseHover;
                                flpnMultiQuestion.Controls.Add(rtbTitlePart);
                                flpnMultiQuestion.Height += rtbTitlePart.Height + 5;
                                XHeader = rtbTitlePart.Bottom + 5;

                                //Chỉnh titleOfPart ra giữa
                                rtbTitlePart.SelectAll();
                                rtbTitlePart.SelectionAlignment = TRichTextBox.AdvanRichTextBox.TextAlign.Center;
                                rtbTitlePart.DeselectAll();

                                break;
                            }

                        }
                    }
                    foreach (Questions q in obj.Question)
                    {

                        if (q.IsQuestionContent && q.IsSingleChoice)
                        {

                            if (nextd != 0)
                            {
                                q.NO = nextd + 1;
                                nextd = 0;
                            }
                            else
                            {
                                q.NO = next + 1;
                            }
                            ucQuestionsRTF ucRTF = new ucQuestionsRTF(Sql);
                            ucRTF.Location = new Point(0, XHeader);

                            ucRTF.Width = obj.Width;
                        
                            //if (count != 0)
                            //{
                            //    q.NO +=(count-1);
                            //}
                            next = q.NO;
                            SendQuestion sq = new SendQuestion(ucRTF.HandleQuestion);
                            sq(q, obj.AnswerSheetID);
                            ucRTF.Tag = q.SubQuestionID;
                            ucRTF.SendWorking += UcRTF_SendWorking;
                            flpnMultiQuestion.Controls.Add(ucRTF);
                            flpnMultiQuestion.Height += ucRTF.Height + 5;
                        }
                        else
                        {
                            if (nextd != 0)
                            {
                                q.NO = nextd + 1;
                                nextd = 0;
                            }
                            else
                            {
                                q.NO = next + 1;
                            }
                            ucQuestionsRTF ucRTF = new ucQuestionsRTF(Sql);
                            ucRTF.Location = new Point(0, XHeader);


                            ucRTF.Width = obj.Width;
                            //if (count != 0)
                            //{
                            //    q.NO +=(count-1);
                            //}
                            next = q.NO;
                            SendQuestion sq = new SendQuestion(ucRTF.HandleQuestion);
                            sq(q, obj.AnswerSheetID);
                            ucRTF.Tag = q.SubQuestionID;
                            ucRTF.SendWorking += UcRTF_SendWorking;
                            flpnMultiQuestion.Controls.Add(ucRTF);
                            flpnMultiQuestion.Height += ucRTF.Height + 5;
                        }

                        lstControlQuestions.Add(flpnMultiQuestion);
                    }
                }


            }
            s(true);
        }

        private void RtbTitleOfQuestion_ContentsResized(object sender, ContentsResizedEventArgs e)
        {

            ((RichTextBox)sender).Height = e.NewRectangle.Height + 10;
        }

        private void UcRTF_SendWorking(object sender, EventArgs e)
        {
            TXTextControl.TextControl rtf = (TXTextControl.TextControl)sender;

            if (DTO.ViewTextControlFrom.IsDisposed)
            {
                DTO.ViewTextControlFrom = new frmViewTextControl();
            }
            DTO.ViewTextControlFrom.TopMost = true;
            DTO.ViewTextControlFrom.Show();
            DTO.ViewTextControlFrom.UpdateView(rtf, rtf.Width);
        }

        private void RtbTitleOfQuestion_MouseHover(object sender, EventArgs e)
        {
            TXTextControl.TextControl rtbTitleOfQuestion = sender as TXTextControl.TextControl;
            ToolTip toolTip1 = new ToolTip();
            toolTip1.Show("Bạn có thể nhấp đúp chuột để xem rõ nội dung", rtbTitleOfQuestion);
        }

        private void RtbTitleOfQuestion_DoubleClick(object sender, EventArgs e)
        {

            TXTextControl.TextControl rtf = (TXTextControl.TextControl)sender;

            if (DTO.ViewTextControlFrom.IsDisposed)
            {
                DTO.ViewTextControlFrom = new frmViewTextControl();
            }
            DTO.ViewTextControlFrom.TopMost = true;
            DTO.ViewTextControlFrom.Show();
            DTO.ViewTextControlFrom.UpdateView(rtf, rtf.Width);
        }

        /// <summary>
        /// Generate control button questions
        /// </summary>
        /// <returns></returns>
        private void GenerateControlBtnQuestions()
        {
            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | GENERATION_BUTTON_QUESTIONS | GenerateControlBtnQuestions", Properties.Resources.MSG_MESS_0008);
            lstObjControl = new List<ObjControl>();
            Application.DoEvents();
            int NumberOfButton = 0;
            foreach (string top in lstbtnQuestions)
            {
                lstObjControl.Add(new ObjControl(top, NumberOfButton >= 5 ? 5 : NumberOfButton));
                NumberOfButton++;
            }
            foreach (var obj in lstObjControl.Select((value, index) => new { data = value, index = index }))
            {
                MetroButton mbtnQuestion = new MetroButton();
                mbtnQuestion.UseCustomBackColor = true;
                mbtnQuestion.UseCustomForeColor = true;
                mbtnQuestion.Cursor = Cursors.Hand;
                mbtnQuestion.FontSize = MetroButtonSize.Medium;
                mbtnQuestion.FontWeight = MetroButtonWeight.Bold;
                mbtnQuestion.ForeColor = Constant.FORCECOLOR_BUTTON_SUBMIT;
                mbtnQuestion.BackColor = Constant.BACKCOLOR_BUTTON_CONTROLLER;
                //baoanh
                int numOfQuestionsPerRow = Constant.WIDTH_PANEL_INFORMATION / Constant.WIDTH_QUESTION_BUTTON;
                //if (Constant.WIDTH_SCREEN > 1280)
                //    numOfQuestionsPerRow = 8;
                //else
                //    numOfQuestionsPerRow = 6;
                int numOfQuestionsPerCol = NumberOfButton / numOfQuestionsPerRow;
                int w = Constant.WIDTH_PANEL_INFORMATION / numOfQuestionsPerRow - 2;
                if (NumberOfButton % numOfQuestionsPerRow != 0)
                {
                    numOfQuestionsPerCol++;
                }
                int h = flpnListOfButtonQuestions.Height / numOfQuestionsPerCol - 2;

                mbtnQuestion.Size = new Size(w, h);
                mbtnQuestion.Name = "mbtn" + obj.index;
                //if (NumberOfButton > 50)
                mbtnQuestion.Text = Convert.ToString(obj.index + 1);
                //else
                //mbtnQuestion.Text = string.Format(Properties.Resources.MSG_GUI_0020, obj.index + 1);
                mbtnQuestion.Tag = obj.data.Top;
                //obj.data.Width : the number of questions per row
                // obj.index : the index of question 
                //Constant.WIDTH_PANEL_INFORMATION : 0.3 * screen width

                /*if (obj.index % obj.data.Width == 0)
                {
                    int marginLeft = Convert.ToInt32((Constant.WIDTH_PANEL_INFORMATION - ((w + 5) * obj.data.Width)) / 2);
                    mbtnQuestion.Margin = new Padding(marginLeft, 5, 0, 5);
                }
                else
                {
                    mbtnQuestion.Margin = new Padding(5, 5, 0, 5);
                }*/
                mbtnQuestion.Margin = new Padding(1, 1, 1, 1);
                lstControlBtnQuestions.Add(mbtnQuestion);
            }
            s(true);
            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | GENERATION_BUTTON_QUESTIONS | GenerateControlBtnQuestions", Properties.Resources.MSG_MESS_0009);
        }
        #endregion
        private void HandlePushAnswerSheet()
        {
            foreach (Control e in lstControlQuestions)
            {
                if (e.GetType().ToString().EndsWith(Constant.STRING_FLOWLAYOUTPANEL))
                {
                    foreach (Control c in e.Controls)
                    {
                        if (c.GetType().ToString().EndsWith(Constant.STRING_QUESTION_LISTENNING))
                        {
                            ucListenning ucListenning = (c as ucListenning);
                            int TestDetailID = ucListenning.TestDetailID;
                            if (ucListenning.CheckPlay)
                            {
                                try
                                {
                                    //TODO
                                    int currentPosition = ucListenning.TimeChecked;
                                    if (currentPosition < (int)ucListenning.maxAudio && currentPosition != 0)
                                    {
                                        TestBUS.Instance.UpdateTimeForAudioQuestion(TestDetailID, -currentPosition, Sql);

                                    }
                                    //else 
                                    //{
                                    //    ucListenning.CheckPlay = false;
                                    //}
                                }
                                catch
                                { }
                            }
                        }
                        // Đoạn này là chỗ lưu sau 5s
                        if (c.GetType().ToString().EndsWith(Constant.STRING_QUESTION_HTML) || c.GetType().ToString().EndsWith(Constant.STRING_QUESTION_RTF))
                        {
                            if (/*((c as ucQuestionsRTF).q.Type == (int)Constant.QuizTypeEnum.Fill || (c as ucQuestionsRTF).q.Type == (int)Constant.QuizTypeEnum.FillAudio || (c as ucQuestionsRTF).q.Type == (int)Constant.QuizTypeEnum.Essay) && */(c as ucQuestionsRTF).TrangThaiThayDoi)
                            {

                                ErrorController rEC = new ErrorController();
                                // câu trả lời tự luận
                                AnswersheetDetail ansd = new AnswersheetDetail();
                                ansd = (c as ucQuestionsRTF).AD;
                                if (ansd.AnswerContent != null)
                                    AnswersheetDetailBUS.Instance.PushAnswerSheetDetail(ansd, out rEC, Sql);
                                if (rEC.ErrorCode == Constant.STATUS_OK)
                                {
                                    Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "USER_ANSWER", "Đã trả lời câu hỏi");

                                     lblTimeSavedWritingQuestion.Text = "Bài thi viết được lưu tự động lúc " + GetCurrentTimeWithFallback().ToString("dd/MM/yyyy HH:mm:ss");

                                    //Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "USER_ANSWER", (c as ucQuestionsRTF).AD.AnswerContent);
                                }
                                else
                                {
                                    Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));
                                }
                                (c as ucQuestionsRTF).TrangThaiThayDoi = false;

                            }
                        }
                    }
                }

            }
        }
        private void StopAudio()
        {
            foreach (Control e in lstControlQuestions)
            {
                if (e.GetType().ToString().EndsWith(Constant.STRING_FLOWLAYOUTPANEL))
                {
                    foreach (Control c in e.Controls)
                    {
                        if (c.GetType().ToString().EndsWith(Constant.STRING_QUESTION_LISTENNING))
                        {
                            ucListenning ucListenning = (c as ucListenning);
                            ucListenning.wplayer.controls.stop();

                        }

                    }
                }

            }
        }
        #region Handle render layout
        /// <summary>
        /// Generate question to FlowLayoutPanel
        /// </summary>
        /// 
        private int NumberOfPage = 50; // số câu trên trang
        private int NumberFlpnOfPage = 0; // Tổng số flow panel trên 1 trang.
        private void GenerateLayoutQuestions()
        {
            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | GENERATE_LAYOUT_QUESTION | GenerateLayoutQuestions", Properties.Resources.MSG_MESS_0008);
            Application.DoEvents();

            int Number = 1;
            int Xheader = 0;
            //    flpnListOfQuestions.SuspendLayout();
            flpnListOfQuestions2 = new FlowLayoutPanel();
            flpnListOfQuestions2.Dock = DockStyle.Right;
            flpnListOfQuestions2.BackColor = Constant.BACKCOLOR_PANEL_INFORMATION;
            flpnListOfQuestions2.Width = Constant.WIDTH_SCREEN - Constant.WIDTH_PANEL_INFORMATION;
            flpnListOfQuestions2.Controls.Clear();
            flpnListOfQuestions2.FlowDirection = FlowDirection.LeftToRight;
            flpnListOfQuestions2.AutoScroll = true;
            // flpnListOfQuestions2.AutoSize = true;
            this.Controls.Add(flpnListOfQuestions2);

            foreach (Control e in lstControlQuestions)
            {
                if (e.GetType().ToString().EndsWith(Constant.STRING_FLOWLAYOUTPANEL))
                {

                    if (Number <= NumberOfPage)
                    {
                        e.Visible = true;
                        flpnListOfQuestions.Controls.Add(e);
                        NumberFlpnOfPage++;
                    }
                    else
                    {
                        e.Visible = true;
                        //e.Location = new Point(0, Xheader);
                        flpnListOfQuestions2.Controls.Add(e);
                        Xheader = e.Height + 10;

                        //e.Visible = false;
                    }
                    foreach (Control c in e.Controls)
                    {


                        if (c.GetType().ToString().EndsWith(Constant.STRING_QUESTION_HTML) || c.GetType().ToString().EndsWith(Constant.STRING_QUESTION_RTF))
                        {
                            lstbtnQuestions.Add(string.Format("{0}_{1}", e.Top + c.Top, c.Tag));
                            Number++;
                        }
                        else
                        {
                            //   Xheader = c.Top;
                        }

                    }

                }
                else
                {
                    lstbtnQuestions.Add(string.Format("{0}_{1}", e.Top, e.Tag));
                }
            }

            // Do TextControl phải load lên form trước, sau đó mới load dữ liệu lên được
            // Khác với RichTextBox, ta có thể gán dữ liệu vào trước khi load lên
            // Đoạn code này là để load dữ liệu lên TextControl sau khi đã load TextControl lên form.
            foreach (Control flowcontrol in lstControlQuestions)
            {
                foreach (Control e in flowcontrol.Controls)
                {
                    if (e.GetType().ToString().EndsWith(Constant.STRING_TEXTCONTROL))
                    {
                        TXTextControl.TextControl title_of_question = e as TXTextControl.TextControl;
                        //title_of_question.Height = 1000;

                        title_of_question.ViewMode = TXTextControl.ViewMode.SimpleControl;
                        title_of_question.AutoControlSize.AutoExpand = TXTextControl.AutoSizeDirection.Both;
                        title_of_question.MaximumSize = new Size(title_of_question.Width, 500);

                        if (title_of_question.Height < 400)
                        {
                            title_of_question.Load(title_of_question.Text, TXTextControl.StringStreamType.RichTextFormat);
                            title_of_question.AutoControlSize.AutoExpand = TXTextControl.AutoSizeDirection.None;
                        }
                        else
                        {
                            title_of_question.AutoControlSize.AutoExpand = TXTextControl.AutoSizeDirection.None;
                            title_of_question.ViewMode = TXTextControl.ViewMode.FloatingText;
                            title_of_question.ScrollBars = ScrollBars.Vertical;

                            title_of_question.Load(title_of_question.Text, TXTextControl.StringStreamType.RichTextFormat);
                        }
                        //title_of_question.Load(title_of_question.Text, TXTextControl.StringStreamType.RichTextFormat);
                        //title_of_question.AutoControlSize.AutoExpand = TXTextControl.AutoSizeDirection.None;
                        //if (title_of_question.Height > 300)
                        //{
                        //    title_of_question.ViewMode = TXTextControl.ViewMode.FloatingText;
                        //    title_of_question.ScrollBars = ScrollBars.Vertical;
                        //}

                    }
                }
            }



            flpnListOfQuestions2.Visible = false;

            flpnListOfQuestions.ResumeLayout();

            s(true);


        }

        private void MbtnPrevious_Click(object sender, EventArgs e)
        {

            this.flpnListOfQuestions.Visible = true;
            this.flpnListOfQuestions2.Visible = false;
        }


        private void MbtnNext_Click(object sender, EventArgs e)
        {



            this.flpnListOfQuestions.Visible = false;

            this.flpnListOfQuestions2.Visible = true;


        }

        private void CbPage_SelectedValueChanged(object sender, EventArgs e)
        {
            MetroComboBox cbPage = (sender as MetroComboBox);
            NumberOfPage = (int)cbPage.SelectedItem;
            int Number = 1;

            foreach (Control c in flpnListOfQuestions.Controls)
            {

                if (c.GetType().ToString().EndsWith(Constant.STRING_FLOWLAYOUTPANEL))
                {

                    if (Number > NumberOfPage)
                    {
                        c.Visible = false;
                    }
                    else
                    {
                        c.Visible = true;
                    }

                    Number++;

                }
            }

        }

        /// <summary>
        /// Thực hiện đánh dấu các câu đã làm
        /// </summary>
        private void HandleUCQuestionPerformClick()
        {
            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | RELOAD_TOPIC | HandleUCQuestionPerformClick", Properties.Resources.MSG_MESS_0008);
            Application.DoEvents();
            for (int i = 0; i < lstControlQuestions.Count; i++)
            {
                if (IsContinute)
                {
                    foreach (Control c in flpnListOfQuestions.Controls)
                    {
                        if (c.GetType().ToString().EndsWith(Constant.STRING_FLOWLAYOUTPANEL))
                        {
                            foreach (Control c1 in c.Controls)
                            {
                                if (c1.GetType().ToString().EndsWith(Constant.STRING_QUESTION_HTML))
                                {
                                    (c1 as ucQuestionsHTML).HandleClickMrbtnAnswer();
                                }
                                else if (c1.GetType().ToString().EndsWith(Constant.STRING_QUESTION_RTF))
                                {
                                    (c1 as ucQuestionsRTF).HandleClickRadioAnswer();
                                    if ((c1 as ucQuestionsRTF).q.AnswerSheetContent != null)
                                    {
                                        (c1 as ucQuestionsRTF).mbtnControl.BackColor = Constant.BACKCOLOR_BUTTON_QUESTION;
                                        (c1 as ucQuestionsRTF).mbtnControl.ForeColor = Constant.FORCECOLOR_BUTTON_QUESTION;
                                        (c1 as ucQuestionsRTF).mbtnControl.Update();
                                    }

                                }
                            }
                        }
                        else
                        {
                            if (c.GetType().ToString().EndsWith(Constant.STRING_QUESTION_HTML))
                            {
                                (c as ucQuestionsHTML).HandleClickMrbtnAnswer();
                            }
                            else if (c.GetType().ToString().EndsWith(Constant.STRING_QUESTION_RTF))
                            {
                                (c as ucQuestionsRTF).HandleClickRadioAnswer();
                            }
                        }
                    }
                    flpnListOfQuestions2.Visible = true;
                    foreach (Control c in flpnListOfQuestions2.Controls)
                    {
                        if (c.GetType().ToString().EndsWith(Constant.STRING_FLOWLAYOUTPANEL))
                        {
                            foreach (Control c1 in c.Controls)
                            {
                                if (c1.GetType().ToString().EndsWith(Constant.STRING_QUESTION_HTML))
                                {
                                    (c1 as ucQuestionsHTML).HandleClickMrbtnAnswer();
                                }
                                else if (c1.GetType().ToString().EndsWith(Constant.STRING_QUESTION_RTF))
                                {
                                    (c1 as ucQuestionsRTF).HandleClickRadioAnswer();
                                    if ((c1 as ucQuestionsRTF).q.AnswerSheetContent != null)
                                    {
                                        (c1 as ucQuestionsRTF).mbtnControl.BackColor = Constant.BACKCOLOR_BUTTON_QUESTION;
                                        (c1 as ucQuestionsRTF).mbtnControl.ForeColor = Constant.FORCECOLOR_BUTTON_QUESTION;
                                        (c1 as ucQuestionsRTF).mbtnControl.Update();
                                    }

                                }
                            }
                        }
                        else
                        {
                            if (c.GetType().ToString().EndsWith(Constant.STRING_QUESTION_HTML))
                            {
                                (c as ucQuestionsHTML).HandleClickMrbtnAnswer();
                            }
                            else if (c.GetType().ToString().EndsWith(Constant.STRING_QUESTION_RTF))
                            {
                                (c as ucQuestionsRTF).HandleClickRadioAnswer();
                            }
                        }
                    }
                    flpnListOfQuestions2.Visible = false;
                }
            }
            s(IsContinute);
        }

        /// <summary>
        /// Danh sách button lối tắt cho mỗi câu hỏi
        /// </summary>
        private void GenerateLayoutButtonQuestions()
        {
            //MetroPanel mpnController = (MetroPanel)pnInformation.Controls["mpnController"];
            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "GENERATE_LAYOUT_BUTTON_QUESTION", Properties.Resources.MSG_MESS_0008);
            Application.DoEvents();
            MetroPanel mpnContestantWrapper1 = new MetroPanel();
            Controllers.Instance.SetCanChangeMetroPanelColor(mpnContestantWrapper1);
            mpnContestantWrapper1.Name = "mpnContestantWrapper1";
            mpnContestantWrapper1.BackColor = Constant.BACKCOLOR_PANEL_WRAPPER_CONTENT;
            mpnContestantWrapper1.Width = Constant.WIDTH_PANEL_INFORMATION;
            MetroPanel mpnContestantContent1 = new MetroPanel();

            Controllers.Instance.SetCanChangeMetroPanelColor(mpnContestantContent1);
            mpnContestantContent1.Name = "mpnContestantContent1";

            Label lbHeaderIOC = new Label();
            lbHeaderIOC.Name = "lbHeaderIOC1";
            lbHeaderIOC.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
            lbHeaderIOC.ForeColor = Constant.FORCECOLOR_LABEL_HEADER_CONTENT;
            lbHeaderIOC.BackColor = Constant.COLOR_TRANSPARENT;
            lbHeaderIOC.AutoSize = true;
            lbHeaderIOC.Text = Properties.Resources.MSG_GUI_0004;
            lbHeaderIOC.Location = new Point(0, 0);

            // Tên thí sinh
            ucRowInfor ucFullname = new ucRowInfor();
            SendUCRowInfor sucFullname = new SendUCRowInfor(ucFullname.HandleUC);
            sucFullname(Properties.Resources.MSG_GUI_0001, Controllers.Instance.CapitalizeString(CILogged.Fullname));
            ucFullname.Location = new Point(0, lbHeaderIOC.Bounds.Height);
            mpnContestantContent1.Controls.Add(ucFullname);

            // Số Báo Danh
            ucRowInfor ucSBD = new ucRowInfor();
            SendUCRowInfor sucSBD = new SendUCRowInfor(ucSBD.HandleUC);
            sucSBD(Properties.Resources.MSG_GUI_0017, CILogged.ContestantCode);
            ucSBD.Location = new Point(0, ucFullname.Bottom);
            mpnContestantContent1.Controls.Add(ucSBD);

            // Số CMND
            ucRowInfor ucStudentIdentify = new ucRowInfor();
            SendUCRowInfor sucStudentIdentify = new SendUCRowInfor(ucStudentIdentify.HandleUC);
            sucStudentIdentify(Properties.Resources.MSG_GUI_0068, CILogged.Unit);
            ucStudentIdentify.Location = new Point(0, ucSBD.Bottom);
            mpnContestantContent1.Controls.Add(ucStudentIdentify);

            // Tên môn thi
            ucRowInfor ucContestSubject = new ucRowInfor();
            SendUCRowInfor sucContestSubject = new SendUCRowInfor(ucContestSubject.HandleUC);
            sucContestSubject(Properties.Resources.MSG_GUI_0008, CILogged.SubjectName);
            ucContestSubject.Location = new Point(0, ucStudentIdentify.Bottom);
            mpnContestantContent1.Controls.Add(ucContestSubject);

            mpnContestantContent1.AutoSize = true;
            mpnContestantContent1.AutoScroll = false;
            mpnContestantContent1.Height = mpnContestantContent1.Height + 30;
            mpnContestantWrapper1.Location = new Point(0, lbHeaderIOC.Bottom - lbHeaderIOC.Height / 2);
            mpnContestantContent1.Location = new Point(40, 0);

            mpnContestantWrapper1.Height = mpnContestantContent1.Height + 80;
            mpnInformationWrapper1.Controls.Add(lbHeaderIOC);
            mpnContestantWrapper1.Controls.Add(mpnContestantContent1);
            mpnInformationWrapper1.Controls.Add(mpnContestantWrapper1);
            mpnInformationWrapper1.Size = new Size(Constant.WIDTH_PANEL_INFORMATION, 195);
            mpnInformationWrapper1.HorizontalScroll.Enabled = false;
            mpnInformationWrapper1.VerticalScroll.Enabled = false;
            foreach (Control e in lstControlBtnQuestions)
            {
                e.Click += new System.EventHandler(this.mbtnQuestion_Click);
                string[] arrTag = e.Tag.ToString().Split('_');
                int CurrentSubQuestionID = Convert.ToInt32(arrTag[1]);
                int Top = Convert.ToInt32(arrTag[0]);
                //flowlayoupanl
                foreach (Control c in flpnListOfQuestions.Controls)
                {
                    if (c.GetType().ToString().EndsWith(Constant.STRING_FLOWLAYOUTPANEL))
                    {
                        foreach (Control c1 in c.Controls)
                        {
                            if (Convert.ToInt32(c1.Tag) == CurrentSubQuestionID)
                            {
                                if (c1.GetType().ToString().EndsWith(Constant.STRING_QUESTION_HTML))
                                {
                                    (c1 as ucQuestionsHTML).mbtnControl = e;
                                }
                                else if (c1.GetType().ToString().EndsWith(Constant.STRING_QUESTION_RTF))
                                {
                                    (c1 as ucQuestionsRTF).mbtnControl = e;

                                }
                            }
                        }
                    }
                    else
                    {
                        if (Convert.ToInt32(c.Tag) == CurrentSubQuestionID)
                        {
                            if (c.GetType().ToString().EndsWith(Constant.STRING_QUESTION_HTML))
                            {
                                (c as ucQuestionsHTML).mbtnControl = e;
                            }
                            else if (c.GetType().ToString().EndsWith(Constant.STRING_QUESTION_RTF))
                            {
                                (c as ucQuestionsRTF).mbtnControl = e;
                            }
                        }
                    }
                }
                //flowlayoupanl2
                foreach (Control c in flpnListOfQuestions2.Controls)
                {
                    if (c.GetType().ToString().EndsWith(Constant.STRING_FLOWLAYOUTPANEL))
                    {
                        foreach (Control c1 in c.Controls)
                        {
                            if (Convert.ToInt32(c1.Tag) == CurrentSubQuestionID)
                            {
                                if (c1.GetType().ToString().EndsWith(Constant.STRING_QUESTION_HTML))
                                {
                                    (c1 as ucQuestionsHTML).mbtnControl = e;
                                }
                                else if (c1.GetType().ToString().EndsWith(Constant.STRING_QUESTION_RTF))
                                {
                                    (c1 as ucQuestionsRTF).mbtnControl = e;

                                }
                            }
                        }
                    }
                    else
                    {
                        if (Convert.ToInt32(c.Tag) == CurrentSubQuestionID)
                        {
                            if (c.GetType().ToString().EndsWith(Constant.STRING_QUESTION_HTML))
                            {
                                (c as ucQuestionsHTML).mbtnControl = e;
                            }
                            else if (c.GetType().ToString().EndsWith(Constant.STRING_QUESTION_RTF))
                            {
                                (c as ucQuestionsRTF).mbtnControl = e;
                            }
                        }
                    }
                }
                flpnListOfButtonQuestions.Controls.Add(e);
            }
            s(true);
        }
        #endregion



        #region Handle thread
        private void mbtnQuestion_Click(object sender, EventArgs e)
        {

            MetroButton mbtn = sender as MetroButton;
            string[] arrTag = mbtn.Tag.ToString().Split('_');
            int CurrentSubQuestionID = Convert.ToInt32(arrTag[1]);
            int Top = Convert.ToInt32(arrTag[0]);
            if (flpnListOfQuestions.Visible == true)
            {
                if (PreSubQuestionID != Constant.STATUS_NORMAL)
                {
                    foreach (Control c in flpnListOfQuestions.Controls)
                    {
                        if (c.GetType().ToString().EndsWith(Constant.STRING_FLOWLAYOUTPANEL))
                        {
                            foreach (Control c1 in c.Controls)
                            {
                                if (Convert.ToInt32(c1.Tag) == PreSubQuestionID)
                                {
                                    c1.BackColor = Constant.COLOR_WHITE;
                                }
                            }
                        }
                        else
                        {
                            if (Convert.ToInt32(c.Tag) == PreSubQuestionID)
                            {
                                c.BackColor = Constant.COLOR_WHITE;
                            }
                        }
                    }
                }

                foreach (Control c in flpnListOfQuestions.Controls)
                {
                    if (c.GetType().ToString().EndsWith(Constant.STRING_FLOWLAYOUTPANEL))
                    {
                        foreach (Control c1 in c.Controls)
                        {
                            if (Convert.ToInt32(c1.Tag) == CurrentSubQuestionID)
                            {
                                flpnListOfQuestions.AutoScrollPosition = new Point(0, Top - (Constant.HEIGHT_SCREEN - c1.Height) / 2);
                                c1.BackColor = Constant.BACKCOLOR_PANEL_QUESTION;
                            }
                        }
                    }
                    else
                    {
                        if (Convert.ToInt32(c.Tag) == CurrentSubQuestionID)
                        {
                            flpnListOfQuestions.AutoScrollPosition = new Point(0, Top - (Constant.HEIGHT_SCREEN - c.Height) / 2);
                            c.BackColor = Constant.BACKCOLOR_PANEL_QUESTION;
                        }
                    }
                }
            }
            else
            {
                if (PreSubQuestionID != Constant.STATUS_NORMAL)
                {
                    foreach (Control c in flpnListOfQuestions2.Controls)
                    {
                        if (c.GetType().ToString().EndsWith(Constant.STRING_FLOWLAYOUTPANEL))
                        {
                            foreach (Control c1 in c.Controls)
                            {
                                if (Convert.ToInt32(c1.Tag) == PreSubQuestionID)
                                {
                                    c1.BackColor = Constant.COLOR_WHITE;
                                }
                            }
                        }
                        else
                        {
                            if (Convert.ToInt32(c.Tag) == PreSubQuestionID)
                            {
                                c.BackColor = Constant.COLOR_WHITE;
                            }
                        }
                    }
                }

                foreach (Control c in flpnListOfQuestions2.Controls)
                {
                    if (c.GetType().ToString().EndsWith(Constant.STRING_FLOWLAYOUTPANEL))
                    {
                        foreach (Control c1 in c.Controls)
                        {

                            if (c1.GetType().ToString().EndsWith(Constant.STRING_QUESTION_HTML) || c1.GetType().ToString().EndsWith(Constant.STRING_QUESTION_RTF))
                            {
                                if (Convert.ToInt32(c1.Tag) == CurrentSubQuestionID)
                                {
                                    flpnListOfQuestions2.AutoScrollPosition = new Point(0, Top - (Constant.HEIGHT_SCREEN - c1.Height) / 2);
                                    c1.BackColor = Constant.BACKCOLOR_PANEL_QUESTION;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Convert.ToInt32(c.Tag) == CurrentSubQuestionID)
                        {
                            flpnListOfQuestions2.AutoScrollPosition = new Point(0, Top - (Constant.HEIGHT_SCREEN - c.Height) / 2);
                            c.BackColor = Constant.BACKCOLOR_PANEL_QUESTION;
                        }
                    }
                }
            }
            PreSubQuestionID = CurrentSubQuestionID;
        }
        #endregion

        /// <summary>
        /// Lấy thời gian hiện tại: ưu tiên giờ server, fallback sang Stopwatch + giờ server đã sync lúc bật app.
        /// Đảm bảo luôn có thời gian kể cả khi mất kết nối mạng.
        /// </summary>
        private DateTime GetCurrentTimeWithFallback()
        {
            try
            {
                return DAO.DAO.ConvertDateTime.GetDateTimeServer();
            }
            catch
            {
                // Mất kết nối: cộng thời gian đã trôi qua từ lúc sync với server
                return _serverTimeAtStart + _stopwatch.Elapsed;
            }
        }

        private DateTime GetCurrentTimeWithSource(out string source)
        {
            try
            {
                DateTime t = DAO.DAO.ConvertDateTime.GetDateTimeServer();
                source = "SERVER";
                return t;
            }
            catch
            {
                source = "LOCAL";
                return _serverTimeAtStart + _stopwatch.Elapsed;
            }
        }

        private void EnsureCurrentSessionFolderInitialized()
        {
            if (string.IsNullOrWhiteSpace(_currentSegmentGuid))
            {
                _currentSegmentGuid = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrWhiteSpace(_currentSessionFolder))
            {
                _currentSessionFolder = Path.Combine(
                    @"C:\ProgramData\EXON\ExamCache\active",
                    "CSTS_" + CILogged.ContestantShiftID + "_" + _currentSegmentGuid);
            }

            try
            {
                Directory.CreateDirectory(_currentSessionFolder);
            }
            catch
            {
                _currentSessionFolder = null;
            }
        }

        private void WriteSessionInfoFile()
        {
            EnsureCurrentSessionFolderInitialized();
            if (string.IsNullOrWhiteSpace(_currentSessionFolder))
            {
                return;
            }
            string path = Path.Combine(_currentSessionFolder, "session.info");
            LocalStateWriter.UpsertKeyValues(path, new Dictionary<string, string>
            {
                { "SegmentGuid", _currentSegmentGuid },
                { "ContestantShiftId", CILogged.ContestantShiftID.ToString() },
                { "ContestantId", CILogged.ContestantID.ToString() },
                { "DivisionShiftId", CILogged.DivisionShiftID.ToString() },
                { "AnswerSheetId", CILogged.AnswerSheetID.ToString() },
                { "ContestantCode", CILogged.ContestantCode },
                { "ComputerName", Environment.MachineName }
            });
        }

        private void WriteRuntimeStateFile()
        {
            EnsureCurrentSessionFolderInitialized();
            if (string.IsNullOrWhiteSpace(_currentSessionFolder))
            {
                return;
            }
            string path = Path.Combine(_currentSessionFolder, "runtime.state");
            LocalStateWriter.UpsertKeyValues(path, new Dictionary<string, string>
            {
                { "ContestantShiftId", CILogged.ContestantShiftID.ToString() },
                { "ContestantId", CILogged.ContestantID.ToString() },
                { "DivisionShiftId", CILogged.DivisionShiftID.ToString() },
                { "AnswerSheetId", CILogged.AnswerSheetID.ToString() },
                { "Status", CILogged.Status.ToString() },
                { "SubmitTimeUnixMs", CILogged.SubmitTimeUnixMs > 0 ? CILogged.SubmitTimeUnixMs.ToString() : string.Empty },
                { "SubmitTimeText", CILogged.SubmitTimeText },
                { "TimeWorkedMs", CILogged.TimeWorkedMs > 0 ? CILogged.TimeWorkedMs.ToString() : string.Empty },
                { "TimeWorkedText", CILogged.TimeWorkedMsText },
                { "TimeSource", CILogged.TimeSource },
                { "ComputerName", Environment.MachineName },
                { "UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") }
            });
        }

        private void WriteFinalResultFile()
        {
            EnsureCurrentSessionFolderInitialized();
            if (string.IsNullOrWhiteSpace(_currentSessionFolder))
            {
                return;
            }
            string path = Path.Combine(_currentSessionFolder, "final.result");
            LocalStateWriter.UpsertKeyValues(path, new Dictionary<string, string>
            {
                { "ContestantShiftId", CILogged.ContestantShiftID.ToString() },
                { "ContestantId", CILogged.ContestantID.ToString() },
                { "DivisionShiftId", CILogged.DivisionShiftID.ToString() },
                { "AnswerSheetId", CILogged.AnswerSheetID.ToString() },
                { "SubmitTimeUnixMs", CILogged.SubmitTimeUnixMs > 0 ? CILogged.SubmitTimeUnixMs.ToString() : string.Empty },
                { "SubmitTimeText", CILogged.SubmitTimeText },
                { "TimeWorkedMs", CILogged.TimeWorkedMs > 0 ? CILogged.TimeWorkedMs.ToString() : string.Empty },
                { "TimeWorkedText", CILogged.TimeWorkedMsText },
                { "TimeSource", CILogged.TimeSource },
                { "Score", rASH != null && rASH.TestScores.HasValue ? rASH.TestScores.Value.ToString("F2") : string.Empty },
                { "ComputerName", Environment.MachineName },
                { "UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") }
            });
        }

        private void CopyFileToSessionFolder(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                return;
            }

            EnsureCurrentSessionFolderInitialized();
            if (string.IsNullOrWhiteSpace(_currentSessionFolder))
            {
                return;
            }
            string targetPath = Path.Combine(_currentSessionFolder, Path.GetFileName(sourcePath));
            try
            {
                File.Copy(sourcePath, targetPath, true);
            }
            catch
            {
            }
        }

        private string GetConnectionLogPath()
        {
            return Path.Combine(@"C:\ProgramData\EXON\Logs",
                string.Format("{0}_{1}_connection.log", CILogged.ContestantCode, CILogged.ContestantShiftID));
        }

        private string GetActionLogPath()
        {
            return Path.Combine(@"C:\ProgramData\EXON\Logs",
                string.Format("{0}_{1}_actions.log", CILogged.ContestantCode, CILogged.ContestantShiftID));
        }

        private string GetLatestSubmitLogPath()
        {
            try
            {
                string folder = @"C:\ProgramData\EXON\SubmitLogs";
                if (!Directory.Exists(folder))
                {
                    return null;
                }

                string pattern = string.Format("{0}_{1}_*.submitlog", CILogged.ContestantCode, CILogged.ContestantShiftID);
                string[] files = Directory.GetFiles(folder, pattern);
                if (files.Length == 0)
                {
                    return null;
                }

                return files.OrderByDescending(x => x).FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private string GetAnswerHistoryPath()
        {
            return Path.Combine(@"C:\ProgramData\EXON\AnswerHistory", CILogged.ContestantShiftID + ".json");
        }

        private void CaptureCurrentExamLogsToSessionFolder()
        {
            EnsureCurrentSessionFolderInitialized();
            WriteSessionInfoFile();
            WriteRuntimeStateFile();
            WriteFinalResultFile();
            CopyFileToSessionFolder(GetConnectionLogPath());
            CopyFileToSessionFolder(GetActionLogPath());
            CopyFileToSessionFolder(GetLatestSubmitLogPath());
            CopyFileToSessionFolder(GetAnswerHistoryPath());
        }

        private void UploadCurrentExamLogsToServer()
        {
            try
            {
                EnsureCurrentSessionFolderInitialized();
                CaptureCurrentExamLogsToSessionFolder();

                SessionLogUploadService service = new SessionLogUploadService();
                long? answerSheetId = CILogged.AnswerSheetID > 0 ? (long?)CILogged.AnswerSheetID : null;

                service.UploadCurrentSessionLogs(
                    _currentSessionFolder,
                    CILogged.ContestantShiftID,
                    CILogged.ContestantID,
                    CILogged.DivisionShiftID,
                    answerSheetId,
                    _currentSegmentGuid,
                    Sql);

                LocalStateWriter.UpsertKeyValues(Path.Combine(_currentSessionFolder, "runtime.state"), new Dictionary<string, string>
                {
                    { "UploadStatus", "Success" },
                    { "UploadedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") },
                    { "LastUploadMessage", string.Empty }
                });
            }
            catch (Exception ex)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(_currentSessionFolder))
                    {
                        LocalStateWriter.UpsertKeyValues(Path.Combine(_currentSessionFolder, "runtime.state"), new Dictionary<string, string>
                        {
                            { "UploadStatus", "Fail" },
                            { "UploadedAt", string.Empty },
                            { "LastUploadMessage", ex.Message }
                        });
                    }
                }
                catch
                {
                }

                try
                {
                    Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, "UPLOAD_EXAM_LOGS | " + ex);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Ghi log nộp bài ra file local tại C:\ProgramData\EXON\
        /// Dùng khi mất mạng không lưu được vào CSDL, để làm căn cứ xếp hạng sau.
        /// Format: key=value, mỗi dòng 1 field.
        /// </summary>
        private void WriteSubmitLog(float score, float maxScore, string submitTimeText, string timeWorkedMsText, string reason, string timeSource = "SERVER")
        {
            try
            {
                string folder = @"C:\ProgramData\EXON\SubmitLogs";
                System.IO.Directory.CreateDirectory(folder);

                string fileName = string.Format("{0}_{1}_{2}.submitlog",
                    CILogged.ContestantCode,
                    CILogged.ContestantShiftID,
                    DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));

                string path = System.IO.Path.Combine(folder, fileName);

                var lines = new System.Text.StringBuilder();
                lines.AppendLine("ContestantCode=" + (CILogged.ContestantCode ?? ""));
                lines.AppendLine("ContestantShiftID=" + CILogged.ContestantShiftID);
                lines.AppendLine("ContestantID=" + CILogged.ContestantID);
                lines.AppendLine("Score=" + score.ToString("F2"));
                lines.AppendLine("MaxScore=" + maxScore.ToString("F2"));
                lines.AppendLine("SubmitTimeText=" + (submitTimeText ?? ""));
                lines.AppendLine("TimeWorkedMsText=" + (timeWorkedMsText ?? ""));
                lines.AppendLine("TimeSource=" + timeSource);
                lines.AppendLine("Reason=" + reason);
                lines.AppendLine("LoggedAt=" + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff"));
                lines.AppendLine("ComputerName=" + System.Net.Dns.GetHostName());

                System.IO.File.WriteAllText(path, lines.ToString(), System.Text.Encoding.UTF8);
                Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "SUBMIT_LOG", "Đã ghi log nộp bài: " + path);
            }
            catch (Exception ex)
            {
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, "WriteSubmitLog lỗi: " + ex.Message);
            }
        }

        /// <summary>
        /// Thay đổi trạng thái thí sinh sang đã hoàn thành bài thi STATUS_FINISHED
        /// </summary>
        private void ChangeContestantStatusToFinished()
        {
            ErrorController rEC = new ErrorController();
            // Change status contestant to STATUS_FINISHED
            CILogged.Status = WarningCount == 3 ? Constant.STATUS_REJECT : Constant.STATUS_FINISHED;
            CILogged.IsDisconnected = true;

            ContestantBUS.Instance.ChangeStatusContestant(CILogged, CLogged, out rEC, Sql);
            if (rEC.ErrorCode == Constant.STATUS_OK)
            {
                CILogged.SubmitSuccess = true;
                Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | CHANGE_STATUS_CONTESTANT | STATUS_FINISHED | ChangeContestantStatusToFinished", Controllers.Instance.HandleStringErrorController(rEC));
                UpdateExamHistoryButtonState();

            }
            else
            {
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));
                ShowRuntimeServerMessage("Mất kết nối server, vui lòng thử lại hoặc liên hệ giám thị.");
            }
        }

        private void SafeSetAuthenticationLogFileAttributes(FileAttributes attributes)
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

        private void ShowRuntimeServerMessage(string message)
        {
            if (_hasShownServerDisconnectMessage)
            {
                return;
            }

            _hasShownServerDisconnectMessage = true;
            MessageBox.Show(message, Properties.Resources.MSG_MESS_0001, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void HandleRuntimeServerDisconnect()
        {
            Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, "MAIN | SERVER_DISCONNECT");
            if (TimerSubmit != null)
            {
                TimerSubmit.Stop();
            }

            if (timerGetMaxTime != null)
            {
                timerGetMaxTime.Stop();
            }

            ShowRuntimeServerMessage("Mất kết nối server. Bài thi vẫn được giữ trên máy, vui lòng liên hệ giám thị.");
        }

        private bool CanViewExamHistory()
        {
            return CILogged != null && CILogged.Status == Constant.STATUS_FINISHED;
        }

        private void UpdateExamHistoryButtonState()
        {
            if (mbtnViewExamHistory != null)
            {
                mbtnViewExamHistory.Enabled = CanViewExamHistory();
            }
        }

        /// <summary>
        /// Xử lý thoát chương trình khi không thể kết nối đến máy giám thị
        /// </summary>
        private void ClosingSocket()
        {
            Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_INFO, Properties.Resources.MSG_CON_0002, this);
            if (DTO.NotificationForm.DialogResult == DialogResult.OK)
            {

                Application.ExitThread();
                Environment.Exit(0);
            }
        }
        /// <summary>
        /// Xử khi client không có kết nôi với máy giám thị
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>

        public int countcheck = 0;

        /// <summary>
        /// Xử lý nhận thông tin nhận được từ server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>


        private int GetValueOfWarningCount(int warningCount)
        {
            if (warningCount == 1)
            {
                return Properties.Settings.Default.Warning1;
            }
            else if (warningCount == 2)
            {
                return Properties.Settings.Default.Warning2;
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// Xử lý khi tắt chương trình
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmMainForm_FormClosing(object sender, FormClosingEventArgs e)
        {

            ErrorController rEC = new ErrorController();
            if (CILogged.Status == Constant.STATUS_DOING)
            {
                if (maxTime != 0)
                {
                    // Đổi trạng thái thí sinh sang trạng thái  STATUS_DOING_BUT_INTERRUPT
                    CILogged.Status = Constant.STATUS_DOING_BUT_INTERRUPT;
                    ConnectionLogger.Log("DISCONNECT", "FormClosing STATUS_DOING");
                    CILogged.IsDisconnected = true;
                    // ms fields để null khi gián đoạn (chưa nộp bài)
                    CILogged.EndTimeMsText = null;
                    CILogged.TimeWorkedMsText = null;
                    CILogged.SubmitTimeText = null;
                    CILogged.SubmitTimeUnixMs = 0;
                    CILogged.TimeWorkedMs = 0;
                    CILogged.TimeSource = null;
                    ContestantBUS.Instance.ChangeStatusContestant(CILogged, CLogged, out rEC, Sql);
                    if (rEC.ErrorCode == Constant.STATUS_OK)
                    {
                        Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | CHANGE_STATUS_CONTESTANT | STATUS_DOING_BUT_INTERRUPT | frmMainForm_FormClosing | STATUS_DOING", Controllers.Instance.HandleStringErrorController(rEC));
                        // Send message to supervisor STATUS_DOING_BUT_INTERRUPT

                    }
                    else
                    {
                        Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));
                        Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), this);
                        Controllers.Instance.ExitApplicationFromNotificationForm(this);
                    }
                }
                else
                {
                    // Hết giờ (maxTime == 0): tính ms trước khi lưu FINISHED, dùng fallback nếu mất mạng
                    try
                    {
                        string _srcTimeout;
                        DateTime endTimeNow = GetCurrentTimeWithSource(out _srcTimeout);
                        CILogged.EndTimeMsText = endTimeNow.ToString("HH:mm:ss:fff");
                        CILogged.SubmitTimeText = endTimeNow.ToString("dd-MM-yyyy HH:mm:ss.fff");
                        CILogged.SubmitTimeUnixMs = (long)(endTimeNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds;
                        CILogged.TimeSource = _srcTimeout;

                        int timeStartUnix = BUS.ContestantBUS.Instance.GetTimeStartFromContestant(CILogged.ContestantShiftID);
                        if (timeStartUnix > 0)
                        {
                            long timeStartMs = (long)timeStartUnix * 1000L;
                            long thoiGianBuMs = (long)ThoiGianBu * 1000L;
                            long workedMs = CILogged.SubmitTimeUnixMs - timeStartMs - thoiGianBuMs;
                            if (workedMs < 0) workedMs = 0;
                            CILogged.TimeWorkedMs = workedMs;
                            CILogged.TimeWorkedMsText = TimeSpan.FromMilliseconds(workedMs).ToString(@"hh\:mm\:ss\:fff");
                        }
                        WriteSubmitLog(0f, 0f, CILogged.SubmitTimeText, CILogged.TimeWorkedMsText, "TIMEOUT", _srcTimeout);
                        ConnectionLogger.Log("SUBMIT", string.Format("TIMEOUT Source={0}", _srcTimeout));
                        ConnectionLogger.WriteToFile(CILogged.ContestantCode, CILogged.ContestantShiftID);
                        AnswerSelectionLogger.WriteToFile(CILogged.ContestantCode, CILogged.ContestantShiftID);
                    }
                    catch { }

                    ChangeContestantStatusToFinished();
                }
            }
            else if (CILogged.Status == Constant.STATUS_READY || CILogged.Status == Constant.STATUS_LOGGED_DO_NOT_FINISH)
            {
                // Đổi trạng thái thí sinh sang trạng thái  STATUS_DOING_BUT_INTERRUPT
                CILogged.Status = Constant.STATUS_DOING_BUT_INTERRUPT;
                ContestantBUS.Instance.ChangeStatusContestant(CILogged, CLogged, out rEC, Sql);
                if (rEC.ErrorCode == Constant.STATUS_OK)
                {
                    Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | CHANGE_STATUS_CONTESTANT | STATUS_DOING_BUT_INTERRUPT | frmMainForm_FormClosing | STATUS_READY||STATUS_LOGGED_DO_NOT_FINISH", Controllers.Instance.HandleStringErrorController(rEC));
                    // Send message to supervisor STATUS_DOING_BUT_INTERRUPT

                }
                else
                {
                    Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));
                    Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), this);
                    Controllers.Instance.ExitApplicationFromNotificationForm(this);
                }
            }
            else if (CILogged.Status == Constant.STATUS_LOGGED || CILogged.Status == Constant.STATUS_READY_TO_GET_TEST)
            {
                // Đổi trạng thái thí sinh sang trạng thái  STATUS_INITIALIZE
                CILogged.Status = Constant.STATUS_INITIALIZE;
                ContestantBUS.Instance.ChangeStatusContestant(CILogged, CLogged, out rEC, Sql);
                if (rEC.ErrorCode == Constant.STATUS_OK)
                {
                    Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | CHANGE_STATUS_CONTESTANT | STATUS_INITIALIZE | frmMainForm_FormClosing | STATUS_LOGGED||STATUS_READY_TO_GET_TEST", Controllers.Instance.HandleStringErrorController(rEC));
                    // Send message to supervisor STATUS_INITIALIZE

                }
                else
                {
                    Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));
                    Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), this);
                    Controllers.Instance.ExitApplicationFromNotificationForm(this);
                }
            }


            string pathfile = Constant.PATH_EXON;
            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "MAIN | " + Properties.Resources.MSG_MESS_0010, Properties.Resources.MSG_MESS_0011);
            if (Directory.Exists(Path.Combine(pathfile, "temp")))
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(Path.Combine(pathfile) + "\\temp");
                foreach (FileInfo file in di.GetFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {

                    }

                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
            try
            {
                if (Sql != null && Sql.State != ConnectionState.Closed)
                {
                    Sql.Close();
                }
            }
            catch
            {
            }
            // kill process nghe
            foreach (Process process in Process.GetProcessesByName("WMPLib"))
            {

                process.Kill();

            }
            Application.ExitThread();
            Environment.Exit(0);
        }



        /// <summary>
        /// Alt +f4
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        //protected override System.Boolean ProcessCmdKey(ref
        //    System.Windows.Forms.Message msg, System.Windows.Forms.Keys
        //    keyData)
        //{

        //    if (keyData == (System.Windows.Forms.Keys.Menu | System.Windows.Forms.Keys.Alt))
        //        return false;
        //    return true;
        //}
    }
}
public struct ObjControl
{
    public List<Questions> Question;
    public int Width;
    public string Top;
    public int AnswerSheetID;

    public ObjControl(List<Questions> question, int answerSheetID, int width)
    {
        this.Question = question;
        this.Width = width;
        this.Top = string.Empty;
        this.AnswerSheetID = answerSheetID;
    }
    public ObjControl(string top, int width)
    {
        this.Question = null;
        this.Width = width;
        this.Top = top;
        this.AnswerSheetID = 0;
    }
}
