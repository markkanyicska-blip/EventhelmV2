import {
    Anchor,
    ExternalLink,
    Pencil,
    Plus,
    Trash2,
    Users,
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";

import {
    createBoat,
    deleteBoat,
    getBoats,
    updateBoat,
} from "../api/boats";
import {
    Badge,
    Button,
    Card,
    Modal,
    PageHeader,
} from "../components/ui";
import type { Boat } from "../types";

import "./CrudPage.css";
import "./BoatsPage.css";

type BoatForm = {
    callSign: string;
    name: string;
    type: string;
    status: string;
    capacity: number;
    notes: string;
};

const emptyForm: BoatForm = {
    callSign: "",
    name: "",
    type: "Motorboat",
    status: "Active",
    capacity: 1,
    notes: "",
};

function getStatusVariant(
    status: string,
): "success" | "warning" | "neutral" {
    if (status === "Active") {
        return "success";
    }

    if (status === "Maintenance") {
        return "warning";
    }

    return "neutral";
}

function getStatusLabel(status: string) {
    switch (status) {
        case "Active":
            return "Aktív";

        case "Inactive":
            return "Inaktív";

        case "Maintenance":
            return "Karbantartás";

        default:
            return status;
    }
}

export default function BoatsPage() {
    const navigate = useNavigate();

    const [items, setItems] = useState<Boat[]>([]);
    const [search, setSearch] = useState("");
    const [form, setForm] = useState<BoatForm>(emptyForm);
    const [editing, setEditing] = useState<Boat | null>(null);
    const [open, setOpen] = useState(false);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState("");

    async function load() {
        try {
            setLoading(true);
            setError("");

            const boats = await getBoats();
            setItems(boats);
        } catch (loadError) {
            console.error(loadError);

            setError(
                "A motorcsónakok nem tölthetők be. Ellenőrizd, hogy fut-e az EventHelm API.",
            );
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        void load();
    }, []);

    const filtered = useMemo(() => {
        const normalizedSearch = search.trim().toLowerCase();

        if (!normalizedSearch) {
            return items;
        }

        return items.filter((boat) =>
            [
                boat.callSign,
                boat.name,
                boat.type,
                boat.status,
                boat.notes ?? "",
            ]
                .join(" ")
                .toLowerCase()
                .includes(normalizedSearch),
        );
    }, [items, search]);

    function openCreateModal() {
        setEditing(null);
        setForm(emptyForm);
        setOpen(true);
    }

    function openEditModal(boat: Boat) {
        setEditing(boat);

        setForm({
            callSign: boat.callSign,
            name: boat.name,
            type: boat.type,
            status: boat.status,
            capacity: boat.capacity,
            notes: boat.notes ?? "",
        });

        setOpen(true);
    }

    function closeModal() {
        if (saving) {
            return;
        }

        setOpen(false);
        setEditing(null);
        setForm(emptyForm);
    }

    async function save() {
        if (!form.callSign.trim() || !form.name.trim()) {
            window.alert("A hívójel és a név megadása kötelező.");
            return;
        }

        if (form.capacity < 1) {
            window.alert("A kapacitás legalább 1 fő legyen.");
            return;
        }

        try {
            setSaving(true);

            const normalizedForm = {
                ...form,
                callSign: form.callSign.trim(),
                name: form.name.trim(),
                type: form.type.trim(),
                notes: form.notes.trim(),
            };

            if (editing) {
                await updateBoat({
                    ...editing,
                    ...normalizedForm,
                });
            } else {
                await createBoat(normalizedForm);
            }

            closeModal();
            await load();
        } catch (saveError) {
            console.error(saveError);

            window.alert(
                "A motorcsónak mentése nem sikerült. Ellenőrizd az API működését.",
            );
        } finally {
            setSaving(false);
        }
    }

    async function removeBoat(boat: Boat) {
        const confirmed = window.confirm(
            `Biztosan törlöd ezt a motorcsónakot?\n\n${boat.name} (${boat.callSign})`,
        );

        if (!confirmed) {
            return;
        }

        try {
            await deleteBoat(boat.id);
            await load();
        } catch (deleteError) {
            console.error(deleteError);

            window.alert(
                "A motorcsónak törlése nem sikerült. Ellenőrizd az API működését.",
            );
        }
    }

    return (
        <div className="eh-crud boats-page">
            <PageHeader
                eyebrow="FLOTTA"
                title="Motorcsónakok"
                description="Operációs egységek, kapacitás és műszaki állapot."
                actions={
                    <Button
                        icon={<Plus size={17} />}
                        onClick={openCreateModal}
                    >
                        Új motorcsónak
                    </Button>
                }
            />

            <div className="boats-page__summary">
                <Card className="boats-page__summary-card">
                    <span>Összes motorcsónak</span>
                    <strong>{items.length}</strong>
                </Card>

                <Card className="boats-page__summary-card">
                    <span>Aktív</span>
                    <strong>
                        {
                            items.filter(
                                (boat) => boat.status === "Active",
                            ).length
                        }
                    </strong>
                </Card>

                <Card className="boats-page__summary-card">
                    <span>Karbantartás alatt</span>
                    <strong>
                        {
                            items.filter(
                                (boat) =>
                                    boat.status === "Maintenance",
                            ).length
                        }
                    </strong>
                </Card>

                <Card className="boats-page__summary-card">
                    <span>Összes kapacitás</span>
                    <strong>
                        {items.reduce(
                            (total, boat) =>
                                total + boat.capacity,
                            0,
                        )}{" "}
                        fő
                    </strong>
                </Card>
            </div>

            <div className="eh-crud__toolbar boats-page__toolbar">
                <input
                    className="eh-crud__search"
                    placeholder="Keresés név, hívójel, típus vagy státusz alapján..."
                    value={search}
                    onChange={(event) =>
                        setSearch(event.target.value)
                    }
                />

                <span className="boats-page__result-count">
                    {filtered.length} találat
                </span>
            </div>

            {loading && (
                <Card>
                    <div className="boats-page__message">
                        Motorcsónakok betöltése...
                    </div>
                </Card>
            )}

            {!loading && error && (
                <Card>
                    <div className="boats-page__message boats-page__message--error">
                        <p>{error}</p>

                        <Button
                            variant="secondary"
                            onClick={() => void load()}
                        >
                            Újrapróbálás
                        </Button>
                    </div>
                </Card>
            )}

            {!loading && !error && filtered.length === 0 && (
                <Card>
                    <div className="boats-page__empty">
                        <div className="boats-page__empty-icon">
                            <Anchor size={30} />
                        </div>

                        <h3>
                            {search
                                ? "Nincs találat"
                                : "Még nincs motorcsónak"}
                        </h3>

                        <p>
                            {search
                                ? "Próbálj meg más keresési kifejezést."
                                : "Hozd létre az első motorcsónakot a flotta nyilvántartásához."}
                        </p>

                        {!search && (
                            <Button
                                icon={<Plus size={17} />}
                                onClick={openCreateModal}
                            >
                                Új motorcsónak
                            </Button>
                        )}
                    </div>
                </Card>
            )}

            {!loading && !error && filtered.length > 0 && (
                <div className="boats-page__grid">
                    {filtered.map((boat) => (
                        <Card
                            key={boat.id}
                            className="boats-page__card"
                        >
                            <div className="boats-page__card-header">
                                <div className="boats-page__boat-icon">
                                    <Anchor size={24} />
                                </div>

                                <Badge
                                    variant={getStatusVariant(
                                        boat.status,
                                    )}
                                >
                                    {getStatusLabel(boat.status)}
                                </Badge>
                            </div>

                            <div className="boats-page__card-title">
                                <span>{boat.callSign}</span>
                                <h2>{boat.name}</h2>
                                <p>{boat.type}</p>
                            </div>

                            <div className="boats-page__metadata">
                                <div className="boats-page__metadata-item">
                                    <Users size={17} />

                                    <div>
                                        <span>Kapacitás</span>
                                        <strong>
                                            {boat.capacity} fő
                                        </strong>
                                    </div>
                                </div>

                                <div className="boats-page__metadata-item">
                                    <Anchor size={17} />

                                    <div>
                                        <span>Hívójel</span>
                                        <strong>
                                            {boat.callSign}
                                        </strong>
                                    </div>
                                </div>
                            </div>

                            <div className="boats-page__notes">
                                <span>Megjegyzés</span>

                                <p>
                                    {boat.notes?.trim() ||
                                        "Nincs megjegyzés rögzítve."}
                                </p>
                            </div>

                            <div className="boats-page__actions">
                                <Button
                                    icon={
                                        <ExternalLink size={16} />
                                    }
                                    onClick={() =>
                                        navigate(
                                            `/boats/${boat.id}`,
                                        )
                                    }
                                >
                                    Megnyitás
                                </Button>

                                <Button
                                    variant="secondary"
                                    icon={<Pencil size={16} />}
                                    onClick={() =>
                                        openEditModal(boat)
                                    }
                                >
                                    Szerkesztés
                                </Button>

                                <Button
                                    variant="danger"
                                    icon={<Trash2 size={16} />}
                                    onClick={() =>
                                        void removeBoat(boat)
                                    }
                                >
                                    Törlés
                                </Button>
                            </div>
                        </Card>
                    ))}
                </div>
            )}

            <Modal
                open={open}
                title={
                    editing
                        ? "Motorcsónak szerkesztése"
                        : "Új motorcsónak"
                }
                onClose={closeModal}
                footer={
                    <>
                        <Button
                            variant="secondary"
                            onClick={closeModal}
                            disabled={saving}
                        >
                            Mégse
                        </Button>

                        <Button
                            onClick={() => void save()}
                            disabled={saving}
                        >
                            {saving ? "Mentés..." : "Mentés"}
                        </Button>
                    </>
                }
            >
                <div className="eh-form">
                    <div className="eh-form__grid">
                        <label>
                            Hívójel
                            <input
                                value={form.callSign}
                                onChange={(event) =>
                                    setForm({
                                        ...form,
                                        callSign:
                                            event.target.value,
                                    })
                                }
                                placeholder="Például: RIB-01"
                            />
                        </label>

                        <label>
                            Név
                            <input
                                value={form.name}
                                onChange={(event) =>
                                    setForm({
                                        ...form,
                                        name: event.target.value,
                                    })
                                }
                                placeholder="Például: Sirály"
                            />
                        </label>

                        <label>
                            Típus
                            <input
                                value={form.type}
                                onChange={(event) =>
                                    setForm({
                                        ...form,
                                        type: event.target.value,
                                    })
                                }
                                placeholder="Például: RIB 550"
                            />
                        </label>

                        <label>
                            Kapacitás
                            <input
                                type="number"
                                min={1}
                                value={form.capacity}
                                onChange={(event) =>
                                    setForm({
                                        ...form,
                                        capacity: Math.max(
                                            1,
                                            Number(
                                                event.target
                                                    .value,
                                            ),
                                        ),
                                    })
                                }
                            />
                        </label>

                        <label>
                            Státusz
                            <select
                                value={form.status}
                                onChange={(event) =>
                                    setForm({
                                        ...form,
                                        status:
                                            event.target.value,
                                    })
                                }
                            >
                                <option value="Active">
                                    Aktív
                                </option>

                                <option value="Inactive">
                                    Inaktív
                                </option>

                                <option value="Maintenance">
                                    Karbantartás
                                </option>
                            </select>
                        </label>
                    </div>

                    <label>
                        Megjegyzés
                        <textarea
                            value={form.notes}
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    notes: event.target.value,
                                })
                            }
                            placeholder="Operatív vagy műszaki megjegyzések..."
                        />
                    </label>
                </div>
            </Modal>
        </div>
    );
}