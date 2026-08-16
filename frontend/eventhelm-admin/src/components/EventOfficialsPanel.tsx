import {
    Pencil,
    Plus,
    Radio,
    Sailboat,
    Trash2,
    UserCheck,
    Users,
} from "lucide-react";
import {
    useCallback,
    useEffect,
    useMemo,
    useState,
} from "react";
import axios from "axios";

import { getBoats } from "../api/boats";
import {
    createEventOfficialAssignment,
    deleteEventOfficialAssignment,
    getEventOfficialAssignments,
    updateEventOfficialAssignment,
} from "../api/eventOfficialAssignments";
import { getOfficials } from "../api/officials";
import {
    Badge,
    Button,
    Card,
    Modal,
} from "./ui";
import type {
    AttendanceStatus,
    EventOfficialAssignment,
    EventOfficialAssignmentPayload,
} from "../types/eventOfficialAssignments";

import "./EventOfficialsPanel.css";

type OfficialOption = {
    id: string;
    name: string;
};

type BoatOption = {
    id: string;
    name: string;
    callSign?: string | null;
};

type FormState = {
    officialId: string;
    role: string;
    trunkRadioNumber: string;
    vhfRadioNumber: string;
    boatId: string;
    attendanceStatus: AttendanceStatus;
    notes: string;
};

const emptyForm: FormState = {
    officialId: "",
    role: "",
    trunkRadioNumber: "",
    vhfRadioNumber: "",
    boatId: "",
    attendanceStatus: "Planned",
    notes: "",
};

