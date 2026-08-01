"use client";

import { useI18n } from "@/lib/i18n/context";
import { formatMoney } from "@/lib/utils";
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

type Point = { month: string; total: number; label: string };

export function SpendTrendChart({
  data,
  currency,
}: {
  data: { month: string; total: number }[];
  currency: string;
}) {
  const { t, locale } = useI18n();

  const chartData: Point[] = data.map((d) => {
    // month may be "2026-01" or display string
    let label = d.month;
    const m = /^(\d{4})-(\d{2})$/.exec(d.month);
    if (m) {
      const date = new Date(Number(m[1]), Number(m[2]) - 1, 1);
      label = date.toLocaleDateString(locale === "tr" ? "tr-TR" : "en-US", {
        month: "short",
      });
    }
    return { month: d.month, total: d.total, label };
  });

  if (!chartData.length) {
    return (
      <div className="flex h-[240px] items-center justify-center text-sm text-muted">
        {t("empty")}
      </div>
    );
  }

  return (
    <div className="h-[260px] w-full">
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart
          data={chartData}
          margin={{ top: 12, right: 12, left: 0, bottom: 0 }}
        >
          <defs>
            <linearGradient id="spendFill" x1="0" y1="0" x2="0" y2="1">
              <stop
                offset="0%"
                stopColor="var(--primary)"
                stopOpacity={0.45}
              />
              <stop
                offset="100%"
                stopColor="var(--primary)"
                stopOpacity={0.02}
              />
            </linearGradient>
          </defs>
          <CartesianGrid
            stroke="var(--border)"
            strokeDasharray="3 6"
            vertical={false}
          />
          <XAxis
            dataKey="label"
            tick={{ fill: "var(--muted)", fontSize: 12 }}
            axisLine={false}
            tickLine={false}
          />
          <YAxis
            tick={{ fill: "var(--muted)", fontSize: 11 }}
            axisLine={false}
            tickLine={false}
            width={48}
            tickFormatter={(v: number) =>
              v >= 1000 ? `${Math.round(v / 1000)}k` : String(Math.round(v))
            }
          />
          <Tooltip
            contentStyle={{
              background: "var(--surface)",
              border: "1px solid var(--border)",
              borderRadius: 12,
              fontSize: 12,
              color: "var(--foreground)",
            }}
            formatter={(value) => [
              formatMoney(Number(value ?? 0), currency, locale),
              t("monthlyTotal"),
            ]}
            labelFormatter={(_, payload) =>
              (payload?.[0]?.payload as Point | undefined)?.label ?? ""
            }
          />
          <Area
            type="monotone"
            dataKey="total"
            stroke="var(--primary)"
            strokeWidth={2.5}
            fill="url(#spendFill)"
            dot={{
              r: 4,
              fill: "var(--surface)",
              stroke: "var(--primary)",
              strokeWidth: 2,
            }}
            activeDot={{ r: 6, fill: "var(--primary)" }}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}
