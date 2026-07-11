import {
    Activity,
    Radio,
    ShieldCheck,
} from "lucide-react";

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
            <div className="eh-hero__background" />

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
                        label="Aktív rendező"
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

            <div className="eh-hero__control-panel">
                <div className="eh-hero__control-overlay" />

                <div className="eh-hero__control-content">
                    <div className="eh-hero__control-label">
                        RACE CONTROL
                    </div>

                    <div className="eh-hero__control-title">
                        Operációs központ
                    </div>

                    <div className="eh-hero__control-description">
                        Élő rendszerállapot és versenyirányítás
                    </div>
                </div>

                <div className="eh-hero__signal">
                    <span />
                    LIVE
                </div>
            </div>
        </section>
    );
}

function HeroMetric({
    icon,
    label,
    value,
}: {
    icon: React.ReactNode;
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

export default HeroBanner;