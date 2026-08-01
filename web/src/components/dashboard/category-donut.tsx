"use client";

import { useI18n } from "@/lib/i18n/context";
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";

const FALLBACK_COLORS = [
  "#8B5CF6",
  "#3B82F6",
  "#14B8A6",
  "#22C55E",
  "#F59E0B",
  "#F97316",
  "#EC4899",
];

type Slice = {
  name: string;
  value: number;
  percentage: number;
  color: string;
};

export function CategoryDonut({
  data,
}: {
  data: {
    name: string;
    total: number;
    percentage: number;
    color?: string | null;
  }[];
}) {
  const { t } = useI18n();

  const slices: Slice[] = data.map((d, i) => ({
    name: d.name,
    value: d.total,
    percentage: d.percentage,
    color: d.color || FALLBACK_COLORS[i % FALLBACK_COLORS.length],
  }));

  if (!slices.length) {
    return (
      <div className="flex h-[200px] items-center justify-center text-sm text-muted">
        {t("empty")}
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center gap-4 sm:flex-row sm:items-center">
      <div className="h-[180px] w-[180px] shrink-0">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={slices}
              dataKey="value"
              nameKey="name"
              innerRadius={52}
              outerRadius={78}
              paddingAngle={2}
              stroke="transparent"
            >
              {slices.map((s) => (
                <Cell key={s.name} fill={s.color} />
              ))}
            </Pie>
            <Tooltip
              contentStyle={{
                background: "var(--surface)",
                border: "1px solid var(--border)",
                borderRadius: 12,
                fontSize: 12,
              }}
              formatter={(value, name, item) => {
                const p = (item?.payload as Slice | undefined)?.percentage;
                return [
                  `${Number(value ?? 0).toFixed(0)} (${p ?? 0}%)`,
                  String(name ?? ""),
                ];
              }}
            />
          </PieChart>
        </ResponsiveContainer>
      </div>
      <ul className="flex w-full flex-col gap-2 text-sm">
        {slices.map((s) => (
          <li key={s.name} className="flex items-center gap-2">
            <span
              className="h-2.5 w-2.5 shrink-0 rounded-full"
              style={{ background: s.color }}
            />
            <span className="min-w-0 flex-1 truncate text-muted">{s.name}</span>
            <span className="font-medium tabular-nums">{s.percentage}%</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
