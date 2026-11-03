import type { LucideIcon } from "lucide-react";
import "./StatCard.css";

export default function StatCard({
    icon: Icon,
    label,
    value,
    description,
    variant,
}: {
    icon: LucideIcon;
    label: string;
    value: number;
    description: string;
    variant: "gold" | "green" | "blue" | "red";
}) {
    return (
        <article className={`eh-stat eh-stat--${variant}`}>
            <div><Icon size={21}/><span>{label}</span></div>
            <strong>{value}</strong>
            <small>{description}</small>
        </article>
    );
}
