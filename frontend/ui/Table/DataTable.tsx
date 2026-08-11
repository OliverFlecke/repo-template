import type { ReactTable, RowData } from "@tanstack/react-table";
import { Button } from "@/ui/Button/Button";
import { cx } from "@/ui/util/cx";
import styles from "./DataTable.module.css";
import type { features } from "./tableFeatures";

export interface DataTableSort {
	/** Id of the currently sorted column's header that should be clickable. */
	columnId: string;
	desc: boolean;
	onToggle: () => void;
}

export interface DataTablePagination {
	page: number;
	pageCount: number;
	onPageChange: (page: number) => void;
}

export interface DataTableProps<TData extends RowData> {
	table: ReactTable<typeof features, TData>;
	emptyMessage?: string;
	sort?: DataTableSort;
	pagination?: DataTablePagination;
	className?: string;
}

export function DataTable<TData extends RowData>({
	table,
	emptyMessage = "No results found.",
	sort,
	pagination,
	className,
}: DataTableProps<TData>) {
	const rows = table.getRowModel().rows;
	const columnCount = table.getAllLeafColumns().length;

	return (
		<div className={cx(styles.wrapper, className)}>
			<table className={styles.table}>
				<thead>
					{table.getHeaderGroups().map((headerGroup) => (
						<tr key={headerGroup.id}>
							{headerGroup.headers.map((header) => (
								<th key={header.id}>
									{header.isPlaceholder ? null : sort &&
										sort.columnId === header.column.id ? (
										<Button
											variant="text"
											color="secondary"
											size="sm"
											className={styles.sortButton}
											onClick={sort.onToggle}
										>
											<table.FlexRender header={header} />
											{sort.desc ? " ▼" : " ▲"}
										</Button>
									) : (
										<table.FlexRender header={header} />
									)}
								</th>
							))}
						</tr>
					))}
				</thead>
				<tbody>
					{rows.length === 0 ? (
						<tr>
							<td colSpan={columnCount} className={styles.empty}>
								{emptyMessage}
							</td>
						</tr>
					) : (
						rows.map((row) => (
							<tr key={row.id}>
								{row.getAllCells().map((cell) => (
									<td key={cell.id}>
										<table.FlexRender cell={cell} />
									</td>
								))}
							</tr>
						))
					)}
				</tbody>
			</table>

			{pagination && (
				<div className={styles.pagination}>
					<Button
						variant="outlined"
						size="sm"
						disabled={pagination.page <= 1}
						onClick={() => pagination.onPageChange(pagination.page - 1)}
					>
						Previous
					</Button>
					<span>
						Page {pagination.page} of {Math.max(pagination.pageCount, 1)}
					</span>
					<Button
						variant="outlined"
						size="sm"
						disabled={pagination.page >= pagination.pageCount}
						onClick={() => pagination.onPageChange(pagination.page + 1)}
					>
						Next
					</Button>
				</div>
			)}
		</div>
	);
}
