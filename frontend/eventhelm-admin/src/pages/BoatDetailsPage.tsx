import {
    ArrowLeft,
    FileText,
    Gauge,
    MapPin,
    Settings,
    ShipWheel,
    Users,
    Wrench,
} from "lucide-react";
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";

import { getBoats } from "../api/boats";
import { Badge, Button, Card, PageHeader } from "../components/ui";
import type { Boat } from "../types";

import "./BoatDetailsPage.css";

type TabId =
    | "general"
    | "crew"
    | "equipment"
    | "maintenance"
    | "documents"
    | "map"
    | "history";

type TabItem = {
    id: TabId;
    label: string;
    icon: typeof ShipWheel;
};

const tabs: TabItem[] = [
    {
        id: "general",
        label: "Általános",
        icon: ShipWheel,
    },
    {
        id: "crew",
        label: "Személyzet",
        icon: Users,
    },
    {
        id: "equipment",
        label: "Felszerelés",
        icon: Settings,
    },
    {
        id: "maintenance",
        label: "Karbantartás",
        icon: Wrench,
    },
    {
        id: "documents",
        label: "Dokumentumok",
        icon: FileText,
    },
    {
        id: "map",
        label: "Térkép",
        icon: MapPin,
    },
    {
        id: "history",
        label: "Előzmények",
        icon: Gauge,
    },
];

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

