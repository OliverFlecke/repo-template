"use client";

import { useTable } from "@tanstack/react-table";
import type { ClientInfo } from "@/api-flare/types.gen";
import { DataTable } from "@/ui/Table/DataTable";
import { createTableColumnHelper, features } from "@/ui/Table/tableFeatures";

const columnHelper = createTableColumnHelper<ClientInfo>();
const columns = columnHelper.columns([
	columnHelper.accessor("name", { header: "Name" }),
	columnHelper.accessor("last_connect_time", {
		header: "Last connected",
		cell: (info) => {
			const value = info.getValue();
			return value === null ? "unknown" : new Date(value * 1000).toLocaleString();
		},
	}),
]);

type Props = {
	data: ClientInfo[];
};

export default function ClientsTable({ data }: Props) {
	const table = useTable({ features, columns, data });

	return <DataTable table={table} emptyMessage="No clients connected." />;
}
