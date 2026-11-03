import { useEffect } from "react";
import type { ReactNode } from "react";
import { X } from "lucide-react";
import "./Modal.css";

export default function Modal({
    open,
    title,
    children,
    footer,
    onClose,
}: {
    open: boolean;
    title: string;
    children: ReactNode;
    footer?: ReactNode;
    onClose: () => void;
}) {
    useEffect(() => {
        if (!open) return;
        const handler = (event: KeyboardEvent) => {
            if (event.key === "Escape") onClose();
        };
        window.addEventListener("keydown", handler);
        return () => window.removeEventListener("keydown", handler);
    }, [open, onClose]);

    if (!open) return null;

    return (
        <div
            className="eh-modal-backdrop"
            onMouseDown={(event) => {
                if (event.target === event.currentTarget) onClose();
            }}
        >
            <section className="eh-modal" role="dialog" aria-modal="true">
                <header className="eh-modal__header">
                    <h2>{title}</h2>
                    <button type="button" onClick={onClose}>
                        <X size={19} />
                    </button>
                </header>
                <div className="eh-modal__content">{children}</div>
                {footer && <footer className="eh-modal__footer">{footer}</footer>}
            </section>
        </div>
    );
}
