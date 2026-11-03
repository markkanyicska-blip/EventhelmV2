namespace EventHelm.Api.Models;

public sealed class EventAssignmentImportPreview
{
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int UniqueOfficials { get; set; }
    public int NewOfficials { get; set; }
    public int ExistingOfficials { get; set; }
    public int NewBoats { get; set; }
    public int Groups { get; set; }
    public int AssignmentsToCreate { get; set; }
    public List<EventAssignmentImportRow> Rows { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class EventAssignmentImportRow
{
    public int RowNumber { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string SubgroupName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public string BoatName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ImportKey { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public bool OfficialExists { get; set; }
    public bool BoatExists { get; set; }
    public bool AssignmentExists { get; set; }
    public List<string> Errors { get; set; } = [];
}

public sealed class EventAssignmentImportCommitRequest
{
    public string FileName { get; set; } = string.Empty;
    public bool ReplacePreviousImport { get; set; }
    public List<EventAssignmentImportRow> Rows { get; set; } = [];
}

public sealed class EventAssignmentImportResult
{
    public Guid BatchId { get; set; }
    public int ImportedRows { get; set; }
    public int SkippedRows { get; set; }
    public int CreatedOfficials { get; set; }
    public int UpdatedOfficials { get; set; }
    public int CreatedBoats { get; set; }
    public int CreatedAssignments { get; set; }
    public int UpdatedAssignments { get; set; }
    public int RemovedAssignments { get; set; }
    public int Groups { get; set; }
    public List<string> Warnings { get; set; } = [];
}
