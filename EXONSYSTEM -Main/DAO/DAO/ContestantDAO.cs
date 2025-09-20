using DAO.DataProvider;
using UserHelper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DAO.DAO
{
    public class ContestantDAO
    {
        private static ContestantDAO instance;

        public static ContestantDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ContestantDAO();
                }
                return instance;
            }
        }

        private ContestantDAO()
        {
        }
        public string GetConnectStringSqlDenpendecy()
        {
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {

                string _constr = DB.Database.Connection.ConnectionString;
                return _constr;
            }

        }
        public void HandleDivisionShiftNext(int ContestantID, out Contest rC, out ContestantInformation rCI, out ErrorController EC)
        {
            ContestantInformation CI = new ContestantInformation();
            Contest C = new Contest();
            int count = 0;
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                try
                {
                    int datenow = DAO.ConvertDateTime.ConvertDateTimeToUnix(DAO.ConvertDateTime.GetDateTimeServer()) / 86400;
                    CONTESTANT CS = new CONTESTANT();
                    ErrorController _EC = new ErrorController();
                    CS = DB.CONTESTANTS.SingleOrDefault(x => x.ContestantID == ContestantID);
                    List<CONTESTANTS_SHIFTS> lstCSH = DB.CONTESTANTS_SHIFTS.Where(x => x.ContestantID == ContestantID && x.DIVISION_SHIFTS.StartDate / 86400 == datenow).OrderBy(x => x.DIVISION_SHIFTS.StartTime).ToList();
                    foreach (CONTESTANTS_SHIFTS CSH in lstCSH)
                    {
                        if ((CSH.DIVISION_SHIFTS.Status != Common.STATUS_DIVISION_COMPLETE && CSH.DIVISION_SHIFTS.Status >= 5 && CSH.DIVISION_SHIFTS.StartDate / 86400 == datenow))
                        {

                            // tranjg thais thi sinh da ky
                            if (CSH.Status == Common.STATUS_SIGNED)
                            {
                                break;
                            }
                            // Thông tin thí sinh
                            CI.Fullname = CS.FullName;
                            CI.ContestantID = CS.ContestantID;
                            CI.ContestantCode = CS.ContestantCode;
                            CI.DOB = CS.DOB.Value;
                            CI.SEX = CS.Sex.Value;
                            CI.ContestantShiftID = CSH.ContestantShiftID;
                            CI.Ethnic = CS.Ethnic;
                            CI.HighSchool = CS.HighSchool;
                            CI.IdentityCardName = CS.IdentityCardNumber;
                            CI.CurrentAddress = CS.CurrentAddress;
                            CI.TrainingSystem = CS.TrainingSystem;
                            CI.StudentCode = CS.StudentCode;
                            CI.SubjectName = CSH.SCHEDULE.SUBJECT.SubjectName;
                            CI.TimeOfTest = CSH.SCHEDULE.TimeOfTest;
                            CI.TimeToSubmit = CSH.SCHEDULE.TimeToSubmit;
                            CI.DivisionShiftID = CSH.DivisionShiftID;
                            CI.ScheduleID = CSH.ScheduleID;
                            CI.Unit = CS.Unit;
                            CI.Status = 3000;
                            CI.Warning = CSH.VIOLATIONS_CONTESTANTS.Where(x => x.ContestantShiftID == CI.ContestantShiftID).ToList().Count;
                            // thông tin ca thi tiếp theo
                            C.ContestName = CSH.DIVISION_SHIFTS.SHIFT.CONTEST.ContestName;
                            C.RoomID = CSH.DIVISION_SHIFTS.RoomTestID;
                            C.RoomName = CSH.DIVISION_SHIFTS.ROOMTEST.RoomTestName;
                            C.ComputerCode = CSH.ROOMDIAGRAM.ComputerCode;
                            C.ShiftDate = CSH.DIVISION_SHIFTS.StartDate ?? default(int);
                            C.ComputerName = CSH.ROOMDIAGRAM.ComputerName;
                            C.StartTime = CSH.DIVISION_SHIFTS.StartTime ?? default(int);
                            C.EndTime = CSH.DIVISION_SHIFTS.EndTime ?? default(int);

                            break;
                        }
                        if (CSH.DIVISION_SHIFTS.Status == Common.STATUS_DIVISION_COMPLETE && CSH.DIVISION_SHIFTS.StartDate / 86400 == datenow)
                        {
                            count++;
                        }
                    }
                    if (CI.ContestantID != 0)
                    {
                        rC = C;
                        rCI = CI;
                        EC = new ErrorController(Common.STATUS_OK, "Đăng nhập thành công!");
                    }

                    else
                    {
                        rC = C;
                        rCI = null;
                        EC = new ErrorController(Common.STATUS_LOGIN_FAIL, "Đăng nhập thất bại!");
                    }
                    if (count == lstCSH.Count)
                    {
                        rC = C;
                        rCI = CI;
                        EC = new ErrorController(Common.STATUS_COMPLETE, "Hoàn thành nhiệm vụ!");
                    }
                }
                catch (Exception ex)
                {
                    rC = C;
                    EC = new ErrorController(Common.STATUS_UNKOWN_EXCEPTION, string.Format(Common.STR_STATUS_UNKOWN_EXCEPTION, ex.Message));
                    rCI = null;
                }
            }
        }

        public void GetListContestantPause(int ContestantShiftID, out List<CONTESTANTPAUSE> lstCP)
        {
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                try
                {
                    List<CONTESTANTPAUSE> lstContestantPaust = new List<CONTESTANTPAUSE>();
                    lstContestantPaust = DB.CONTESTANTPAUSEs.Where(x => x.CONTESTANTS_TESTS.ContestantShiftID == ContestantShiftID).ToList();
                    lstCP = lstContestantPaust;
                }
                catch
                {
                    lstCP = null;
                }
            }
        }
        public void UpdateLastimeConnect(int ContestantShiftID, int Lasttime, SqlConnection sql)
        {

            try
            {
                SqlCommand sqlcmd = new SqlCommand("update CONTESTANTS_SHIFTS set TimeCheck  =@Lasttime where ContestantShiftID=@id;", sql);
                sqlcmd.Parameters.Add("@id", ContestantShiftID);
                sqlcmd.Parameters.Add("@Lasttime", Lasttime);
                int row = 0;
                while (row == 0)
                {
                    row = sqlcmd.ExecuteNonQuery();

                }



            }
            catch (Exception ex)
            {


            }

        }
        public void UpdateForContestantPause(int ContestantShiftID, out ErrorController EC, out int ThoiGianBu, SqlConnection sql)
        {
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                try
                {
                    List<CONTESTANTPAUSE> lstContestantPaust = new List<CONTESTANTPAUSE>();
                    lstContestantPaust = DB.CONTESTANTPAUSEs.Where(x => x.CONTESTANTS_TESTS.ContestantShiftID == ContestantShiftID).ToList();
                    CONTESTANTPAUSE cp = new CONTESTANTPAUSE();
                    if (lstContestantPaust.Count > 0)
                    {
                        foreach (CONTESTANTPAUSE item in lstContestantPaust)
                        {
                            if (item.ContestantRealRestartTime == null)
                            {
                                cp = DB.CONTESTANTPAUSEs.Where(x => x.ContestantPauseID == item.ContestantPauseID).SingleOrDefault();
                                cp.ContestantRealRestartTime = Common.ConvertDateTimeToUnix(ConvertDateTime.GetDateTimeServer());
                                DB.Entry(cp).State = EntityState.Modified;
                                DB.SaveChangesAsync();
                            }

                        }

                        EC = new ErrorController(Common.STATUS_OK, "Cập nhật thành công");
                        int time = 0;
                        int subtime = 0;
                        foreach (CONTESTANTPAUSE item in lstContestantPaust)
                        {
                            int ContestantRealPauseTime = item.ContestantRealPauseTime ?? default(int);
                            if (item.ContestantRealRestartTime == null)
                            {
                                int TimeServer = Common.ConvertDateTimeToUnix(ConvertDateTime.GetDateTimeServer());
                                /*item.ContestantRealRestartTime = TimeServer;
                                DB.Entry(item).State = EntityState.Modified;
                                DB.SaveChanges();*/
                                subtime = TimeServer - ContestantRealPauseTime + 1;

                                SqlCommand sqlcmd = new SqlCommand("update   CONTESTANTPAUS set ContestantRealRestartTime=@TimeServer where ContestantTestID=@id;", sql);

                                sqlcmd.Parameters.Add("@id", item.ContestantPauseID);
                                sqlcmd.Parameters.Add("@TimeServer", TimeServer );
                                int row = 0;
                                while (row == 0)
                                {
                                    row = sqlcmd.ExecuteNonQuery();

                                }

                                EC = new ErrorController(Common.STATUS_OK, "Cập nhật CONTESTANTPAUSE thành công");

                            }
                            if (item.ContestantRealRestartTime != null)
                            {
                                int ContestantRealRestartTime = item.ContestantRealRestartTime ?? default(int);
                                subtime = ContestantRealRestartTime - ContestantRealPauseTime + 1;
                            }


                            time += subtime;
                        }
                        ThoiGianBu = time;
                    }
                    else
                    {
                        ThoiGianBu = 0;
                        EC = new ErrorController(Common.STATUS_OK, "Không có thời gian tạm dừng để cập nhật!");
                    }

                }
                catch (Exception ex)
                {
                    ThoiGianBu = 0;
                    EC = new ErrorController(Common.STATUS_UNKOWN_EXCEPTION, string.Format(Common.STR_STATUS_UNKOWN_EXCEPTION, ex.Message));

                }
            }

        }
        public int GetThoiGianBu(int ContestantShiftID)
        {
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                try
                {
                    List<CONTESTANTPAUSE> lstContestantPaust = new List<CONTESTANTPAUSE>();
                    lstContestantPaust = DB.CONTESTANTPAUSEs.Where(x => x.CONTESTANTS_TESTS.ContestantShiftID == ContestantShiftID).ToList();
                    int ThoiGianBu = 0;
                    int subtime = 0;

                    // các lần gián đoạn
                    if (lstContestantPaust.Count > 0)
                    {
                        foreach (CONTESTANTPAUSE item in lstContestantPaust)
                        {
                            int ContestantRealPauseTime = item.ContestantRealPauseTime ?? default(int);
                            if (item.ContestantRealRestartTime == null)
                            {
                                item.ContestantRealRestartTime = Common.ConvertDateTimeToUnix(ConvertDateTime.GetDateTimeServer());
                                subtime = Common.ConvertDateTimeToUnix(ConvertDateTime.GetDateTimeServer()) - ContestantRealPauseTime + 1;

                            }
                            if (item.ContestantRealRestartTime != null)
                            {
                                int ContestantRealRestartTime = item.ContestantRealRestartTime ?? default(int);
                                subtime = ContestantRealRestartTime - ContestantRealPauseTime + 1;
                            }


                            ThoiGianBu += subtime;
                        }
                    }
                    return ThoiGianBu;
                }

                catch
                {
                    return 0;

                }
            }

        }
        public void Authen(Contest C, string ContestantCode, string ComputerName, int LoginType, out ContestantInformation rCI, out ErrorController EC, SqlConnection sql)
        {

            ContestantInformation CI = new ContestantInformation();
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                try
                {
                    CONTESTANT CS = null;
                    if (LoginType == Common.LOGIN_WITH_CONTESTANT_CODE)
                    {
                        CS = DB.CONTESTANTS.SingleOrDefault(x => x.ContestantCode == ContestantCode);
                    }
                    else if (LoginType == Common.LOGIN_WITH_IDENTITY_CARD_NAME)
                    {
                        CS = DB.CONTESTANTS.SingleOrDefault(x => x.IdentityCardNumber.ToLower() == ContestantCode.ToLower());
                    }
                    else if (LoginType == Common.LOGIN_WITH_STUDENT_CODE)
                    {
                        CS = DB.CONTESTANTS.SingleOrDefault(x => x.StudentCode.ToLower() == ContestantCode.ToLower());
                    }
                    if (CS != null)
                    {

                        ErrorController _EC = new ErrorController();
                        ContestantInformation _rCI = new ContestantInformation();
                        List<CONTESTANTS_SHIFTS> lstCSH = DB.CONTESTANTS_SHIFTS.Where(x => x.ContestantID == CS.ContestantID).OrderBy(x => x.DIVISION_SHIFTS.StartTime).ToList();

                        int datenow = DAO.ConvertDateTime.ConvertDateTimeToUnix(DAO.ConvertDateTime.GetDateTimeServer()) / 86400;
                        foreach (CONTESTANTS_SHIFTS CSH in lstCSH)
                        {
                            // trang thai cấm thi
                            if (CSH.Status == Common.STATUS_BAN)
                            {
                                _EC = new ErrorController(Common.STATUS_BAN, "Thí sinh bị cấm thi");
                                _rCI = null;
                            }
                            else if (CSH.Status == Common.STATUS_SIGNED)
                            {
                                _EC = new ErrorController(Common.STATUS_SIGNED, "Thí sinh đã ký nộp bài");
                                _rCI = null;
                            }
                            // trạng thái tạm dừng
                            else if (CSH.Status != Common.STATUS_PAUSE)
                            {
                                List<CONTESTANTPAUSE> lstContestantPaust = new List<CONTESTANTPAUSE>();
                                lstContestantPaust = DB.CONTESTANTPAUSEs.Where(x => x.CONTESTANTS_TESTS.ContestantShiftID == CSH.ContestantShiftID).ToList();
                                int ThoiGianBu = 0;
                                int subtime = 0;

                                // các lần gián đoạn
                                if (lstContestantPaust.Count > 0)
                                {
                                    foreach (CONTESTANTPAUSE item in lstContestantPaust)
                                    {
                                        int ContestantRealPauseTime = item.ContestantRealPauseTime ?? default(int);
                                        if (item.ContestantRealRestartTime == null)
                                        {

                                            subtime = Common.ConvertDateTimeToUnix(ConvertDateTime.GetDateTimeServer()) - ContestantRealPauseTime + 1;
                                        }
                                        if (item.ContestantRealRestartTime != null)
                                        {
                                            int ContestantRealRestartTime = item.ContestantRealRestartTime ?? default(int);
                                            subtime = ContestantRealRestartTime - ContestantRealPauseTime + 1;
                                        }


                                        ThoiGianBu += subtime;
                                    }
                                }
                                int temp = Common.ConvertDateTimeToUnix(ConvertDateTime.GetDateTimeServer());
                                int TimeRemain = Common.ConvertDateTimeToUnix(ConvertDateTime.GetDateTimeServer()) - (CSH.TimeStarted ?? default(int));

                                //check gian doan
                                if (CSH.DIVISION_SHIFTS.Status == Common.STATUS_DIVISION_PAUSE)
                                {
                                    _EC = new ErrorController(Common.STATUS_PAUSE, "Thí sinh tạm dừng");
                                    _rCI = null;
                                }
                                else
                                {

                                    if (CSH.DIVISION_SHIFTS.Status != Common.STATUS_DIVISION_COMPLETE && CSH.DIVISION_SHIFTS.Status >= Common.STATUS_DIVISION_GENERATE && CSH.DIVISION_SHIFTS.StartDate / 86400 == datenow)
                                    {

                                        if (CSH.TimeStarted != null && CSH.SCHEDULE.TimeOfTest > TimeRemain - ThoiGianBu)
                                        {

                                            {

                                                ROOMDIAGRAM rd = new ROOMDIAGRAM();
                                                CONTESTANTS_SHIFTS ctexist = new CONTESTANTS_SHIFTS();
                                                // List<CONTESTANTS_SHIFTS> lscs = new List<CONTESTANTS_SHIFTS>();
                                                CONTESTANTS_SHIFTS lscs = new CONTESTANTS_SHIFTS();
                                                rd = DB.ROOMDIAGRAMS.Where(x => x.ComputerName == ComputerName).SingleOrDefault();
                                                // them may neu khong nam trong db
                                                if (rd == null)
                                                {
                                                    /*rd = new ROOMDIAGRAM();
                                                    rd.ComputerName = ComputerName;
                                                    rd.ComputerCode = ComputerName;
                                                    rd.RoomTestID = CSH.DIVISION_SHIFTS.RoomTestID;
                                                    rd.Status = 4001;
                                                    DB.ROOMDIAGRAMS.Add(rd);
                                                    DB.SaveChanges();*/
                                                    //Phaan suwa
                                                    SqlCommand sqlcmd = new SqlCommand("INSERT INTO ROOMDIAGRAMS(ComputerName,ComputerCode,RoomTestID,Status) values (@ComputerName,@ComputerCode,@RoomTestID,@Status) ;", sql);

                                                    sqlcmd.Parameters.Add("@ComputerName", ComputerName ?? (object)DBNull.Value);
                                                    sqlcmd.Parameters.Add("@ComputerCode", ComputerName ?? (object)DBNull.Value);
                                                    sqlcmd.Parameters.Add("@RoomTestID", CSH.DIVISION_SHIFTS.RoomTestID );
                                                    sqlcmd.Parameters.Add("@Status", 4001);
                                                    int row = 0;
                                                    while (row == 0)
                                                    {
                                                        row = sqlcmd.ExecuteNonQuery();

                                                    }
                                                    //
                                                    rd = new ROOMDIAGRAM();
                                                    rd = DB.ROOMDIAGRAMS.Where(x => x.ComputerName == ComputerName).SingleOrDefault();
                                                }
                                                if (rd != null)
                                                {  

                                                    if (CSH != null)
                                                    {
                                                        CI.Fullname = CS.FullName;
                                                        CI.ContestantID = CS.ContestantID;
                                                        CI.ContestantCode = CS.ContestantCode;
                                                        CI.DOB = CS.DOB.Value;
                                                        CI.SEX = CS.Sex.Value;
                                                        CI.ContestantShiftID = CSH.ContestantShiftID;
                                                        CI.Ethnic = CS.Ethnic;
                                                        CI.HighSchool = CS.HighSchool;
                                                        CI.IdentityCardName = CS.IdentityCardNumber;
                                                        CI.CurrentAddress = CS.CurrentAddress;
                                                        CI.TrainingSystem = CS.TrainingSystem;
                                                        CI.StudentCode = CS.StudentCode;
                                                        CI.SubjectName = CSH.SCHEDULE.SUBJECT.SubjectName;
                                                        CI.TimeOfTest = CSH.SCHEDULE.TimeOfTest;
                                                        CI.TimeRemained = (CSH.SCHEDULE.TimeOfTest - TimeRemain); /// 5pkiem tra bai
                                                        CI.TimeToSubmit = CSH.SCHEDULE.TimeToSubmit;
                                                        CI.RoomDiagramID = rd.RoomDiagramID;
                                                        CI.DivisionShiftID = CSH.DivisionShiftID;
                                                        CI.ScheduleID = CSH.ScheduleID;
                                                        CI.Unit = CS.Unit;
                                                        CI.Status = CSH.Status;
                                                        CI.ThoiGianBu = ThoiGianBu;

                                                        if (CSH.Status == Common.STATUS_DOING_BUT_INTERRUPT || CI.Status == Common.STATUS_LOGGED_DO_NOT_FINISH || CI.Status == Common.STATUS_DOING)
                                                        {
                                                            CONTESTANTS_TESTS CT = DB.CONTESTANTS_TESTS.SingleOrDefault(y => y.ContestantShiftID == CSH.ContestantShiftID);
                                                            if (CT != null)
                                                            {
                                                                CI.ContestantTestID = CT.ContestantTestID;
                                                                CI.TestID = CT.TestID;
                                                            }
                                                        }

                                                        CI.Warning = CSH.VIOLATIONS_CONTESTANTS.Where(x => x.ContestantShiftID == CI.ContestantShiftID).ToList().Count;
                                                        int timenow = Common.ConvertDateTimeToUnix(ConvertDateTime.GetDateTimeServer());
                                                        int lasttime = 0;
                                                        if (CSH.TimeCheck.HasValue)
                                                        {
                                                            lasttime = CSH.TimeCheck.Value;
                                                        }

                                                        //trường hợp đăng nhập với trang tahis đang thi - Check kết nối gần nhất mà bé hơn timenow-10s thì cho phép đăng nhập sang máy thi khác
                                                        if (CI.Status == Common.STATUS_LOGGED_DO_NOT_FINISH || CSH.Status == Common.STATUS_DOING_BUT_INTERRUPT || (CSH.Status == Common.STATUS_DOING && lasttime < timenow - 15)
                                                           || (CSH.Status == Common.STATUS_INITIALIZE && lasttime < timenow - 15))
                                                        {
                                                            _rCI = CI;
/*                                                            CSH.RoomDiagramID = rd.RoomDiagramID;
                                                            DB.Entry(CSH).State = EntityState.Modified;
                                                            DB.SaveChanges();*/
                                                            // Da sua Savechange
                                                            SqlCommand sqlcmd = new SqlCommand("update CONTESTANTS_SHIFTS set RoomDiagramID=@RoomDiagramID where ContestantShiftID=@id;", sql);

                                                            sqlcmd.Parameters.Add("@id", CSH.ContestantShiftID);
                                                            sqlcmd.Parameters.Add("@RoomDiagramID", rd.RoomDiagramID );
                                                            int row = 0;
                                                            while (row == 0)
                                                            {
                                                                row = sqlcmd.ExecuteNonQuery();

                                                            }

                                                            _EC = new ErrorController(Common.STATUS_OK, "Đăng nhập thành công!");

                                                        }
                                                        ///trường hợp đăng nhập sai máy thi status là đang thi thì block
                                                        else
                                                        {
                                                            if (CI.Status == Common.STATUS_DOING)
                                                            {
                                                                _EC = new ErrorController(Common.STATUS_DOING, "Thí sinh đang thi");
                                                                _rCI = null;
                                                            }
                                                            else if (CI.Status == Common.STATUS_FINISHED)
                                                            {
                                                                _EC = new ErrorController(Common.STATUS_FINISHED, "Thí sinh hoàn thành bài thi, vui lòng chờ hết giờ làm để xem lại");
                                                                _rCI = null;
                                                            }
                                                        }                                                      
                                                    }
                                                    //foreach (CONTESTANTS_SHIFTS item in lscs)
                                                    //{
                                                    //    item.RoomDiagramID = rd.RoomDiagramID;
                                                    //    DB.Entry(item).State = EntityState.Modified;
                                                    //}
                                                    //DB.SaveChanges();


                                                }
                                                else
                                                {
                                                    _EC = new ErrorController(Common.STATUS_DUPLICATE_NAMECOMPUTER, "DUPLICATE NAME COMPUTER.");
                                                    _rCI = null;
                                                }

                                            }
                                            break;
                                        }
                                        else if (CSH.TimeStarted != null && CSH.SCHEDULE.TimeOfTest < TimeRemain - ThoiGianBu)
                                        {

                                            ROOMDIAGRAM rd = new ROOMDIAGRAM();

                                            List<CONTESTANTS_SHIFTS> lscs = new List<CONTESTANTS_SHIFTS>();
                                            rd = DB.ROOMDIAGRAMS.Where(x => x.ComputerName == ComputerName).SingleOrDefault();
                                            _EC = new ErrorController(Common.STATUS_lOGIN_OUTTIME, "Hết thời gian đăng nhập");
                                            if (rd != null)
                                            {


                                                CI.Fullname = CS.FullName;
                                                CI.ContestantID = CS.ContestantID;
                                                CI.ContestantCode = CS.ContestantCode;
                                                CI.DOB = CS.DOB.Value;
                                                CI.SEX = CS.Sex.Value;
                                                CI.ContestantShiftID = CSH.ContestantShiftID;
                                                CI.Ethnic = CS.Ethnic;
                                                CI.HighSchool = CS.HighSchool;
                                                CI.IdentityCardName = CS.IdentityCardNumber;
                                                CI.CurrentAddress = CS.CurrentAddress;
                                                CI.TrainingSystem = CS.TrainingSystem;
                                                CI.StudentCode = CS.StudentCode;
                                                CI.SubjectName = CSH.SCHEDULE.SUBJECT.SubjectName;
                                                CI.TimeOfTest = CSH.SCHEDULE.TimeOfTest;
                                                CI.TimeRemained = (CSH.SCHEDULE.TimeOfTest - TimeRemain);
                                                CI.TimeToSubmit = CSH.SCHEDULE.TimeToSubmit;
                                                CI.RoomDiagramID = rd.RoomDiagramID;
                                                CI.DivisionShiftID = CSH.DivisionShiftID;
                                                CI.ScheduleID = CSH.ScheduleID;
                                                CI.Unit = CS.Unit;
                                                CI.Status = CSH.Status;
                                                CI.ThoiGianBu = ThoiGianBu;
                                                if (CSH.Status == Common.STATUS_DOING_BUT_INTERRUPT || CI.Status == Common.STATUS_LOGGED_DO_NOT_FINISH || CSH.Status == Common.STATUS_FINISHED)
                                                {
                                                    CONTESTANTS_TESTS CT = DB.CONTESTANTS_TESTS.SingleOrDefault(y => y.ContestantShiftID == CSH.ContestantShiftID);
                                                    if (CT != null)
                                                    {
                                                        CI.ContestantTestID = CT.ContestantTestID;
                                                        CI.TestID = CT.TestID;
                                                    }
                                                }
                                                CI.Warning = CSH.VIOLATIONS_CONTESTANTS.Where(x => x.ContestantShiftID == CI.ContestantShiftID).ToList().Count;
                                                _rCI = CI;
                                            }
                                            break;
                                        }
                                        else if (CSH.TimeStarted == null && CSH.DIVISION_SHIFTS.Status > 1)
                                        {

                                            ROOMDIAGRAM rd = new ROOMDIAGRAM();

                                            List<CONTESTANTS_SHIFTS> lscs = new List<CONTESTANTS_SHIFTS>();
                                            rd = DB.ROOMDIAGRAMS.Where(x => x.ComputerName == ComputerName).SingleOrDefault();
                                            // them may neu khong nam trong db
                                            if (rd == null)
                                            {
                                                /*rd = new ROOMDIAGRAM();

                                                rd.ComputerName = ComputerName;
                                                rd.ComputerCode = ComputerName;
                                                rd.RoomTestID = CSH.DIVISION_SHIFTS.RoomTestID;
                                                rd.Status = 4001;
                                                DB.ROOMDIAGRAMS.Add(rd);
                                                DB.SaveChanges();*/
                                                // DA SUA SAVECHANGES
                                                SqlCommand sqlcmd = new SqlCommand("INSERT INTO ROOMDIAGRAMS(ComputerName,ComputerCode,RoomTestID,Status) values (@ComputerName,@ComputerCode,@RoomTestID,@Status) ;", sql);

                                                sqlcmd.Parameters.Add("@ComputerName", ComputerName ?? (object)DBNull.Value);
                                                sqlcmd.Parameters.Add("@ComputerCode", ComputerName ?? (object)DBNull.Value);
                                                sqlcmd.Parameters.Add("@RoomTestID", CSH.DIVISION_SHIFTS.RoomTestID );
                                                sqlcmd.Parameters.Add("@Status", 4001);
                                                int row = 0;
                                                while (row == 0)
                                                {
                                                    row = sqlcmd.ExecuteNonQuery();

                                                }

                                                rd = new ROOMDIAGRAM();
                                                rd = DB.ROOMDIAGRAMS.Where(x => x.ComputerName == ComputerName).SingleOrDefault();
                                            }
                                            if (rd != null)
                                            {

                                                lscs = DB.CONTESTANTS_SHIFTS.Where(x => x.ContestantID == CS.ContestantID).ToList();
                                                
                                                DB.SaveChanges();
                                                CI.Fullname = CS.FullName;
                                                CI.ContestantID = CS.ContestantID;
                                                CI.ContestantCode = CS.ContestantCode;
                                                CI.DOB = CS.DOB.Value;
                                                CI.SEX = CS.Sex.Value;
                                                CI.ContestantShiftID = CSH.ContestantShiftID;
                                                CI.Ethnic = CS.Ethnic;
                                                CI.HighSchool = CS.HighSchool;
                                                CI.IdentityCardName = CS.IdentityCardNumber;
                                                CI.CurrentAddress = CS.CurrentAddress;
                                                CI.TrainingSystem = CS.TrainingSystem;
                                                CI.StudentCode = CS.StudentCode;
                                                CI.SubjectName = CSH.SCHEDULE.SUBJECT.SubjectName;
                                                CI.TimeOfTest = CSH.SCHEDULE.TimeOfTest;
                                                CI.TimeRemained = (CSH.SCHEDULE.TimeOfTest - TimeRemain);
                                                CI.TimeToSubmit = CSH.SCHEDULE.TimeToSubmit;
                                                CI.RoomDiagramID = rd.RoomDiagramID;
                                                CI.DivisionShiftID = CSH.DivisionShiftID;
                                                CI.ScheduleID = CSH.ScheduleID;
                                                CI.Unit = CS.Unit;
                                                CI.Status = CSH.Status;
                                                CI.ThoiGianBu = ThoiGianBu;

                                                // Code Dai them vao de sua loi 2 may dnhap cung 1 thi sinh luc chua lam bai
                                                // trường hợp đăng nhập với trang thai san sang nhan de - Check kết nối gần nhất mà bé hơn timenow-10s thì cho phép đăng nhập sang máy thi khác
                                                // 3010 la trang thai san sang nhan de
                                                if (CI.Status == Common.STATUS_READY || CI.Status == 3010)
                                                {
                                                    _rCI = null;
                                                    _EC = new ErrorController(Common.STATUS_ERROR, "Thí sinh đã đăng nhập!");
                                                    break;
                                                }
                                                else
                                                {
                                                    //CSH.RoomDiagramID = rd.RoomDiagramID;
                                                    //DB.Entry(CSH).State = EntityState.Modified;
                                                    //DB.SaveChanges();
                                                    lscs = DB.CONTESTANTS_SHIFTS.Where(x => x.ContestantID == CS.ContestantID).ToList();
                                                    foreach (CONTESTANTS_SHIFTS item in lscs)
                                                    {
                                                        /*item.RoomDiagramID = rd.RoomDiagramID;
                                                        DB.Entry(item).State = EntityState.Modified;
                                                        DB.SaveChanges();*/
                                                        SqlCommand sqlcmd = new SqlCommand("update CONTESTANTS_SHIFTS set RoomDiagramID=@RoomDiagramID where ContestantShiftID=@id;", sql);

                                                        sqlcmd.Parameters.Add("@id", CSH.ContestantShiftID);
                                                        sqlcmd.Parameters.Add("@RoomDiagramID", rd.RoomDiagramID );
                                                        int row = 0;
                                                        while (row == 0)
                                                        {
                                                            row = sqlcmd.ExecuteNonQuery();

                                                        }
                                                    }
                                                    //DB.SaveChanges();
                                                }

                                                if (CSH.Status == Common.STATUS_DOING_BUT_INTERRUPT || CI.Status == Common.STATUS_LOGGED_DO_NOT_FINISH)
                                                {
                                                    CONTESTANTS_TESTS CT = DB.CONTESTANTS_TESTS.SingleOrDefault(y => y.ContestantShiftID == CSH.ContestantShiftID);
                                                    if (CT != null)
                                                    {
                                                        CI.ContestantTestID = CT.ContestantTestID;
                                                        CI.TestID = CT.TestID;
                                                    }
                                                }
                                                CI.Warning = CSH.VIOLATIONS_CONTESTANTS.Where(x => x.ContestantShiftID == CI.ContestantShiftID).ToList().Count;

                                                _rCI = CI;
                                                _EC = new ErrorController(Common.STATUS_OK, "Đăng nhập thành công!");

                                                if (CI.Status == Common.STATUS_DOING)
                                                {
                                                    _EC = new ErrorController(Common.STATUS_DOING, "Thí sinh đang thi");
                                                    _rCI = null;
                                                }


                                            }
                                            //else
                                            //{
                                            //    _EC = new ErrorController(Common.STATUS_DUPLICATE_NAMECOMPUTER, "DUPLICATE NAME COMPUTER.");
                                            //    _rCI = null;
                                            //}


                                            break;
                                        }

                                    }
                                    else
                                    {
                                        EC = new ErrorController(Common.STATUS_ERROR, "Ca thi chưa được mở hoặc đã kết thúc!");
                                        _rCI = null;
                                    }
                                }
                            }
                            else
                            {
                                _EC = new ErrorController(Common.STATUS_PAUSE, "Thí sinh tạm dừng");
                                _rCI = null;
                            }


                        }
                        EC = _EC;
                        rCI = _rCI;
                        if (_rCI == null || rCI.ContestantID == 0)
                        {
                            EC = _EC;
                            rCI = null;
                        }
                    }
                    else
                    {
                        EC = new ErrorController(Common.STATUS_LOGIN_FAIL, "Đăng nhập sai tên người dùng hoặc số định danh hoặc mã sinh viên");
                        rCI = null;
                    }
                }
                catch (Exception ex)
                {
                    EC = new ErrorController(Common.STATUS_UNKOWN_EXCEPTION, string.Format(Common.STR_STATUS_UNKOWN_EXCEPTION, ex.Message));
                    rCI = null;
                }
            }
        }

        public int GetTimeStartFromContestant(int contestantShiftID)
        {
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                try
                {
                    CONTESTANTS_SHIFTS cs = new CONTESTANTS_SHIFTS();
                    cs = DB.CONTESTANTS_SHIFTS.Where(x => x.ContestantShiftID == contestantShiftID).SingleOrDefault();
                    if (cs != null)
                    {
                        return cs.TimeStarted ?? default(int);
                    }
                    else
                    {
                        return -1;

                    }
                }
                catch
                {
                    return -1;
                }
            }
        }

        public int GetTimeStartFromAnswer(int AnswersheetID)
        {
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                try
                {
                    ANSWERSHEET
                            aw = new ANSWERSHEET();
                    aw = DB.ANSWERSHEETS.Where(x => x.AnswerSheetID == AnswersheetID).SingleOrDefault();
                    CONTESTANTS_SHIFTS cs = new CONTESTANTS_SHIFTS();
                    cs = DB.CONTESTANTS_SHIFTS.Where(x => x.ContestantShiftID == aw.CONTESTANTS_TESTS.ContestantShiftID).SingleOrDefault();
                    if (cs != null)
                    {
                        return cs.TimeStarted ?? default(int);
                    }
                    else
                    {
                        return -1;

                    }
                }
                catch
                {
                    return -1;
                }
            }
        }
        public int GetTimeOfTestFromAnswer(int AnswersheetID)
        {
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                try
                {
                    ANSWERSHEET aw = new ANSWERSHEET();
                    aw = DB.ANSWERSHEETS.Where(x => x.AnswerSheetID == AnswersheetID).SingleOrDefault();
                    CONTESTANTS_SHIFTS cs = new CONTESTANTS_SHIFTS();
                    cs = DB.CONTESTANTS_SHIFTS.Where(x => x.ContestantShiftID == aw.CONTESTANTS_TESTS.ContestantShiftID).SingleOrDefault();
                    if (cs != null)
                    {
                        return cs.SCHEDULE.TimeOfTest;
                    }
                    else
                    {
                        return -1;

                    }
                }
                catch
                {
                    return -1;
                }
            }
        }
        public void ChangeStatusContestant(ContestantInformation CI, Contest Contest, out ErrorController EC, SqlConnection sql)
        {
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                try
                {
                    // CONTESTANT C = DB.CONTESTANTS.SingleOrDefault(x => x.ContestantID == CI.ContestantID);
                    //CONTESTANTS_SHIFTS CSH = C.CONTESTANTS_SHIFTS.Single(x => x.ContestantID == C.ContestantID && x.DivisionShiftID == CI.DivisionShiftID);
                    //if (C != null && CSH != null)
                    //{
                    //    CSH.Status = CI.Status;
                    //    if (CI.RoomDiagramID > 0)
                    //    {
                    //        CSH.RoomDiagramID = CI.RoomDiagramID;
                    //    }
                    //    if (CI.IsNewStarted)
                    //    {

                    //        CSH.TimeWorked = 0;
                    //    }
                    //    if (CI.IsDisconnected)
                    //    {
                    //        CSH.TimeWorked = Common.ConvertDateTimeToUnix(ConvertDateTime.GetDateTimeServer()) - CI.TimeStarted;
                    //    }

                    //    DB.Entry(CSH).State = EntityState.Modified;
                    //    DB.SaveChanges();
                    //    DB.Entry(CSH).State = EntityState.Modified;
                    SqlCommand sqlcmd = new SqlCommand(
                        "update CONTESTANTS_SHIFTS set Status=@status, EndTimeMsText=@endTimeMsText, TimeWorkedMsText=@timeWorkedMsText, SubmitTimeUnixMs=@submitTimeUnixMs, SubmitTimeText=@submitTimeText, TimeWorkedMs=@timeWorkedMs where ContestantShiftID=@id;",
                        sql);
                    sqlcmd.Parameters.Add("@status", CI.Status);
                    sqlcmd.Parameters.Add("@id", CI.ContestantShiftID);
                    sqlcmd.Parameters.AddWithValue("@endTimeMsText",
                        (object)CI.EndTimeMsText ?? DBNull.Value);
                    sqlcmd.Parameters.AddWithValue("@timeWorkedMsText",
                        (object)CI.TimeWorkedMsText ?? DBNull.Value);
                    sqlcmd.Parameters.AddWithValue("@submitTimeUnixMs", CI.SubmitTimeUnixMs > 0 ? (object)CI.SubmitTimeUnixMs : DBNull.Value);
                    sqlcmd.Parameters.AddWithValue("@submitTimeText", (object)CI.SubmitTimeText ?? DBNull.Value);
                    sqlcmd.Parameters.AddWithValue("@timeWorkedMs", CI.TimeWorkedMs > 0 ? (object)CI.TimeWorkedMs : DBNull.Value);
                    int row = 0;
                    while (row == 0)
                    {
                        row = sqlcmd.ExecuteNonQuery();

                    }


                    EC = new ErrorController(Common.STATUS_OK, string.Format("{0}: {1}", CI.Status, Common.GetStringStatus(CI.Status)));

                    //else
                    //{
                    //    EC = new ErrorController(Common.STATUS_ERROR, "Can not get CONTESTANT by ContestantID");
                    //}
                }
                catch (Exception ex)
                {
                    EC = new ErrorController(Common.STATUS_UNKOWN_EXCEPTION, string.Format(Common.STR_STATUS_UNKOWN_EXCEPTION, ex.Message));
                }
            }
        }

        public void GetContestantTestInformation(ContestantInformation CI, out ContestantInformation rCI, out ErrorController EC)
        {
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                try
                {
                    CONTESTANTS_TESTS CT = DB.CONTESTANTS_TESTS.SingleOrDefault(x => x.ContestantShiftID == CI.ContestantShiftID);
                    CONTESTANTS_SHIFTS CSH = DB.CONTESTANTS_SHIFTS.SingleOrDefault(x => x.ContestantShiftID == CI.ContestantShiftID);

                    if (CT != null && CSH != null)
                    {
                        CI.ContestantTestID = CT.ContestantTestID;
                        CI.TestID = CT.TestID;

                        rCI = CI;
                        EC = new ErrorController(Common.STATUS_OK, "Nhận CONTESTANTS_TESTS thành công bởi ContestantShiftID.");
                    }
                    else
                    {
                        rCI = null;
                        EC = new ErrorController(Common.STATUS_ERROR, "Không thể nhận CONTESTANTS_TESTS bởi ContestantID | GetContestantTestInformation");
                    }
                }
                catch (Exception ex)
                {
                    rCI = null;
                    EC = new ErrorController(Common.STATUS_UNKOWN_EXCEPTION, string.Format(Common.STR_STATUS_UNKOWN_EXCEPTION, ex.Message));
                }
            }
        }
        public int GetStatusContestantShift(int _contestantShiftID)
        {
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                CONTESTANTS_SHIFTS CS = new CONTESTANTS_SHIFTS();
                CS = DB.CONTESTANTS_SHIFTS.SingleOrDefault(x => x.ContestantShiftID == _contestantShiftID);
                if (CS != null)
                {
                    return CS.Status;
                }
                else
                {
                    return -1;
                }
            }
        }
        public void GetContestantTimeStart(ContestantInformation CI, out ContestantInformation rCI, out ErrorController EC)
        {
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                try
                {
                    CONTESTANTS_SHIFTS CT = DB.CONTESTANTS_SHIFTS.SingleOrDefault(x => x.ContestantShiftID == CI.ContestantShiftID);
                    if (CT != null)
                    {
                        CI.TimeStarted = CT.TimeStarted ?? 0;
                        rCI = CI;
                        EC = new ErrorController(Common.STATUS_OK, "Nhận CONTESTANTS_Shift Timestarted thành công bởi ContestantShiftID.");
                    }
                    else
                    {
                        rCI = null;
                        EC = new ErrorController(Common.STATUS_ERROR, "Không thể nhận CONTESTANTS_TESTS bởi ContestantID | GetContestantTestInformation");
                    }
                }
                catch (Exception ex)
                {
                    rCI = null;
                    EC = new ErrorController(Common.STATUS_UNKOWN_EXCEPTION, string.Format(Common.STR_STATUS_UNKOWN_EXCEPTION, ex.Message));
                }
            }
        }

        //Viết Tên máy, mã đăng nhập vào bảng [VIOLATIONS]
        //ViolationName lưu ComputerName
        //Description lưu chuỗi Json: Mã thí sinh, Họ tên, Ca thi, Phòng thi, Môn thi, Thời điểm đổi
        //Level lưu thời gian lúc đăng nhập
        public void ChangeContestantCode(UserLoginComputerDifferent ULCD, ContestantInformation CI, SqlConnection sql)
        {
            //Lấy số lượng các bảng, để tính ra điền ViolationID tiếp theo
            SqlCommand sqlCommand = new SqlCommand("Select Count(ViolationID) From VIOLATIONS", sql);
            int id = (int)sqlCommand.ExecuteScalar();

            SqlCommand sqlcmd = new SqlCommand("INSERT INTO VIOLATIONS(ViolationID, ViolationName, Description, Level, Status) VALUES(@ViolationID, @ViolationName, @Description, @Level, @Status)", sql);

            sqlcmd.Parameters.Add("@ViolationID", System.Data.SqlDbType.Int).Value = id + 1;
            sqlcmd.Parameters.Add("@ViolationName", System.Data.SqlDbType.NVarChar).Value = Dns.GetHostName();
            sqlcmd.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar).Value = ULCD.JsonDescription;
            sqlcmd.Parameters.Add("@Level", System.Data.SqlDbType.Int).Value = Common.ConvertDateTimeToUnix(ConvertDateTime.GetDateTimeServer());
            sqlcmd.Parameters.Add("@Status", System.Data.SqlDbType.Int).Value = CI.Status;


            int row = 0;
            while (row == 0)
            {
                row = sqlcmd.ExecuteNonQuery();

            }
        }

        public int GetNumberOfOvertime(ContestantInformation CI, SqlConnection sql)
        {
            //Lấy số lượng các lần mà thí sinh đã được bù giờ 
            SqlCommand sqlCommand = new SqlCommand("select COUNT(ContestantTestID) from CONTESTANTPAUSE where ContestantTestID = @ContestantTestID", sql);
            sqlCommand.Parameters.Add("@ContestantTestID", System.Data.SqlDbType.Int).Value = CI.ContestantTestID;
            int numbers = (int)sqlCommand.ExecuteScalar();
            return numbers;
        }
    }
}
