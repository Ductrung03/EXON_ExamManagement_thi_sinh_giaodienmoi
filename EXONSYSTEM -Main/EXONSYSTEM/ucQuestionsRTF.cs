using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using EXONSYSTEM.Common;
using MetroFramework.Controls;
using DAO.DataProvider;

using System.Diagnostics;
using BUS;
using System.Threading;
using EXONSYSTEM.Layout;
using EXONSYSTEM.Controls;
using System.IO;

using System.Reflection;
using System.Data.SqlClient;
using TXTextControl.Windows.Forms;

namespace EXONSYSTEM
{
    public partial class ucQuestionsRTF : MetroUserControl
    {
        public Control mbtnControl { get; set; }
        public Questions q;
        public AnswersheetDetail AD;
        //  private frmViewRtf frm;
        public event EventHandler SendWorking;
        private string pathfile = Common.Constant.PATH_EXON;
        private string pathfileHelp = Common.Constant.pathTempHelp;
        public bool CheckCombobox = false;
        private SqlConnection Sql;
        private TRichTextBox.AdvanRichTextBox temp;
        private bool _isRestoringAnswer;
        public ucQuestionsRTF(SqlConnection sql)
        {
            InitializeComponent();
            this.UseCustomBackColor = true;
            this.BackColor = Constant.COLOR_WHITE;
            mpnAnswers.BackColor = Constant.COLOR_WHITE;
            pnTitleOfQuestion.BackColor = Constant.COLOR_WHITE;
            Sql = sql;

        }

