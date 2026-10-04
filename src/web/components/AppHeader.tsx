"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { clearSession, loadSession } from "@/lib/session";
import type { Session } from "@/lib/types";

export default function AppHeader() {
  const router = useRouter();
  const pathname = usePathname();
  const [session, setSession] = useState<Session | null>(null);

  useEffect(() => {
    setSession(loadSession());
  }, [pathname]);

  function logout() {
    clearSession();
    router.push("/login");
  }

  return (
    <header className="topbar">
      <div className="brand">
        <span className="brand-mark">Plant Ops</span>
        <span className="brand-sub">Manufacturing portal</span>
      </div>
      {session ? (
        <nav className="nav">
          <Link href="/" className={pathname === "/" ? "active" : undefined}>Dashboard</Link>
          <Link href="/alerts" className={pathname.startsWith("/alerts") ? "active" : undefined}>Alerts</Link>
          <span className="who">
            {session.username} · {session.role}
          </span>
          <button type="button" className="linkish" onClick={logout}>
            Log out
          </button>
        </nav>
      ) : null}
    </header>
  );
}
