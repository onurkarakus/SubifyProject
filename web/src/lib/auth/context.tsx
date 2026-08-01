"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useRouter } from "next/navigation";
import { api, ApiError } from "@/lib/api/client";
import type { LoginResponse, SetupStatus } from "@/lib/api/types";
import { tokenStorage, type AuthUser } from "@/lib/auth/storage";
import { toast } from "sonner";

type AuthContextValue = {
  user: AuthUser | null;
  loading: boolean;
  isAuthenticated: boolean;
  isAdmin: boolean;
  isSuperAdmin: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (fullName: string, email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: (user: AuthUser) => void;
  checkSetup: () => Promise<SetupStatus | null>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    setUser(tokenStorage.getUser());
    setLoading(false);
  }, []);

  const applyLogin = useCallback((data: LoginResponse) => {
    const authUser: AuthUser = {
      id: data.user.id,
      email: data.user.email,
      fullName: data.user.fullName,
      locale: data.user.locale,
      roles: data.user.roles ?? [],
      isSetupComplete: data.user.isSetupComplete,
    };
    tokenStorage.setSession(data.accessToken, data.refreshToken, authUser);
    setUser(authUser);
  }, []);

  const login = useCallback(
    async (email: string, password: string) => {
      const data = await api.post<LoginResponse>(
        "/auth/login",
        { email, password },
        false,
      );
      applyLogin(data);
      if (data.user.isSetupComplete === false) {
        router.push("/setup");
      } else {
        router.push("/dashboard");
      }
    },
    [applyLogin, router],
  );

  const register = useCallback(
    async (fullName: string, email: string, password: string) => {
      await api.post(
        "/auth/register",
        { fullName, email, password },
        false,
      );
      await login(email, password);
    },
    [login],
  );

  const logout = useCallback(async () => {
    try {
      const refresh = tokenStorage.getRefresh();
      await api.post("/auth/logout", { refreshToken: refresh, allSessions: false });
    } catch {
      // still clear local session
    }
    tokenStorage.clear();
    setUser(null);
    router.push("/login");
  }, [router]);

  const refreshUser = useCallback((next: AuthUser) => {
    const access = tokenStorage.getAccess();
    const refresh = tokenStorage.getRefresh();
    if (access && refresh) {
      tokenStorage.setSession(access, refresh, next);
    }
    setUser(next);
  }, []);

  const checkSetup = useCallback(async () => {
    try {
      return await api.get<SetupStatus>("/setup/status", false);
    } catch (e) {
      if (e instanceof ApiError) toast.error(e.message);
      return null;
    }
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      loading,
      isAuthenticated: Boolean(user && tokenStorage.getAccess()),
      isAdmin:
        Boolean(user?.roles?.some((r) => r === "Admin" || r === "SuperAdmin")),
      isSuperAdmin: Boolean(user?.roles?.includes("SuperAdmin")),
      login,
      register,
      logout,
      refreshUser,
      checkSetup,
    }),
    [user, loading, login, register, logout, refreshUser, checkSetup],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
