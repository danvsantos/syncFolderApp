namespace syncFolder.Models;

public class LogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PairId { get; set; }
    public string PairName { get; set; } = string.Empty;
    public SyncResult? Result { get; set; }
    public string? Error { get; set; }
    public DateTime Date { get; set; }

    public LogEntry() { }

    public LogEntry(Guid pairId, string pairName, SyncResult result, DateTime date)
    {
        PairId = pairId;
        PairName = pairName;
        Result = result;
        Error = result.HasErrors ? result.Errors.FirstOrDefault() : null;
        Date = date;
    }

    public LogEntry(Guid pairId, string pairName, string error, DateTime date)
    {
        PairId = pairId;
        PairName = pairName;
        Error = error;
        Date = date;
    }

    public bool IsError => Error != null && Result == null;

    public string Icon
    {
        get
        {
            if (IsError) return "Warning";
            if (Result?.HasErrors == true) return "ErrorCircle";
            return "CheckCircle";
        }
    }
}
