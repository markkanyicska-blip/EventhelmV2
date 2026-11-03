import type { CSSProperties } from "react";
import type { Boat } from "./boatTypes";

type BoatCardProps = {
    boat: Boat;
    deleting?: boolean;
    onEdit: () => void;
    onDelete: () => void;
};

function BoatCard({
    boat,
    deleting = false,
    onEdit,
    onDelete,
}: BoatCardProps) {
    const status = getStatusPresentation(boat.status);

    return (
        <article style={styles.card}>
            <header style={styles.header}>
                <div style={styles.identity}>
                    <div style={styles.icon}>🚤</div>
                    <div style={styles.headingText}>
                        <h3 style={styles.title}>{boat.name}</h3>
                        <p style={styles.subtitle}>
                            {boat.callSign
                                ? `Hívójel: ${boat.callSign}`
                                : "Nincs megadott hívójel"}
                        </p>
                    </div>
                </div>

                <span
                    style={{
                        ...styles.statusBadge,
                        background: status.background,
                        color: status.color,
                    }}
                >
                    <span
                        style={{
                            ...styles.statusDot,
                            background: status.dotColor,
                        }}
                    />
                    {status.label}
                </span>
            </header>

            <section style={styles.detailsGrid}>
                <Detail label="Lajstromszám" value={boat.registrationNumber || "Nincs megadva"} />
                <Detail label="Típus" value={boat.type || "Nincs megadva"} />
                <Detail label="Kapacitás" value={boat.capacity ? `${boat.capacity} fő` : "Nincs megadva"} />
                <Detail label="Gyártó" value={boat.manufacturer || "Nincs megadva"} />
                <Detail label="Motor" value={boat.engine || "Nincs megadva"} />
                <Detail label="Teljesítmény" value={boat.enginePower ? `${boat.enginePower} LE` : "Nincs megadva"} />
                <Detail label="Üzemanyag" value={boat.fuelType || "Nincs megadva"} />
            </section>

            <section style={styles.assignmentNotice}>
                <div style={styles.assignmentIcon}>📅</div>
                <div>
                    <strong style={styles.assignmentTitle}>Nincs állandó személyzet</strong>
                    <p style={styles.assignmentText}>
                        A kormányos és a személyzet mindig az adott rendezvényen belül kerül hozzárendelésre.
                    </p>
                </div>
            </section>

            {boat.notes && (
                <section style={styles.notes}>
                    <span style={styles.notesLabel}>Megjegyzés</span>
                    <p style={styles.notesText}>{boat.notes}</p>
                </section>
            )}

            <footer style={styles.actions}>
                <button type="button" style={styles.secondaryButton} onClick={onEdit} disabled={deleting}>
                    Szerkesztés
                </button>
                <button
                    type="button"
                    style={{ ...styles.deleteButton, ...(deleting ? styles.disabledButton : {}) }}
                    onClick={onDelete}
                    disabled={deleting}
                >
                    {deleting ? "Törlés..." : "Törlés"}
                </button>
            </footer>
        </article>
    );
}

function Detail({ label, value }: { label: string; value: string }) {
    return (
        <div style={styles.detail}>
            <span style={styles.detailLabel}>{label}</span>
            <strong style={styles.detailValue}>{value}</strong>
        </div>
    );
}

function getStatusPresentation(statusValue: string) {
    const normalizedStatus = statusValue.trim().toLowerCase();

    if (normalizedStatus === "active" || normalizedStatus === "aktív") {
        return { label: "Aktív", background: "#DCFCE7", color: "#166534", dotColor: "#22C55E" };
    }

    if (normalizedStatus === "service" || normalizedStatus === "maintenance" || normalizedStatus === "szerviz") {
        return { label: "Szerviz", background: "#FEF3C7", color: "#92400E", dotColor: "#F59E0B" };
    }

    if (normalizedStatus === "unavailable" || normalizedStatus === "inactive" || normalizedStatus === "nem elérhető") {
        return { label: "Nem elérhető", background: "#FEE2E2", color: "#991B1B", dotColor: "#EF4444" };
    }

    return {
        label: statusValue || "Ismeretlen állapot",
        background: "#E2E8F0",
        color: "#334155",
        dotColor: "#64748B",
    };
}

const styles: Record<string, CSSProperties> = {
    card: { display: "flex", flexDirection: "column", minHeight: 450, padding: 20, border: "1px solid #E2E8F0", borderRadius: 18, background: "#FFFFFF", boxShadow: "0 10px 30px rgba(15, 23, 42, 0.07)" },
    header: { display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 16, paddingBottom: 17, borderBottom: "1px solid #E2E8F0" },
    identity: { display: "flex", alignItems: "center", minWidth: 0, gap: 12 },
    headingText: { minWidth: 0 },
    icon: { display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0, width: 46, height: 46, borderRadius: 13, background: "#DBEAFE", fontSize: 23 },
    title: { margin: 0, overflow: "hidden", color: "#0F172A", fontSize: 20, textOverflow: "ellipsis", whiteSpace: "nowrap" },
    subtitle: { margin: "5px 0 0", overflow: "hidden", color: "#64748B", fontSize: 13, textOverflow: "ellipsis", whiteSpace: "nowrap" },
    statusBadge: { display: "inline-flex", alignItems: "center", flexShrink: 0, gap: 7, padding: "7px 10px", borderRadius: 999, fontSize: 11, fontWeight: 900 },
    statusDot: { width: 7, height: 7, borderRadius: 999 },
    detailsGrid: { display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12, padding: "17px 0" },
    detail: { minWidth: 0, padding: 12, borderRadius: 11, background: "#F8FAFC" },
    detailLabel: { display: "block", marginBottom: 6, color: "#94A3B8", fontSize: 11, fontWeight: 800, textTransform: "uppercase", letterSpacing: "0.05em" },
    detailValue: { display: "block", overflow: "hidden", color: "#1E293B", fontSize: 13, textOverflow: "ellipsis", whiteSpace: "nowrap" },
    assignmentNotice: { display: "flex", gap: 12, marginTop: 2, padding: 14, border: "1px solid #BFDBFE", borderRadius: 12, background: "#EFF6FF" },
    assignmentIcon: { fontSize: 22 },
    assignmentTitle: { color: "#1E3A8A", fontSize: 13 },
    assignmentText: { margin: "5px 0 0", color: "#475569", fontSize: 12, lineHeight: 1.5 },
    notes: { padding: "16px 0" },
    notesLabel: { color: "#94A3B8", fontSize: 11, fontWeight: 800, textTransform: "uppercase", letterSpacing: "0.05em" },
    notesText: { display: "-webkit-box", margin: "7px 0 0", overflow: "hidden", color: "#475569", fontSize: 12, lineHeight: 1.55, WebkitBoxOrient: "vertical", WebkitLineClamp: 3 },
    actions: { display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8, marginTop: "auto", paddingTop: 16 },
    secondaryButton: { border: "1px solid #CBD5E1", borderRadius: 10, padding: "10px 12px", background: "#FFFFFF", color: "#334155", fontSize: 12, fontWeight: 800, cursor: "pointer" },
    deleteButton: { border: "1px solid #FCA5A5", borderRadius: 10, padding: "10px 12px", background: "#FEF2F2", color: "#B91C1C", fontSize: 12, fontWeight: 800, cursor: "pointer" },
    disabledButton: { cursor: "not-allowed", opacity: 0.55 },
};

export default BoatCard;