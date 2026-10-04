import type { Metadata } from "next";
import AppHeader from "@/components/AppHeader";
import "./globals.css";

export const metadata: Metadata = {
  title: "Plant Ops",
  description: "Digital manufacturing operations portal",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <AppHeader />
        <main className="page">{children}</main>
      </body>
    </html>
  );
}
