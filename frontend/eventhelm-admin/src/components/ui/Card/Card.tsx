import type { ReactNode } from "react";
import "./Card.css";

export default function Card({
    children,
    title,
    eyebrow,
    action,
}: {
    children: ReactNode;
    title?: string;
    eyebrow?: string;
    action?: ReactNode;
}) {
    return (
        <article className="eh-card">
            {(title || eyebrow || action) && (
                <header className="eh-card__header">
                    <div>
                        {eyebrow && (
                            <div className="eh-card__eyebrow">
                                {eyebrow}
                            </div>
                        )}
                        {title && <h2>{title}</h2>}
                    </div>
                    {action}
                </header>
            )}
            <div className="eh-card__content">
                {children}
            </div>
        </article>
    );
}
