import { useEffect, useState } from "react";
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { InventoryStatusBadge } from "@/components/inventory/InventoryStatusBadge";
import { deriveStatus } from "@/components/products/status";
import type { InventoryItem, StockAdjustmentRequest } from "@/lib/api";

export interface AdjustStockDrawerProps {
  item: InventoryItem | null;
  open: boolean;
  saving: boolean;
  error: string | null;
  onOpenChange: (open: boolean) => void;
  onSave: (id: number, body: StockAdjustmentRequest) => void;
}

interface FieldErrors {
  onHand?: string;
  reason?: string;
}

const labelClass = "text-[12.5px] font-medium text-muted-foreground";
const fieldGroupClass = "flex flex-col gap-1.5";

function validate(onHand: string, reason: string): FieldErrors {
  const errors: FieldErrors = {};
  const quantity = Number(onHand);
  if (onHand.trim() === "" || !Number.isInteger(quantity) || quantity < 0) {
    errors.onHand = "Enter a valid on-hand quantity.";
  }
  if (!reason.trim()) errors.reason = "A reason for the adjustment is required.";
  return errors;
}

export function AdjustStockDrawer({ item, open, saving, error, onOpenChange, onSave }: AdjustStockDrawerProps) {
  const [onHand, setOnHand] = useState("");
  const [reason, setReason] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({});

  useEffect(() => {
    setOnHand(item ? String(item.onHand) : "");
    setReason("");
    setErrors({});
  }, [item]);

  if (!item) {
    return <Sheet open={open} onOpenChange={onOpenChange} />;
  }

  const handleSave = () => {
    const found = validate(onHand, reason);
    setErrors(found);
    if (Object.keys(found).length > 0) return;
    onSave(item.id, { newOnHand: Number(onHand), reason: reason.trim() });
  };

  const parsed = Number(onHand);
  const previewStatus = deriveStatus(Number.isNaN(parsed) ? 0 : parsed, item.reorderThreshold);
  const delta = Number.isNaN(parsed) ? 0 : parsed - item.onHand;

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent
        side="right"
        className="flex w-[440px] max-w-[440px] flex-col gap-0 p-0 sm:max-w-[440px]"
        aria-describedby={undefined}
      >
        <div className="flex items-center justify-between border-b border-border px-6 pb-[18px] pt-5">
          <div className="flex flex-col gap-0.5">
            <SheetTitle className="text-[22px] tracking-tight">Adjust stock</SheetTitle>
            <p className="text-xs text-muted-foreground">{item.sku}</p>
          </div>
        </div>

        <div className="flex flex-1 flex-col gap-[18px] overflow-y-auto px-6 py-[22px]">
          <div className="flex items-center gap-3.5">
            <div className="flex size-16 items-center justify-center rounded-[10px] bg-accent/10 font-mono text-[22px] text-accent">
              {item.name.charAt(0).toUpperCase()}
            </div>
            <div className="flex flex-col gap-1.5 text-sm">
              <p className="font-medium text-foreground">{item.name}</p>
              <p className="text-xs text-muted-foreground">
                Currently {item.onHand} on hand · reorder at {item.reorderThreshold}
              </p>
            </div>
          </div>

          <div className={fieldGroupClass}>
            <label htmlFor="adjust-onhand" className={labelClass}>New on-hand quantity</label>
            <Input
              id="adjust-onhand"
              inputMode="numeric"
              value={onHand}
              onChange={(e) => setOnHand(e.target.value)}
              className="font-mono"
              aria-invalid={errors.onHand ? true : undefined}
            />
            {errors.onHand
              ? <p className="text-xs text-destructive">{errors.onHand}</p>
              : delta !== 0 && (
                  <p className="text-xs text-muted-foreground">
                    {delta > 0 ? "+" : ""}{delta} vs current
                  </p>
                )}
          </div>

          <div className={fieldGroupClass}>
            <label htmlFor="adjust-reason" className={labelClass}>Reason</label>
            <Input
              id="adjust-reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="e.g. Cycle count correction"
              aria-invalid={errors.reason ? true : undefined}
            />
            {errors.reason && <p className="text-xs text-destructive">{errors.reason}</p>}
          </div>

          <div className="flex items-center justify-between rounded-lg bg-secondary px-3.5 py-3">
            <div className="flex flex-col gap-0.5">
              <p className="text-sm font-medium text-foreground">Resulting status</p>
              <p className="text-xs text-muted-foreground">Derived from on-hand vs reorder point</p>
            </div>
            <InventoryStatusBadge status={previewStatus} />
          </div>
        </div>

        <div className="flex items-center justify-end gap-2.5 border-t border-border px-6 py-4">
          {error && <p className="mr-auto text-xs text-destructive">{error}</p>}
          <Button variant="secondary" size="sm" onClick={() => onOpenChange(false)} disabled={saving}>
            Cancel
          </Button>
          <Button size="sm" onClick={handleSave} disabled={saving}>
            {saving ? "Saving…" : "Save adjustment"}
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
}
