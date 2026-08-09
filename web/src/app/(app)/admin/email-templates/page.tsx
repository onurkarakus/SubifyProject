"use client";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PageLoader } from "@/components/ui/spinner";
import { api, ApiError } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/context";
import { useI18n } from "@/lib/i18n/context";
import { useRouter } from "next/navigation";
import { FormEvent, useCallback, useEffect, useState } from "react";
import { toast } from "sonner";

type EmailTemplate = {
  id: string;
  name: string;
  languageCode: string;
  subject: string;
  body: string;
};

type Preview = {
  subject: string;
  htmlBody: string;
};

/** 7.4 — SuperAdmin email template editor + preview/test-send. */
export default function AdminEmailTemplatesPage() {
  const { t } = useI18n();
  const { isSuperAdmin, loading: authLoading } = useAuth();
  const router = useRouter();
  const [list, setList] = useState<EmailTemplate[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [subject, setSubject] = useState("");
  const [body, setBody] = useState("");
  const [preview, setPreview] = useState<Preview | null>(null);
  const [testTo, setTestTo] = useState("");
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);

  const selected = list.find((x) => x.id === selectedId) ?? null;

  const load = useCallback(async () => {
    try {
      const res = await api.get<{ data: EmailTemplate[] }>("/admin/email-templates");
      setList(res.data ?? []);
      if (!selectedId && res.data?.length) {
        const first = res.data[0];
        setSelectedId(first.id);
        setSubject(first.subject);
        setBody(first.body);
      }
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : t("errorGeneric"));
    } finally {
      setLoading(false);
    }
  }, [selectedId, t]);

  useEffect(() => {
    if (!authLoading && !isSuperAdmin) {
      router.replace("/dashboard");
      return;
    }
    if (isSuperAdmin) void load();
  }, [authLoading, isSuperAdmin, router, load]);

  function selectTemplate(tpl: EmailTemplate) {
    setSelectedId(tpl.id);
    setSubject(tpl.subject);
    setBody(tpl.body);
    setPreview(null);
  }

  async function onSave(e: FormEvent) {
    e.preventDefault();
    if (!selectedId) return;
    setBusy(true);
    try {
      const updated = await api.put<EmailTemplate>(
        `/admin/email-templates/${selectedId}`,
        { subject, body },
      );
      setList((prev) =>
        prev.map((x) => (x.id === updated.id ? { ...x, ...updated } : x)),
      );
      toast.success(t("save"));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  async function onPreview() {
    if (!selectedId) return;
    setBusy(true);
    try {
      const res = await api.post<Preview>(
        `/admin/email-templates/${selectedId}/preview`,
        {},
      );
      setPreview(res);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  async function onTestSend() {
    if (!selectedId) return;
    setBusy(true);
    try {
      const bodyPayload =
        testTo.trim().length > 0 ? { toEmail: testTo.trim() } : {};
      await api.post(
        `/admin/email-templates/${selectedId}/test-send`,
        bodyPayload,
      );
      toast.success(t("emailTemplateTestSent"));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("errorGeneric"));
    } finally {
      setBusy(false);
    }
  }

  if (authLoading || loading) return <PageLoader />;

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <h1 className="text-2xl font-bold">{t("emailTemplates")}</h1>
      <p className="text-sm text-muted">{t("emailTemplatesHint")}</p>

      <div className="grid gap-6 lg:grid-cols-[240px_1fr]">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t("templates")}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-1 p-2">
            {list.map((tpl) => (
              <button
                key={tpl.id}
                type="button"
                onClick={() => selectTemplate(tpl)}
                className={`w-full rounded-lg px-3 py-2 text-left text-sm transition-colors ${
                  selectedId === tpl.id
                    ? "bg-primary/15 text-primary"
                    : "hover:bg-muted/40"
                }`}
              >
                <div className="font-medium">{tpl.name}</div>
                <div className="text-xs text-muted">{tpl.languageCode}</div>
              </button>
            ))}
            {list.length === 0 ? (
              <p className="p-3 text-xs text-muted">{t("empty")}</p>
            ) : null}
          </CardContent>
        </Card>

        {selected ? (
          <div className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">
                  {selected.name} · {selected.languageCode}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <form className="space-y-4" onSubmit={onSave}>
                  <div className="space-y-2">
                    <Label>{t("emailSubject")}</Label>
                    <Input
                      value={subject}
                      onChange={(e) => setSubject(e.target.value)}
                      required
                    />
                  </div>
                  <div className="space-y-2">
                    <Label>{t("emailBodyHtml")}</Label>
                    <textarea
                      className="min-h-[220px] w-full rounded-lg border border-border bg-surface px-3 py-2 font-mono text-xs"
                      value={body}
                      onChange={(e) => setBody(e.target.value)}
                      required
                    />
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Button type="submit" disabled={busy}>
                      {t("save")}
                    </Button>
                    <Button
                      type="button"
                      variant="secondary"
                      disabled={busy}
                      onClick={() => void onPreview()}
                    >
                      {t("preview")}
                    </Button>
                  </div>
                </form>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-base">{t("testSend")}</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="space-y-1">
                  <Label>{t("testSmtpTo")}</Label>
                  <Input
                    type="email"
                    value={testTo}
                    onChange={(e) => setTestTo(e.target.value)}
                    placeholder="you@example.com"
                  />
                </div>
                <Button
                  type="button"
                  variant="secondary"
                  disabled={busy}
                  onClick={() => void onTestSend()}
                >
                  {t("testSend")}
                </Button>
              </CardContent>
            </Card>

            {preview ? (
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">{t("preview")}</CardTitle>
                </CardHeader>
                <CardContent className="space-y-2">
                  <p className="text-sm font-medium">{preview.subject}</p>
                  <div
                    className="max-h-[360px] overflow-auto rounded-lg border border-border bg-white p-4 text-black"
                    dangerouslySetInnerHTML={{ __html: preview.htmlBody }}
                  />
                </CardContent>
              </Card>
            ) : null}
          </div>
        ) : null}
      </div>
    </div>
  );
}
