"use client";

import {
	type ColumnDef,
	flexRender,
	getCoreRowModel,
	useReactTable,
} from "@tanstack/react-table";
import type { Organization } from "@/api/types.gen";
import styles from "./OrganizationsTable.module.css";

const columns: ColumnDef<Organization>[] = [
	{
		accessorKey: "name",
		header: "Name",
	},
];

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
	const table = useReactTable({
		data,
		columns,
		manualPagination: true,
		pageCount,
		getCoreRowModel: getCoreRowModel(),
	});

	return (
		<div>
			<table className={styles.table}>
				<thead>
					{table.getHeaderGroups().map((headerGroup) => (
						<tr key={headerGroup.id}>
							{headerGroup.headers.map((header) => (
								<th key={header.id}>
									<button
										type="button"
										onClick={() => onSortDescendingChange(!sortDescending)}
									>
										{flexRender(
											header.column.columnDef.header,
											header.getContext(),
										)}
										{sortDescending ? " ▼" : " ▲"}
									</button>
								</th>
							))}
						</tr>
					))}
				</thead>
				<tbody>
					{table.getRowModel().rows.map((row) => (
						<tr key={row.id}>
							{row.getVisibleCells().map((cell) => (
								<td key={cell.id}>
									{flexRender(cell.column.columnDef.cell, cell.getContext())}
								</td>
							))}
						</tr>
					))}
					{data.length === 0 && (
						<tr>
							<td colSpan={columns.length} className={styles.empty}>
								No organizations found.
							</td>
						</tr>
					)}
				</tbody>
			</table>

			<div className={styles.pagination}>
				<button
					type="button"
					disabled={page <= 1}
					onClick={() => onPageChange(page - 1)}
				>
					Previous
				</button>
				<span>
					Page {page} of {Math.max(pageCount, 1)}
				</span>
				<button
					type="button"
					disabled={page >= pageCount}
					onClick={() => onPageChange(page + 1)}
				>
					Next
				</button>
			</div>
		</div>
	);
}