        private void RichTexBox_Click(object sender, EventArgs e)
        {
            TXTextControl.TextControl rtb = sender as TXTextControl.TextControl;

            switch (rtb.Name)
            {
                case "rtbAnswerA":
                    mrbAnswerA.PerformClick();
                    break;
                case "rtbAnswerB":
                    mrbAnswerB.PerformClick();
                    break;
                case "rtbAnswerC":
                    mrbAnswerC.PerformClick();
                    break;
                case "rtbAnswerD":
                    mrbAnswerD.PerformClick();
                    break;
            }
        }
        private void ucQuestionsRTF_Load(object sender, EventArgs e)
        {
            try
            {
                temp = new TRichTextBox.AdvanRichTextBox();
                temp.Width = pnTitleOfQuestion.Width - 10;
                pnTitleOfQuestion.Location = new Point(10, 10);
                pnTitleOfQuestion.Width = this.Width - 20;

                lbNumber.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Underline | FontStyle.Bold);

                lbNumber.Text = string.Format(Properties.Resources.MSG_GUI_0067, q.NO, $"{q.Score:F3}");
                lbNumber.Width = 250;

                lbNumber.Location = new Point(0, 0);

                rtbTitleOfQuestion.Location = new Point(0, lbNumber.Bottom + 5);
                Controllers.Instance.HandleRichTextBoxStyle(rtbTitleOfQuestion);

                rtbTitleOfQuestion.Width = pnTitleOfQuestion.Width - 15;
                mpnAnswers.Width = pnTitleOfQuestion.Width;
                int locationLeftRadio = 50 / 2;

                //  viết có đáp án
                if (q.Type == (int)Constant.QuizTypeEnum.Fill || q.Type == (int)Constant.QuizTypeEnum.FillAudio)
                {

                    mpnAnswers.Controls.Clear();
                    mpnAnswers.Update();
                    rtbTitleOfQuestion.Load(q.TitleOfQuestion, TXTextControl.StringStreamType.RichTextFormat);
                    temp.Rtf = q.TitleOfQuestion;
                    rtbTitleOfQuestion.Height = temp.Height;
                    pnTitleOfQuestion.Height = Controllers.Instance.GetHeightBetter(lbNumber.Height, rtbTitleOfQuestion.Height);
                    Label lblAnswser = new Label();
                    lblAnswser.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
                    lblAnswser.Text = "Trả lời: ";

                    lblAnswser.Location = new Point(locationLeftRadio, 5);
                    mpnAnswers.Controls.Add(lblAnswser);

                    //TXTextControl.TextControl mtxtAnswer = new TXTextControl.TextControl();
                    RichTextBox mtxtAnswer = new RichTextBox();
                    mtxtAnswer.Location = new Point(lblAnswser.Right + 4, 3);
                    mtxtAnswer.Name = "mtxtAnswer";
                    mtxtAnswer.Width = 600;
                    mtxtAnswer.Height = 50;
                    mtxtAnswer.Text = q.AnswerSheetContent;
                    //mtxtAnswer.ScrollBars = ScrollBars.Both;
                    mtxtAnswer.ScrollBars = RichTextBoxScrollBars.Both;
                    mtxtAnswer.Multiline = true;
                    mtxtAnswer.TextChanged += RtbAnswerA_TextChanged;

                    mpnAnswers.Controls.Add(mtxtAnswer);
                    mpnAnswers.Location = new Point(10, pnTitleOfQuestion.Bottom);
                    MetroButton mbtnSave = new MetroButton();
                    mbtnSave.Name = "mbtnSave";
                    mbtnSave.Text = "Lưu";
                    mbtnSave.UseCustomBackColor = true;
                    mbtnSave.UseCustomForeColor = true;

                    mbtnSave.Location = new Point(locationLeftRadio + 50, mtxtAnswer.Bottom + 8);
                    mbtnSave.BackColor = Constant.BACKCOLOR_BUTTON_CONTROLLER;
                    mbtnSave.ForeColor = Constant.FORCECOLOR_BUTTON_SUBMIT;
                    mbtnSave.Size = new Size(150, 40);
                    mbtnSave.Click += new EventHandler(MbtnSaveforText_Click);
                    mpnAnswers.Controls.Add(mbtnSave);
                    mpnAnswers.Height = mbtnSave.Bottom + 10;

                    // Code bỏ chức năng paste cho câu hỏi viết lại câu
                    // Chỉ sử dụng cho bản Thi NN
                    // Author: Dai Pham
                    mtxtAnswer.KeyDown += mtxtAnswer_KeyDown;      
                }
                // rewritting
                else if (q.Type == (int)Constant.QuizTypeEnum.ReWritting)
                {
                  
                    mpnAnswers.Controls.Clear();
                    mpnAnswers.Update();
                    rtbTitleOfQuestion.Load(q.TitleOfQuestion, TXTextControl.StringStreamType.RichTextFormat);
                    temp.Rtf = q.TitleOfQuestion;
                    rtbTitleOfQuestion.Height = temp.Height;
                    pnTitleOfQuestion.Height = Controllers.Instance.GetHeightBetter(lbNumber.Height, rtbTitleOfQuestion.Height);
                    //TODO
                    Label lblAnswser = new Label();
                    lblAnswser.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
                    lblAnswser.Text = "Trả lời: ";
                    lblAnswser.Location = new Point(5, 5);
                    mpnAnswers.Controls.Add(lblAnswser);
                    //TRichTextBox.RTFAnswer mtxtAnswer = new TRichTextBox.RTFAnswer();
                    RichTextBox mtxtAnswer = new RichTextBox();
                    mtxtAnswer.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT);
                    mtxtAnswer.BackColor = Color.White;
                    mtxtAnswer.Name = "mtxtAnswer";
                    mtxtAnswer.Location = new Point(0, lblAnswser.Bottom + 3);
                    mtxtAnswer.Width = mpnAnswers.Width;
                    mtxtAnswer.Height = 150;
                    mtxtAnswer.Rtf = q.AnswerSheetContent;


                    mtxtAnswer.TextChanged += RtbAnswerAForEssay_TextChanged;

                    mtxtAnswer.PreviewKeyDown += MtxtAnswer_PreviewKeyDown;

                    mpnAnswers.Font = mtxtAnswer.Font;
                    mpnAnswers.Controls.Add(mtxtAnswer);
                    mpnAnswers.Location = new Point(10, pnTitleOfQuestion.Bottom);
                    MetroButton mbtnSave = new MetroButton();
                    mbtnSave.Name = "mbtnSave";
                    mbtnSave.Text = "Lưu";
                    mbtnSave.UseCustomBackColor = true;
                    mbtnSave.UseCustomForeColor = true;

                    mbtnSave.Location = new Point(locationLeftRadio + 8, mtxtAnswer.Bottom + 8);
                    mbtnSave.BackColor = Constant.BACKCOLOR_BUTTON_CONTROLLER;
                    mbtnSave.ForeColor = Constant.FORCECOLOR_BUTTON_SUBMIT;
                    mbtnSave.Size = new Size(150, 40);
                    mbtnSave.Click += new EventHandler(MbtnSave_Click);
                    mpnAnswers.Controls.Add(mbtnSave);
                    mpnAnswers.Height = mpnAnswers.Bottom + 10;

                    // Code bỏ chức năng paste cho câu hỏi viết lại câu
                    // Chỉ sử dụng cho bản Thi NN
                    // Author: Dai Pham
                    mtxtAnswer.KeyDown += mtxtAnswer_KeyDown;

                }
                //cho câu hỏi writing
                else if (q.Type == (int)Constant.QuizTypeEnum.Essay)
                {
                    rtbTitleOfQuestion.Load(q.TitleOfQuestion, TXTextControl.StringStreamType.RichTextFormat);
                    temp.Rtf = q.TitleOfQuestion;
                    rtbTitleOfQuestion.Height = temp.Height;
                    pnTitleOfQuestion.Height = Controllers.Instance.GetHeightBetter(lbNumber.Height, rtbTitleOfQuestion.Height);
                    mpnAnswers.Controls.Clear();
                    mpnAnswers.Update();
                    //TODO
                    Label lblAnswser = new Label();
                    lblAnswser.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Bold);
                    lblAnswser.Text = "Trả lời: ";
                    lblAnswser.Location = new Point(5, 5);
                    mpnAnswers.Controls.Add(lblAnswser);
                    //TRichTextBox.RTFAnswer mtxtAnswer = new TRichTextBox.RTFAnswer();
                    RichTextBox mtxtAnswer = new RichTextBox();
                    mtxtAnswer.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT);
                    mtxtAnswer.BackColor = Color.White;
                    mtxtAnswer.Name = "mtxtAnswer";
                    mtxtAnswer.Location = new Point(0, lblAnswser.Bottom + 3);
                    mtxtAnswer.Width = mpnAnswers.Width;
                    mtxtAnswer.Height = 400;
                    mtxtAnswer.Rtf = q.AnswerSheetContent;


                    mtxtAnswer.TextChanged += RtbAnswerAForEssay_TextChanged;

