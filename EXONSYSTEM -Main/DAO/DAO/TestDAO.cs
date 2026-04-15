using DAO.DataProvider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Script.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Threading;
using System.Data.SqlClient;

namespace DAO.DAO
{
    // class đề thi //TODO Tải đề thi bằng sql thuần
    public class TestDAO
    {
        private static TestDAO instance;

        public static TestDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TestDAO();
                }
                return instance;
            }
        }

        private TestDAO()
        {

        }
        public void UpdateTimeForAudioQuestion(int TestDetailID, int QuestionFormat, SqlConnection sql)
        {

            try
            {
                SqlCommand sqlcmd = new SqlCommand("update TEST_DETAILS  set status =  @QuestionFormat " +
              "where TestDetailID = @id", sql);

                sqlcmd.Parameters.Add("@id", TestDetailID);
                sqlcmd.Parameters.Add("@QuestionFormat", QuestionFormat);

                sqlcmd.ExecuteNonQuery();
            }
            catch
            {

            }
        }
        /// <summary>
        /// Lấy câu hỏi
        /// </summary>
        /// <param name="CI"></param>
        /// <param name="rLLstQuest"></param>
        /// <param name="rlstPartOfTest"></param>
        /// <param name="IsContinute"></param>
        /// <param name="numberQuestionsOfTest"></param>
        /// <param name="EC"></param>
        public void GetListQuestionByContestantInformation(ContestantInformation CI, out List<List<Questions>> rLLstQuest, out List<PartOfTest> rlstPartOfTest, out bool IsContinute, out int numberQuestionsOfTest, out ErrorController EC, SqlConnection sql)
        {
            IsContinute = false;
            numberQuestionsOfTest = 0;
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                try
                {
                    int count = 0;
                    List<SqlParameter> para = new List<SqlParameter>();
                    para.Add(new SqlParameter("@TestID", CI.TestID));
                    List<TEST_DETAILS> lstTD = Utils.ExcuteObject<TEST_DETAILS>("SELECT * FROM TEST_DETAILS WHERE TestID = @TestID", para, sql).ToList();
                    //List<TEST_DETAILS> lstTD = DB.TEST_DETAILS.Where(x => x.TestID == CI.TestID).ToList();

                    List<PartOfTest> lstPOFT = new List<PartOfTest>();

                    try
                    {
                        para = new List<SqlParameter>();
                        para.Add(new SqlParameter("@ScheduleID", CI.ScheduleID));
                        List<PART> lstpt = Utils.ExcuteObject<PART>("SELECT * FROM PARTS WHERE ScheduleID = @ScheduleID", para, sql).ToList();
                        //List<PART> lstpt = new List<PART>();
                        //lstpt = DB.PARTS.Where(x => x.ScheduleID == CI.ScheduleID).ToList();

                        if (lstpt.Count > 0)
                        {
                            foreach (PART pt in lstpt)
                            {
                                lstPOFT.Add(new PartOfTest(pt.Name, pt.OrderOfQuestion.Value));
                            }
                            rlstPartOfTest = lstPOFT;
                        }
                        else
                        {
                            rlstPartOfTest = null;
                        }
                    }
                    catch
                    {
                        rlstPartOfTest = null;
                    }

                    lstTD.OrderBy(x => x.NumberIndex);
                    //Debug.WriteLine("lstTD.Count {0}", lstTD.Count);
                    List<Questions> lstQuestions;
                    List<List<Questions>> lstLQuestion = new List<List<Questions>>();
                    Hashtable hstbAnswersheetDetail = null;

                    if (CI.Status == Common.STATUS_DOING_BUT_INTERRUPT || CI.Status == Common.STATUS_LOGGED_DO_NOT_FINISH || CI.Status == Common.STATUS_DOING || CI.Status == Common.STATUS_FINISHED)
                    {
                        hstbAnswersheetDetail = new Hashtable();
                        AnswersheetDetailDAO.Instance.GetHastableAnswersheetDetailByAnswerSheetID(CI, out hstbAnswersheetDetail, out EC, sql);
                        IsContinute = true;
                    }

                    if (lstTD.Count > 0)
                    {
                        List<SubQuestion> lstSubQuestiton = new List<SubQuestion>();
                        foreach (TEST_DETAILS td in lstTD)
                        {
                            lstQuestions = new List<Questions>();
                            lstSubQuestiton = GetListSubQuestionByQuestionID(td.QuestionID, td.TestID, sql);

                            para = new List<SqlParameter>();
                            para.Add(new SqlParameter("@QuestionID", td.QuestionID));
                            QUESTION ques = Utils.ExcuteObject<QUESTION>("SELECT * FROM QUESTIONS WHERE QuestionID = @QuestionID", para, sql).ToList()[0];

                            //int TypeQ = td.QUESTION.Type ?? default(int);
                            int TypeQ = ques.Type ?? default(int);
                            if (TypeQ != 5)
                            {
                                int firstIndex = 0;
                                foreach (var sq in lstSubQuestiton.Select((value, index) => new { data = value, index = index }))
                                {
                                    para = new List<SqlParameter>();
                                    para.Add(new SqlParameter("@SubQuestionID", sq.data.SubQuestionID));
                                    SUBQUESTION SQ = Utils.ExcuteObject<SUBQUESTION>("SELECT * FROM SUBQUESTIONS WHERE SubQuestionID = @SubQuestionID", para, sql).ToList()[0];
                                    //SUBQUESTION SQ = DB.SUBQUESTIONS.SingleOrDefault(x => x.SubQuestionID == sq.data.SubQuestionID);

                                    para = new List<SqlParameter>();
                                    para.Add(new SqlParameter("@QuestionID", SQ.QuestionID));
                                    ques = Utils.ExcuteObject<QUESTION>("SELECT * FROM QUESTIONS WHERE QuestionID = @QuestionID", para, sql).ToList()[0];

                                    Questions q = new Questions();
                                    q.NO = td.NumberIndex + sq.index + 1;
                                    // Todo

                                    q.FormatQuestion = td.Status;
                                    q.TestDetailID = td.TestDetailID;
                                    q.QuestionID = SQ.QuestionID;
                                    q.SubQuestionID = SQ.SubQuestionID;
                                    q.TestID = td.TestID;
                                    q.TitleOfQuestion = SQ.SubQuestionContent;
                                    q.AnswerChecked = 2000;
                                    //q.IsSingleChoice = SQ.QUESTION.IsSingleChoice;
                                    //q.IsQuestionContent = SQ.QUESTION.IsQuestionContent;
                                    q.IsSingleChoice = ques.IsSingleChoice;
                                    q.IsQuestionContent = ques.IsQuestionContent;
                                    q.Score = SQ.Score != null ? (float)SQ.Score : default(float);
                                    q.ListAnswer = GetListAnswerByListAnswerID(sq.data.ListAnswerID, sql);

                                    //// bonus 2025
                                    q.TopicID = ques.TopicID;
                                    // q.QuestionTypeID = ques.QuestionTypeID;
                                    /////
                                    int HeightToDisplayForSub = 0;
                                    if (SQ.HeightToDisplay.HasValue)
                                    {
                                        HeightToDisplayForSub = SQ.HeightToDisplay.Value;
                                    }
                                    q.HighToDisplayForSubQuestion = HeightToDisplayForSub;
                                    if (hstbAnswersheetDetail != null && (CI.Status == Common.STATUS_DOING_BUT_INTERRUPT || CI.Status == Common.STATUS_LOGGED_DO_NOT_FINISH || CI.Status == Common.STATUS_DOING || CI.Status == Common.STATUS_FINISHED))
                                    {
                                        //if (hstbAnswersheetDetail.ContainsKey(q.SubQuestionID) && hstbAnswersheetDetail[q.SubQuestionID].GetType() == typeof(int) && SQ.QUESTION.Type != (int)EXON.Common.QuizTypeEnum.Match)
                                        if (hstbAnswersheetDetail.ContainsKey(q.SubQuestionID) && hstbAnswersheetDetail[q.SubQuestionID].GetType() == typeof(int) && ques.Type != (int)EXON.Common.QuizTypeEnum.Match)
                                        {
                                            q.AnswerChecked = 2000 + sq.data.ListAnswerID.IndexOf(Convert.ToInt32(hstbAnswersheetDetail[q.SubQuestionID])) + 1;
                                        }
                                        // câu hỏi nối
                                        //else if (hstbAnswersheetDetail.ContainsKey(q.SubQuestionID) && hstbAnswersheetDetail[q.SubQuestionID].GetType() == typeof(int) && SQ.QUESTION.Type == (int)EXON.Common.QuizTypeEnum.Match)
                                        else if (hstbAnswersheetDetail.ContainsKey(q.SubQuestionID) && hstbAnswersheetDetail[q.SubQuestionID].GetType() == typeof(int) && ques.Type == (int)EXON.Common.QuizTypeEnum.Match)
                                        {
                                            ANSWER ansMatch = new ANSWER();

                                            int AnswerID = Convert.ToInt32(hstbAnswersheetDetail[q.SubQuestionID]);
                                            para = new List<SqlParameter>();
                                            para.Add(new SqlParameter("@AnswerID", AnswerID));
                                            List<ANSWER> ansMatchtemp = Utils.ExcuteObject<ANSWER>("SELECT * FROM ANSWERS WHERE AnswerID = @AnswerID", para, sql).ToList();
                                            ansMatch = ansMatchtemp.Count == 1 ? ansMatchtemp[0] : null;
                                            //ansMatch = DB.ANSWERS.Where(x => x.AnswerID == AnswerID).SingleOrDefault();
                                            q.AnswerSheetContent = ansMatch.AnswerContent;

                                        }
                                        else if (hstbAnswersheetDetail.ContainsKey(q.SubQuestionID) && hstbAnswersheetDetail[q.SubQuestionID].GetType() == typeof(string))
                                        {
                                            q.AnswerSheetContent = hstbAnswersheetDetail[q.SubQuestionID].ToString();
                                        }

                                    }

                                    //q.Type = SQ.QUESTION.Type ?? default(int);
                                    q.Type = ques.Type ?? default(int);

                                    //q.NumberQuestion = SQ.QUESTION.NumberSubQuestion;
                                    q.NumberQuestion = ques.NumberSubQuestion;
                                    count++;
                                    lstQuestions.Add(q);
                                    // chieeuf cao cau hoi
                                    int HeightToDisplayForQ = 0;
                                    //if (SQ.QUESTION.HeightToDisplay.HasValue)
                                    if (ques.HeightToDisplay.HasValue)
                                    {
                                        //HeightToDisplayForQ = SQ.QUESTION.HeightToDisplay.Value;
                                        HeightToDisplayForQ = ques.HeightToDisplay.Value;
                                    }
                                    //if (SQ.QUESTION.Audio != null && firstIndex == 0)
                                    if (ques.Audio != null && firstIndex == 0)
                                    {

                                        //lstQuestions.Insert(0, new Questions(SQ.QUESTION.Audio, SQ.QuestionID, td.TestDetailID, td.Status, q.Type));
                                        //lstQuestions.Insert(1, new Questions(SQ.QUESTION.QuestionContent, td.Status, HeightToDisplayForQ, td.TestDetailID));
                                        lstQuestions.Insert(0, new Questions(ques.Audio, SQ.QuestionID, td.TestDetailID, td.Status, q.Type));
                                        lstQuestions.Insert(1, new Questions(ques.QuestionContent, td.Status, HeightToDisplayForQ, td.TestDetailID));
                                        firstIndex = 1;
                                    }
                                    //else if (lstQuestions.Count == SQ.QUESTION.NumberSubQuestion && SQ.QUESTION.NumberSubQuestion > 1 && firstIndex == 0)
                                    else if (lstQuestions.Count == ques.NumberSubQuestion && ques.NumberSubQuestion > 1 && firstIndex == 0)
                                    {
                                        //lstQuestions.Insert(0, new Questions(SQ.QUESTION.QuestionContent, td.Status, HeightToDisplayForQ, td.TestDetailID));
                                        lstQuestions.Insert(0, new Questions(ques.QuestionContent, td.Status, HeightToDisplayForQ, td.TestDetailID));
                                        firstIndex = 1;
                                    }
                                    //Thread.Sleep(100);
                                }
                                lstLQuestion.Add(lstQuestions);
                            }
                        }
                        numberQuestionsOfTest = count;
                        //Debug.WriteLine("lstLQuestion: " + lstLQuestion.Count);

                        rLLstQuest = lstLQuestion;
                        EC = new ErrorController(Common.STATUS_OK, "Nhận danh sách các câu hỏi thành công bằng TestID=" + CI.TestID);
                    }
                    else
                    {
                        rLLstQuest = null;
                        EC = new ErrorController(Common.STATUS_ERROR, "Không thể nhận TEST_DETAIL bằng TestID=" + CI.TestID);
                    }



                }
                catch (Exception ex)
                {
                    rLLstQuest = null;
                    rlstPartOfTest = null;
                    EC = new ErrorController(Common.STATUS_UNKOWN_EXCEPTION, string.Format(Common.STR_STATUS_UNKOWN_EXCEPTION, ex.Message));
                }
            }
        }

        public List<QuesIDwithBonus> GetListQuestionWithBonusScore(SqlConnection sql)
        {

            // quesIDwithBonus = new QuesIDwithBonus();
            string queryString = @"select QUESTIONS.QuestionID, SDB.[From], SDB.[To], SDB.Bonus    

                                  from QUESTIONS
                                  join TOPICS on QUESTIONS.TopicID = TOPICS.TopicID
                                  join STRUCTURE_DETAILS SD on TOPICS.TopicID = SD.TopicID
                                  join STRUCTURE_DETAIL_BONUS SDB on SD.StructureDetailID = SDB.StructureDetailID
                                  where QUESTIONS.Level = SD.Level";

            try
            {
                List<QuesIDwithBonus> LquesIDwithBonus = Utils.ExcuteObject<QuesIDwithBonus>(queryString, sql).ToList();
                return LquesIDwithBonus;
            }
            catch
            {
                return new List<QuesIDwithBonus>();
            }


        }

        public List<StructureDetailIDwithMaxBonus> GetListMaxBonusWithStructureID(int ScheduleID, SqlConnection sql)
        {

            // quesIDwithBonus = new QuesIDwithBonus();
            string queryString = @"select   STRUCTURE_DETAILS.NumberQuestions/NULLIF(QUESTIONS.NumberSubQuestion, 0)  as NumOfQuestionInTest,STRUCTURE_DETAILS.StructureDetailID, max (STRUCTURE_DETAIL_BONUS.Bonus) as maxBonus

                                    from STRUCTURES
                                    join STRUCTURE_DETAILS on STRUCTURES.StructureID =STRUCTURE_DETAILS.StructureID
                                    join STRUCTURE_DETAIL_BONUS on STRUCTURE_DETAIL_BONUS.StructureDetailID = STRUCTURE_DETAILS.StructureDetailID

									join TOPICS on TOPICS.TopicID = STRUCTURE_DETAILS.TopicID
									join QUESTIONS on QUESTIONS .TopicID = TOPICS.TopicID
				
                                    where STRUCTURES.StructureID = ( select top(1) STRUCTURES.StructureID
									                                    from SCHEDULES
									                                    join STRUCTURES on SCHEDULES.ScheduleID = STRUCTURES.ScheduleID
									                                    where SCHEDULES.ScheduleID = @ScheduleID
								                                    )

																

                                    group by STRUCTURE_DETAILS.NumberQuestions, STRUCTURE_DETAILS.StructureDetailID ,STRUCTURES.StructureID ,QUESTIONS.NumberSubQuestion";

            List<SqlParameter> para = new List<SqlParameter>();


            para.Add(new SqlParameter("@ScheduleID", ScheduleID));

            try
            {
                List<StructureDetailIDwithMaxBonus> LstructDetailWithMaxBonus = Utils.ExcuteObject<StructureDetailIDwithMaxBonus>(queryString, para, sql).ToList();
                return LstructDetailWithMaxBonus;

            }
            catch
            {

                return new List<StructureDetailIDwithMaxBonus>();
            }




        }


        private List<int> HandleGetNumberIdexAnswerInQuestion(string data)
        {
            List<int> lstIndex = new List<int>();

            string[] arrDataSplit = data.Split(',');
            foreach (string s in arrDataSplit)
            {
                lstIndex.Add(Convert.ToInt32(s));
            }
            return lstIndex;
        }

        private List<SubQuestion> GetListSubQuestionByQuestionID(int questionID, int testID, SqlConnection sql)
        {
            List<SubQuestion> lstSubQuestiton = new List<SubQuestion>();
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                List<SqlParameter> para = new List<SqlParameter>();
                para.Add(new SqlParameter("@QuestionID", questionID));
                para.Add(new SqlParameter("@TestID", testID));
                List<TEST_DETAILS> TDtemp = Utils.ExcuteObject<TEST_DETAILS>("SELECT * FROM TEST_DETAILS WHERE QuestionID = @QuestionID and TestID = @TestID", para, sql).ToList();
                TEST_DETAILS TD = TDtemp.Count == 1 ? TDtemp[0] : null;
                //TEST_DETAILS TD = DB.TEST_DETAILS.SingleOrDefault(x => x.QuestionID == questionID && x.TestID == testID);

                lstSubQuestiton = new JavaScriptSerializer().Deserialize<List<SubQuestion>>(TD.RandomAnswer);
            }
            return lstSubQuestiton;
        }

        private List<Answer> GetListAnswerByListAnswerID(List<int> lstAnswerID, SqlConnection sql)
        {
            List<Answer> lstAnswer = new List<Answer>();
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                foreach (int index in lstAnswerID)
                {
                    List<SqlParameter> para = new List<SqlParameter>();
                    para.Add(new SqlParameter("@AnswerID", index));
                    List<ANSWER> Atemp = Utils.ExcuteObject<ANSWER>("SELECT * FROM ANSWERS WHERE AnswerID = @AnswerID", para, sql).ToList();
                    ANSWER A = Atemp.Count == 1 ? Atemp[0] : null;
                    //ANSWER A = DB.ANSWERS.Where(x => x.AnswerID == index).SingleOrDefault();

                    if (A != null)
                    {
                        int height = 0;
                        if (A.HeightToDisplay.HasValue)
                            height = A.HeightToDisplay.Value;
                        double Score = 0;
                        para = new List<SqlParameter>();
                        para.Add(new SqlParameter("@SubQuestionID", A.SubQuestionID));
                        SUBQUESTION sub_ques = Utils.ExcuteObject<SUBQUESTION>("SELECT * FROM SUBQUESTIONS WHERE SubQuestionID = @SubQuestionID", para, sql).ToList()[0];
                        //if (A.SUBQUESTION.Score.HasValue)
                        if (sub_ques.Score.HasValue)
                            Score = sub_ques.Score.Value;
                        lstAnswer.Add(new Answer(A.AnswerID, A.AnswerContent, height, A.IsCorrect, A.SubQuestionID, Score));
                    }
                }
            }
            return lstAnswer;
        }

        public float SumScore(ContestantInformation CI, SqlConnection sql)
        {
            float result = 0;

            List<TEST_DETAILS> lstTD = new List<TEST_DETAILS>();

            List<SUBQUESTION> lstSub = new List<SUBQUESTION>();
            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                List<SqlParameter> para = new List<SqlParameter>();
                para.Add(new SqlParameter("@TestID", CI.TestID));
                lstTD = Utils.ExcuteObject<TEST_DETAILS>("SELECT * FROM TEST_DETAILS WHERE TestID = @TestID", para, sql).ToList();
                //lstTD = DB.TEST_DETAILS.Where(x => x.TestID == CI.TestID).ToList();

                if (lstTD.Count > 0)
                {
                    foreach (TEST_DETAILS td in lstTD)
                    {
                        para = new List<SqlParameter>();
                        para.Add(new SqlParameter("@QuestionID", td.QuestionID));
                        lstSub = Utils.ExcuteObject<SUBQUESTION>("SELECT * FROM SUBQUESTIONS WHERE QuestionID = @QuestionID", para, sql).ToList();
                        //lstSub = DB.SUBQUESTIONS.Where(x => x.QuestionID == td.QuestionID).ToList();
                        foreach (SUBQUESTION sub in lstSub)
                        {
                            para = new List<SqlParameter>();
                            para.Add(new SqlParameter("@QuestionID", sub.QuestionID));
                            QUESTION ques = Utils.ExcuteObject<QUESTION>("SELECT * FROM QUESTIONS WHERE QuestionID = @QuestionID", para, sql).ToList()[0];
                            // essay speaking rewritting
                            //if ((sub.QUESTION.Type == (int)EXON.Common.QuizTypeEnum.Essay) || (sub.QUESTION.Type == (int)EXON.Common.QuizTypeEnum.Speaking)
                            //    || (sub.QUESTION.Type == (int)EXON.Common.QuizTypeEnum.ReWritting))
                            if ((ques.Type == (int)EXON.Common.QuizTypeEnum.Essay) || (ques.Type == (int)EXON.Common.QuizTypeEnum.Speaking)
                                || (ques.Type == (int)EXON.Common.QuizTypeEnum.ReWritting))
                            {
                                //TODO
                            }
                            else
                            {

                                result += (float)(sub.Score ?? default(float));
                            }
                        }
                    }
                }
            }
            return result;
        }



        public float CheckAnswers(AnswersheetDetail ad, List<List<Questions>> lstLQuestion, ref Dictionary<int, int> numOfcorrectSubQues, SqlConnection sql)
        {
            float result = 0;
            int key = 0;
            int count = 0;



            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                ANSWERSHEET_DETAILS ans = new ANSWERSHEET_DETAILS();
                if (ad.AnswerContent != null)
                {
                    List<SqlParameter> para = new List<SqlParameter>();
                    para.Add(new SqlParameter("@AnswerSheetDetailID", ad.AnswerSheetDetailID));
                    List<ANSWERSHEET_DETAILS> anstemp = Utils.ExcuteObject<ANSWERSHEET_DETAILS>("SELECT * FROM ANSWERSHEET_DETAILS WHERE AnswerSheetDetailID = @AnswerSheetDetailID", para, sql).ToList();
                    ans = anstemp.Count == 1 ? anstemp[0] : null;
                    //ans = DB.ANSWERSHEET_DETAILS.Where(x => x.AnswerSheetDetailID == ad.AnswerSheetDetailID).SingleOrDefault();

                    if (ans != null && ans.Score.HasValue)
                    {
                        result = (float)ans.Score.Value;
                    }
                }
                else
                {
                    foreach (List<Questions> lstQuestion in lstLQuestion) // may be lstQuestion is question in DB
                    {
                        //numOfcorrectSubQues = new Dictionary<int, int>();

                        foreach (Questions questions in lstQuestion) // may be question is subquestion in DB
                        {
                            List<Answer> lstAn = questions.ListAnswer.Where(x => x.SubQuestionID == ad.SubQuestionID).ToList();
                            if (lstAn.Count > 0)
                            {
                                Answer A = lstAn.SingleOrDefault(y => y.AnswerID == ad.ChoosenAnswer);
                                if (A != null)
                                {
                                    if (A.IsCorrect)
                                    {
                                        result = (float)A.Score.Value;
                                        if (!numOfcorrectSubQues.ContainsKey(questions.QuestionID))
                                        {
                                            key = questions.QuestionID;
                                            numOfcorrectSubQues.Add(key, 1);
                                        }
                                        else
                                        {
                                            numOfcorrectSubQues.TryGetValue(questions.QuestionID, out count);
                                            count++;
                                            numOfcorrectSubQues[questions.QuestionID] = count;
                                        }
                                        //InsertScoreIntoAnswersheetDetail(ad,result,sql);
                                    }
                                }
                            }
                        }

                    }


                    //List<ANSWER> lstA = DB.ANSWERS.Where(x => x.SubQuestionID == ad.SubQuestionID).ToList();
                    //if (lstA.Count > 0)
                    //{
                    //    ANSWER A = lstA.SingleOrDefault(y => y.AnswerID == ad.ChoosenAnswer);
                    //    if (A != null)
                    //    {
                    //        if (A.IsCorrect)
                    //        {
                    //            result = (float)A.SUBQUESTION.Score.Value;
                    //        }
                    //    }
                    //}
                }
            }
            return result;
        }



        private float InsertScoreIntoAnswersheetDetail(AnswersheetDetail ad, float result, SqlConnection sql)
        {

            ErrorController EC;

            // nếu có bonus 
            // 



            // nếu ko có bonus
            SqlCommand sqlcmd = new SqlCommand("update ANSWERSHEET_DETAILS set " + " Score=@Score where AnswerSheetDetailID=@id ;", sql);
            sqlcmd.Parameters.Add("@id", ad.AnswerSheetDetailID);
            sqlcmd.Parameters.Add("@Score", result);
            int row = 0;
            while (row == 0)
            {
                row = sqlcmd.ExecuteNonQuery();

            }
            EC = new ErrorController(Common.STATUS_OK, "Thay đổi status thành STATUS_CHANGED: 4002");


            return result;
        }




        private float AddBonusScore(AnswersheetDetail ad, List<List<Questions>> lstLQuestion, SqlConnection sql)
        {

            float result = 0;



            using (EXON_SYSTEM_TESTEntities DB = new EXON_SYSTEM_TESTEntities())
            {
                ANSWERSHEET_DETAILS ans = new ANSWERSHEET_DETAILS();



                foreach (List<Questions> lstQuestion in lstLQuestion) // may be lstQuestion is question in DB
                {




                    int count_corerectAns_inQues = 0;
                    foreach (Questions questions in lstQuestion) // may be question is subquestion in DB
                    {
                        List<Answer> lstAn = questions.ListAnswer.Where(x => x.SubQuestionID == ad.SubQuestionID).ToList();
                        if (lstAn.Count > 0)
                        {
                            Answer A = lstAn.SingleOrDefault(y => y.AnswerID == ad.ChoosenAnswer);
                            if (A != null)
                            {
                                if (A.IsCorrect)
                                {
                                    count_corerectAns_inQues++;

                                    result = (float)A.Score.Value + count_corerectAns_inQues; // tthêm query để tính bonus ;
                                }
                            }
                        }
                    }
                }




            }
            return result;

        }

    }
}