export default function EventOfficialsPanel({
    eventId,
}: {
    eventId: string;
}) {
    const [assignments, setAssignments] =
        useState<EventOfficialAssignment[]>([]);

    const [officials, setOfficials] =
        useState<OfficialOption[]>([]);

    const [boats, setBoats] =
        useState<BoatOption[]>([]);

    const [form, setForm] =
        useState<FormState>(emptyForm);

    const [editing, setEditing] =
        useState<EventOfficialAssignment | null>(null);

    const [open, setOpen] =
        useState(false);

    const [loading, setLoading] =
        useState(true);

    const [saving, setSaving] =
        useState(false);

    const [error, setError] =
        useState<string | null>(null);

    const load = useCallback(async () => {
        try {
            setLoading(true);

            const [
                assignmentItems,
                officialItems,
                boatItems,
            ] = await Promise.all([
                getEventOfficialAssignments(
                    eventId,
                ),
                getOfficials(),
                getBoats(),
            ]);

            setAssignments(assignmentItems);

            setOfficials(
                officialItems.map((item) => ({
                    id: item.id,
                    name: item.name,
                })),
            );

            setBoats(
                boatItems.map((item) => ({
                    id: item.id,
                    name: item.name,
                    callSign: item.callSign,
                })),
            );

            setError(null);
        } catch (loadError) {
            setError(getErrorMessage(loadError));
        } finally {
            setLoading(false);
        }
    }, [eventId]);

    useEffect(() => {
        void load();
    }, [load]);

    const assignedOfficialIds = useMemo(
        () =>
            new Set(
                assignments.map(
                    (item) => item.officialId,
                ),
            ),
        [assignments],
    );

    const statistics = useMemo(
        () => ({
            total: assignments.length,
            present: assignments.filter(
                (item) =>
                    item.attendanceStatus ===
                    "Present",
            ).length,
            radios: assignments.filter(
                (item) =>
                    item.trunkRadioNumber ||
                    item.vhfRadioNumber,
            ).length,
            boats: assignments.filter(
                (item) => item.boatId,
            ).length,
        }),
        [assignments],
    );

    function openCreate() {
        setEditing(null);
        setForm(emptyForm);
        setError(null);
        setOpen(true);
    }

    function openEdit(
        item: EventOfficialAssignment,
    ) {
        setEditing(item);

        setForm({
            officialId: item.officialId,
            role: item.role,
            trunkRadioNumber:
                item.trunkRadioNumber ?? "",
            vhfRadioNumber:
                item.vhfRadioNumber ?? "",
            boatId: item.boatId ?? "",
            attendanceStatus:
                item.attendanceStatus,
            notes: item.notes ?? "",
        });

        setError(null);
        setOpen(true);
    }

    async function save() {
        if (!form.officialId) {
            setError(
                "Válassz rendezőt.",
            );
            return;
        }

        if (!form.role.trim()) {
            setError(
                "Add meg a versenybeli szerepkört.",
            );
            return;
        }

        if (
            form.trunkRadioNumber &&
            !/^\d{3}$/.test(
                form.trunkRadioNumber,
            )
        ) {
            setError(
                "A trönk rádió azonosítója pontosan 3 számjegy.",
            );
            return;
        }

        if (
            form.vhfRadioNumber &&
            !/^\d{4}$/.test(
                form.vhfRadioNumber,
            )
        ) {
            setError(
                "A VHF rádió azonosítója pontosan 4 számjegy.",
            );
            return;
        }

        const payload:
            EventOfficialAssignmentPayload = {
                officialId: form.officialId,
                role: form.role.trim(),
                trunkRadioNumber:
                    form.trunkRadioNumber ||
                    null,
                vhfRadioNumber:
                    form.vhfRadioNumber ||
                    null,
                boatId: form.boatId || null,
                attendanceStatus:
                    form.attendanceStatus,
                notes:
                    form.notes.trim() ||
                    null,
            };

        try {
            setSaving(true);

            if (editing) {
                await updateEventOfficialAssignment(
                    eventId,
                    editing.id,
                    payload,
                );
            } else {
                await createEventOfficialAssignment(
                    eventId,
                    payload,
                );
            }

            setOpen(false);
            await load();
        } catch (saveError) {
            setError(getErrorMessage(saveError));
        } finally {
            setSaving(false);
        }
    }

    async function remove(
        item: EventOfficialAssignment,
    ) {
        const confirmed = window.confirm(
            `Biztosan eltávolítod a versenyből?\n\n${item.officialName}`,
        );

        if (!confirmed) {
            return;
        }

        try {
            await deleteEventOfficialAssignment(
                eventId,
                item.id,
            );

            await load();
        } catch (deleteError) {
            setError(
                getErrorMessage(deleteError),
            );
        }
    }

    return (
        <div className="eh-event-officials">
            {error && (
                <div className="eh-event-officials__error">
                    {error}
                </div>
            )}

            <section className="eh-event-officials__metrics">
                <Metric
                    icon={Users}
                    label="Rendezők"
                    value={statistics.total}
                />

                <Metric
                    icon={UserCheck}
                    label="Jelen"
                    value={statistics.present}
                />

                <Metric
                    icon={Radio}
                    label="Rádióval"
                    value={statistics.radios}
                />

                <Metric
                    icon={Sailboat}
                    label="Hajóval"
                    value={statistics.boats}
                />
            </section>

            <Card
                eyebrow="VERSENYCSAPAT"
                title="Rendezői hozzárendelések"
                action={
                    <Button
                        icon={<Plus size={17} />}
                        onClick={openCreate}
                    >
                        Rendező hozzáadása
                    </Button>
                }
            >
                <div className="eh-event-officials__table-wrap">
                    <table className="eh-event-officials__table">
                        <thead>
                            <tr>
                                <th>Rendező</th>
                                <th>Szerepkör</th>
                                <th>Trönk</th>
                                <th>VHF</th>
                                <th>Motorcsónak</th>
                                <th>Jelenlét</th>
                                <th />
                            </tr>
                        </thead>

                        <tbody>
                            {loading ? (
                                <tr>
                                    <td
                                        colSpan={7}
                                        className="eh-event-officials__empty"
                                    >
                                        Betöltés...
                                    </td>
                                </tr>
                            ) : assignments.length ===
                              0 ? (
                                <tr>
                                    <td
                                        colSpan={7}
                                        className="eh-event-officials__empty"
                                    >
                                        Ehhez a
                                        versenyhez még
                                        nincs rendező
                                        hozzárendelve.
                                    </td>
                                </tr>
                            ) : (
                                assignments.map(
                                    (item) => (
                                        <tr
                                            key={
                                                item.id
                                            }
                                        >
                                            <td>
                                                <strong>
                                                    {
                                                        item.officialName
                                                    }
                                                </strong>

                                                <small>
                                                    {item.officialPhone ??
                                                        item.officialEmail ??
                                                        "Nincs elérhetőség"}
                                                </small>
                                            </td>

                                            <td>
                                                {
                                                    item.role
                                                }
                                            </td>

                                            <td>
                                                <CodeValue
                                                    value={
                                                        item.trunkRadioNumber
                                                    }
                                                />
                                            </td>

                                            <td>
                                                <CodeValue
                                                    value={
                                                        item.vhfRadioNumber
                                                    }
                                                />
                                            </td>

                                            <td>
                                                {item.boatName ? (
                                                    <span className="eh-event-officials__boat">
                                                        <Sailboat
                                                            size={
                                                                15
                                                            }
                                                        />
                                                        {
                                                            item.boatName
                                                        }
                                                        {item.boatCallSign
                                                            ? ` · ${item.boatCallSign}`
                                                            : ""}
                                                    </span>
                                                ) : (
                                                    "—"
                                                )}
                                            </td>

                                            <td>
                                                <Badge
                                                    variant={attendanceVariant(
                                                        item.attendanceStatus,
                                                    )}
                                                >
                                                    {attendanceLabel(
                                                        item.attendanceStatus,
                                                    )}
                                                </Badge>
                                            </td>

                                            <td>
                                                <div className="eh-event-officials__actions">
                                                    <Button
                                                        variant="ghost"
                                                        icon={
                                                            <Pencil
                                                                size={
                                                                    16
                                                                }
                                                            />
                                                        }
                                                        onClick={() =>
                                                            openEdit(
                                                                item,
                                                            )
                                                        }
                                                    >
                                                        Szerkesztés
                                                    </Button>

                                                    <Button
                                                        variant="danger"
                                                        icon={
                                                            <Trash2
                                                                size={
                                                                    16
                                                                }
                                                            />
                                                        }
                                                        onClick={() =>
                                                            void remove(
                                                                item,
                                                            )
                                                        }
                                                    >
                                                        Törlés
                                                    </Button>
                                                </div>
                                            </td>
                                        </tr>
                                    ),
                                )
                            )}
                        </tbody>
                    </table>
                </div>
            </Card>

            <Modal
                open={open}
                title={
                    editing
                        ? "Rendezői hozzárendelés szerkesztése"
                        : "Rendező hozzáadása a versenyhez"
                }
                onClose={() =>
                    setOpen(false)
                }
                footer={
                    <>
                        <Button
                            variant="secondary"
                            onClick={() =>
                                setOpen(false)
                            }
                        >
                            Mégse
                        </Button>

                        <Button
                            loading={saving}
                            onClick={() =>
                                void save()
                            }
                        >
                            Mentés
                        </Button>
                    </>
                }
            >
                <div className="eh-event-officials__form">
                    <label>
                        Rendező
                        <select
                            value={
                                form.officialId
                            }
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    officialId:
                                        event.target
                                            .value,
                                })
                            }
                        >
                            <option value="">
                                Válassz rendezőt
                            </option>

                            {officials.map(
                                (official) => {
                                    const used =
                                        assignedOfficialIds.has(
                                            official.id,
                                        ) &&
                                        official.id !==
                                            editing?.officialId;

                                    return (
                                        <option
                                            key={
                                                official.id
                                            }
                                            value={
                                                official.id
                                            }
                                            disabled={
                                                used
                                            }
                                        >
                                            {
                                                official.name
                                            }
                                            {used
                                                ? " – már hozzárendelve"
                                                : ""}
                                        </option>
                                    );
                                },
                            )}
                        </select>
                    </label>

                    <label>
                        Versenybeli szerepkör
                        <input
                            value={form.role}
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    role:
                                        event.target
                                            .value,
                                })
                            }
                            placeholder="Például rajthajó, pályafelelős"
                        />
                    </label>

                    <label>
                        Trönk rádió
                        <input
                            value={
                                form.trunkRadioNumber
                            }
                            inputMode="numeric"
                            maxLength={3}
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    trunkRadioNumber:
                                        digitsOnly(
                                            event
                                                .target
                                                .value,
                                            3,
                                        ),
                                })
                            }
                            placeholder="301"
                        />
                    </label>

                    <label>
                        VHF rádió
                        <input
                            value={
                                form.vhfRadioNumber
                            }
                            inputMode="numeric"
                            maxLength={4}
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    vhfRadioNumber:
                                        digitsOnly(
                                            event
                                                .target
                                                .value,
                                            4,
                                        ),
                                })
                            }
                            placeholder="4001"
                        />
                    </label>

                    <label>
                        Motorcsónak
                        <select
                            value={form.boatId}
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    boatId:
                                        event.target
                                            .value,
                                })
                            }
                        >
                            <option value="">
                                Nincs hozzárendelve
                            </option>

                            {boats.map((boat) => (
                                <option
                                    key={boat.id}
                                    value={boat.id}
                                >
                                    {boat.name}
                                    {boat.callSign
                                        ? ` – ${boat.callSign}`
                                        : ""}
                                </option>
                            ))}
                        </select>
                    </label>

                    <label>
                        Jelenléti állapot
                        <select
                            value={
                                form.attendanceStatus
                            }
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    attendanceStatus:
                                        event.target
                                            .value as AttendanceStatus,
                                })
                            }
                        >
                            <option value="Planned">
                                Tervezett
                            </option>
                            <option value="Present">
                                Jelen
                            </option>
                            <option value="Standby">
                                Készenlét
                            </option>
                            <option value="Absent">
                                Nincs jelen
                            </option>
                        </select>
                    </label>

                    <label className="eh-event-officials__form-full">
                        Megjegyzés
                        <textarea
                            rows={4}
                            value={form.notes}
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    notes:
                                        event.target
                                            .value,
                                })
                            }
                        />
                    </label>
                </div>
            </Modal>
        </div>
    );
}