                    mtxtAnswer.PreviewKeyDown += MtxtAnswer_PreviewKeyDown;
                    mpnAnswers.Location = new Point(10, pnTitleOfQuestion.Bottom);

                    mpnAnswers.Font = mtxtAnswer.Font;
                    mpnAnswers.Controls.Add(mtxtAnswer);
                    //mpnAnswers.Height = mtxtAnswer.Bottom + 10;
                    // button soạn thảo

                    MetroButton mbtnOpen = new MetroButton();
                    mbtnOpen.Name = "mbtnOpen";
                    mbtnOpen.Text = "Soạn thảo với Word";
                    mbtnOpen.UseCustomBackColor = true;
                    mbtnOpen.UseCustomForeColor = true;
                    mbtnOpen.Location = new Point(locationLeftRadio + 8, mtxtAnswer.Bottom + 8);
                    mbtnOpen.BackColor = Constant.BACKCOLOR_BUTTON_CONTROLLER;
                    mbtnOpen.ForeColor = Constant.FORCECOLOR_BUTTON_SUBMIT;
                    mbtnOpen.Size = new Size(150, 40);
                    mbtnOpen.Click += new EventHandler(MbtnOpen_Click);
                    mpnAnswers.Controls.Add(mbtnOpen);
                    /// Button refresh

                    MetroButton mbtnRefresh = new MetroButton();
                    mbtnRefresh.Name = "mbtnRefresh";
                    mbtnRefresh.Text = "Đọc từ file đã soạn thảo";
                    mbtnRefresh.UseCustomBackColor = true;
                    mbtnRefresh.UseCustomForeColor = true;

                    mbtnRefresh.Location = new Point(mbtnOpen.Right + 8, mtxtAnswer.Bottom + 8);
                    mbtnRefresh.BackColor = Constant.BACKCOLOR_BUTTON_CONTROLLER;
                    mbtnRefresh.ForeColor = Constant.FORCECOLOR_BUTTON_SUBMIT;
                    mbtnRefresh.Size = new Size(150, 40);
                    mbtnRefresh.Click += new EventHandler(MbtnRefresh_Click);
                    mpnAnswers.Controls.Add(mbtnRefresh);
                    // button lưu
                    MetroButton mbtnSave = new MetroButton();
                    mbtnSave.Name = "mbtnSave";
                    mbtnSave.Text = "Lưu";
                    mbtnSave.UseCustomBackColor = true;
                    mbtnSave.UseCustomForeColor = true;

                    mbtnSave.Location = new Point(mbtnRefresh.Right + 8, mtxtAnswer.Bottom + 8);
                    mbtnSave.BackColor = Constant.BACKCOLOR_BUTTON_CONTROLLER;
                    mbtnSave.ForeColor = Constant.FORCECOLOR_BUTTON_SUBMIT;
                    mbtnSave.Size = new Size(150, 40);
                    mbtnSave.Click += new EventHandler(MbtnSave_Click);
                    mpnAnswers.Controls.Add(mbtnSave);

                    //trợ giúp 
                    MetroButton mbtnHelp = new MetroButton();
                    mbtnHelp.Name = "mbtnHelp";
                    mbtnHelp.Text = "Trợ giúp";
                    mbtnHelp.UseCustomBackColor = true;
                    mbtnHelp.UseCustomForeColor = true;

                    mbtnHelp.Location = new Point(mbtnSave.Right + 8, mtxtAnswer.Bottom + 8);
                    mbtnHelp.BackColor = Constant.BACKCOLOR_BUTTON_CONTROLLER;
                    mbtnHelp.ForeColor = Constant.FORCECOLOR_BUTTON_SUBMIT;
                    mbtnHelp.Size = new Size(150, 40);
                    mbtnHelp.Click += new EventHandler(MbtnHelp_Click);
                    mpnAnswers.Controls.Add(mbtnHelp);
                    mpnAnswers.Height = mbtnOpen.Bottom + 10;

