import {
    Activity,
    Compass,
    Radio,
    ShieldCheck,
    Signal,
    Wind,
} from "lucide-react";
import type { ReactNode } from "react";

import "./HeroBanner.css";

type HeroBannerProps = {
    activeEvents: number;
    activeOfficials: number;
    backendOnline: boolean;
};

function HeroBanner({
    activeEvents,
    activeOfficials,
    backendOnline,
}: HeroBannerProps) {
    return (
        <section className="eh-hero">
            <div className="eh-hero__ambient" />

            <div className="eh-hero__content">
                <div className="eh-hero__eyebrow">
                    <Radio size={16} strokeWidth={1.9} />
                    <span>SAILING RACE OPERATIONS</span>
                </div>

                <h1 className="eh-hero__title">
                    Command the race.
                    <span>Lead with precision.</span>
                </h1>

                <p className="eh-hero__description">
                    Irányítsd a rendezvényeket, a személyzetet és
                    az operációs egységeket egyetlen központból.
                </p>

                <div className="eh-hero__metrics">
                    <HeroMetric
                        icon={<Activity size={18} />}
                        label="Aktív rendezvény"
                        value={activeEvents}
                    />

                    <HeroMetric
                        icon={<ShieldCheck size={18} />}
                        label="Szolgálatban"
                        value={activeOfficials}
                    />

                    <div
                        className={[
                            "eh-hero__system-status",
                            backendOnline
                                ? "eh-hero__system-status--online"
                                : "eh-hero__system-status--offline",
                        ].join(" ")}
                    >
                        <span className="eh-hero__system-dot" />

                        {backendOnline
                            ? "Rendszer online"
                            : "Backend offline"}
                    </div>
                </div>
            </div>

            <RaceControlPanel
                activeEvents={activeEvents}
                activeOfficials={activeOfficials}
                backendOnline={backendOnline}
            />
        </section>
    );
}

function RaceControlPanel({
    activeEvents,
    activeOfficials,
    backendOnline,
}: HeroBannerProps) {
    const currentTime = new Intl.DateTimeFormat("hu-HU", {
        hour: "2-digit",
        minute: "2-digit",
    }).format(new Date());

    return (
        <aside className="eh-control">
            <div className="eh-control__grid" />
            <div className="eh-control__sweep" />

            <header className="eh-control__header">
                <div>
                    <span className="eh-control__eyebrow">
                        RACE CONTROL
                    </span>

                    <h2>Operációs központ</h2>
                </div>

                <div
                    className={[
                        "eh-control__live",
                        backendOnline
                            ? "eh-control__live--online"
                            : "eh-control__live--offline",
                    ].join(" ")}
                >
                    <span />
                    {backendOnline ? "LIVE" : "OFFLINE"}
                </div>
            </header>

            <div className="eh-control__race-meta">
                <div>
                    <span>VERSENY</span>
                    <strong>Race 07</strong>
                </div>

                <div>
                    <span>HELYSZÍN</span>
                    <strong>Balaton</strong>
                </div>

                <div>
                    <span>SZÉL</span>
                    <strong>NW 12 kn</strong>
                </div>

                <div>
                    <span>HELYI IDŐ</span>
                    <strong>{currentTime}</strong>
                </div>
            </div>

            <div className="eh-control__radar">
                <div className="eh-control__radar-ring eh-control__radar-ring--outer" />
                <div className="eh-control__radar-ring eh-control__radar-ring--middle" />
                <div className="eh-control__radar-ring eh-control__radar-ring--inner" />

                <span className="eh-control__radar-axis eh-control__radar-axis--horizontal" />
                <span className="eh-control__radar-axis eh-control__radar-axis--vertical" />

                <span className="eh-control__target eh-control__target--one" />
                <span className="eh-control__target eh-control__target--two" />
                <span className="eh-control__target eh-control__target--three" />

                <div className="eh-control__radar-center">
                    <Compass size={26} strokeWidth={1.5} />
                </div>
            </div>

            <div className="eh-control__telemetry">
                <ControlMetric
                    icon={<Wind size={17} />}
                    label="Szél"
                    value="12 kn"
                    secondary="NW 318°"
                />

                <ControlMetric
                    icon={<Signal size={17} />}
                    label="Rádió"
                    value={backendOnline ? "Online" : "Offline"}
                    secondary="VHF 72"
                />

                <ControlMetric
                    icon={<Activity size={17} />}
                    label="Esemény"
                    value={String(activeEvents)}
                    secondary="aktív"
                />

                <ControlMetric
                    icon={<ShieldCheck size={17} />}
                    label="Személyzet"
                    value={String(activeOfficials)}
                    secondary="szolgálatban"
                />
            </div>

            <footer className="eh-control__footer">
                <span>SYSTEM STATUS</span>

                <strong>
                    {backendOnline
                        ? "Minden rendszer üzemkész"
                        : "Kapcsolódás szükséges"}
                </strong>
            </footer>
        </aside>
    );
}

function HeroMetric({
    icon,
    label,
    value,
}: {
    icon: ReactNode;
    label: string;
    value: number;
}) {
    return (
        <div className="eh-hero__metric">
            <span className="eh-hero__metric-icon">
                {icon}
            </span>

            <span className="eh-hero__metric-value">
                {value}
            </span>

            <span className="eh-hero__metric-label">
                {label}
            </span>
        </div>
    );
}

function ControlMetric({
    icon,
    label,
    value,
    secondary,
}: {
    icon: ReactNode;
    label: string;
    value: string;
    secondary: string;
}) {
    return (
        <div className="eh-control__metric">
            <div className="eh-control__metric-icon">
                {icon}
            </div>

            <div>
                <span>{label}</span>
                <strong>{value}</strong>
                <small>{secondary}</small>
            </div>
        </div>
    );
}

export default HeroBanner;
