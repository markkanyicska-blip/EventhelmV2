import type { ReactNode } from "react";
import "./Card.css";

type CardProps = {
    children: ReactNode;
    title?: string;
    eyebrow?: string;
    action?: ReactNode;
    className?: string;
};

export default function Card({
    children,
    title,
    eyebrow,
    action,
    className,
}: CardProps) {
    const cardClassName = [
        "eh-card",
        className,
    ]
        .filter(Boolean)
        .join(" ");

    return (
        <article className={cardClassName}>
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

                    {action && (
                        <div className="eh-card__action">
                            {action}
                        </div>
                    )}
                </header>
            )}

            <div className="eh-card__content">
                {children}
            </div>
        </article>
    );
}