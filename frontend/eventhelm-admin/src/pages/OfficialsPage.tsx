import {
    ExternalLink,
    Pencil,
    Plus,
    Trash2,
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";

import {
    createOfficialProfile,
    deleteOfficialProfile,
    getOfficialProfiles,
    updateOfficialProfile,
} from "../api/officialProfiles";
import {
    Badge,
    Button,
    Card,
    Modal,
    PageHeader,
} from "../components/ui";
import OfficialDocumentUpload from "../components/OfficialDocumentUpload";

import {
    BOAT_LICENSE_TYPES,
    ORGANIZER_QUALIFICATIONS,
    JUDGE_QUALIFICATIONS,
    MEASURER_QUALIFICATIONS,
} from "../constants/officialQualifications";

import type {
    OfficialPayload,
    OfficialProfile,
} from "../types/officialProfiles";

import "./CrudPage.css";
import "./OfficialsPage.css";

const emptyForm: OfficialPayload = {
    name: "",
    phone: "",
    email: "",
    role: "",
    status: "Available",
    club: "",
    shirtSize: "",
    profileImageUrl: "",

    organizerQualification: "",
    judgeQualification: "",
    measurerQualification: "",

    isCompetitionAdministrator: false,
    isResultScorer: false,

    qualificationValidUntil: "",
    qualificationDocumentUrl: "",

    boatLicenseType: "",
    boatLicenseValidUntil: "",
    boatLicenseDocumentUrl: "",

    foodAllergies: "",
    drugAllergies: "",
    healthNotes: "",

    emergencyContactName: "",
    emergencyContactPhone: "",
    emergencyContactRelation: "",

    languages: "",
    otherDocumentUrl: "",
};

export default function OfficialsPage() {
    const [items, setItems] = useState<OfficialProfile[]>([]);
    const [search, setSearch] = useState("");
    const [form, setForm] = useState<OfficialPayload>(emptyForm);
    const [editing, setEditing] = useState<OfficialProfile | null>(null);
    const [open, setOpen] = useState(false);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    async function load() {
        try {
            setLoading(true);
            setItems(await getOfficialProfiles());
            setError(null);
        } catch (loadError) {
            console.error(loadError);
            setError("A rendezők nem tölthetők be.");
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        void load();
    }, []);

    const filtered = useMemo(() => {
        const query = search.trim().toLowerCase();

        if (!query) {
            return items;
        }

        return items.filter((item) =>
            [
                item.name,
                item.phone,
                item.email,
                item.club,
                item.organizerQualification,
                item.judgeQualification,
                item.measurerQualification,
                item.boatLicenseType,
            ]
                .filter(Boolean)
                .join(" ")
                .toLowerCase()
                .includes(query),
        );
    }, [items, search]);

    function openCreate() {
        setEditing(null);
        setForm(emptyForm);
        setError(null);
        setOpen(true);
    }

    function openEdit(item: OfficialProfile) {
        setEditing(item);
        setForm(toPayload(item));
        setError(null);
        setOpen(true);
    }

    async function save() {
        if (!form.name.trim()) {
            setError("A rendező neve kötelező.");
            return;
        }

        try {
            setLoading(true);

            const payload = normalizePayload(form);

            if (editing) {
                await updateOfficialProfile(editing.id, payload);
            } else {
                await createOfficialProfile(payload);
            }

            setOpen(false);
            await load();
        } catch (saveError) {
            console.error(saveError);
            setError("A rendező mentése nem sikerült.");
        } finally {
            setLoading(false);
        }
    }

    async function remove(item: OfficialProfile) {
        if (!window.confirm(`Biztosan törlöd?\n\n${item.name}`)) {
            return;
        }

        try {
            await deleteOfficialProfile(item.id);
            await load();
        } catch (deleteError) {
            console.error(deleteError);
            setError(
                "A rendező törlése nem sikerült. Lehet, hogy versenyhez van rendelve.",
            );
        }
    }

    function update<K extends keyof OfficialPayload>(
        key: K,
        value: OfficialPayload[K],
    ) {
        setForm((current) => ({
            ...current,
            [key]: value,
        }));
    }

    return (
        <div className="eh-crud">
            <PageHeader
                eyebrow="RENDEZŐK"
                title="Rendezői nyilvántartás"
                description="Személyes adatok, minősítések, hajóvezetői képesítések és versenytörténet."
                actions={
                    <Button
                        icon={<Plus size={17} />}
                        onClick={openCreate}
                    >
                        Új rendező
                    </Button>
                }
            />

            {error && (
                <div className="eh-officials__error">
                    {error}
                </div>
            )}

            <div className="eh-crud__toolbar">
                <input
                    className="eh-crud__search"
                    placeholder="Keresés név, klub vagy minősítés alapján..."
                    value={search}
                    onChange={(event) => setSearch(event.target.value)}
                />
            </div>

            <Card>
                <div className="eh-officials__table-wrap">
                    <table className="eh-crud__table">
                        <thead>
                            <tr>
                                <th>Név</th>
                                <th>Klub</th>
                                <th>Minősítés</th>
                                <th>Hajóvezetés</th>
                                <th>Elérhetőség</th>
                                <th>Műveletek</th>
                            </tr>
                        </thead>

                        <tbody>
                            {filtered.length === 0 ? (
                                <tr>
                                    <td
                                        colSpan={6}
                                        className="eh-officials__empty"
                                    >
                                        {loading
                                            ? "Betöltés..."
                                            : "Nincs megjeleníthető rendező."}
                                    </td>
                                </tr>
                            ) : (
                                filtered.map((item) => (
                                    <tr key={item.id}>
                                        <td>
                                            <strong>{item.name}</strong>
                                        </td>

                                        <td>
                                            {item.club || "—"}
                                        </td>

                                        <td>
                                            <>
                                                {item.organizerQualification && (
                                                    <div>Rendező: {item.organizerQualification}</div>
                                                )}

                                                {item.judgeQualification && (
                                                    <div>Bíró: {item.judgeQualification}</div>
                                                )}

                                                {item.measurerQualification && (
                                                    <div>Felmérő: {item.measurerQualification}</div>
                                                )}

                                                {!item.organizerQualification &&
                                                    !item.judgeQualification &&
                                                    !item.measurerQualification &&
                                                    "—"}
                                            </>
                                        </td>

                                        <td>
                                            <Badge
                                                variant={
                                                    item.canDriveMotorboat
                                                        ? "success"
                                                        : "neutral"
                                                }
                                            >
                                                {item.canDriveMotorboat
                                                    ? "Vezethet"
                                                    : "Nem vezethet"}
                                            </Badge>
                                        </td>

                                        <td>
                                            <span className="eh-officials__contact">
                                                {item.phone ||
                                                    item.email ||
                                                    "—"}
                                            </span>
                                        </td>

                                        <td>
                                            <div className="eh-crud__actions">
                                                <Link
                                                    to={`/officials/${item.id}`}
                                                    className="eh-officials__profile-link"
                                                >
                                                    <ExternalLink size={16} />
                                                    Adatlap
                                                </Link>

                                                <Button
                                                    variant="ghost"
                                                    icon={<Pencil size={16} />}
                                                    onClick={() =>
                                                        openEdit(item)
                                                    }
                                                >
                                                    Szerkesztés
                                                </Button>

                                                <Button
                                                    variant="danger"
                                                    icon={<Trash2 size={16} />}
                                                    onClick={() =>
                                                        void remove(item)
                                                    }
                                                >
                                                    Törlés
                                                </Button>
                                            </div>
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>
            </Card>

           <Modal
    open={open}
    title={
        editing
            ? "Rendező szerkesztése"
            : "Új rendező"
    }
    onClose={() => setOpen(false)}
    footer={
        <>
            <Button
                variant="secondary"
                onClick={() => setOpen(false)}
            >
                Mégse
            </Button>

            <Button
                loading={loading}
                onClick={() => void save()}
            >
                Mentés
            </Button>
        </>
    }
>
    <div className="eh-officials__quick-form">
        <label>
            Név
            <input
                value={form.name}
                onChange={(event) =>
                    update("name", event.target.value)
                }
            />
        </label>

        <label>
            Telefon
            <input
                value={form.phone ?? ""}
                onChange={(event) =>
                    update("phone", event.target.value)
                }
            />
        </label>

        <label>
            E-mail
            <input
                type="email"
                value={form.email ?? ""}
                onChange={(event) =>
                    update("email", event.target.value)
                }
            />
        </label>

        <label>
            Klub
            <input
                value={form.club ?? ""}
                onChange={(event) =>
                    update("club", event.target.value)
                }
            />
        </label>

        <label>
            Pólóméret
            <select
                value={form.shirtSize ?? ""}
                onChange={(event) =>
                    update("shirtSize", event.target.value)
                }
            >
                <option value="">Nincs megadva</option>
                <option value="XS">XS</option>
                <option value="S">S</option>
                <option value="M">M</option>
                <option value="L">L</option>
                <option value="XL">XL</option>
                <option value="XXL">XXL</option>
            </select>
        </label>
                    <label>
                        Rendezői minősítés
                        <select
                            value={form.organizerQualification ?? ""}
                            onChange={(event) =>
                                update(
                                    "organizerQualification",
                                    event.target.value,
                                )
                            }
                        >
                            {ORGANIZER_QUALIFICATIONS.map((item) => (
                                <option
                                    key={item.value}
                                    value={item.value}
                                >
                                    {item.label}
                                </option>
                            ))}
                        </select>
                    </label>
                    <label>
                        Versenybírói minősítés
                        <select
                            value={form.judgeQualification ?? ""}
                            onChange={(event) =>
                                update(
                                    "judgeQualification",
                                    event.target.value,
                                )
                            }
                        >
                            {JUDGE_QUALIFICATIONS.map((item) => (
                                <option
                                    key={item.value}
                                    value={item.value}
                                >
                                    {item.label}
                                </option>
                            ))}
                        </select>
                    </label>

                    <label>
                        Felmérői minősítés
                        <select
                            value={form.measurerQualification ?? ""}
                            onChange={(event) =>
                                update(
                                    "measurerQualification",
                                    event.target.value,
                                )
                            }
                        >
                            {MEASURER_QUALIFICATIONS.map((item) => (
                                <option
                                    key={item.value}
                                    value={item.value}
                                >
                                    {item.label}
                                </option>
                            ))}
                        </select>
                    </label>

                    <label className="eh-checkbox">
                        <input
                            type="checkbox"
                            checked={form.isCompetitionAdministrator}
                            onChange={(event) =>
                                update(
                                    "isCompetitionAdministrator",
                                    event.target.checked,
                                )
                            }
                        />
                        Versenyadminisztrátor
                    </label>

                    <label className="eh-checkbox">
                        <input
                            type="checkbox"
                            checked={form.isResultScorer}
                            onChange={(event) =>
                                update(
                                    "isResultScorer",
                                    event.target.checked,
                                )
                            }
                        />
                        Eredményszámító
                    </label>
        <OfficialDocumentUpload
            label="Rendezői képesítés dokumentuma"
            value={form.qualificationDocumentUrl}
                        disabled={
                            !form.organizerQualification &&
                            !form.judgeQualification &&
                            !form.measurerQualification
                        }
            onChange={(url) =>
                update(
                    "qualificationDocumentUrl",
                    url ?? "",
                )
            }
        />

        <label>
            Hajóvezetői képesítés
            <select
                value={form.boatLicenseType ?? ""}
                onChange={(event) =>
                    update(
                        "boatLicenseType",
                        event.target.value,
                    )
                }
            >
                {BOAT_LICENSE_TYPES.map((license) => (
                    <option
                        key={license.value}
                        value={license.value}
                    >
                        {license.label}
                    </option>
                ))}
            </select>
        </label>

        <label>
            Hajós képesítés érvényessége
            <input
                type="date"
                value={
                    form.boatLicenseValidUntil ?? ""
                }
                onChange={(event) =>
                    update(
                        "boatLicenseValidUntil",
                        event.target.value,
                    )
                }
                disabled={!form.boatLicenseType}
            />
        </label>

        <OfficialDocumentUpload
            label="Hajós képesítés dokumentuma"
            value={form.boatLicenseDocumentUrl}
            disabled={!form.boatLicenseType}
            onChange={(url) =>
                update(
                    "boatLicenseDocumentUrl",
                    url ?? "",
                )
            }
        />

        <OfficialDocumentUpload
            label="Egyéb dokumentum"
            value={form.otherDocumentUrl}
            onChange={(url) =>
                update(
                    "otherDocumentUrl",
                    url ?? "",
                )
            }
        />
    </div>
</Modal>
        </div>
    );
}

function toPayload(
    item: OfficialProfile,
): OfficialPayload {
    const {
        id: _id,
        createdAt: _createdAt,
        canDriveMotorboat: _canDrive,
        ...payload
    } = item;

    return payload;
}

function normalizePayload(
    payload: OfficialPayload,
): OfficialPayload {
    const normalized = { ...payload };

    for (
        const key of Object.keys(
            normalized,
        ) as Array<keyof OfficialPayload>
    ) {
        const value = normalized[key];

        if (typeof value === "string") {
            (normalized as Record<string, unknown>)[key] =
                value.trim() || null;
        }
    }

    normalized.name = payload.name.trim();
    normalized.status =
        payload.status || "Available";

    if (!payload.boatLicenseType) {
        normalized.boatLicenseType = null;
        normalized.boatLicenseValidUntil = null;
        normalized.boatLicenseDocumentUrl = null;
    }

    return normalized;
}