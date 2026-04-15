using DAO.DAO;
using EXON.Common;
using EXONSYSTEM.Common;
using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace EXON.ForRegister
{
    public partial class FormMain : MetroForm
    {


        public FormMain()
        {
            InitializeComponent();
            //int TimeNow = EXONSYSTEM.Common.Controllers.Instance.ConvertDateTimeToUnix(ConvertDateTime.GetDateTimeServer());
            //MessageBox.Show(TimeNow.ToString());
            try
            {
                TXTextControl.TextControl textControlTest = new TXTextControl.TextControl();
            }
            catch (Exception ex)
            {
                throw new TXControlInitializationException("Máy chưa cài TX Control, vui lòng cài TX Control!", ex);
            }

        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            //if (CheckConnectionString())
            //{
            //    EXON.Register.frmRegister frm = new Register.frmRegister();
            //    this.Hide();
            //    frm.ShowDialog();
            //    this.Show();
            //}
        }

        private void btnExam_Click(object sender, EventArgs e)
        {
            try
            {
                EXONSYSTEM.Layout.frmConfigApplication frm = new EXONSYSTEM.Layout.frmConfigApplication();
                this.Hide();
                frm.runwork();
                //if (frm.check == 1)
                //{
                //    this.TopMost = true;
                //    this.Show();
                //}
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Chương trình gặp sự cố thoát chương trình và liên vệ với quản trị viên ");
            }
           
            //if(frm!=null)
            //{
            //    this.Show();
            //}
            //frm.ShowDialog();
            //this.Show();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("WMPLib"))
                {
                    process.Kill();
                }

                foreach (var process in Process.GetProcessesByName("audiodg"))
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {

            }            
        }

        private bool CheckConnectionString()
        {
            string conn2 = ConfigurationManager.ConnectionStrings["EXON_DbContext"].ConnectionString;
            SqlConnection connect2 = new SqlConnection(conn2);
            try
            {
                connect2.Open();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Kết nối máy chủ thất bại\n" + ex.Message);
                return false;
            }
            finally
            {
                connect2.Close();
            }
            return true;
        }

    }
}
