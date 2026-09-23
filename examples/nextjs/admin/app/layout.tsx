import type { Metadata } from "next";
import type { ReactNode } from "react";
import "../styles/identity-access-admin.css";

export const metadata: Metadata = {
  title: "Identity Access Administration",
  description: "Server-first administration host for Generic Identity Access.",
};

export default function RootLayout({ children }: { readonly children: ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
