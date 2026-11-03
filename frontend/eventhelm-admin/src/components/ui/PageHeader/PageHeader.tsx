import type { ReactNode } from "react";
import "./PageHeader.css";

export default function PageHeader({
    eyebrow,
    title,
    description,
    actions,
}: {
    eyebrow?: string;
    title: string;
    description?: string;
    actions?: ReactNode;
}) {
    return (
        <header className="eh-page-header">
            <div>
                {eyebrow && <div>{eyebrow}</div>}
                <h1>{title}</h1>
                {description && <p>{description}</p>}
            </div>
            {actions && <aside>{actions}</aside>}
        </header>
    );
}
