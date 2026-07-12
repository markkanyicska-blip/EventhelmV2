import type { ReactNode } from "react";
import "./Badge.css";

export default function Badge({
    children,
    variant = "neutral",
}: {
    children: ReactNode;
    variant?: "success" | "warning" | "danger" | "info" | "neutral";
}) {
    return (
        <span className={`eh-badge eh-badge--${variant}`}>
            {children}
        </span>
    );
}
