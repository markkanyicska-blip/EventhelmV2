import type { LucideIcon } from "lucide-react";

import "./DashboardStatCard.css";

type DashboardStatCardProps = {
    icon: LucideIcon;
    label: string;
    value: number;
    description: string;
    variant?: "gold" | "green" | "blue" | "red";
};

function DashboardStatCard({
    icon: Icon,
    label,
    value,
    description,
    variant = "gold",
}: DashboardStatCardProps) {
    return (
        <article
            className={[
                "eh-stat-card",
                `eh-stat-card--${variant}`,
            ].join(" ")}
        >
            <div className="eh-stat-card__top">
                <div className="eh-stat-card__icon">
                    <Icon size={21} strokeWidth={1.8} />
                </div>

                <div className="eh-stat-card__label">
                    {label}
                </div>
            </div>

            <div className="eh-stat-card__value">
                {value}
            </div>

            <div className="eh-stat-card__description">
                {description}
            </div>

            <div className="eh-stat-card__accent" />
        </article>
    );
}

export default DashboardStatCard;