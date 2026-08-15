"use client";

import { useTable } from "@tanstack/react-table";
import type { ClientStatus } from "@/api-flare/types.gen";
import { DataTable } from "@/ui/Table/DataTable";
import { createTableColumnHelper, features } from "@/ui/Table/tableFeatures";
import styles from "./ClientsTable.module.css";

const columnHelper = createTableColumnHelper<ClientStatus>();
const columns = columnHelper.columns([
	columnHelper.accessor("name", { header: "Name" }),
	columnHelper.accessor("org", {
		header: "Org",
		cell: (info) => info.getValue() ?? "—",
	}),
	columnHelper.accessor("connected", {
		header: "Status",
		cell: (info) => (
			<span
				className={`${styles.status} ${info.getValue() ? styles.connected : styles.disconnected}`}
			>
				{info.getValue() ? "Connected" : "Disconnected"}
			</span>
		),
	}),
	columnHelper.accessor("last_connect_time", {
		header: "Last connected",
		cell: (info) => {
			const value = info.getValue();
			return value ? new Date(value * 1000).toLocaleString() : "never";
		},
	}),
]);

type Props = {
	data: ClientStatus[];
};

export default function ClientsTable({ data }: Props) {
	const table = useTable({ features, columns, data });

	return <DataTable table={table} emptyMessage="No clients provisioned." />;
}
