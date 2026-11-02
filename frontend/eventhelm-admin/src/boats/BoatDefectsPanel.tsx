import { useCallback, useEffect, useState, type FormEvent } from "react";
import { createBoatDefect, getBoatDefects, updateBoatDefectStatus, type BoatDefect, type DefectPriority, type DefectStatus } from "../api/boatDefects";
import "./BoatDefectsPanel.css";

const priorities: Record<DefectPriority, string> = { Low: "Alacsony", Normal: "Normál", High: "Magas", Critical: "Kritikus" };
const statuses: Record<DefectStatus, string> = { Reported: "Bejelentve", InProgress: "Javítás alatt", Resolved: "Javítva" };
const formatDate = (date: string) => new Date(date).toLocaleString("hu-HU", { dateStyle: "medium", timeStyle: "short" });

export default function BoatDefectsPanel({ boatId }: { boatId: string }) {
    const [items, setItems] = useState<BoatDefect[]>([]);
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const [reportedBy, setReportedBy] = useState("");
    const [description, setDescription] = useState("");
    const [priority, setPriority] = useState<DefectPriority>("Normal");

    const refresh = useCallback(async () => {
        try { setError(""); setItems(await getBoatDefects(boatId)); }
        catch { setError("A hibabejelentések nem tölthetők be. Ellenőrizd az API-t és az adatbázist."); }
        finally { setLoading(false); }
    }, [boatId]);
    useEffect(() => { void refresh(); }, [refresh]);

    async function submit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        if (!reportedBy.trim() || !description.trim() || busy) return;
        setBusy(true); setError("");
        try {
            await createBoatDefect(boatId, { reportedBy: reportedBy.trim(), description: description.trim(), priority });
            setDescription(""); setPriority("Normal");
            await refresh();
        } catch { setError("A hibabejelentés mentése sikertelen. Kérjük, próbáld újra."); }
        finally { setBusy(false); }
    }
    async function changeStatus(defect: BoatDefect, status: DefectStatus) {
        if (status === defect.status || busy) return;
        setBusy(true); setError("");
        try { await updateBoatDefectStatus(boatId, defect.id, status); await refresh(); }
        catch { setError("Az állapot módosítása nem sikerült."); }
        finally { setBusy(false); }
    }
    const open = items.filter(item => item.status !== "Resolved").length;
    return <div className="boat-defects">
        <div className="boat-defects__heading"><div><h2>Szerviz és hibabejelentések</h2><p>Hibák bejelentése akár a vízről, mobiltelefonon is.</p></div><strong>{open} nyitott / {items.length} összes</strong></div>
        <form className="boat-defects__form" onSubmit={submit}>
            <h3>Új hibabejelentés</h3>
            <div className="boat-defects__fields">
                <label>Bejelentő neve<input required maxLength={150} value={reportedBy} onChange={e => setReportedBy(e.target.value)} placeholder="Név" /></label>
                <label>Sürgősség<select value={priority} onChange={e => setPriority(e.target.value as DefectPriority)}>{Object.entries(priorities).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
            </div>
            <label>Hiba leírása<textarea required maxLength={4000} rows={4} value={description} onChange={e => setDescription(e.target.value)} placeholder="Mi hibásodott meg? Hol és hogyan jelentkezik a hiba?" /></label>
            <p className="boat-defects__hint">A bejelentés időpontját a rendszer automatikusan rögzíti.</p>
            <button type="submit" disabled={busy}>{busy ? "Mentés..." : "Hiba bejelentése"}</button>
        </form>
        {error && <p className="boat-defects__error" role="alert">{error}</p>}
        <div className="boat-defects__list"><h3>Hibajegyzék</h3>
            {loading ? <p>Betöltés...</p> : items.length === 0 ? <p>Még nincs rögzített hibabejelentés.</p> : items.map(item =>
                <article className="boat-defects__item" key={item.id}>
                    <div className="boat-defects__item-top"><strong>{priorities[item.priority]} prioritás</strong><span>{formatDate(item.reportedAt)}</span></div>
                    <p>{item.description}</p><small>Bejelentő: {item.reportedBy}</small>
                    <label>Állapot<select aria-label="Hibabejelentés állapota" value={item.status} disabled={busy} onChange={e => void changeStatus(item, e.target.value as DefectStatus)}>{Object.entries(statuses).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
                </article>)}
        </div>
    </div>;
}
