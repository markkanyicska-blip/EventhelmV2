import type { LucideIcon } from "lucide-react";

import "./EventTabBar.css";

export type EventTabItem<T extends string> = {
    id: T;
    label: string;
    icon: LucideIcon;
};

type EventTabBarProps<T extends string> = {
    items: readonly EventTabItem<T>[];
    activeTab: T;
    onChange: (tab: T) => void;
    ariaLabel?: string;
};

export default function EventTabBar<T extends string>({
    items,
    activeTab,
    onChange,
    ariaLabel = "Rendezvény adatlap navigáció",
}: EventTabBarProps<T>) {
    return (
        <nav
            className="eh-event-tabs"
            aria-label={ariaLabel}
        >
            <div className="eh-event-tabs__scroll">
                {items.map((item) => {
                    const Icon = item.icon;
                    const active = item.id === activeTab;

                    return (
                        <button
                            key={item.id}
                            type="button"
                            className={
                                active
                                    ? "eh-event-tabs__item eh-event-tabs__item--active"
                                    : "eh-event-tabs__item"
                            }
                            aria-current={
                                active ? "page" : undefined
                            }
                            onClick={() =>
                                onChange(item.id)
                            }
                        >
                            <Icon
                                className="eh-event-tabs__icon"
                                size={21}
                                strokeWidth={1.9}
                                aria-hidden="true"
                            />

                            <span>
                                {item.label}
                            </span>
                        </button>
                    );
                })}
            </div>
        </nav>
    );
}
