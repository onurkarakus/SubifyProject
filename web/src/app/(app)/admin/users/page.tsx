"use client";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PasswordInput } from "@/components/ui/password-input";
import { PageLoader } from "@/components/ui/spinner";
import { api, ApiError } from "@/lib/api/client";
import type { AdminUser } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/context";
import { useI18n } from "@/lib/i18n/context";
import { formatDate } from "@/lib/utils";
import { useRouter } from "next/navigation";
import { FormEvent, useCallback, useEffect, useState } from "react";
import { toast } from "sonner";

export default function AdminUsersPage() {
  const { t, locale } = useI18n();
  const { isAdmin, isSuperAdmin, loading: authLoading } = useAuth();
  const router = useRouter();
  const [loading, setLoading] = useState(true);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [email, setEmail] = useState("");
  const [fullName, setFullName] = useState("");
  const [password, setPassword] = useState("");
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteUrl, setInviteUrl] = useState<string | null>(null);
  const [inviteEmailSent, setInviteEmailSent] = useState<boolean | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await api.get<{ data: AdminUser[] }>(
        "/admin/users?page=1&pageSize=50",
      );
      setUsers(res.data ?? []);
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : t("errorGeneric"));
    } finally {
      setLoading(false);
    }
  }, [t]);

  useEffect(() => {
    if (!authLoading && !isAdmin) {
      router.replace("/dashboard");
      return;
    }
    if (isAdmin) void load();
  }, [authLoading, isAdmin, load, router]);

  async function createUser(e: FormEvent) {
    e.preventDefault();
    try {
      await api.post("/admin/users", {
        email,
        fullName,
        password,
        role: "User",
      });
      toast.success(t("createUser"));
      setEmail("");
      setFullName("");
      setPassword("");
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    }
  }

  async function sendInvite(e: FormEvent) {
    e.preventDefault();
    try {
      const res = await api.post<{
        inviteUrl: string;
        token: string;
        emailSent?: boolean;
      }>("/admin/invites", { email: inviteEmail, expiryDays: 7 });
      setInviteUrl(res.inviteUrl);
      setInviteEmailSent(!!res.emailSent);
      toast.success(
        res.emailSent ? t("inviteEmailSent") : t("inviteEmailNotSent"),
      );
      setInviteEmail("");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    }
  }

  async function resetPassword(id: string) {
    const pwd = prompt(t("newPassword"));
    if (!pwd) return;
    try {
      await api.post(`/admin/users/${id}/reset-password`, {
        newPassword: pwd,
      });
      toast.success(t("resetPassword"));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    }
  }

  if (authLoading || loading) return <PageLoader />;

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <h1 className="text-2xl font-bold">{t("adminUsers")}</h1>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t("createUser")}</CardTitle>
          </CardHeader>
          <CardContent>
            <form className="space-y-3" onSubmit={createUser}>
              <div className="space-y-1">
                <Label>{t("fullName")}</Label>
                <Input
                  required
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                />
              </div>
              <div className="space-y-1">
                <Label>{t("email")}</Label>
                <Input
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
              </div>
              <div className="space-y-1">
                <Label>{t("password")}</Label>
                <PasswordInput
                  required
                  minLength={8}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  showLabel={t("showPassword")}
                  hideLabel={t("hidePassword")}
                />
              </div>
              <Button type="submit">{t("createUser")}</Button>
            </form>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t("invite")}</CardTitle>
          </CardHeader>
          <CardContent>
            <form className="space-y-3" onSubmit={sendInvite}>
              <div className="space-y-1">
                <Label>{t("email")}</Label>
                <Input
                  type="email"
                  required
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                />
              </div>
              <Button type="submit">{t("invite")}</Button>
            </form>
            {inviteUrl ? (
              <div className="mt-3 space-y-1">
                <p className="text-xs text-muted">
                  {inviteEmailSent
                    ? t("inviteEmailSent")
                    : t("inviteEmailNotSent")}
                </p>
                <p className="break-all text-xs text-muted">Link: {inviteUrl}</p>
              </div>
            ) : null}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardContent className="overflow-x-auto p-0">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-border bg-muted/20">
              <tr>
                <th className="px-4 py-3 font-medium">{t("fullName")}</th>
                <th className="px-4 py-3 font-medium">{t("email")}</th>
                <th className="px-4 py-3 font-medium">{t("role")}</th>
                <th className="px-4 py-3 font-medium">Subs</th>
                <th className="px-4 py-3 font-medium" />
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.id} className="border-b border-border">
                  <td className="px-4 py-3">
                    {u.fullName}
                    <div className="text-xs text-muted">
                      {formatDate(u.createdAt, locale)}
                    </div>
                  </td>
                  <td className="px-4 py-3">{u.email}</td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap gap-1">
                      {u.roles.map((r) => (
                        <Badge key={r}>{r}</Badge>
                      ))}
                      {u.isDisabled ? (
                        <Badge variant="danger">{t("disabled")}</Badge>
                      ) : null}
                      {u.isLockedOut ? (
                        <Badge variant="warning">{t("locked")}</Badge>
                      ) : null}
                    </div>
                  </td>
                  <td className="px-4 py-3">{u.activeSubscriptionCount}</td>
                  <td className="px-4 py-3 text-right">
                    {isSuperAdmin ? (
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => resetPassword(u.id)}
                      >
                        {t("resetPassword")}
                      </Button>
                    ) : null}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </CardContent>
      </Card>
    </div>
  );
}