                    // Code ẩn nút Soạn thảo với word, Đọc từ file đã soạn thảo, Trợ giúp, bỏ chức năng paste cho câu hỏi viết
                    // Chỉ sử dụng cho bản Thi NN
                    // Author: Dai Pham
                    mbtnOpen.Visible = false;
                    mbtnRefresh.Visible = false;
                    mbtnHelp.Visible = false;
                    mtxtAnswer.KeyDown += mtxtAnswer_KeyDown;

                }
                // cho câu hỏi nối và nghe nối
                else if (q.Type == (int)Constant.QuizTypeEnum.Match || q.Type == (int)Constant.QuizTypeEnum.ListenMatching)
                {

                    rtbTitleOfQuestion.Load(q.TitleOfQuestion, TXTextControl.StringStreamType.RichTextFormat);
                    temp.Rtf = q.TitleOfQuestion;
                    rtbTitleOfQuestion.Height = temp.Height;
                    pnTitleOfQuestion.Height = Controllers.Instance.GetHeightBetter(lbNumber.Height, rtbTitleOfQuestion.Height);
                    MetroComboBox mcbAnswer = new MetroComboBox();

                    mcbAnswer.Font = new Font(Constant.FONT_FAMILY_DEFAULT, Constant.FONT_SIZE_DEFAULT, FontStyle.Regular);
                    mcbAnswer.Location = new Point(locationLeftRadio, 5);



                    List<AnswerMatch> lstAnswer = new List<AnswerMatch>();
                    for (int i = 0; i < q.ListAnswer.Count; i++)
                    {
                        RichTextBox rtfAnswer = new RichTextBox();
                        rtfAnswer.Rtf = q.ListAnswer[i].AnswerContent;
                        AnswerMatch awm = new AnswerMatch();
                        string str = rtfAnswer.Text.Replace("\n", String.Empty);
                        awm.AnswerContent = str;
                        awm.AnswerID = q.ListAnswer[i].AnswerID;
                        lstAnswer.Add(awm);
                    }
                    lstAnswer = lstAnswer.OrderBy(x => x.AnswerContent).ToList();
                    mcbAnswer.DisplayMember = "AnswerContent";
                    mcbAnswer.ValueMember = "AnswerID";
                    mcbAnswer.DataSource = lstAnswer; 


                    RichTextBox MatchingAnswer = new RichTextBox();

                    MatchingAnswer.Rtf = q.AnswerSheetContent;
                    string str2 = MatchingAnswer.Text.Replace("\n", String.Empty);
                    mcbAnswer.Text = str2;

                    mcbAnswer.SelectedValueChanged += McbAnswer_SelectedValueChanged;
                    mpnAnswers.Controls.Add(mcbAnswer);
                    mpnAnswers.Height = mcbAnswer.Bottom + 10;
                    // ẩn control không dùng đến
                    mrbAnswerA.Visible = false;
                    mrbAnswerB.Visible = false;
                    mrbAnswerC.Visible = false;
                    mrbAnswerD.Visible = false;
                    rtbAnswerA.Visible = false;
                    rtbAnswerB.Visible = false;
                    rtbAnswerC.Visible = false;
                    rtbAnswerD.Visible = false;
                }
                else
                {
                 
                    mrbAnswerA.Location = new Point(locationLeftRadio, 0);
                    Controllers.Instance.SetStyleRadioButton(mrbAnswerA, Properties.Resources.MSG_GUI_0022);
                    Controllers.Instance.HandleRichTextBoxStyle(rtbAnswerA);
                    rtbAnswerA.Location = new Point(mrbAnswerA.Right + 10, 5);
                    rtbAnswerA.Width = mpnAnswers.Width - locationLeftRadio - mrbAnswerA.Width - 20;

                    rtbTitleOfQuestion.Load(q.TitleOfQuestion, TXTextControl.StringStreamType.RichTextFormat);
                    temp.Rtf = q.TitleOfQuestion;
                    //rtbTitleOfQuestion.Height = temp.Height;

                    pnTitleOfQuestion.Height = rtbTitleOfQuestion.Bottom + 10;

                    mpnAnswers.Location = new Point(0, pnTitleOfQuestion.Bottom);

                    rtbAnswerA.ViewMode = TXTextControl.ViewMode.SimpleControl;
                    rtbAnswerA.Load(q.ListAnswer[0].AnswerContent, TXTextControl.StringStreamType.RichTextFormat);
     

                    temp.Width = mpnAnswers.Width;
                    temp.Rtf = q.ListAnswer[0].AnswerContent;
                    //rtbAnswerA.Height = temp.Height + 5;
                    //rtbAnswerA.ViewMode = TXTextControl.ViewMode.SimpleControl;
                    if (q.ListAnswer.Count > 1)
                    {
                        mrbAnswerB.Location = new Point(locationLeftRadio, rtbAnswerA.Bottom + 5);
                        Controllers.Instance.SetStyleRadioButton(mrbAnswerB, Properties.Resources.MSG_GUI_0023);
                        rtbAnswerB.Location = new Point(mrbAnswerB.Right + 10, mrbAnswerB.Top + 3);
                        rtbAnswerB.Width = mpnAnswers.Width - locationLeftRadio - mrbAnswerA.Width - 20;
                        rtbAnswerB.Load(q.ListAnswer[1].AnswerContent, TXTextControl.StringStreamType.RichTextFormat);
                        temp.Width = mpnAnswers.Width;
                        temp.Rtf = q.ListAnswer[1].AnswerContent;
                        rtbAnswerB.ViewMode = TXTextControl.ViewMode.SimpleControl;
                        //rtbAnswerB.Height = temp.Height + 5;
                        Controllers.Instance.HandleRichTextBoxStyle(rtbAnswerB);
                    }
                    //TODO
                    if (q.ListAnswer.Count > 2)
                    {
                        mrbAnswerC.Location = new Point(locationLeftRadio, rtbAnswerB.Bottom + 5);
                        Controllers.Instance.SetStyleRadioButton(mrbAnswerC, Properties.Resources.MSG_GUI_0024);
                        rtbAnswerC.Location = new Point(mrbAnswerC.Right + 10, mrbAnswerC.Top + 3);
                        rtbAnswerC.Width = mpnAnswers.Width - locationLeftRadio - mrbAnswerA.Width - 20;
                        rtbAnswerC.Load(q.ListAnswer[2].AnswerContent, TXTextControl.StringStreamType.RichTextFormat);
                        temp.Width = mpnAnswers.Width;
                        temp.Rtf = q.ListAnswer[2].AnswerContent;

                        //if (rtbAnswerC.Height < q.ListAnswer[2].HighToDisplay)
                        //{ }
                        //  rtbAnswerC.Height = q.ListAnswer[2].HighToDisplay;
                        // else
                        //rtbAnswerC.Height = temp.Height + 5;
                        rtbAnswerC.ViewMode = TXTextControl.ViewMode.SimpleControl;
                        Controllers.Instance.HandleRichTextBoxStyle(rtbAnswerC);


                    }
                    if (q.ListAnswer.Count > 3)
                    {
                        mrbAnswerD.Location = new Point(locationLeftRadio, rtbAnswerC.Bottom + 5);
                        Controllers.Instance.SetStyleRadioButton(mrbAnswerD, Properties.Resources.MSG_GUI_0025);
                        rtbAnswerD.Location = new Point(mrbAnswerD.Right + 10, mrbAnswerD.Top + 3);
                        rtbAnswerD.Width = mpnAnswers.Width - locationLeftRadio - mrbAnswerA.Width - 20;
                        rtbAnswerD.Load(q.ListAnswer[3].AnswerContent, TXTextControl.StringStreamType.RichTextFormat);
                        temp.Width = mpnAnswers.Width;
                        temp.Rtf = q.ListAnswer[3].AnswerContent;
                        //rtbAnswerD.Height = temp.Height + 5;
                        rtbAnswerD.ViewMode = TXTextControl.ViewMode.SimpleControl;
                        Controllers.Instance.HandleRichTextBoxStyle(rtbAnswerD);
                        mpnAnswers.Height = rtbAnswerD.Bottom + 10;
                    }

                    if (q.ListAnswer.Count == 3)
                    {
                        rtbAnswerD.Visible = false;
                        mrbAnswerD.Visible = false;
                        mpnAnswers.Height = rtbAnswerC.Bottom + 10;


                    }
                    else if (q.ListAnswer.Count == 2)
                    {
                        mpnAnswers.Height = rtbAnswerB.Bottom + 10;

                        rtbAnswerD.Visible = false;
                        mrbAnswerD.Visible = false;
                        mrbAnswerC.Visible = false;
                        rtbAnswerC.Visible = false;
                        // mpnAnswers.Height = Controllers.Instance.GetHeightBetter(rtbAnswerB.Bottom, mrbAnswerB.Bottom);    
                    }
                }
                this.Height = mpnAnswers.Bottom + 20;
               
            }
            catch (Exception ex)
            {
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR,"Lỗi khi render câu hỏi" +ex.ToString());

            }


        }



        private void MbtnHelp_Click(object sender, EventArgs e)
        {
            try
            {

                frmHelp frm = new frmHelp();
                frm.ShowDialog();


            }
            catch (Exception ex)
            {
                //   MessageBox.Show("Gặp sự cố trong việc mở word", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MbtnRefresh_Click(object sender, EventArgs e)
        {
            string path = (pathfile + "\\temp\\" + q.NO + DateTime.Now.DayOfYear.ToString() + ".rtf");
            if (File.Exists(path))
            {

                DialogResult dr1 = MessageBox.Show("Bạn đã chắc chắn tắt hết cửa sổ word?", "Cảnh báo!!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr1 == DialogResult.No)
                {
                    MessageBox.Show("Đề nghị tắt cửa sổ word bằng tay");
                }
                else
                {
                    killProcessMSWord();
                    frmLoadFileRTF frm = new frmLoadFileRTF(path);
                    DialogResult dr = frm.ShowDialog();
                    if (dr == DialogResult.OK)
                    {
                        RichTextBox mrtfAnswer = (RichTextBox)mpnAnswers.Controls["mtxtAnswer"];
                        mrtfAnswer.LoadFile(path);

                        mrtfAnswer.Update();
                        mbtnControl.BackColor = Constant.BACKCOLOR_BUTTON_QUESTION;
                        mbtnControl.ForeColor = Constant.FORCECOLOR_BUTTON_QUESTION;
                        mbtnControl.Update();
                        AD.AnswerContent = mrtfAnswer.Rtf;
                        AD.LastTime = Controllers.Instance.ConvertDateTimeToUnix(DAO.DAO.ConvertDateTime.GetDateTimeServer());
                        ErrorController rEC = new ErrorController();
                        AnswersheetDetailBUS.Instance.PushAnswerSheetDetail(AD, out rEC,Sql);
                        if (rEC.ErrorCode == Constant.STATUS_OK)
                        {
                            Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "USER_ANSWER", "đã trả lời câu hỏi");
                        }
                        else
                        {
                            Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), DTO.MainForm);
                            Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));
                        }
                        File.Delete(path);
                    }
                    else if (dr == DialogResult.Cancel)
                    {
                        File.Delete(path);
                    }

                }
            }
            else
            {
                MessageBox.Show("Bạn chưa trả lời câu hỏi này!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }
        private void MbtnSaveforText_Click(object sender, EventArgs e)
        {
            //string path = (pathfile + "\\temp\\" + q.NO + DateTime.Now.DayOfYear.ToString() + ".rtf");

            //if (File.Exists(path))
            //{
            MetroButton mbtnSave = (MetroButton)sender;

            // string rtffile = System.IO.File.ReadAllText(path);
            //RichTextBox rtf = new RichTextBox();
            //rtf.Rtf = AD.AnswerContent;
            RichTextBox mrtfAnswer = (RichTextBox)mpnAnswers.Controls["mtxtAnswer"];


            AD.AnswerContent = mrtfAnswer.Text;
            ExamAnswerHistoryStore.AddEntry(q.NO, "Tự luận: " + mrtfAnswer.Text, DateTime.Now);
            AnswerSelectionLogger.Log(q.NO, "Tự luận: " + mrtfAnswer.Text);

            AD.LastTime = Controllers.Instance.ConvertDateTimeToUnix(DAO.DAO.ConvertDateTime.GetDateTimeServer());
            ErrorController rEC = new ErrorController();
            AnswersheetDetailBUS.Instance.PushAnswerSheetDetail(AD, out rEC,Sql);
            if (rEC.ErrorCode == Constant.STATUS_OK)
            {
                Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "USER_ANSWER", "Đã trả lời câu hỏi");
                mbtnSave.Text = "Đã lưu";

                mbtnControl.BackColor = Constant.BACKCOLOR_BUTTON_QUESTION;
                mbtnControl.ForeColor = Constant.FORCECOLOR_BUTTON_QUESTION;
                mbtnControl.Update();
                MessageBox.Show("Lưu thành công!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            else
            {
                Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), DTO.MainForm);
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));

            }


        }
        private void MbtnSave_Click(object sender, EventArgs e)
        {
            //string path = (pathfile + "\\temp\\" + q.NO + DateTime.Now.DayOfYear.ToString() + ".rtf");

            //if (File.Exists(path))
            //{
            MetroButton mbtnSave = (MetroButton)sender;

            // string rtffile = System.IO.File.ReadAllText(path);
            //RichTextBox rtf = new RichTextBox();
            //rtf.Rtf = AD.AnswerContent;
            RichTextBox mrtfAnswer = (RichTextBox)mpnAnswers.Controls["mtxtAnswer"];


            AD.AnswerContent = mrtfAnswer.Rtf;
            ExamAnswerHistoryStore.AddEntry(q.NO, "Tự luận: " + mrtfAnswer.Text, DateTime.Now);
            AnswerSelectionLogger.Log(q.NO, "Tự luận: " + mrtfAnswer.Text);

            AD.LastTime = Controllers.Instance.ConvertDateTimeToUnix(DAO.DAO.ConvertDateTime.GetDateTimeServer());
            ErrorController rEC = new ErrorController();
            AnswersheetDetailBUS.Instance.PushAnswerSheetDetail(AD, out rEC,Sql);
            if (rEC.ErrorCode == Constant.STATUS_OK)
            {
                Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "USER_ANSWER", "Đã trả lời câu hỏi");
                mbtnSave.Text = "Đã lưu";

                mbtnControl.BackColor = Constant.BACKCOLOR_BUTTON_QUESTION;
                mbtnControl.ForeColor = Constant.FORCECOLOR_BUTTON_QUESTION;
                mbtnControl.Update();
                MessageBox.Show("Lưu thành công!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            else
            {
                Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), DTO.MainForm);
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));

            }


        }
        private void MbtnOpen_Click(object sender, EventArgs e)
        {
            try
            {
                Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNINGOPENWORD, Properties.Resources.MSG_MESS_0041, this.ParentForm);
                if (DTO.NotificationForm.DialogResult == DialogResult.OK)
                {
                    killProcessMSWord();
                    MetroButton mbtnSave = (MetroButton)mpnAnswers.Controls["mbtnSave"];
                    mbtnSave.Text = "Lưu";
                    mbtnSave.Enabled = true;
                    if (!Directory.Exists(Path.Combine(pathfile, "temp")))
                        Directory.CreateDirectory(Path.Combine(pathfile, "temp"));

                    object szPath = pathfile + "\\temp\\" + q.NO + DateTime.Now.DayOfYear.ToString() + ".rtf";
                    if (File.Exists(szPath.ToString()))
                    {
                        MessageBox.Show("Đề nghị chọn đọc dữ liệu trước khi mở word", "Thông báo");
                        return;
                    }
                    RichTextBox mrtfAnswer = (RichTextBox)mpnAnswers.Controls["mtxtAnswer"];
                    mrtfAnswer.Update();
                    RichTextBox rtf = new RichTextBox();
                    rtf.Rtf = q.TitleOfQuestion;
                    rtf.Select(rtf.TextLength, 0);
                    rtf.SelectedRtf = mrtfAnswer.Rtf;
                    rtf.SaveFile(szPath.ToString());

                    ProcessStartInfo startInfo = new ProcessStartInfo();

                    startInfo.FileName = "WINWORD.exe";
                    startInfo.WindowStyle = ProcessWindowStyle.Maximized;
                    startInfo.Arguments = szPath.ToString();

                    int TimeStart = BUS.ContestantBUS.Instance.GetTimeStartFromAnswer(AD.AnswerSheetID);
                    int TimeNow = Controllers.Instance.ConvertDateTimeToUnix(DAO.DAO.ConvertDateTime.GetDateTimeServer());
                    int maxTime = BUS.ContestantBUS.Instance.GetTimeOfTestFromAnswer(AD.AnswerSheetID) - (TimeNow - TimeStart);
                    using (Process exeProcess = Process.Start(startInfo))
                    {
                        exeProcess.WaitForExit(maxTime);

                    }
                }



            }
            catch (Exception ex)
            {
                MessageBox.Show("Gặp sự cố trong việc mở word", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool CheckProcessMSWord()
        {
            foreach (Process process in Process.GetProcessesByName("WINWORD"))
            {

                return true;

            }
            return false;
        }
        public void killProcessMSWord()
        {
            foreach (Process process in Process.GetProcessesByName("WINWORD"))
            {

                process.Kill();

            }
        }
        private void McbAnswer_SelectedValueChanged(object sender, EventArgs e)
        {
            mbtnControl.BackColor = Constant.BACKCOLOR_BUTTON_QUESTION;
            mbtnControl.ForeColor = Constant.FORCECOLOR_BUTTON_QUESTION;
            mbtnControl.Update();
            ErrorController rEC = new ErrorController();
            MetroComboBox mcbAnswer = (sender) as MetroComboBox;
            if (mcbAnswer == null || mcbAnswer.SelectedValue == null || _isRestoringAnswer)
            {
                return;
            }
            AD.ChoosenAnswer = int.Parse(mcbAnswer.SelectedValue.ToString());
            //AD.AnswerContent = mcbAnswer.Text;
            ExamAnswerHistoryStore.AddEntry(q.NO, "Ghép nối: " + mcbAnswer.Text, DateTime.Now);
            AnswerSelectionLogger.Log(q.NO, "Ghép nối: " + mcbAnswer.Text);
            AD.LastTime = Controllers.Instance.ConvertDateTimeToUnix(DAO.DAO.ConvertDateTime.GetDateTimeServer());
            //TrangThaiThayDoi = true;
            AnswersheetDetailBUS.Instance.PushAnswerSheetDetail(AD, out rEC,Sql);
            if (rEC.ErrorCode == Constant.STATUS_OK)
            {

                Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "USER_SELECT_ANSWER", mcbAnswer.Text);
            }
            else
            {
                Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), DTO.MainForm);
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));
                Controllers.Instance.ExitApplicationFromNotificationForm(DTO.MainForm);
            }
        }

        private void MtxtAnswer_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                RichTextBox rtf = (sender) as RichTextBox;
                rtf.Select(rtf.SelectionStart, 0);
                rtf.SelectedText = "\n";

            }
        }

        private void mtxtAnswer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                // Không làm gì cả
                //MessageBox.Show("Thí sinh không được copy/paste");
                Clipboard.Clear();
            }
        }


        public bool TrangThaiThayDoi = false;
        private void RtbAnswerAForEssay_TextChanged(object sender, EventArgs e)
        {

            mbtnControl.BackColor = Constant.BACKCOLOR_BUTTON_QUESTION;
            mbtnControl.ForeColor = Constant.FORCECOLOR_BUTTON_QUESTION;
            mbtnControl.Update();

            //  ErrorController rEC = new ErrorController();
            RichTextBox rtf = (sender) as RichTextBox;
            AD.AnswerContent = rtf.Rtf;

            AD.LastTime = Controllers.Instance.ConvertDateTimeToUnix(DAO.DAO.ConvertDateTime.GetDateTimeServer());
            TrangThaiThayDoi = true;



        }
        private void RtbAnswerA_TextChanged(object sender, EventArgs e)
        {
            mbtnControl.BackColor = Constant.BACKCOLOR_BUTTON_QUESTION;
            mbtnControl.ForeColor = Constant.FORCECOLOR_BUTTON_QUESTION;
            mbtnControl.Update();

            RichTextBox rtf = (sender) as RichTextBox;
            AD.AnswerContent = rtf.Text;
            AD.LastTime = Controllers.Instance.ConvertDateTimeToUnix(DAO.DAO.ConvertDateTime.GetDateTimeServer());
            TrangThaiThayDoi = true;

        }

        private void mrbAnswer_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                ErrorController rEC = new ErrorController();
                RadioButton mrb = sender as RadioButton;
                if (mrb == null || !mrb.Checked)
                {
                    return;
                }

                ResetAnswerLabelColors();

                string selectedAnswerText = string.Empty;
                switch (mrb.Name)
                {
                    case "mrbAnswerA":
                        AD.ChoosenAnswer = q.ListAnswer[0].AnswerID;
                        StyleLabel(mrbAnswerA);
                        mbtnControl.Text = string.Format(Properties.Resources.MSG_GUI_0021, q.NO, Properties.Resources.MSG_GUI_0022);
                        selectedAnswerText = BuildAnswerHistoryText(Properties.Resources.MSG_GUI_0022, q.ListAnswer[0].AnswerContent);
                        break;
                    case "mrbAnswerB":
                        AD.ChoosenAnswer = q.ListAnswer[1].AnswerID;
                        StyleLabel(mrbAnswerB);
                        mbtnControl.Text = string.Format(Properties.Resources.MSG_GUI_0021, q.NO, Properties.Resources.MSG_GUI_0023);
                        selectedAnswerText = BuildAnswerHistoryText(Properties.Resources.MSG_GUI_0023, q.ListAnswer[1].AnswerContent);
                        break;
                    case "mrbAnswerC":
                        AD.ChoosenAnswer = q.ListAnswer[2].AnswerID;
                        StyleLabel(mrbAnswerC);
                        mbtnControl.Text = string.Format(Properties.Resources.MSG_GUI_0021, q.NO, Properties.Resources.MSG_GUI_0024);
                        selectedAnswerText = BuildAnswerHistoryText(Properties.Resources.MSG_GUI_0024, q.ListAnswer[2].AnswerContent);
                        break;
                    case "mrbAnswerD":
                        AD.ChoosenAnswer = q.ListAnswer[3].AnswerID;
                        StyleLabel(mrbAnswerD);
                        mbtnControl.Text = string.Format(Properties.Resources.MSG_GUI_0021, q.NO, Properties.Resources.MSG_GUI_0025);
                        selectedAnswerText = BuildAnswerHistoryText(Properties.Resources.MSG_GUI_0025, q.ListAnswer[3].AnswerContent);
                        break;
                }
                if (!_isRestoringAnswer)
                {
                    ExamAnswerHistoryStore.AddEntry(q.NO, selectedAnswerText, DateTime.Now);
                    AnswerSelectionLogger.Log(q.NO, selectedAnswerText);
                }
                AD.LastTime = Controllers.Instance.ConvertDateTimeToUnix(DAO.DAO.ConvertDateTime.GetDateTimeServer());
                AnswersheetDetailBUS.Instance.PushAnswerSheetDetail(AD, out rEC,Sql);
                if (rEC.ErrorCode == Constant.STATUS_OK)
                {
                    mbtnControl.BackColor = Constant.BACKCOLOR_BUTTON_QUESTION;
                    mbtnControl.ForeColor = Constant.FORCECOLOR_BUTTON_QUESTION;
                    mbtnControl.Update();
                    Log.Instance.WriteLog(Properties.Resources.MSG_LOG_INFO, "USER_SELECT_ANSWER", mbtnControl.Text);
                }
                else
                {
                    Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, Controllers.Instance.HandleStringErrorController(rEC), DTO.MainForm);
                    Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR, Controllers.Instance.HandleStringErrorController(rEC));
                    Controllers.Instance.ExitApplicationFromNotificationForm(DTO.MainForm);
                }
            }
            catch(Exception ex)
            {
                Controllers.Instance.ShowNotificationForm(Constant.TYPE_NOTIFICATION_WARNING, "Mất kết nối với server. Liên hệ với giám sát!", DTO.MainForm);
                Log.Instance.WriteErrorLog(Properties.Resources.MSG_LOG_ERROR,ex.Message);
                Controllers.Instance.ExitApplicationFromNotificationForm(DTO.MainForm);
            }
        }
        private void ResetAnswerLabelColors()
        {
            mrbAnswerA.ForeColor = Constant.COLOR_BLACK;
            mrbAnswerB.ForeColor = Constant.COLOR_BLACK;
            mrbAnswerC.ForeColor = Constant.COLOR_BLACK;
            mrbAnswerD.ForeColor = Constant.COLOR_BLACK;
        }

        private void StyleLabel(RadioButton mrb)
        {
            if (mrb.Checked)
            {
                mrb.ForeColor = Constant.COLOR_RED;
            }
            else
            {
                mrb.ForeColor = Constant.COLOR_BLACK;
            }
        }

        public void HandleQuestion(Questions qs, int AnswerSheetID)
        {
            q = qs;
            AD = new AnswersheetDetail();
            AD.SubQuestionID = q.SubQuestionID;
            AD.AnswerSheetID = AnswerSheetID;
        }
        public void HandleClickRadioAnswer()
        {
            _isRestoringAnswer = true;
            switch (this.q.AnswerChecked)
            {
                //ANS_CHECKED_A
                case 2001:
                    mrbAnswerA.PerformClick();
                    break;
                //ANS_CHECKED_B
                case 2002:
                    mrbAnswerB.PerformClick();
                    break;
                //ANS_CHECKED_C
                case 2003:
                    mrbAnswerC.PerformClick();
                    break;
                //ANS_CHECKED_D
                case 2004:
                    mrbAnswerD.PerformClick();
                    break;
            }
            _isRestoringAnswer = false;
        }

        private string BuildAnswerHistoryText(string answerLabel, string answerContent)
        {
            RichTextBox richTextBox = new RichTextBox();
            try
            {
                richTextBox.Rtf = answerContent ?? string.Empty;
                return string.Format("{0}. {1}", answerLabel, richTextBox.Text.Replace("\n", " ").Trim());
            }
            catch
            {
                return string.Format("{0}. {1}", answerLabel, answerContent ?? string.Empty);
            }
        }

        private void rtbTitleOfQuestion_DoubleClick(object sender, EventArgs e)
        {
            this.SendWorking(sender, e);


        }

        public string rtf()
        {
            string s;
            rtbTitleOfQuestion.Save(out s, TXTextControl.StringStreamType.RichTextFormat);
            return s;
        }



        private void rtbAnswerA_DoubleClick(object sender, EventArgs e)
        {
            this.SendWorking(sender, e);
        }

        private void rtbAnswerB_DoubleClick(object sender, EventArgs e)
        {
            this.SendWorking(sender, e);
        }

        private void rtbAnswerC_DoubleClick(object sender, EventArgs e)
        {
            this.SendWorking(sender, e);
        }

        private void rtbAnswerD_DoubleClick(object sender, EventArgs e)
        {
            this.SendWorking(sender, e);
        }
        private void rtbAnswer_DoubleClick(object sender, EventArgs e)
        {
            this.SendWorking(sender, e);
        }

    }
    public class AnswerMatch
    {
        public string AnswerContent { get; set; }
        public int AnswerID { get; set; }
    }
}
