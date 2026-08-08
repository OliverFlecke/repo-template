import { createColumnHelper, tableFeatures } from "@tanstack/react-table";
import type { RowData } from "@tanstack/react-table";

/**
 * Shared feature registration for every data table in the app, so all tables
 * are built against the same `TFeatures` type and stay consistent as the app
 * grows into features like sorting or pagination row models.
 */
export const features = tableFeatures({});

export function createTableColumnHelper<TData extends RowData>() {
	return createColumnHelper<typeof features, TData>();
}
