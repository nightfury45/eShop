import { useEffect, useState } from "react";
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { ProductStatusBadge } from "@/components/products/ProductStatusBadge";
import { deriveStatus } from "@/components/products/status";
import type { AdminProduct, CatalogRef, ProductUpdateRequest } from "@/lib/api";

export interface ProductEditDrawerProps {
  product: AdminProduct | null;
  categories: CatalogRef[];
  brands: CatalogRef[];
  open: boolean;
  saving: boolean;
  error: string | null;
  onOpenChange: (open: boolean) => void;
  onSave: (id: number, body: ProductUpdateRequest) => void;
}

interface FormState {
  name: string;
  price: string;
  stock: string;
  categoryId: string;
  brandId: string;
  description: string;
}

interface FieldErrors {
  name?: string;
  price?: string;
  stock?: string;
}

function toForm(product: AdminProduct): FormState {
  return {
    name: product.name,
    price: String(product.price),
    stock: String(product.stock),
    categoryId: String(product.categoryId),
    brandId: String(product.brandId),
    description: product.description ?? "",
  };
}

function validate(form: FormState): FieldErrors {
  const errors: FieldErrors = {};
  if (!form.name.trim()) errors.name = "Product name is required.";
  const price = Number(form.price);
  if (form.price.trim() === "" || Number.isNaN(price) || price < 0) errors.price = "Enter a valid price.";
  const stock = Number(form.stock);
  if (form.stock.trim() === "" || !Number.isInteger(stock) || stock < 0) errors.stock = "Enter a valid stock count.";
  return errors;
}

const labelClass = "text-[12.5px] font-medium text-muted-foreground";
const fieldGroupClass = "flex flex-col gap-1.5";

export function ProductEditDrawer({
  product,
  categories,
  brands,
  open,
  saving,
  error,
  onOpenChange,
  onSave,
}: ProductEditDrawerProps) {
  const [form, setForm] = useState<FormState | null>(null);
  const [errors, setErrors] = useState<FieldErrors>({});

  useEffect(() => {
    setForm(product ? toForm(product) : null);
    setErrors({});
  }, [product]);

  if (!product || !form) {
    return <Sheet open={open} onOpenChange={onOpenChange} />;
  }

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) =>
    setForm((prev) => (prev ? { ...prev, [key]: value } : prev));

  const handleSave = () => {
    const found = validate(form);
    setErrors(found);
    if (Object.keys(found).length > 0) return;
    onSave(product.id, {
      name: form.name.trim(),
      price: Number(form.price),
      stock: Number(form.stock),
      categoryId: Number(form.categoryId),
      brandId: Number(form.brandId),
      description: form.description.trim() === "" ? null : form.description.trim(),
    });
  };

  const previewStatus = deriveStatus(Number(form.stock), product.restockThreshold);

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent
        side="right"
        className="flex w-[440px] max-w-[440px] flex-col gap-0 p-0 sm:max-w-[440px]"
        aria-describedby={undefined}
      >
        <div className="flex items-center justify-between border-b border-border px-6 pb-[18px] pt-5">
          <div className="flex flex-col gap-0.5">
            <SheetTitle className="text-[22px] tracking-tight">Edit product</SheetTitle>
            <p className="text-xs text-muted-foreground">{product.sku} · {product.brand}</p>
          </div>
        </div>

        <div className="flex flex-1 flex-col gap-[18px] overflow-y-auto px-6 py-[22px]">
          <div className="flex items-center gap-3.5">
            <div className="flex size-16 items-center justify-center rounded-[10px] bg-accent/10 font-mono text-[22px] text-accent">
              {product.name.charAt(0).toUpperCase()}
            </div>
            <div className="flex flex-col gap-1.5 text-sm">
              <p className="font-medium text-foreground">{product.name}</p>
              <p className="text-xs text-muted-foreground">{product.category}</p>
            </div>
          </div>

          <div className={fieldGroupClass}>
            <label htmlFor="product-name" className={labelClass}>Product name</label>
            <Input
              id="product-name"
              value={form.name}
              onChange={(e) => set("name", e.target.value)}
              aria-invalid={errors.name ? true : undefined}
            />
            {errors.name && <p className="text-xs text-destructive">{errors.name}</p>}
          </div>

          <div className={fieldGroupClass}>
            <label className={labelClass}>SKU</label>
            <Input value={product.sku} readOnly disabled className="font-mono" />
          </div>

          <div className="grid grid-cols-2 gap-3.5">
            <div className={fieldGroupClass}>
              <label className={labelClass}>Category</label>
              <Select value={form.categoryId} onValueChange={(v) => set("categoryId", v)}>
                <SelectTrigger className="h-10" aria-label="Category">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {categories.map((c) => (
                    <SelectItem key={c.id} value={String(c.id)}>{c.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className={fieldGroupClass}>
              <label className={labelClass}>Brand</label>
              <Select value={form.brandId} onValueChange={(v) => set("brandId", v)}>
                <SelectTrigger className="h-10" aria-label="Brand">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {brands.map((b) => (
                    <SelectItem key={b.id} value={String(b.id)}>{b.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3.5">
            <div className={fieldGroupClass}>
              <label htmlFor="product-price" className={labelClass}>Price (USD)</label>
              <Input
                id="product-price"
                inputMode="decimal"
                value={form.price}
                onChange={(e) => set("price", e.target.value)}
                className="font-mono"
                aria-invalid={errors.price ? true : undefined}
              />
              {errors.price && <p className="text-xs text-destructive">{errors.price}</p>}
            </div>
            <div className={fieldGroupClass}>
              <label htmlFor="product-stock" className={labelClass}>Stock on hand</label>
              <Input
                id="product-stock"
                inputMode="numeric"
                value={form.stock}
                onChange={(e) => set("stock", e.target.value)}
                className="font-mono"
                aria-invalid={errors.stock ? true : undefined}
              />
              {errors.stock && <p className="text-xs text-destructive">{errors.stock}</p>}
            </div>
          </div>

          <div className={fieldGroupClass}>
            <label htmlFor="product-description" className={labelClass}>Description</label>
            <textarea
              id="product-description"
              value={form.description}
              onChange={(e) => set("description", e.target.value)}
              className="flex min-h-[84px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
            />
          </div>

          <div className="flex items-center justify-between rounded-lg bg-secondary px-3.5 py-3">
            <div className="flex flex-col gap-0.5">
              <p className="text-sm font-medium text-foreground">Status</p>
              <p className="text-xs text-muted-foreground">Derived from stock on hand</p>
            </div>
            <ProductStatusBadge status={previewStatus} />
          </div>
        </div>

        <div className="flex items-center justify-end gap-2.5 border-t border-border px-6 py-4">
          {error && <p className="mr-auto text-xs text-destructive">{error}</p>}
          <Button variant="secondary" size="sm" onClick={() => onOpenChange(false)} disabled={saving}>
            Cancel
          </Button>
          <Button size="sm" onClick={handleSave} disabled={saving}>
            {saving ? "Saving…" : "Save changes"}
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
}
