-- Migration: Thêm các trường thời gian đến mili giây vào bảng CONTESTANTS_SHIFTS
-- Chạy script này trên database trước khi deploy phiên bản mới của ứng dụng

-- EndTimeMsText: Thời điểm nộp bài, định dạng HH:mm:ss:fff
-- TimeWorkedMsText: Tổng thời gian đã làm bài (đã trừ thời gian bù), định dạng hh:mm:ss:fff
-- Cả 2 trường chỉ có giá trị khi Status = STATUS_FINISHED hoặc STATUS_REJECT, NULL trong các trạng thái khác

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'CONTESTANTS_SHIFTS' AND COLUMN_NAME = 'EndTimeMsText'
)
BEGIN
    ALTER TABLE CONTESTANTS_SHIFTS ADD EndTimeMsText VARCHAR(12) NULL;
    PRINT 'Đã thêm cột EndTimeMsText vào bảng CONTESTANTS_SHIFTS';
END
ELSE
BEGIN
    PRINT 'Cột EndTimeMsText đã tồn tại, bỏ qua.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'CONTESTANTS_SHIFTS' AND COLUMN_NAME = 'TimeWorkedMsText'
)
BEGIN
    ALTER TABLE CONTESTANTS_SHIFTS ADD TimeWorkedMsText VARCHAR(12) NULL;
    PRINT 'Đã thêm cột TimeWorkedMsText vào bảng CONTESTANTS_SHIFTS';
END
ELSE
BEGIN
    PRINT 'Cột TimeWorkedMsText đã tồn tại, bỏ qua.';
END

-- Kiểm tra kết quả
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'CONTESTANTS_SHIFTS'
  AND COLUMN_NAME IN ('EndTimeMsText', 'TimeWorkedMsText', 'TimeCheck', 'TimeStarted', 'TimeWorked')
ORDER BY COLUMN_NAME;
