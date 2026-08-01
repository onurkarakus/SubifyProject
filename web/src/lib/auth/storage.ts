/**
 * Auth token storage (10.1.6)
 *
 * Self-host practical choice: sessionStorage for access + refresh tokens.
 * - Cleared when the tab closes (better than permanent localStorage)
 * - No BFF/httpOnly cookie layer required for single-origin or CORS setups
 * - XSS still risky: never inject untrusted HTML; keep CSP tight in production
 *
 * Alternatives: memory-only access + refresh in httpOnly cookie via BFF.
 */

const ACCESS = "subify.accessToken";
const REFRESH = "subify.refreshToken";
const USER = "subify.user";

export type AuthUser = {
  id: string;
  email: string;
  fullName: string;
  locale: string;
  roles: string[];
  isSetupComplete?: boolean | null;
};

function canUseStorage() {
  return typeof window !== "undefined" && typeof sessionStorage !== "undefined";
}

export const tokenStorage = {
  getAccess(): string | null {
    if (!canUseStorage()) return null;
    return sessionStorage.getItem(ACCESS);
  },
  getRefresh(): string | null {
    if (!canUseStorage()) return null;
    return sessionStorage.getItem(REFRESH);
  },
  getUser(): AuthUser | null {
    if (!canUseStorage()) return null;
    const raw = sessionStorage.getItem(USER);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      return null;
    }
  },
  setSession(accessToken: string, refreshToken: string, user: AuthUser) {
    if (!canUseStorage()) return;
    sessionStorage.setItem(ACCESS, accessToken);
    sessionStorage.setItem(REFRESH, refreshToken);
    sessionStorage.setItem(USER, JSON.stringify(user));
  },
  setAccess(accessToken: string) {
    if (!canUseStorage()) return;
    sessionStorage.setItem(ACCESS, accessToken);
  },
  clear() {
    if (!canUseStorage()) return;
    sessionStorage.removeItem(ACCESS);
    sessionStorage.removeItem(REFRESH);
    sessionStorage.removeItem(USER);
  },
  isAuthenticated(): boolean {
    return Boolean(this.getAccess());
  },
};
