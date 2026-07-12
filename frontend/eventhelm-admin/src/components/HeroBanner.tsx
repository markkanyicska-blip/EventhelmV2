import {
    Activity,
    Compass,
    Radio,
    ShieldCheck,
    Signal,
    Wind,
} from "lucide-react";
import "./HeroBanner.css";

export default function HeroBanner({
    activeEvents,
    activeOfficials,
    backendOnline,
}: {
    activeEvents: number;
    activeOfficials: number;
    backendOnline: boolean;
}) {
    const time = new Intl.DateTimeFormat("hu-HU", {
        hour: "2-digit",
        minute: "2-digit",
    }).format(new Date());

    return (
        <section className="eh-hero">
            <div className="eh-hero__copy">
                <div className="eh-hero__eyebrow">
                    <Radio size={16} />
                    SAILING RACE OPERATIONS
                </div>
                <h1>
                    Command the race.
                    <span>Lead with precision.</span>
                </h1>
                <p>
                    Irányítsd a rendezvényeket, a személyzetet és az
                    operációs egységeket egyetlen központból.
                </p>
                <div className="eh-hero__metrics">
                    <div><Activity size={18}/><strong>{activeEvents}</strong><span>Aktív esemény</span></div>
                    <div><ShieldCheck size={18}/><strong>{activeOfficials}</strong><span>Szolgálatban</span></div>
                    <div className={backendOnline ? "online" : "offline"}>
                        <i />{backendOnline ? "Rendszer online" : "Backend offline"}
                    </div>
                </div>
            </div>

            <aside className="eh-control">
                <header>
                    <div><small>RACE CONTROL</small><h2>Operációs központ</h2></div>
                    <b className={backendOnline ? "online" : "offline"}>
                        {backendOnline ? "LIVE" : "OFFLINE"}
                    </b>
                </header>
                <div className="eh-control__meta">
                    <div><span>VERSENY</span><strong>Race 07</strong></div>
                    <div><span>HELYSZÍN</span><strong>Balaton</strong></div>
                    <div><span>SZÉL</span><strong>NW 12 kn</strong></div>
                    <div><span>HELYI IDŐ</span><strong>{time}</strong></div>
                </div>
                <div className="eh-control__radar">
                    <div /><div /><div />
                    <Compass size={26} />
                </div>
                <div className="eh-control__telemetry">
                    <div><Wind size={17}/><span>12 kn<small>NW 318°</small></span></div>
                    <div><Signal size={17}/><span>{backendOnline ? "Online":"Offline"}<small>VHF 72</small></span></div>
                    <div><Activity size={17}/><span>{activeEvents}<small>aktív</small></span></div>
                    <div><ShieldCheck size={17}/><span>{activeOfficials}<small>szolgálatban</small></span></div>
                </div>
            </aside>
        </section>
    );
}
