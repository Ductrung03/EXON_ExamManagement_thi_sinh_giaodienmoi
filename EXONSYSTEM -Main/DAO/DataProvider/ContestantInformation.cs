using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAO.DataProvider
{
	public class ContestantInformation
	{
		public string ContestantCode { get; set; }
		public int ContestantID { get; set; }
		public int ContestantTestID { get; set; }
		public int ContestantShiftID { get; set; }
        public int TimeOfTest { get; set; }
        public int TestID { get; set; }
        public int DivisionShiftID { get; set; }
        public string Fullname { get; set; }
		public int DOB { get; set; }
		public int SEX { get; set; }
		public string Ethnic { get; set; }
        public int ScheduleID { get; set; }
		public string HighSchool { get; set; }
		public string IdentityCardName { get; set; }
		public string Unit { get; set; }
		public string CurrentAddress { get; set; }
		public bool IsNewStarted { get; set; }
		public int TimeStarted { get; set; }
		public int TimeRemained { get; set; }
        public int ThoiGianBu { get; set; }
		public bool IsDisconnected { get; set; }
		public int Status { get; set; }
		public int AnswerSheetID { get; set; }
		public string TrainingSystem { get; set; }
		public string StudentCode { get; set; }
		public int Warning { get; set; }
        public string SubjectName { get; set; }
        public int TimeToSubmit { get; set; }
        public int RoomDiagramID { get; set; }
		public bool ReadOnly { get; set; }
        /// <summary>Thời điểm nộp bài, định dạng HH:mm:ss:fff (mili giây). Chỉ có giá trị khi Status = FINISHED.</summary>
        public string EndTimeMsText { get; set; }
        /// <summary>Tổng thời gian đã làm bài, định dạng hh:mm:ss:fff (mili giây). Chỉ có giá trị khi Status = FINISHED.</summary>
        public string TimeWorkedMsText { get; set; }
        //public List<int> {}
        public ContestantInformation()
		{
			this.IsDisconnected = false;
			this.IsNewStarted = false;
		}
	}
}