function Metric({
    icon: Icon,
    label,
    value,
}: {
    icon: typeof Users;
    label: string;
    value: number;
}) {
    return (
        <article className="eh-event-officials__metric">
            <div>
                <Icon size={20} />
            </div>

            <span>{label}</span>
            <strong>{value}</strong>
        </article>
    );
}

function CodeValue({
    value,
}: {
    value?: string | null;
}) {
    return value ? (
        <code className="eh-event-officials__code">
            {value}
        </code>
    ) : (
        <span className="eh-event-officials__none">
            —
        </span>
    );
}

function digitsOnly(
    value: string,
    maxLength: number,
): string {
    return value
        .replace(/\D/g, "")
        .slice(0, maxLength);
}

function attendanceLabel(
    value: AttendanceStatus,
): string {
    if (value === "Present") {
        return "Jelen";
    }

    if (value === "Absent") {
        return "Nincs jelen";
    }

    if (value === "Standby") {
        return "Készenlét";
    }

    return "Tervezett";
}

function attendanceVariant(
    value: AttendanceStatus,
): "success" | "warning" | "neutral" {
    if (value === "Present") {
        return "success";
    }

    if (value === "Standby") {
        return "warning";
    }

    return "neutral";
}

function getErrorMessage(
    error: unknown,
): string {
    if (axios.isAxiosError(error)) {
        const data = error.response?.data as
            | { message?: string }
            | undefined;

        return (
            data?.message ??
            error.message ??
            "API-hiba történt."
        );
    }

    return error instanceof Error
        ? error.message
        : "Ismeretlen hiba történt.";
}
