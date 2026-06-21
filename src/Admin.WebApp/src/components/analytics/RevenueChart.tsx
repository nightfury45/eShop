import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import type { RevenuePoint } from "@/lib/api";
import { formatCurrency, formatShortDate } from "@/lib/format";

export function RevenueChart({ data }: { data: RevenuePoint[] }) {
  return (
    <div className="h-72 w-full" data-testid="revenue-chart">
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: 8 }}>
          <CartesianGrid vertical={false} stroke="hsl(var(--border))" />
          <XAxis
            dataKey="date"
            tickFormatter={formatShortDate}
            tickLine={false}
            axisLine={false}
            minTickGap={32}
            tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
          />
          <YAxis
            width={56}
            tickFormatter={(v: number) => formatCurrency(v)}
            tickLine={false}
            axisLine={false}
            tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
          />
          <Tooltip
            formatter={(value: number) => formatCurrency(value)}
            labelFormatter={(label: string) => formatShortDate(label)}
            contentStyle={{ fontSize: 12, borderRadius: 8, borderColor: "hsl(var(--border))" }}
          />
          <Line
            type="monotone"
            dataKey="previous"
            name="Prev period"
            stroke="hsl(var(--muted-foreground))"
            strokeDasharray="4 4"
            strokeWidth={1.5}
            dot={false}
          />
          <Line
            type="monotone"
            dataKey="current"
            name="This period"
            stroke="hsl(var(--primary))"
            strokeWidth={2}
            dot={false}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
