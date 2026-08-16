import {
    ExternalLink,
    Pencil,
    Plus,
    Trash2,
} from "lucide-react";
import {
    useEffect,
    useMemo,
    useState,
} from "react";
import { Link } from "react-router-dom";

import {
    createEvent,
    deleteEvent,
    getEvents,
    updateEvent,
} from "../api/events";
import {
    Badge,
    Button,
    Card,
    Modal,
    PageHeader,
} from "../components/ui";
import type { EventItem } from "../types";

import "./CrudPage.css";
import "./EventsPage.css";

const emptyForm = {
    name: "",
    status: "Draft",
};

export default function EventsPage() {
    const [items, setItems] =
        useState<EventItem[]>([]);
    const [search, setSearch] =
        useState("");
    const [form, setForm] =
        useState(emptyForm);
    const [editing, setEditing] =
        useState<EventItem | null>(null);
    const [open, setOpen] =
        useState(false);

    const load = () =>
        getEvents().then(setItems);

    useEffect(() => {
        void load();
    }, []);

    const filtered = useMemo(
        () =>
            items.filter((item) =>
                `${item.name} ${item.status}`
                    .toLowerCase()
                    .includes(
                        search.toLowerCase(),
                    ),
            ),
        [items, search],
    );

    async function save() {
        if (!form.name.trim()) {
            return;
        }

        if (editing) {
            await updateEvent(
                editing.id,
                form,
            );
        } else {
            await createEvent(form);
        }

        setOpen(false);
        setEditing(null);
        setForm(emptyForm);

        await load();
    }

    return (
        <div className="eh-crud">
            <PageHeader
                eyebrow="RENDEZVÉNYEK"
                title="Versenyprogram"
                description="Vitorlás események létrehozása, kezelése és részletes adatlapja."
                actions={
                    <Button
                        icon={<Plus size={17} />}
                        onClick={() => {
                            setEditing(null);
                            setForm(emptyForm);
                            setOpen(true);
                        }}
                    >
                        Új rendezvény
                    </Button>
                }
            />

            <div className="eh-crud__toolbar">
                <input
                    className="eh-crud__search"
                    placeholder="Keresés..."
                    value={search}
                    onChange={(event) =>
                        setSearch(
                            event.target.value,
                        )
                    }
                />
            </div>

            <Card>
                <table className="eh-crud__table">
                    <thead>
                        <tr>
                            <th>Név</th>
                            <th>Státusz</th>
                            <th>Műveletek</th>
                        </tr>
                    </thead>

                    <tbody>
                        {filtered.map((item) => (
                            <tr key={item.id}>
                                <td>
                                    <strong>
                                        {item.name}
                                    </strong>
                                </td>

                                <td>
                                    <Badge
                                        variant={
                                            item.status ===
                                                "Active"
                                                ? "success"
                                                : item.status ===
                                                    "Draft"
                                                    ? "warning"
                                                    : "neutral"
                                        }
                                    >
                                        {item.status}
                                    </Badge>
                                </td>

                                <td>
                                    <div className="eh-crud__actions">
                                        <Link
                                            className="eh-events__open-link"
                                            to={`/events/${item.id}`}
                                        >
                                            <ExternalLink
                                                size={
                                                    16
                                                }
                                            />
                                            Adatlap
                                        </Link>

                                        <Button
                                            variant="ghost"
                                            icon={
                                                <Pencil
                                                    size={
                                                        16
                                                    }
                                                />
                                            }
                                            onClick={() => {
                                                setEditing(
                                                    item,
                                                );
                                                setForm({
                                                    name: item.name,
                                                    status:
                                                        item.status,
                                                });
                                                setOpen(
                                                    true,
                                                );
                                            }}
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
                                            onClick={async () => {
                                                if (
                                                    confirm(
                                                        "Biztosan törlöd?",
                                                    )
                                                ) {
                                                    await deleteEvent(
                                                        item.id,
                                                    );
                                                    await load();
                                                }
                                            }}
                                        >
                                            Törlés
                                        </Button>
                                    </div>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </Card>

            <Modal
                open={open}
                title={
                    editing
                        ? "Rendezvény szerkesztése"
                        : "Új rendezvény"
                }
                onClose={() => setOpen(false)}
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
                            onClick={() =>
                                void save()
                            }
                        >
                            Mentés
                        </Button>
                    </>
                }
            >
                <div className="eh-form">
                    <label>
                        Név
                        <input
                            value={form.name}
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    name: event.target
                                        .value,
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
                                        event.target
                                            .value,
                                })
                            }
                        >
                            <option value="Draft">
                                Draft
                            </option>
                            <option value="Active">
                                Active
                            </option>
                            <option value="Finished">
                                Finished
                            </option>
                        </select>
                    </label>
                </div>
            </Modal>
        </div>
    );
}