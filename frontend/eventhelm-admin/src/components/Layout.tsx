import type { ReactNode } from "react";

type LayoutProps = {
    children: ReactNode;
};

function Layout({ children }: LayoutProps) {
    return <>{children}</>;
}

export default Layout;