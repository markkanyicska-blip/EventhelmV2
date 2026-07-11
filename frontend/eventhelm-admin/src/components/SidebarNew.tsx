import {
    BarChart3,
    CalendarDays,
    ChevronDown,
    Compass,
    Flag,
    LayoutDashboard,
    RadioTower,
    Sailboat,
    Settings,
    ShieldCheck,
    Users,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { NavLink } from "react-router-dom";

import "./SidebarNew.css";

type NavigationItem = {
    label: string;
    path: string;
    icon: LucideIcon;
};

const operationItems: NavigationItem[] = [
    {
        label: "Irányítópult",
        path: "/dashboard",
        icon: LayoutDashboard,
    },
    {
        label: "Rendezvények",
        path: "/events",
        icon: CalendarDays,
    },
    {
        label: "Rendezők",
        path: "/officials",
        icon: ShieldCheck,
    },
    {
        label: "Motorcsónakok",
        path: "/boats",
        icon: Sailboat,
    },
    {
        label: "Felhasználók",
        path: "/users",
        icon: Users,
    },
];

function SidebarNew() {
    return (
        <aside className="eh-sidebar">
            <div className="eh-sidebar__brand">
                <div
                    className="eh-sidebar__brand-mark"
                    aria-hidden="true"
                >
                    <Compass size={27} strokeWidth={1.6} />
                </div>

                <div className="eh-sidebar__brand-text">
                    <div className="eh-sidebar__brand-name">
                        EVENTHELM
                    </div>

                    <div className="eh-sidebar__brand-subtitle">
                        SAILING OPERATIONS
                    </div>
                </div>
            </div>

            <SidebarSectionLabel>
                OPERÁCIÓ
            </SidebarSectionLabel>

            <nav
                className="eh-sidebar__navigation"
                aria-label="Fő navigáció"
            >
                {operationItems.map((item) => (
                    <SidebarLink
                        key={item.path}
                        item={item}
                    />
                ))}
            </nav>

            <SidebarSectionLabel>
                VERSENYRENDEZÉS
            </SidebarSectionLabel>

            <div className="eh-sidebar__secondary-navigation">
                <DisabledNavigationItem
                    icon={Flag}
                    label="Versenyirányítás"
                    badge="Hamarosan"
                />

                <DisabledNavigationItem
                    icon={RadioTower}
                    label="Kommunikáció"
                />

                <DisabledNavigationItem
                    icon={BarChart3}
                    label="Jelentések"
                />

                <DisabledNavigationItem
                    icon={Settings}
                    label="Beállítások"
                />
            </div>

            <div
                className="eh-sidebar__decoration"
                aria-hidden="true"
            >
                <Compass size={180} strokeWidth={0.45} />
            </div>

            <div className="eh-sidebar__profile">
                <div className="eh-sidebar__avatar">
                    AU
                </div>

                <div className="eh-sidebar__profile-information">
                    <div className="eh-sidebar__profile-name">
                        Admin User
                    </div>

                    <div className="eh-sidebar__profile-role">
                        Adminisztrátor
                    </div>
                </div>

                <button
                    type="button"
                    className="eh-sidebar__profile-button"
                    aria-label="Profilmenü megnyitása"
                >
                    <ChevronDown size={17} />
                </button>
            </div>
        </aside>
    );
}

function SidebarSectionLabel({
    children,
}: {
    children: string;
}) {
    return (
        <div className="eh-sidebar__section-label">
            {children}
        </div>
    );
}

function SidebarLink({
    item,
}: {
    item: NavigationItem;
}) {
    const Icon = item.icon;

    return (
        <NavLink
            to={item.path}
            className={({ isActive }) =>
                [
                    "eh-sidebar__link",
                    isActive
                        ? "eh-sidebar__link--active"
                        : "",
                ]
                    .filter(Boolean)
                    .join(" ")
            }
        >
            <span className="eh-sidebar__link-indicator" />

            <span className="eh-sidebar__link-icon">
                <Icon size={20} strokeWidth={1.8} />
            </span>

            <span className="eh-sidebar__link-label">
                {item.label}
            </span>
        </NavLink>
    );
}

function DisabledNavigationItem({
    icon: Icon,
    label,
    badge,
}: {
    icon: LucideIcon;
    label: string;
    badge?: string;
}) {
    return (
        <div
            className="eh-sidebar__link eh-sidebar__link--disabled"
            aria-disabled="true"
        >
            <span className="eh-sidebar__link-icon">
                <Icon size={20} strokeWidth={1.7} />
            </span>

            <span className="eh-sidebar__link-label">
                {label}
            </span>

            {badge && (
                <span className="eh-sidebar__badge">
                    {badge}
                </span>
            )}
        </div>
    );
}

export default SidebarNew;