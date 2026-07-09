import { ReactNode } from "react";
import Sidebar from "./Sidebar";
import Header from "./Header";

type Props = {
    children: ReactNode;
};

export default function Layout({ children }: Props) {
    return (
        <div
            style={{
                display: "grid",
                gridTemplateColumns: "240px 1fr",
                minHeight: "100vh",
            }}
        >
            <Sidebar />

            <div
                style={{
                    background: "#f5f7fb",
                }}
            >
                <Header />

                <main
                    style={{
                        padding: 30,
                    }}
                >
                    {children}
                </main>
            </div>
        </div>
    );
}