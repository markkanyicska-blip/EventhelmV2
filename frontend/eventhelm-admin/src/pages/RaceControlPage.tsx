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
import {
    useCallback,
    useEffect,
    useMemo,
    useState,
} from "react";

import {
    addRaceTimelineEntry,
    getRaceControlState,
    updateRaceFlag,
    updateRaceStatus,
    updateRaceUnit,
} from "../api/raceControl";
import {
    Badge,
    Button,
    Card,
    PageHeader,
} from "../components/ui";
import type {
    RaceControlState,
    RaceStatus,
    RaceUnitStatus,
} from "../types/raceControl";

import "./RaceControlPage.css";

export default function RaceControlPage() {
    const [state, setState] =
        useState<RaceControlState | null>(null);

    const [loading, setLoading] = useState(true);
    const [error, setError] =
        useState<string | null>(null);

    const loadState = useCallback(async () => {
        try {
            const result =
                await getRaceControlState();

            setState(result);
            setError(null);
        } catch (loadError) {
            console.error(loadError);
            setError(
                "A Race Control backend nem érhető el.",
            );
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        void loadState();

        const timer = window.setInterval(() => {
            void loadState();
        }, 5000);

        return () => {
            window.clearInterval(timer);
        };
    }, [loadState]);

    const activeUnits = useMemo(
        () =>
            state?.units.filter(
                (unit) =>
                    unit.status === "OnWater",
            ).length ?? 0,
        [state],
    );

    async function changeStatus(
        status: RaceStatus,
    ) {
        const result =
            await updateRaceStatus(status);

        setState(result);
    }

    async function changeFlag(flag: string) {
        const result = await updateRaceFlag(flag);

        setState(result);
    }

    async function changeUnitStatus(
        id: string,
        status: RaceUnitStatus,
    ) {
        const result =
            await updateRaceUnit(id, status);

        setState(result);
    }

    async function addRadioLog() {
        const result =
            await addRaceTimelineEntry(
                "radio",
                "VHF 72 rádióellenőrzés sikeres.",
            );

        setState(result);
    }

    if (loading) {
        return (
            <div className="eh-race-control">
                <Card
                    eyebrow="VERSENYIRÁNYÍTÁS"
                    title="Race Control betöltése"
                >
                    <p>Kapcsolódás a backendhez...</p>
                </Card>
            </div>
        );
    }

    if (!state) {
        return (
            <div className="eh-race-control">
                <Card
                    eyebrow="KAPCSOLATI HIBA"
                    title="A Race Control nem érhető el"
                >
                    <p>{error}</p>

                    <Button
                        onClick={() =>
                            void loadState()
                        }
                    >
                        Újrapróbálás
                    </Button>
                </Card>
            </div>
        );
    }

    return (
        <div className="eh-race-control">
            <PageHeader
                eyebrow="VERSENYIRÁNYÍTÁS"
                title="Race Control"
                description="Valódi backend-adatokkal működő központi versenyirányítás."
                actions={
                    <Badge
                        variant={
                            state.status ===
                            "InProgress"
                                ? "success"
                                : state.status ===
                                    "Finished"
                                  ? "neutral"
                                  : "warning"
                        }
                    >
                        {raceStatusLabel(
                            state.status,
                        )}
                    </Badge>
                }
            />

            <section className="eh-race-control__hero">
                <div className="eh-race-control__hero-copy">
                    <div className="eh-race-control__live">
                        <span />
                        LIVE RACE OPERATIONS
                    </div>

                    <h2>{state.raceName}</h2>

                    <p>
                        {state.location} ·{" "}
                        {state.radio.channel} ·{" "}
                        {state.activeFlag} jelzés
                    </p>

                    <div className="eh-race-control__hero-actions">
                        <Button
                            icon={<Flag size={17} />}
                            onClick={() =>
                                void changeStatus(
                                    "Starting",
                                )
                            }
                        >
                            Rajtprocedúra
                        </Button>

                        <Button
                            variant="secondary"
                            icon={<Radio size={17} />}
                            onClick={() =>
                                void addRadioLog()
                            }
                        >
                            Rádióellenőrzés
                        </Button>
                    </div>
                </div>

                <div className="eh-race-control__radar">
                    <div className="eh-race-control__radar-grid" />
                    <div className="eh-race-control__radar-sweep" />

                    {state.units
                        .filter(
                            (unit) =>
                                unit.status !==
                                "Offline",
                        )
                        .slice(0, 3)
                        .map((unit, index) => (
                            <span
                                key={unit.id}
                                title={unit.callSign}
                                className={[
                                    "eh-race-control__target",
                                    `eh-race-control__target--${[
                                        "one",
                                        "two",
                                        "three",
                                    ][index]}`,
                                ].join(" ")}
                            />
                        ))}

                    <div className="eh-race-control__radar-center">
                        <Anchor size={26} />
                    </div>
                </div>
            </section>

            <section className="eh-race-control__metrics">
                <Metric
                    icon={Wind}
                    label="Szél"
                    value={`${state.weather.windSpeedKnots} kn`}
                    meta={`${state.weather.windDirectionDegrees}°`}
                />

                <Metric
                    icon={Gauge}
                    label="Lökések"
                    value={`${state.weather.gustSpeedKnots} kn`}
                    meta={`${state.weather.pressureHpa} hPa`}
                />

                <Metric
                    icon={Signal}
                    label="Rádió"
                    value={
                        state.radio.isOnline
                            ? "Online"
                            : "Offline"
                    }
                    meta={state.radio.channel}
                />

                <Metric
                    icon={ShieldCheck}
                    label="Aktív egység"
                    value={String(activeUnits)}
                    meta={`${state.units.length} összesen`}
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
                                    state.status === status
                                        ? "eh-race-control__status-button--active"
                                        : "",
                                ].join(" ")}
                                onClick={() =>
                                    void changeStatus(
                                        status,
                                    )
                                }
                            >
                                {raceStatusLabel(
                                    status,
                                )}
                            </button>
                        ))}
                    </div>
                </Card>

                <Card
                    eyebrow="JELZÉSEK"
                    title="Zászlópanel"
                >
                    <div className="eh-race-control__flag-grid">
                        {[
                            "AP",
                            "N",
                            "P",
                            "I",
                            "U",
                            "Black",
                        ].map((flag) => (
                            <button
                                key={flag}
                                type="button"
                                className={[
                                    "eh-race-control__flag",
                                    state.activeFlag ===
                                    flag
                                        ? "eh-race-control__flag--active"
                                        : "",
                                ].join(" ")}
                                onClick={() =>
                                    void changeFlag(flag)
                                }
                            >
                                <Flag size={18} />
                                <strong>{flag}</strong>
                            </button>
                        ))}
                    </div>
                </Card>
            </section>

            <section className="eh-race-control__grid">
                <Card
                    eyebrow="OPERÁCIÓS EGYSÉGEK"
                    title="Motorcsónakok"
                >
                    <div className="eh-race-control__unit-list">
                        {state.units.map((unit) => (
                            <div
                                key={unit.id}
                                className="eh-race-control__unit"
                            >
                                <div className="eh-race-control__unit-icon">
                                    <Anchor size={18} />
                                </div>

                                <div>
                                    <strong>
                                        {unit.name}
                                    </strong>
                                    <span>
                                        {unit.callSign} ·{" "}
                                        {unit.crew} fő
                                    </span>
                                </div>

                                <select
                                    value={unit.status}
                                    onChange={(event) =>
                                        void changeUnitStatus(
                                            unit.id,
                                            event.target
                                                .value as RaceUnitStatus,
                                        )
                                    }
                                >
                                    <option value="Ready">
                                        Készenlét
                                    </option>
                                    <option value="OnWater">
                                        Vízen
                                    </option>
                                    <option value="Standby">
                                        Tartalék
                                    </option>
                                    <option value="Offline">
                                        Offline
                                    </option>
                                </select>
                            </div>
                        ))}
                    </div>
                </Card>

                <Card
                    eyebrow="ESEMÉNYNAPLÓ"
                    title="Legutóbbi műveletek"
                >
                    <div className="eh-race-control__unit-list">
                        {state.timeline.length === 0 ? (
                            <div className="eh-race-control__unit">
                                <div className="eh-race-control__unit-icon">
                                    <Activity size={18} />
                                </div>

                                <div>
                                    <strong>
                                        Nincs naplóbejegyzés
                                    </strong>
                                    <span>
                                        A vezérlési
                                        műveletek itt
                                        jelennek meg.
                                    </span>
                                </div>
                            </div>
                        ) : (
                            state.timeline
                                .slice(0, 8)
                                .map((entry) => (
                                    <div
                                        key={entry.id}
                                        className="eh-race-control__unit"
                                    >
                                        <div className="eh-race-control__unit-icon">
                                            <Activity
                                                size={18}
                                            />
                                        </div>

                                        <div>
                                            <strong>
                                                {
                                                    entry.message
                                                }
                                            </strong>
                                            <span>
                                                {formatTime(
                                                    entry.timestamp,
                                                )}
                                            </span>
                                        </div>
                                    </div>
                                ))
                        )}
                    </div>
                </Card>
            </section>

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
                        {state.location} versenyterület
                    </div>
                </div>
            </Card>
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

function formatTime(value: string): string {
    return new Intl.DateTimeFormat("hu-HU", {
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
    }).format(new Date(value));
}
