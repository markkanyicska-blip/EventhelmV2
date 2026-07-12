import type {
    ButtonHTMLAttributes,
    ReactNode,
} from "react";
import "./Button.css";

type Props = ButtonHTMLAttributes<HTMLButtonElement> & {
    children: ReactNode;
    variant?: "primary" | "secondary" | "danger" | "ghost";
    loading?: boolean;
    icon?: ReactNode;
};

export default function Button({
    children,
    variant = "primary",
    loading = false,
    icon,
    className = "",
    disabled,
    type = "button",
    ...rest
}: Props) {
    return (
        <button
            type={type}
            className={`eh-button eh-button--${variant} ${className}`}
            disabled={disabled || loading}
            {...rest}
        >
            {loading ? (
                <span className="eh-button__spinner" />
            ) : (
                icon
            )}
            <span>{children}</span>
        </button>
    );
}
