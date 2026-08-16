import type { LucideIcon } from "lucide-react";
import { ArrowRight, CheckCircle2 } from "lucide-react";
import { Link } from "react-router-dom";

import "./EventModulePanel.css";

export type EventModuleCard = {
    title: string;
    description: string;
    icon: LucideIcon;
    to: string;
    buttonLabel?: string;
    badge?: string;
};

type EventModulePanelProps = {
    eyebrow: string;
    title: string;
    description: string;
    cards: readonly EventModuleCard[];
    statusText?: string;
};

export default function EventModulePanel({
    eyebrow,
    title,
    description,
    cards,
    statusText = "Rendszer aktív",
}: EventModulePanelProps) {
    return (
        <section className="eh-event-module">
            <header className="eh-event-module__header">
                <div>
                    <span className="eh-event-module__eyebrow">
                        {eyebrow}
                    </span>

                    <h2>{title}</h2>

                    <p>{description}</p>
                </div>

                <div className="eh-event-module__status">
                    <CheckCircle2
                        size={15}
                        aria-hidden="true"
                    />
                    {statusText}
                </div>
            </header>

            <div className="eh-event-module__grid">
                {cards.map((card) => {
                    const Icon = card.icon;

                    return (
                        <article
                            key={`${card.title}-${card.to}`}
                            className="eh-event-module__card"
                        >
                            <div className="eh-event-module__card-top">
                                <div className="eh-event-module__icon">
                                    <Icon
                                        size={23}
                                        strokeWidth={1.9}
                                        aria-hidden="true"
                                    />
                                </div>

                                {card.badge && (
                                    <span className="eh-event-module__badge">
                                        {card.badge}
                                    </span>
                                )}
                            </div>

                            <h3>{card.title}</h3>

                            <p>{card.description}</p>

                            <Link
                                to={card.to}
                                className="eh-event-module__button"
                            >
                                {card.buttonLabel ?? "Megnyitás"}

                                <ArrowRight
                                    size={16}
                                    aria-hidden="true"
                                />
                            </Link>
                        </article>
                    );
                })}
            </div>
        </section>
    );
}
