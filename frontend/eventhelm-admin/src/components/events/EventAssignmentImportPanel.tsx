import { useMemo, useState } from "react";
import {
    commitEventAssignmentImport,
    previewEventAssignmentImport,
    type AssignmentImportPreview,
    type AssignmentImportResult,
} from "../../api/eventAssignmentImport";

export function EventAssignmentImportPanel({
    eventId,
    onImported,
}: {
    eventId: string;
    onImported?: () => void | Promise<void>;
}) {
    const [file, setFile] = useState<File | null>(null);
    const [preview, setPreview] = useState<AssignmentImportPreview | null>(null);
    const [result, setResult] = useState<AssignmentImportResult | null>(null);
    const [replacePreviousImport, setReplacePreviousImport] = useState(false);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const hasInvalidRows = (preview?.invalidRows ?? 0) > 0;
    const groupNames = useMemo(
        () =>
            Array.from(
                new Set(
                    preview?.rows
                        .filter((row) => row.isValid && row.groupName)
                        .map((row) => row.groupName) ?? [],
                ),
            ),
        [preview],
    );

    async function createPreview() {
        if (!file) return;

        setLoading(true);
        setError(null);
        setResult(null);

        try {
            setPreview(await previewEventAssignmentImport(eventId, file));
        } catch (exception) {
            setError(readError(exception));
        } finally {
            setLoading(false);
        }
    }

    async function commit() {
        if (!preview) return;

        setLoading(true);
        setError(null);

        try {
            const imported = await commitEventAssignmentImport(
                eventId,
                preview,
                replacePreviousImport,
            );
            setResult(imported);
            await onImported?.();
        } catch (exception) {
            setError(readError(exception));
        } finally {
            setLoading(false);
        }
    }

    return (
        <section className="eh-assignment-import">
            <header className="eh-assignment-import__header">
                <div>
                    <span>BEOSZTÁS IMPORT</span>
                    <h2>Rendezők, feladatok és hajók importálása</h2>
                    <p>
                        Az EventHelm felismeri a csoportokat és alcsoportokat,
                        létrehozza vagy frissíti a rendezőket, majd külön
                        beosztássorként menti a pozíciót, feladatot és hajót.
                    </p>
                </div>
            </header>

            <div className="eh-assignment-import__controls">
                <input
                    type="file"
                    accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    onChange={(event) => {
                        setFile(event.target.files?.[0] ?? null);
                        setPreview(null);
                        setResult(null);
                        setError(null);
                    }}
                />

                <button
                    type="button"
                    onClick={() => void createPreview()}
                    disabled={!file || loading}
                >
                    {loading ? "Feldolgozás..." : "Előnézet készítése"}
                </button>
            </div>

            {error && <p className="eh-assignment-import__error">{error}</p>}

            {preview && (
                <>
                    <div className="eh-assignment-import__stats">
                        <Stat label="Összes sor" value={preview.totalRows} />
                        <Stat label="Importálható" value={preview.validRows} />
                        <Stat label="Hibás" value={preview.invalidRows} />
                        <Stat label="Rendezők" value={preview.uniqueOfficials} />
                        <Stat label="Új rendező" value={preview.newOfficials} />
                        <Stat label="Új hajó" value={preview.newBoats} />
                        <Stat label="Csoport" value={preview.groups} />
                        <Stat
                            label="Új beosztás"
                            value={preview.assignmentsToCreate}
                        />
                    </div>

                    {groupNames.length > 0 && (
                        <div className="eh-assignment-import__groups">
                            <strong>Felismert csoportok:</strong>
                            {groupNames.map((group) => (
                                <span key={group}>{group}</span>
                            ))}
                        </div>
                    )}

                    {preview.warnings.map((warning) => (
                        <p key={warning} className="eh-assignment-import__warning">
                            {warning}
                        </p>
                    ))}

                    <div className="eh-assignment-import__table-wrap">
                        <table className="eh-assignment-import__table">
                            <thead>
                                <tr>
                                    <th>Sor</th>
                                    <th>Csoport</th>
                                    <th>Alcsoport</th>
                                    <th>Név</th>
                                    <th>Pozíció</th>
                                    <th>Feladat</th>
                                    <th>Hajó</th>
                                    <th>Rendező</th>
                                    <th>Beosztás</th>
                                    <th>Állapot</th>
                                </tr>
                            </thead>
                            <tbody>
                                {preview.rows.map((row) => (
                                    <tr
                                        key={`${row.rowNumber}-${row.importKey}`}
                                        className={row.isValid ? undefined : "is-invalid"}
                                    >
                                        <td>{row.rowNumber}</td>
                                        <td>{row.groupName || "—"}</td>
                                        <td>{row.subgroupName || "—"}</td>
                                        <td>{row.name || "—"}</td>
                                        <td>{row.position || "—"}</td>
                                        <td>{row.task || "—"}</td>
                                        <td>{row.boatName || "—"}</td>
                                        <td>
                                            {row.officialExists ? "Meglévő" : "Új"}
                                        </td>
                                        <td>
                                            {row.assignmentExists ? "Frissül" : "Új"}
                                        </td>
                                        <td>
                                            {row.isValid
                                                ? "Importálható"
                                                : row.errors.join("; ")}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>

                    <label className="eh-assignment-import__replace">
                        <input
                            type="checkbox"
                            checked={replacePreviousImport}
                            onChange={(event) =>
                                setReplacePreviousImport(event.target.checked)
                            }
                        />
                        <span>
                            A korábbi Excel-importból származó beosztások
                            lecserélése ennél az eseménynél
                        </span>
                    </label>

                    {replacePreviousImport && (
                        <p className="eh-assignment-import__warning">
                            Ez csak az <strong>xlsx-import</strong> forrású sorokat
                            törli. A kézzel létrehozott beosztásokat nem érinti.
                        </p>
                    )}

                    <div className="eh-assignment-import__actions">
                        <button
                            type="button"
                            onClick={() => void commit()}
                            disabled={loading || preview.validRows === 0}
                        >
                            {loading ? "Importálás..." : "Import jóváhagyása"}
                        </button>
                        {hasInvalidRows && (
                            <span>
                                A hibás sorokat a rendszer automatikusan kihagyja.
                            </span>
                        )}
                    </div>
                </>
            )}

            {result && (
                <div className="eh-assignment-import__result">
                    <strong>Az import befejeződött.</strong>
                    <span>Feldolgozott sorok: {result.importedRows}</span>
                    <span>Új beosztások: {result.createdAssignments}</span>
                    <span>Frissített beosztások: {result.updatedAssignments}</span>
                    <span>Új rendezők: {result.createdOfficials}</span>
                    <span>Frissített rendezők: {result.updatedOfficials}</span>
                    <span>Új hajók: {result.createdBoats}</span>
                    <span>Csoportok: {result.groups}</span>
                    {result.removedAssignments > 0 && (
                        <span>Törölt régi importsorok: {result.removedAssignments}</span>
                    )}
                    {result.skippedRows > 0 && (
                        <span>Kihagyott sorok: {result.skippedRows}</span>
                    )}
                </div>
            )}
        </section>
    );
}

function Stat({ label, value }: { label: string; value: number }) {
    return (
        <article>
            <span>{label}</span>
            <strong>{value}</strong>
        </article>
    );
}

function readError(exception: unknown): string {
    if (typeof exception === "object" && exception !== null) {
        const candidate = exception as {
            response?: {
                data?: string | { title?: string; detail?: string };
            };
            message?: string;
        };

        if (typeof candidate.response?.data === "string") {
            return candidate.response.data;
        }

        if (candidate.response?.data && typeof candidate.response.data === "object") {
            return (
                candidate.response.data.detail ??
                candidate.response.data.title ??
                "Az import sikertelen."
            );
        }

        if (candidate.message) return candidate.message;
    }

    return "Az import sikertelen.";
}
