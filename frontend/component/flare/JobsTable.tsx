"use client";

import { useTable } from "@tanstack/react-table";
import type { Job } from "@/api-flare/types.gen";
import { DataTable } from "@/ui/Table/DataTable";
import { createTableColumnHelper, features } from "@/ui/Table/tableFeatures";
import styles from "./JobsTable.module.css";

function statusVariant(status: string | null | undefined) {
	if (!status) return styles.neutral;
	if (status === "FINISHED:COMPLETED") return styles.success;
	if (status.startsWith("FINISHED")) return styles.danger;
	if (status === "RUNNING") return styles.active;
	return styles.neutral;
}

const columnHelper = createTableColumnHelper<Job>();
const columns = columnHelper.columns([
	columnHelper.accessor("name", {
		header: "Job",
		cell: (info) => info.row.original.name ?? info.row.original.job_id,
	}),
	columnHelper.accessor("status", {
		header: "Status",
		cell: (info) => (
			<span className={`${styles.status} ${statusVariant(info.getValue())}`}>
				{info.getValue() ?? "unknown"}
			</span>
		),
	}),
	columnHelper.accessor("submitter_name", {
		header: "Submitted by",
		cell: (info) => info.getValue() ?? "—",
	}),
	columnHelper.accessor("submit_time_iso", {
		header: "Submitted at",
		cell: (info) => {
			const value = info.getValue();
			return value ? new Date(value).toLocaleString() : "—";
		},
	}),
	columnHelper.accessor("duration", {
		header: "Duration",
		cell: (info) => info.getValue() ?? "—",
	}),
]);

type Props = {
	data: Job[];
	emptyMessage: string;
};

export default function JobsTable({ data, emptyMessage }: Props) {
	const table = useTable({ features, columns, data });

	return <DataTable table={table} emptyMessage={emptyMessage} />;
}
