import {
    Activity,
    Anchor,
    Flag,
    Gauge,
    MapPin,
    Radio,
    ShieldCheck,
    Signal,
    Wind,
} from "lucide-react";
import { useMemo, useState } from "react";

import { Badge, Button, Card, PageHeader } from "../components/ui";

import "./RaceControlPage.css";

type RaceStatus =
    | "Preparation"
    | "Starting"
    | "InProgress"
    | "Finished";

type Unit = {
    id: string;
    name: string;
    callSign: string;
    crew: number;
    status: "Ready" | "OnWater" | "Standby";
};

const units: Unit[] = [
    {
        id: "1",
        name: "Alpha",
        callSign: "RC-01",
        crew: 3,
        status: "OnWater",
    },
    {
        id: "2",
        name: "Bravo",
        callSign: "RC-02",
        crew: 2,
        status: "Ready",
    },
    {
        id: "3",
        name: "Charlie",
        callSign: "RC-03",
        crew: 2,
        status: "Standby",
    },
];

export default function RaceControlPage() {
    const [raceStatus, setRaceStatus] =
        useState<RaceStatus>("Preparation");

    const [selectedFlag, setSelectedFlag] =
        useState("AP");

    const activeUnits = useMemo(
        () =>
            units.filter(
                (unit) => unit.status === "OnWater",
            ).length,
        [],
    );

    return (
        <div className="eh-race-control">
            <PageHeader
                eyebrow="VERSENYIRÁNYÍTÁS"
                title="Race Control"
                description="A verseny aktuális állapotának, jelzéseinek és operációs egységeinek központi kezelése."
                actions={
                    <Badge
                        variant={
                            raceStatus === "InProgress"
                                ? "success"
                                : raceStatus === "Finished"
                                  ? "neutral"
                                  : "warning"
                        }
                    >
                        {raceStatusLabel(raceStatus)}
                    </Badge>
                }
            />

            <section className="eh-race-control__hero">
                <div className="eh-race-control__hero-copy">
                    <div className="eh-race-control__live">
                        <span />
                        LIVE RACE OPERATIONS
                    </div>

                    <h2>Balaton Race 07</h2>

                    <p>
                        Központi versenyirányítás, rádiós
                        kommunikáció és operációs egységek
                        felügyelete.
                    </p>

                    <div className="eh-race-control__hero-actions">
                        <Button
                            icon={<Flag size={17} />}
                            onClick={() =>
                                setRaceStatus("Starting")
                            }
                        >
                            Rajtprocedúra indítása
                        </Button>

                        <Button
                            variant="secondary"
                            icon={<Radio size={17} />}
                        >
                            VHF 72 megnyitása
                        </Button>
                    </div>
                </div>

                <div className="eh-race-control__radar">
                    <div className="eh-race-control__radar-grid" />
                    <div className="eh-race-control__radar-sweep" />
                    <span className="eh-race-control__target eh-race-control__target--one" />
                    <span className="eh-race-control__target eh-race-control__target--two" />
                    <span className="eh-race-control__target eh-race-control__target--three" />

                    <div className="eh-race-control__radar-center">
                        <Anchor size={26} />
                    </div>
                </div>
            </section>

            <section className="eh-race-control__metrics">
                <Metric
                    icon={Wind}
                    label="Szél"
                    value="12 kn"
                    meta="NW 318°"
                />

                <Metric
                    icon={Gauge}
                    label="Lökések"
                    value="17 kn"
                    meta="Maximum"
                />

                <Metric
                    icon={Signal}
                    label="Rádió"
                    value="Online"
                    meta="VHF 72"
                />

                <Metric
                    icon={ShieldCheck}
                    label="Aktív egység"
                    value={String(activeUnits)}
                    meta={`${units.length} összesen`}
                />
            </section>

            <section className="eh-race-control__grid">
                <Card
                    eyebrow="VERSENYÁLLAPOT"
                    title="Operációs vezérlés"
                >
                    <div className="eh-race-control__status-grid">
                        {(
                            [
                                "Preparation",
                                "Starting",
                                "InProgress",
                                "Finished",
                            ] as RaceStatus[]
                        ).map((status) => (
                            <button
                                key={status}
                                type="button"
                                className={[
                                    "eh-race-control__status-button",
                                    raceStatus === status
                                        ? "eh-race-control__status-button--active"
                                        : "",
                                ].join(" ")}
                                onClick={() =>
                                    setRaceStatus(status)
                                }
                            >
                                {raceStatusLabel(status)}
                            </button>
                        ))}
                    </div>
                </Card>

                <Card
                    eyebrow="JELZÉSEK"
                    title="Zászlópanel"
                >
                    <div className="eh-race-control__flag-grid">
                        {["AP", "N", "P", "I", "U", "Black"].map(
                            (flag) => (
                                <button
                                    key={flag}
                                    type="button"
                                    className={[
                                        "eh-race-control__flag",
                                        selectedFlag === flag
                                            ? "eh-race-control__flag--active"
                                            : "",
                                    ].join(" ")}
                                    onClick={() =>
                                        setSelectedFlag(flag)
                                    }
                                >
                                    <Flag size={18} />
                                    <strong>{flag}</strong>
                                </button>
                            ),
                        )}
                    </div>
                </Card>
            </section>

            <section className="eh-race-control__grid">
                <Card
                    eyebrow="OPERÁCIÓS EGYSÉGEK"
                    title="Motorcsónakok"
                >
                    <div className="eh-race-control__unit-list">
                        {units.map((unit) => (
                            <div
                                key={unit.id}
                                className="eh-race-control__unit"
                            >
                                <div className="eh-race-control__unit-icon">
                                    <Anchor size={18} />
                                </div>

                                <div>
                                    <strong>{unit.name}</strong>
                                    <span>
                                        {unit.callSign} ·{" "}
                                        {unit.crew} fő
                                    </span>
                                </div>

                                <Badge
                                    variant={unitVariant(
                                        unit.status,
                                    )}
                                >
                                    {unitStatusLabel(
                                        unit.status,
                                    )}
                                </Badge>
                            </div>
                        ))}
                    </div>
                </Card>

                <Card
                    eyebrow="PÁLYA"
                    title="Versenyterület"
                >
                    <div className="eh-race-control__course">
                        <div className="eh-race-control__course-line" />
                        <CourseMark
                            label="START"
                            className="eh-race-control__mark--start"
                        />
                        <CourseMark
                            label="M1"
                            className="eh-race-control__mark--one"
                        />
                        <CourseMark
                            label="M2"
                            className="eh-race-control__mark--two"
                        />
                        <CourseMark
                            label="FINISH"
                            className="eh-race-control__mark--finish"
                        />

                        <div className="eh-race-control__course-info">
                            <MapPin size={16} />
                            Balatonfüred versenyterület
                        </div>
                    </div>
                </Card>
            </section>
        </div>
    );
}