export default function BoatDetailsPage() {
    const { id } = useParams<{ id: string }>();

    const [boat, setBoat] = useState<Boat | null>(null);
    const [activeTab, setActiveTab] = useState<TabId>("general");
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    useEffect(() => {
        async function loadBoat() {
            try {
                setLoading(true);
                setError("");

                const boats = await getBoats();
                const selectedBoat = boats.find((item) => item.id === id);

                if (!selectedBoat) {
                    setError("A motorcsónak nem található.");
                    setBoat(null);
                    return;
                }

                setBoat(selectedBoat);
            } catch (loadError) {
                console.error(loadError);
                setError(
                    "A motorcsónak adatai nem tölthetők be. Ellenőrizd, hogy fut-e az EventHelm API.",
                );
            } finally {
                setLoading(false);
            }
        }

        void loadBoat();
    }, [id]);

    if (loading) {
        return (
            <div className="boat-details">
                <Card>
                    <div className="boat-details__message">
                        Motorcsónak adatainak betöltése...
                    </div>
                </Card>
            </div>
        );
    }

    if (error || !boat) {
        return (
            <div className="boat-details">
                <PageHeader
                    eyebrow="FLOTTA"
                    title="Motorcsónak"
                    description="A motorcsónak adatlapja nem érhető el."
                    actions={
                        <Link to="/boats">
                            <Button
                                variant="secondary"
                                icon={<ArrowLeft size={17} />}
                            >
                                Vissza a flottához
                            </Button>
                        </Link>
                    }
                />

                <Card>
                    <div className="boat-details__message boat-details__message--error">
                        {error || "A motorcsónak nem található."}
                    </div>
                </Card>
            </div>
        );
    }

    return (
        <div className="boat-details">
            <PageHeader
                eyebrow="MOTORCSÓNAK ADATLAP"
                title={boat.name}
                description={`${boat.callSign} · ${boat.type}`}
                actions={
                    <Link to="/boats">
                        <Button
                            variant="secondary"
                            icon={<ArrowLeft size={17} />}
                        >
                            Vissza
                        </Button>
                    </Link>
                }
            />

            <Card className="boat-details__hero">
                <div className="boat-details__hero-main">
                    <div className="boat-details__hero-icon">
                        <ShipWheel size={34} />
                    </div>

                    <div>
                        <div className="boat-details__hero-heading">
                            <h2>{boat.name}</h2>

                            <Badge variant={getStatusVariant(boat.status)}>
                                {getStatusLabel(boat.status)}
                            </Badge>
                        </div>

                        <p>
                            {boat.callSign} · {boat.type}
                        </p>
                    </div>
                </div>

                <div className="boat-details__summary">
                    <div className="boat-details__summary-item">
                        <span>Hívójel</span>
                        <strong>{boat.callSign}</strong>
                    </div>

                    <div className="boat-details__summary-item">
                        <span>Típus</span>
                        <strong>{boat.type}</strong>
                    </div>

                    <div className="boat-details__summary-item">
                        <span>Kapacitás</span>
                        <strong>{boat.capacity} fő</strong>
                    </div>

                    <div className="boat-details__summary-item">
                        <span>Státusz</span>
                        <strong>{getStatusLabel(boat.status)}</strong>
                    </div>
                </div>
            </Card>

            <div className="boat-details__tabs" role="tablist">
                {tabs.map((tab) => {
                    const Icon = tab.icon;
                    const active = activeTab === tab.id;

                    return (
                        <button
                            key={tab.id}
                            type="button"
                            role="tab"
                            aria-selected={active}
                            className={[
                                "boat-details__tab",
                                active ? "boat-details__tab--active" : "",
                            ]
                                .filter(Boolean)
                                .join(" ")}
                            onClick={() => setActiveTab(tab.id)}
                        >
                            <Icon size={17} />
                            <span>{tab.label}</span>
                        </button>
                    );
                })}
            </div>

            {activeTab === "general" && (
                <div className="boat-details__content-grid">
                    <Card>
                        <div className="boat-details__section-heading">
                            <div>
                                <span>ALAPADATOK</span>
                                <h3>Motorcsónak adatai</h3>
                            </div>
                        </div>

                        <div className="boat-details__data-grid">
                            <div className="boat-details__data-item">
                                <span>Hívójel</span>
                                <strong>{boat.callSign}</strong>
                            </div>

                            <div className="boat-details__data-item">
                                <span>Név</span>
                                <strong>{boat.name}</strong>
                            </div>

                            <div className="boat-details__data-item">
                                <span>Típus</span>
                                <strong>{boat.type}</strong>
                            </div>

                            <div className="boat-details__data-item">
                                <span>Kapacitás</span>
                                <strong>{boat.capacity} fő</strong>
                            </div>

                            <div className="boat-details__data-item">
                                <span>Státusz</span>
                                <Badge
                                    variant={getStatusVariant(boat.status)}
                                >
                                    {getStatusLabel(boat.status)}
                                </Badge>
                            </div>

                            <div className="boat-details__data-item">
                                <span>Létrehozva</span>
                                <strong>
                                    {boat.createdAt
                                        ? new Date(
                                            boat.createdAt,
                                        ).toLocaleDateString("hu-HU")
                                        : "Nincs adat"}
                                </strong>
                            </div>
                        </div>
                    </Card>

                    <Card>
                        <div className="boat-details__section-heading">
                            <div>
                                <span>MEGJEGYZÉS</span>
                                <h3>Operatív információk</h3>
                            </div>
                        </div>

                        <div className="boat-details__notes">
                            {boat.notes?.trim() ||
                                "Ehhez a motorcsónakhoz még nincs megjegyzés rögzítve."}
                        </div>
                    </Card>
                </div>
            )}

            {activeTab === "crew" && (
                <Card>
                    <EmptySection
                        title="Személyzet"
                        description="Itt jelenik majd meg a motorcsónakhoz hozzárendelt kormányos és rendezői személyzet."
                    />
                </Card>
            )}

            {activeTab === "equipment" && (
                <Card>
                    <EmptySection
                        title="Felszerelés"
                        description="Itt lehet majd nyilvántartani a VHF rádiót, GPS-t, horgonyt, bójákat, mentőmellényeket és egyéb felszereléseket."
                    />
                </Card>
            )}

            {activeTab === "maintenance" && (
                <Card>
                    <EmptySection
                        title="Karbantartás"
                        description="Itt jelenik majd meg a motoróra, a szerviztörténet, az aktuális hibák és a következő karbantartás időpontja."
                    />
                </Card>
            )}

            {activeTab === "documents" && (
                <Card>
                    <EmptySection
                        title="Dokumentumok"
                        description="Itt lehet majd kezelni a hajó dokumentumait, biztosítását, engedélyeit, fényképeit és feltöltött PDF-fájljait."
                    />
                </Card>
            )}

            {activeTab === "map" && (
                <Card>
                    <EmptySection
                        title="Térkép"
                        description="Itt jelenik majd meg a motorcsónak aktuális vagy utolsó ismert GPS-pozíciója."
                    />

                    <div className="boat-details__map-action">
                        <Link to="/map">
                            <Button icon={<MapPin size={17} />}>
                                Teljes térkép megnyitása
                            </Button>
                        </Link>
                    </div>
                </Card>
            )}

            {activeTab === "history" && (
                <Card>
                    <EmptySection
                        title="Előzmények"
                        description="Itt jelennek majd meg a motorcsónak korábbi versenyei, feladatai, személyzeti beosztásai és állapotváltozásai."
                    />
                </Card>
            )}
        </div>
    );
}

type EmptySectionProps = {
    title: string;
    description: string;
};

function EmptySection({ title, description }: EmptySectionProps) {
    return (
        <div className="boat-details__empty">
            <div className="boat-details__empty-icon">
                <ShipWheel size={28} />
            </div>

            <h3>{title}</h3>
            <p>{description}</p>

            <span>A funkció a következő fejlesztési lépésben készül el.</span>
        </div>
    );
}