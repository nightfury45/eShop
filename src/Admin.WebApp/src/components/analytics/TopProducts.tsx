import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import type { ProductRow } from "@/lib/api";
import { formatCurrency, formatNumber } from "@/lib/format";

export function TopProducts({ rows }: { rows: ProductRow[] }) {
  if (rows.length === 0) {
    return <p className="px-6 pb-6 text-sm text-muted-foreground">No product sales in this period.</p>;
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Product</TableHead>
          <TableHead>Category</TableHead>
          <TableHead className="text-right">Units</TableHead>
          <TableHead className="text-right">Revenue</TableHead>
          <TableHead className="text-right">Share</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {rows.map((row) => (
          <TableRow key={row.productId}>
            <TableCell className="font-medium text-foreground">{row.name}</TableCell>
            <TableCell className="text-muted-foreground">{row.category}</TableCell>
            <TableCell className="text-right tabular-nums">{formatNumber(row.units)}</TableCell>
            <TableCell className="text-right tabular-nums">{formatCurrency(row.revenue)}</TableCell>
            <TableCell className="text-right tabular-nums text-muted-foreground">{row.sharePercent.toFixed(1)}%</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
