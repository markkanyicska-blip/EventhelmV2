import { Menu } from "lucide-react";
import "./Topbar.css";

export default function Topbar() {
    return (
        <header className="eh-topbar">
            <div>
                <button type="button" aria-label="Menü">
                    <Menu size={21} />
                </button>
                <span />
                <strong>EventHelm operációs rendszer</strong>
            </div>
            <aside>
                <i />
                Fejlesztői környezet
            </aside>
        </header>
    );
}
