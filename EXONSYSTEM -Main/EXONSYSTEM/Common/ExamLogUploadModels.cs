using System.Collections.Generic;

namespace EXONSYSTEM.Common
{
    public class ExamSessionAuditUploadModel
    {
        public long ContestantShiftId { get; set; }
        public long? ContestantId { get; set; }
        public long? DivisionShiftId { get; set; }
        public long? AnswerSheetId { get; set; }
        public string ComputerName { get; set; }
        public long? SubmitTimeUnixMs { get; set; }
        public string SubmitTimeText { get; set; }
        public long? TimeWorkedMs { get; set; }
        public string TimeWorkedText { get; set; }
        public string UploadSource { get; set; }
    }

    public class ExamSessionSegmentUploadModel
    {
        public string SegmentGuid { get; set; }
        public long ContestantShiftId { get; set; }
        public string ComputerName { get; set; }
        public string SessionFolderName { get; set; }
        public string UploadStatus { get; set; }
        public int UploadRetryCount { get; set; }
        public string LastUploadMessage { get; set; }
    }

    public class RawFileUploadModel
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string FileContent { get; set; }
        public long FileSize { get; set; }
    }

    public class SessionFolderReadResult
    {
        public ExamSessionAuditUploadModel Audit { get; set; }
        public ExamSessionSegmentUploadModel Segment { get; set; }
        public List<RawFileUploadModel> Files { get; set; }
    }
}