function Metric({
    icon: Icon,
    label,
    value,
    meta,
}: {
    icon: typeof Activity;
    label: string;
    value: string;
    meta: string;
}) {
    return (
        <article className="eh-race-control__metric">
            <div>
                <Icon size={20} />
            </div>

            <span>{label}</span>
            <strong>{value}</strong>
            <small>{meta}</small>
        </article>
    );
}

function CourseMark({
    label,
    className,
}: {
    label: string;
    className: string;
}) {
    return (
        <div
            className={[
                "eh-race-control__mark",
                className,
            ].join(" ")}
        >
            {label}
        </div>
    );
}

function raceStatusLabel(
    status: RaceStatus,
): string {
    const labels: Record<RaceStatus, string> = {
        Preparation: "Előkészítés",
        Starting: "Rajtprocedúra",
        InProgress: "Verseny folyamatban",
        Finished: "Befejezve",
    };

    return labels[status];
}

function unitStatusLabel(
    status: Unit["status"],
): string {
    const labels = {
        Ready: "Készenlét",
        OnWater: "Vízen",
        Standby: "Tartalék",
    };

    return labels[status];
}

function unitVariant(
    status: Unit["status"],
): "success" | "warning" | "neutral" {
    if (status === "OnWater") {
        return "success";
    }

    if (status === "Ready") {
        return "warning";
    }

    return "neutral";
}
