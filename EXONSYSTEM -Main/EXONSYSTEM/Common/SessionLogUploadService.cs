using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace EXONSYSTEM.Common
{
    public static class SessionFolderReader
    {
        public static SessionFolderReadResult ReadFolder(
            string sessionFolderPath,
            long contestantShiftId,
            long? contestantId,
            long? divisionShiftId,
            long? answerSheetId,
            string fallbackSegmentGuid)
        {
            Dictionary<string, string> runtime = KeyValueFileReader.Read(Path.Combine(sessionFolderPath, "runtime.state"));
            Dictionary<string, string> finalResult = KeyValueFileReader.Read(Path.Combine(sessionFolderPath, "final.result"));
            Dictionary<string, string> sessionInfo = KeyValueFileReader.Read(Path.Combine(sessionFolderPath, "session.info"));

            string computerName = KeyValueFileReader.Get(sessionInfo, "ComputerName") ?? Environment.MachineName;

            ExamSessionAuditUploadModel audit = new ExamSessionAuditUploadModel();
            audit.ContestantShiftId = contestantShiftId;
            audit.ContestantId = contestantId;
            audit.DivisionShiftId = divisionShiftId;
            audit.AnswerSheetId = answerSheetId;
            audit.ComputerName = computerName;
            audit.SubmitTimeUnixMs = KeyValueFileReader.GetLong(finalResult, "SubmitTimeUnixMs") ?? KeyValueFileReader.GetLong(runtime, "SubmitTimeUnixMs");
            audit.SubmitTimeText = KeyValueFileReader.Get(finalResult, "SubmitTimeText") ?? KeyValueFileReader.Get(runtime, "SubmitTimeText");
            audit.TimeWorkedMs = KeyValueFileReader.GetLong(finalResult, "TimeWorkedMs") ?? KeyValueFileReader.GetLong(runtime, "TimeWorkedMs");
            audit.TimeWorkedText = KeyValueFileReader.Get(finalResult, "TimeWorkedText") ?? KeyValueFileReader.Get(runtime, "TimeWorkedText");
            audit.UploadSource = "LOCAL_FILE";

            ExamSessionSegmentUploadModel segment = new ExamSessionSegmentUploadModel();
            segment.SegmentGuid = KeyValueFileReader.Get(sessionInfo, "SegmentGuid") ?? fallbackSegmentGuid ?? Guid.NewGuid().ToString();
            segment.ContestantShiftId = contestantShiftId;
            segment.ComputerName = computerName;
            segment.SessionFolderName = new DirectoryInfo(sessionFolderPath).Name;
            segment.UploadStatus = "Success";
            segment.UploadRetryCount = 0;
            segment.LastUploadMessage = null;

            List<RawFileUploadModel> files = Directory.GetFiles(sessionFolderPath)
                .Select(path => new RawFileUploadModel
                {
                    FileName = Path.GetFileName(path),
                    FileType = Path.GetExtension(path),
                    FileContent = File.ReadAllText(path, Encoding.UTF8),
                    FileSize = new FileInfo(path).Length
                })
                .ToList();

            SessionFolderReadResult result = new SessionFolderReadResult();
            result.Audit = audit;
            result.Segment = segment;
            result.Files = files;
            return result;
        }
    }

    public class SessionLogUploadDao
    {
        public void UploadSessionFolder(
            ExamSessionAuditUploadModel audit,
            ExamSessionSegmentUploadModel segment,
            List<RawFileUploadModel> files,
            SqlConnection sql)
        {
            if (sql == null)
            {
                throw new ArgumentNullException("sql");
            }

            bool shouldClose = false;
            if (sql.State != ConnectionState.Open)
            {
                sql.Open();
                shouldClose = true;
            }

            SqlTransaction tran = null;

            try
            {
                tran = sql.BeginTransaction();

                long sessionAuditId = UpsertSessionAudit(audit, sql, tran);
                long sessionSegmentId = UpsertSessionSegment(sessionAuditId, segment, sql, tran);
                ReplaceRawFiles(sessionSegmentId, files, sql, tran);

                tran.Commit();
            }
            catch
            {
                if (tran != null)
                {
                    tran.Rollback();
                }

                throw;
            }
            finally
            {
                if (shouldClose && sql.State == ConnectionState.Open)
                {
                    sql.Close();
                }
            }
        }

        private long UpsertSessionAudit(ExamSessionAuditUploadModel model, SqlConnection sql, SqlTransaction tran)
        {
            string query = @"
DECLARE @SessionAuditId BIGINT;

SELECT @SessionAuditId = SessionAuditId
FROM EXAM_SESSION_AUDIT
WHERE ContestantShiftId = @ContestantShiftId;

IF @SessionAuditId IS NULL
BEGIN
    INSERT INTO EXAM_SESSION_AUDIT
    (
        ContestantShiftId,
        ContestantId,
        DivisionShiftId,
        AnswerSheetId,
        ComputerName,
        SubmitTimeUnixMs,
        SubmitTimeText,
        TimeWorkedMs,
        TimeWorkedText,
        UploadSource,
        CreatedAt,
        UpdatedAt
    )
    VALUES
    (
        @ContestantShiftId,
        @ContestantId,
        @DivisionShiftId,
        @AnswerSheetId,
        @ComputerName,
        @SubmitTimeUnixMs,
        @SubmitTimeText,
        @TimeWorkedMs,
        @TimeWorkedText,
        @UploadSource,
        GETDATE(),
        GETDATE()
    );

    SET @SessionAuditId = CAST(SCOPE_IDENTITY() AS BIGINT);
END
ELSE
BEGIN
    UPDATE EXAM_SESSION_AUDIT
    SET
        ContestantId = ISNULL(@ContestantId, ContestantId),
        DivisionShiftId = ISNULL(@DivisionShiftId, DivisionShiftId),
        AnswerSheetId = ISNULL(@AnswerSheetId, AnswerSheetId),
        ComputerName = ISNULL(@ComputerName, ComputerName),
        SubmitTimeUnixMs = ISNULL(@SubmitTimeUnixMs, SubmitTimeUnixMs),
        SubmitTimeText = ISNULL(@SubmitTimeText, SubmitTimeText),
        TimeWorkedMs = ISNULL(@TimeWorkedMs, TimeWorkedMs),
        TimeWorkedText = ISNULL(@TimeWorkedText, TimeWorkedText),
        UploadSource = ISNULL(@UploadSource, UploadSource),
        UpdatedAt = GETDATE()
    WHERE SessionAuditId = @SessionAuditId;
END

SELECT @SessionAuditId;";

            using (SqlCommand cmd = new SqlCommand(query, sql, tran))
            {
                cmd.Parameters.AddWithValue("@ContestantShiftId", model.ContestantShiftId);
                cmd.Parameters.AddWithValue("@ContestantId", (object)model.ContestantId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DivisionShiftId", (object)model.DivisionShiftId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AnswerSheetId", (object)model.AnswerSheetId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ComputerName", (object)model.ComputerName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SubmitTimeUnixMs", (object)model.SubmitTimeUnixMs ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SubmitTimeText", (object)model.SubmitTimeText ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TimeWorkedMs", (object)model.TimeWorkedMs ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TimeWorkedText", (object)model.TimeWorkedText ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UploadSource", (object)model.UploadSource ?? DBNull.Value);
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        private long UpsertSessionSegment(long sessionAuditId, ExamSessionSegmentUploadModel model, SqlConnection sql, SqlTransaction tran)
        {
            string query = @"
DECLARE @SessionSegmentId BIGINT;

SELECT @SessionSegmentId = SessionSegmentId
FROM EXAM_SESSION_SEGMENT
WHERE SegmentGuid = @SegmentGuid;

IF @SessionSegmentId IS NULL
BEGIN
    INSERT INTO EXAM_SESSION_SEGMENT
    (
        SessionAuditId,
        SegmentGuid,
        ContestantShiftId,
        ComputerName,
        SessionFolderName,
        UploadStatus,
        UploadedAt,
        UploadRetryCount,
        LastUploadMessage,
        CreatedAt
    )
    VALUES
    (
        @SessionAuditId,
        @SegmentGuid,
        @ContestantShiftId,
        @ComputerName,
        @SessionFolderName,
        @UploadStatus,
        GETDATE(),
        @UploadRetryCount,
        @LastUploadMessage,
        GETDATE()
    );

    SET @SessionSegmentId = CAST(SCOPE_IDENTITY() AS BIGINT);
END
ELSE
BEGIN
    UPDATE EXAM_SESSION_SEGMENT
    SET
        SessionAuditId = @SessionAuditId,
        ContestantShiftId = @ContestantShiftId,
        ComputerName = ISNULL(@ComputerName, ComputerName),
        SessionFolderName = ISNULL(@SessionFolderName, SessionFolderName),
        UploadStatus = @UploadStatus,
        UploadedAt = GETDATE(),
        UploadRetryCount = @UploadRetryCount,
        LastUploadMessage = @LastUploadMessage
    WHERE SessionSegmentId = @SessionSegmentId;
END

SELECT @SessionSegmentId;";

            using (SqlCommand cmd = new SqlCommand(query, sql, tran))
            {
                cmd.Parameters.AddWithValue("@SessionAuditId", sessionAuditId);
                cmd.Parameters.AddWithValue("@SegmentGuid", model.SegmentGuid);
                cmd.Parameters.AddWithValue("@ContestantShiftId", model.ContestantShiftId);
                cmd.Parameters.AddWithValue("@ComputerName", (object)model.ComputerName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SessionFolderName", (object)model.SessionFolderName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UploadStatus", (object)model.UploadStatus ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UploadRetryCount", model.UploadRetryCount);
                cmd.Parameters.AddWithValue("@LastUploadMessage", (object)model.LastUploadMessage ?? DBNull.Value);
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        private void ReplaceRawFiles(long sessionSegmentId, List<RawFileUploadModel> files, SqlConnection sql, SqlTransaction tran)
        {
            using (SqlCommand deleteCmd = new SqlCommand("DELETE FROM EXAM_SESSION_RAW_FILE WHERE SessionSegmentId = @SessionSegmentId;", sql, tran))
            {
                deleteCmd.Parameters.AddWithValue("@SessionSegmentId", sessionSegmentId);
                deleteCmd.ExecuteNonQuery();
            }

            string insertQuery = @"
INSERT INTO EXAM_SESSION_RAW_FILE
(
    SessionSegmentId,
    FileName,
    FileType,
    FileContent,
    FileSize,
    CreatedAt
)
VALUES
(
    @SessionSegmentId,
    @FileName,
    @FileType,
    @FileContent,
    @FileSize,
    GETDATE()
);";

            foreach (RawFileUploadModel file in files)
            {
                using (SqlCommand cmd = new SqlCommand(insertQuery, sql, tran))
                {
                    cmd.Parameters.AddWithValue("@SessionSegmentId", sessionSegmentId);
                    cmd.Parameters.AddWithValue("@FileName", (object)file.FileName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FileType", (object)file.FileType ?? DBNull.Value);
                    SqlParameter contentParam = cmd.Parameters.Add("@FileContent", SqlDbType.NVarChar, -1);
                    contentParam.Value = (object)file.FileContent ?? DBNull.Value;
                    cmd.Parameters.AddWithValue("@FileSize", file.FileSize);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    public class SessionLogUploadService
    {
        private readonly SessionLogUploadDao _dao = new SessionLogUploadDao();

        public void UploadCurrentSessionLogs(
            string sessionFolder,
            long contestantShiftId,
            long? contestantId,
            long? divisionShiftId,
            long? answerSheetId,
            string segmentGuid,
            SqlConnection sql)
        {
            if (string.IsNullOrWhiteSpace(sessionFolder))
            {
                throw new Exception("Session folder is empty.");
            }

            if (!Directory.Exists(sessionFolder))
            {
                throw new Exception("Session folder does not exist: " + sessionFolder);
            }

            SessionFolderReadResult data = SessionFolderReader.ReadFolder(
                sessionFolder,
                contestantShiftId,
                contestantId,
                divisionShiftId,
                answerSheetId,
                segmentGuid);

            _dao.UploadSessionFolder(data.Audit, data.Segment, data.Files, sql);
        }
    }
}
