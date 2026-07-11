import type { CSSProperties } from "react";

type UsersAppProps = {
    embedded?: boolean;
};

function UsersApp({ embedded = false }: UsersAppProps) {
    return (
        <section style={embedded ? styles.embeddedPage : styles.page}>
            <div style={styles.header}>
                <div>
                    <h1 style={styles.title}>Felhasználók</h1>
                    <p style={styles.subtitle}>
                        Az EventHelm felhasználóinak kezelése.
                    </p>
                </div>

                <button type="button" style={styles.primaryButton}>
                    Új felhasználó
                </button>
            </div>

            <div style={styles.card}>
                <div style={styles.emptyState}>
                    <div style={styles.emptyIcon}>👤</div>
                    <h2 style={styles.emptyTitle}>Még nincs megjeleníthető felhasználó</h2>
                    <p style={styles.emptyText}>
                        A felhasználói lista és az API-kapcsolat itt fog megjelenni.
                    </p>
                </div>
            </div>
        </section>
    );
}

const styles: Record<string, CSSProperties> = {
    page: {
        minHeight: "100vh",
        padding: 32,
        background: "#F8FAFC",
    },
    embeddedPage: {
        minHeight: "100vh",
        padding: 32,
        background: "#F8FAFC",
    },
    header: {
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 20,
        marginBottom: 24,
    },
    title: {
        margin: 0,
        color: "#0F172A",
        fontSize: 32,
        fontWeight: 800,
    },
    subtitle: {
        margin: "8px 0 0",
        color: "#64748B",
        fontSize: 16,
    },
    primaryButton: {
        border: 0,
        borderRadius: 12,
        padding: "12px 18px",
        background: "#2563EB",
        color: "#FFFFFF",
        fontSize: 15,
        fontWeight: 700,
        cursor: "pointer",
    },
    card: {
        border: "1px solid #E2E8F0",
        borderRadius: 16,
        background: "#FFFFFF",
        boxShadow: "0 8px 24px rgba(15, 23, 42, 0.06)",
        overflow: "hidden",
    },
    emptyState: {
        padding: "72px 24px",
        textAlign: "center",
    },
    emptyIcon: {
        marginBottom: 16,
        fontSize: 44,
    },
    emptyTitle: {
        margin: 0,
        color: "#0F172A",
        fontSize: 22,
    },
    emptyText: {
        margin: "10px 0 0",
        color: "#64748B",
        fontSize: 15,
    },
};

export default UsersApp;