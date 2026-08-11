"use client";

import { useTable } from "@tanstack/react-table";
import type { Organization } from "@/api/types.gen";
import { DataTable } from "@/ui/Table/DataTable";
import { createTableColumnHelper, features } from "@/ui/Table/tableFeatures";

const columnHelper = createTableColumnHelper<Organization>();
const columns = columnHelper.columns([
	columnHelper.accessor("name", { header: "Name" }),
]);

type Props = {
	data: Organization[];
	page: number;
	pageCount: number;
	sortDescending: boolean;
	onPageChange: (page: number) => void;
	onSortDescendingChange: (value: boolean) => void;
};

export default function OrganizationsTable({
	data,
	page,
	pageCount,
	sortDescending,
	onPageChange,
	onSortDescendingChange,
}: Props) {
	const table = useTable({ features, columns, data });

	return (
		<DataTable
			table={table}
			emptyMessage="No organizations found."
			sort={{
				columnId: "name",
				desc: sortDescending,
				onToggle: () => onSortDescendingChange(!sortDescending),
			}}
			pagination={{ page, pageCount, onPageChange }}
		/>
	);
}
