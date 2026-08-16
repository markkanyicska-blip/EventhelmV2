using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Dapper;
using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public sealed class EventAssignmentImportService : IEventAssignmentImportService
{
    private readonly IConfiguration _configuration;

    public EventAssignmentImportService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private NpgsqlConnection CreateConnection()
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("A DefaultConnection nincs beállítva.");

        return new NpgsqlConnection(connectionString);
    }

    public async Task<EventAssignmentImportPreview> PreviewAsync(
        Guid eventId,
        Stream fileStream,
        string fileName)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await EnsureEventExistsAsync(connection, eventId);

        var rows = ReadRows(fileStream);
        var officialExistence = new Dictionary<string, bool>(StringComparer.Ordinal);
        var boatExistence = new Dictionary<string, bool>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            PrepareAndValidateRow(row);
            if (!row.IsValid)
            {
                continue;
            }

            var officialKey = GetOfficialIdentityKey(row);
            if (!officialExistence.TryGetValue(officialKey, out var officialExists))
            {
                officialExists = await FindOfficialAsync(connection, null, row) is not null;
                officialExistence[officialKey] = officialExists;
            }

            row.OfficialExists = officialExists;

            if (string.IsNullOrWhiteSpace(row.BoatName))
            {
                row.BoatExists = true;
            }
            else
            {
                var boatKey = NormalizeKey(row.BoatName);
                if (!boatExistence.TryGetValue(boatKey, out var boatExists))
                {
                    boatExists = await FindBoatIdAsync(connection, null, row.BoatName) is not null;
                    boatExistence[boatKey] = boatExists;
                }

                row.BoatExists = boatExists;
            }

            row.AssignmentExists = await connection.ExecuteScalarAsync<bool>(
                """
                SELECT EXISTS
                (
                    SELECT 1
                    FROM "EventOfficialAssignments"
                    WHERE "EventId" = @EventId
                      AND "ImportKey" = @ImportKey
                );
                """,
                new { EventId = eventId, row.ImportKey });
        }

        var validRows = rows.Where(row => row.IsValid).ToList();
        var uniqueOfficialRows = validRows
            .GroupBy(GetOfficialIdentityKey)
            .Select(group => group.First())
            .ToList();

        var preview = new EventAssignmentImportPreview
        {
            FileName = Path.GetFileName(fileName),
            TotalRows = rows.Count,
            ValidRows = validRows.Count,
            InvalidRows = rows.Count - validRows.Count,
            UniqueOfficials = uniqueOfficialRows.Count,
            NewOfficials = uniqueOfficialRows.Count(row => !row.OfficialExists),
            ExistingOfficials = uniqueOfficialRows.Count(row => row.OfficialExists),
            NewBoats = validRows
                .Where(row => !string.IsNullOrWhiteSpace(row.BoatName) && !row.BoatExists)
                .Select(row => NormalizeKey(row.BoatName))
                .Distinct(StringComparer.Ordinal)
                .Count(),
            Groups = validRows
                .Select(row => NormalizeKey(row.GroupName))
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Count(),
            AssignmentsToCreate = validRows.Count(row => !row.AssignmentExists),
            Rows = rows
        };

        AddPreviewWarnings(preview, validRows);
        return preview;
    }

    public async Task<EventAssignmentImportResult> CommitAsync(
        Guid eventId,
        EventAssignmentImportCommitRequest request)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await EnsureEventExistsAsync(connection, eventId, transaction);

        var result = new EventAssignmentImportResult
        {
            BatchId = Guid.NewGuid()
        };

        var validRows = new List<EventAssignmentImportRow>();
        foreach (var row in request.Rows)
        {
            PrepareAndValidateRow(row);
            if (row.IsValid)
            {
                validRows.Add(row);
            }
            else
            {
                result.SkippedRows++;
                result.Warnings.Add(
                    $"{row.RowNumber}. sor kihagyva: {string.Join("; ", row.Errors)}");
            }
        }

        if (validRows.Count == 0)
        {
            throw new InvalidOperationException("Nincs érvényes importálható sor.");
        }

        await connection.ExecuteAsync(
            """
            INSERT INTO "EventAssignmentImportBatches"
            (
                "Id", "EventId", "FileName", "ReplacePreviousImport", "Status", "CreatedAt"
            )
            VALUES
            (
                @Id, @EventId, @FileName, @ReplacePreviousImport, 'Processing', @CreatedAt
            );
            """,
            new
            {
                Id = result.BatchId,
                EventId = eventId,
                FileName = SafeFileName(request.FileName),
                request.ReplacePreviousImport,
                CreatedAt = DateTime.UtcNow
            },
            transaction);

        if (request.ReplacePreviousImport)
        {
            result.RemovedAssignments = await connection.ExecuteAsync(
                """
                DELETE FROM "EventOfficialAssignments"
                WHERE "EventId" = @EventId
                  AND "Source" = 'xlsx-import';
                """,
                new { EventId = eventId },
                transaction);
        }

        var officialCache = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var updatedOfficials = new HashSet<Guid>();
        var boatCache = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var processedImportKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in validRows)
        {
            if (!processedImportKeys.Add(row.ImportKey))
            {
                result.SkippedRows++;
                result.Warnings.Add(
                    $"{row.RowNumber}. sor kihagyva: azonos beosztás már szerepelt az importban.");
                continue;
            }

            var officialKey = GetOfficialIdentityKey(row);
            if (!officialCache.TryGetValue(officialKey, out var officialId))
            {
                var official = await FindOfficialAsync(connection, transaction, row);
                if (official is null)
                {
                    officialId = Guid.NewGuid();
                    await CreateOfficialAsync(connection, transaction, officialId, row);
                    result.CreatedOfficials++;
                }
                else
                {
                    officialId = official.Id;
                    await UpdateOfficialAsync(connection, transaction, officialId, row);
                    if (updatedOfficials.Add(officialId))
                    {
                        result.UpdatedOfficials++;
                    }
                }

                officialCache[officialKey] = officialId;
            }

            Guid? boatId = null;
            if (!string.IsNullOrWhiteSpace(row.BoatName))
            {
                var boatKey = NormalizeKey(row.BoatName);
                if (!boatCache.TryGetValue(boatKey, out var resolvedBoatId))
                {
                    resolvedBoatId = await FindBoatIdAsync(connection, transaction, row.BoatName)
                        ?? Guid.Empty;

                    if (resolvedBoatId == Guid.Empty)
                    {
                        resolvedBoatId = Guid.NewGuid();
                        await CreateBoatAsync(
                            connection,
                            transaction,
                            resolvedBoatId,
                            row.BoatName);
                        result.CreatedBoats++;
                    }

                    boatCache[boatKey] = resolvedBoatId;
                }

                boatId = resolvedBoatId;
            }

            var assignmentExists = await connection.ExecuteScalarAsync<bool>(
                """
                SELECT EXISTS
                (
                    SELECT 1
                    FROM "EventOfficialAssignments"
                    WHERE "EventId" = @EventId
                      AND "ImportKey" = @ImportKey
                );
                """,
                new { EventId = eventId, row.ImportKey },
                transaction);

            await connection.ExecuteAsync(
                """
                INSERT INTO "EventOfficialAssignments"
                (
                    "Id", "EventId", "OfficialId", "Role", "AttendanceStatus",
                    "GroupName", "SubgroupName", "Task", "AssignmentOrder", "BoatId", "Source",
                    "ImportKey", "ImportBatchId", "SourceRowNumber", "UpdatedAt"
                )
                VALUES
                (
                    @Id, @EventId, @OfficialId, @Role, 'Planned',
                    @GroupName, @SubgroupName, @Task, @AssignmentOrder, @BoatId, 'xlsx-import',
                    @ImportKey, @ImportBatchId, @SourceRowNumber, @UpdatedAt
                )
                ON CONFLICT ON CONSTRAINT "UQ_EventOfficialAssignments_EventId_ImportKey"
              
                DO UPDATE SET
                    "OfficialId" = EXCLUDED."OfficialId",
                    "Role" = EXCLUDED."Role",
                    "AttendanceStatus" = EXCLUDED."AttendanceStatus",
                    "GroupName" = EXCLUDED."GroupName",
                    "SubgroupName" = EXCLUDED."SubgroupName",
                    "Task" = EXCLUDED."Task",
                    "AssignmentOrder" = EXCLUDED."AssignmentOrder",
                    "BoatId" = EXCLUDED."BoatId",
                    "Source" = EXCLUDED."Source",
                    "ImportBatchId" = EXCLUDED."ImportBatchId",
                    "SourceRowNumber" = EXCLUDED."SourceRowNumber",
                    "UpdatedAt" = EXCLUDED."UpdatedAt";
                """,
                new
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    OfficialId = officialId,
                    Role = NullIfEmpty(row.Position) ?? "Rendező",
                    GroupName = NullIfEmpty(row.GroupName),
                    SubgroupName = NullIfEmpty(row.SubgroupName),
                    Task = NullIfEmpty(row.Task),
                    AssignmentOrder = Math.Max(0, row.RowNumber),
                    BoatId = boatId,
                    row.ImportKey,
                    ImportBatchId = result.BatchId,
                    SourceRowNumber = row.RowNumber,
                    UpdatedAt = DateTime.UtcNow
                },
                transaction);

            if (assignmentExists)
            {
                result.UpdatedAssignments++;
            }
            else
            {
                result.CreatedAssignments++;
            }

            result.ImportedRows++;
        }

        result.Groups = validRows
            .Select(row => NormalizeKey(row.GroupName))
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Count();

        await connection.ExecuteAsync(
            """
            UPDATE "EventAssignmentImportBatches"
            SET
                "Status" = 'Completed',
                "ImportedRows" = @ImportedRows,
                "SkippedRows" = @SkippedRows,
                "CreatedOfficials" = @CreatedOfficials,
                "UpdatedOfficials" = @UpdatedOfficials,
                "CreatedBoats" = @CreatedBoats,
                "CreatedAssignments" = @CreatedAssignments,
                "UpdatedAssignments" = @UpdatedAssignments,
                "RemovedAssignments" = @RemovedAssignments,
                "CompletedAt" = @CompletedAt
            WHERE "Id" = @BatchId;
            """,
            new
            {
                result.ImportedRows,
                result.SkippedRows,
                result.CreatedOfficials,
                result.UpdatedOfficials,
                result.CreatedBoats,
                result.CreatedAssignments,
                result.UpdatedAssignments,
                result.RemovedAssignments,
                CompletedAt = DateTime.UtcNow,
                result.BatchId
            },
            transaction);

        await transaction.CommitAsync();
        return result;
    }

    private static List<EventAssignmentImportRow> ReadRows(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("Az Excel nem tartalmaz munkalapot.");
        var usedRange = worksheet.RangeUsed()
            ?? throw new InvalidOperationException("Az Excel munkalap üres.");

        var headerRowNumber = FindHeaderRow(usedRange);
        var columnMap = BuildColumnMap(worksheet.Row(headerRowNumber));
        RequireColumn(columnMap, "name", "NÉV");
        RequireColumn(columnMap, "position", "POZÍCIÓ");

        var rows = new List<EventAssignmentImportRow>();
        var lastGroup = string.Empty;
        var lastSubgroup = string.Empty;
        var lastTask = string.Empty;
        var lastBoat = string.Empty;

        for (var rowNumber = headerRowNumber + 1;
             rowNumber <= usedRange.LastRow().RowNumber();
             rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var group = ReadCell(row, columnMap, "group");
            var name = ReadCell(row, columnMap, "name");
            var position = ReadCell(row, columnMap, "position");
            var task = ReadCell(row, columnMap, "task");
            var boat = ReadCell(row, columnMap, "boat");
            var phone = ReadCell(row, columnMap, "phone");
            var email = ReadCell(row, columnMap, "email");

            if (!string.IsNullOrWhiteSpace(group))
            {
                lastGroup = group;
            }

            if (IsSectionHeadingRow(worksheet, rowNumber, columnMap, name, position, task, boat, phone, email))
            {
                lastSubgroup = FirstNotEmpty(name, position, task, boat);
                lastTask = string.Empty;
                lastBoat = string.Empty;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(task))
            {
                lastTask = task;
            }

            if (!string.IsNullOrWhiteSpace(boat))
            {
                lastBoat = boat;
            }

            if (string.IsNullOrWhiteSpace(name) &&
                string.IsNullOrWhiteSpace(position) &&
                string.IsNullOrWhiteSpace(task) &&
                string.IsNullOrWhiteSpace(boat) &&
                string.IsNullOrWhiteSpace(phone) &&
                string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            rows.Add(new EventAssignmentImportRow
            {
                RowNumber = rowNumber,
                GroupName = Clean(string.IsNullOrWhiteSpace(group) ? lastGroup : group),
                SubgroupName = Clean(lastSubgroup),
                Name = Clean(name),
                Position = Clean(position),
                Task = Clean(string.IsNullOrWhiteSpace(task) ? lastTask : task),
                BoatName = Clean(string.IsNullOrWhiteSpace(boat) ? lastBoat : boat),
                Phone = Clean(phone),
                Email = Clean(email)
            });
        }

        return rows;
    }

    private static bool IsSectionHeadingRow(
        IXLWorksheet worksheet,
        int rowNumber,
        IReadOnlyDictionary<string, int> columnMap,
        string name,
        string position,
        string task,
        string boat,
        string phone,
        string email)
    {
        if (!string.IsNullOrWhiteSpace(phone) || !string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var values = new[] { name, position, task, boat }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        if (values.Count != 1)
        {
            return false;
        }

        var relevantColumns = new[] { "name", "position", "task", "boat" }
            .Where(columnMap.ContainsKey)
            .Select(key => columnMap[key])
            .ToHashSet();

        foreach (var mergedRange in worksheet.MergedRanges)
        {
            if (mergedRange.FirstRow().RowNumber() != rowNumber ||
                mergedRange.LastRow().RowNumber() != rowNumber)
            {
                continue;
            }

            var coveredRelevantColumns = Enumerable
                .Range(
                    mergedRange.FirstColumn().ColumnNumber(),
                    mergedRange.ColumnCount())
                .Count(relevantColumns.Contains);

            if (coveredRelevantColumns >= 2)
            {
                return true;
            }
        }

        return false;
    }

    private static int FindHeaderRow(IXLRange usedRange)
    {
        foreach (var row in usedRange.Rows())
        {
            var values = row.Cells()
                .Select(cell => NormalizeHeader(cell.GetFormattedString()))
                .ToHashSet(StringComparer.Ordinal);

            if (values.Contains("nev") &&
                (values.Contains("pozicio") || values.Contains("szerepkor") || values.Contains("beosztas")))
            {
                return row.RowNumber();
            }
        }

        throw new InvalidOperationException(
            "Nem található fejlécsor. Kötelező oszlopok: NÉV és POZÍCIÓ/SZEREPKÖR.");
    }

    private static Dictionary<string, int> BuildColumnMap(IXLRow headerRow)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var cell in headerRow.CellsUsed())
        {
            var header = NormalizeHeader(cell.GetFormattedString());
            var key = header switch
            {
                "csoport" or "szekcio" or "terulet" => "group",
                "nev" or "rendezo" or "szemely" => "name",
                "pozicio" or "szerepkor" or "beosztas" => "position",
                "feladat" or "megjegyzes" => "task",
                "hajo" or "motorcsonak" or "motoros" => "boat",
                "telefon" or "telefonszam" or "mobil" => "phone",
                "email" or "emailcim" => "email",
                _ => string.Empty
            };

            if (key.Length > 0 && !result.ContainsKey(key))
            {
                result[key] = cell.Address.ColumnNumber;
            }
        }

        return result;
    }

    private static string ReadCell(
        IXLRow row,
        IReadOnlyDictionary<string, int> map,
        string key)
    {
        return map.TryGetValue(key, out var column)
            ? row.Cell(column).GetFormattedString()
            : string.Empty;
    }

    private static void RequireColumn(
        IReadOnlyDictionary<string, int> map,
        string key,
        string label)
    {
        if (!map.ContainsKey(key))
        {
            throw new InvalidOperationException($"Hiányzó kötelező oszlop: {label}.");
        }
    }

    private static void PrepareAndValidateRow(EventAssignmentImportRow row)
    {
        row.GroupName = Clean(row.GroupName);
        row.SubgroupName = Clean(row.SubgroupName);
        row.Name = Clean(row.Name);
        row.Position = Clean(row.Position);
        row.Task = Clean(row.Task);
        row.BoatName = Clean(row.BoatName);
        row.Phone = NormalizePhone(row.Phone);
        row.Email = Clean(row.Email).ToLowerInvariant();
        row.Errors.Clear();

        if (row.Name.Length == 0)
        {
            row.Errors.Add("Hiányzik a név.");
        }

        if (row.Position.Length == 0)
        {
            row.Errors.Add("Hiányzik a pozíció.");
        }

        if (row.Email.Length > 0 &&
            !Regex.IsMatch(row.Email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
        {
            row.Errors.Add("Hibás e-mail-cím.");
        }

        row.ImportKey = CreateImportKey(row);
        row.IsValid = row.Errors.Count == 0;
    }

    private static void AddPreviewWarnings(
        EventAssignmentImportPreview preview,
        IReadOnlyCollection<EventAssignmentImportRow> validRows)
    {
        var duplicateContacts = validRows
            .Where(row => row.Email.Length > 0)
            .GroupBy(row => NormalizeKey(row.Email))
            .Where(group => group.Select(GetOfficialIdentityKey).Distinct().Count() > 1)
            .Select(group => group.First().Email)
            .ToList();

        if (duplicateContacts.Count > 0)
        {
            preview.Warnings.Add(
                $"Azonos e-mail több eltérő névnél: {string.Join(", ", duplicateContacts)}.");
        }

        var duplicateAssignments = validRows
            .GroupBy(row => row.ImportKey)
            .Where(group => group.Count() > 1)
            .Select(group => group.First().RowNumber)
            .ToList();

        if (duplicateAssignments.Count > 0)
        {
            preview.Warnings.Add(
                $"Teljesen azonos beosztássorok találhatók. Első előfordulási sorok: {string.Join(", ", duplicateAssignments)}.");
        }

        if (preview.InvalidRows > 0)
        {
            preview.Warnings.Add(
                $"{preview.InvalidRows} hibás sor nem kerül importálásra.");
        }
    }

    private static async Task EnsureEventExistsAsync(
        NpgsqlConnection connection,
        Guid eventId,
        NpgsqlTransaction? transaction = null)
    {
        var exists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM \"Events\" WHERE \"Id\" = @EventId);",
            new { EventId = eventId },
            transaction);

        if (!exists)
        {
            throw new KeyNotFoundException("Az esemény nem található.");
        }
    }

    private static async Task<OfficialMatch?> FindOfficialAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        EventAssignmentImportRow row)
    {
        return await connection.QueryFirstOrDefaultAsync<OfficialMatch>(
            """
            SELECT "Id", "Name"
            FROM "Officials"
            WHERE
                (NULLIF(@Email, '') IS NOT NULL AND lower(COALESCE("Email", '')) = lower(@Email))
                OR
                (NULLIF(@PhoneDigits, '') IS NOT NULL AND
                 regexp_replace(COALESCE("Phone", ''), '[^0-9]', '', 'g') = @PhoneDigits)
                OR
                lower(regexp_replace(trim("Name"), '\s+', ' ', 'g')) = lower(@NormalizedName)
            ORDER BY
                CASE
                    WHEN NULLIF(@Email, '') IS NOT NULL AND lower(COALESCE("Email", '')) = lower(@Email) THEN 1
                    WHEN NULLIF(@PhoneDigits, '') IS NOT NULL AND regexp_replace(COALESCE("Phone", ''), '[^0-9]', '', 'g') = @PhoneDigits THEN 2
                    ELSE 3
                END
            LIMIT 1;
            """,
            new
            {
                Email = row.Email,
                PhoneDigits = PhoneDigits(row.Phone),
                NormalizedName = NormalizeSpaces(row.Name)
            },
            transaction);
    }

    private static async Task CreateOfficialAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid officialId,
        EventAssignmentImportRow row)
    {
        await connection.ExecuteAsync(
            """
            INSERT INTO "Officials"
            (
                "Id", "Name", "Phone", "Email", "Role", "Status", "CreatedAt"
            )
            VALUES
            (
                @Id, @Name, @Phone, @Email, @Role, 'Available', @CreatedAt
            );
            """,
            new
            {
                Id = officialId,
                row.Name,
                Phone = NullIfEmpty(row.Phone),
                Email = NullIfEmpty(row.Email),
                Role = NullIfEmpty(row.Position) ?? "Rendező",
                CreatedAt = DateTime.UtcNow
            },
            transaction);
    }

    private static async Task UpdateOfficialAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid officialId,
        EventAssignmentImportRow row)
    {
        await connection.ExecuteAsync(
            """
            UPDATE "Officials"
            SET
                "Name" = @Name,
                "Phone" = COALESCE(@Phone, "Phone"),
                "Email" = COALESCE(@Email, "Email"),
                "Role" = COALESCE(NULLIF("Role", ''), @Role)
            WHERE "Id" = @Id;
            """,
            new
            {
                Id = officialId,
                row.Name,
                Phone = NullIfEmpty(row.Phone),
                Email = NullIfEmpty(row.Email),
                Role = NullIfEmpty(row.Position)
            },
            transaction);
    }

    private static async Task<Guid?> FindBoatIdAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string boatName)
    {
        return await connection.QueryFirstOrDefaultAsync<Guid?>(
            """
            SELECT "Id"
            FROM "Boats"
            WHERE lower(regexp_replace(trim("Name"), '\s+', ' ', 'g')) = lower(@Name)
               OR lower(COALESCE("CallSign", '')) = lower(@Name)
            LIMIT 1;
            """,
            new { Name = NormalizeSpaces(boatName) },
            transaction);
    }

    private static async Task CreateBoatAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid boatId,
        string boatName)
    {
        await connection.ExecuteAsync(
            """
            INSERT INTO "Boats"
            (
                "Id", "Name", "RegistrationNumber", "CallSign",
                "Status", "Notes", "CreatedAt"
            )
            VALUES
            (
                @Id, @Name, NULL, NULL,
                'Active', @Notes, @CreatedAt
            );
            """,
            new
            {
                Id = boatId,
                Name = Clean(boatName),
                Notes = "Excel beosztás-importtal létrehozva. A lajstromszám és a hívójel nincs beállítva.",
                CreatedAt = DateTime.UtcNow
            },
            transaction);
    }

    private static string CreateImportKey(EventAssignmentImportRow row)
    {
        var source = string.Join(
            "|",
            NormalizeKey(row.GroupName),
            NormalizeKey(row.SubgroupName),
            NormalizeKey(row.Name),
            NormalizeKey(row.Position),
            NormalizeKey(row.Task),
            NormalizeKey(row.BoatName));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GetOfficialIdentityKey(EventAssignmentImportRow row)
    {
        if (row.Email.Length > 0)
        {
            return $"email:{NormalizeKey(row.Email)}";
        }

        var phoneDigits = PhoneDigits(row.Phone);
        if (phoneDigits.Length > 0)
        {
            return $"phone:{phoneDigits}";
        }

        return $"name:{NormalizeKey(row.Name)}";
    }

    private static string NormalizeHeader(string value)
    {
        return NormalizeKey(value)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static string NormalizeKey(string value)
    {
        var normalized = Clean(value).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return NormalizeSpaces(builder.ToString().Normalize(NormalizationForm.FormC));
    }

    private static string NormalizeSpaces(string value)
    {
        return Regex.Replace(Clean(value), @"\s+", " ");
    }

    private static string NormalizePhone(string value)
    {
        var clean = Clean(value).TrimStart('\'');
        if (clean.Length == 0)
        {
            return string.Empty;
        }

        var digits = PhoneDigits(clean);
        if (digits.StartsWith("06", StringComparison.Ordinal) && digits.Length >= 10)
        {
            return $"+36{digits[2..]}";
        }

        if (digits.StartsWith("36", StringComparison.Ordinal) && digits.Length >= 10)
        {
            return $"+{digits}";
        }

        return clean;
    }

    private static string PhoneDigits(string value)
    {
        return Regex.Replace(value ?? string.Empty, "[^0-9]", string.Empty);
    }

    private static string Clean(string? value)
    {
        return NormalizeSpacesCore((value ?? string.Empty).Trim());
    }

    private static string NormalizeSpacesCore(string value)
    {
        return Regex.Replace(value, @"\s+", " ");
    }

    private static string? NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string FirstNotEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private static string SafeFileName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(safeName) ? "ismeretlen.xlsx" : safeName;
    }

    private sealed class OfficialMatch
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}