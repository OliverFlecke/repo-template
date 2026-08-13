"use client";

import { useQuery } from "@tanstack/react-query";
import { useRef, useState } from "react";
import { getOrganizationsAdminOptions } from "@/api/@tanstack/react-query.gen";
import { Button } from "@/ui/Button/Button";
import CreateOrganizationDialog, {
	type CreateOrganizationDialogHandle,
} from "./CreateOrganizationDialog";
import styles from "./OrganizationsPage.module.css";
import OrganizationsTable from "./OrganizationsTable";
import SearchBox from "./SearchBox";

const PAGE_SIZE = 20;

export default function OrganizationsPage() {
	const [page, setPage] = useState(1);
	const [search, setSearch] = useState("");
	const [sortDescending, setSortDescending] = useState(false);
	const dialogRef = useRef<CreateOrganizationDialogHandle>(null);

	const { data, isLoading, isError } = useQuery({
		...getOrganizationsAdminOptions({
			query: {
				page,
				page_size: PAGE_SIZE,
				desc: sortDescending,
				search: search || undefined,
			},
		}),
	});

	function handleSearchChange(value: string) {
		setSearch(value);
		setPage(1);
	}

	function handleSortDescendingChange(value: boolean) {
		setSortDescending(value);
		setPage(1);
	}

	return (
		<main className={styles.main}>
			<div className={styles.toolbar}>
				<h1>Organizations</h1>
				<SearchBox
					value={search}
					onChange={handleSearchChange}
					placeholder="Search organizations..."
				/>
				<Button onClick={() => dialogRef.current?.open()}>
					Create organization
				</Button>
			</div>

			{isError && (
				<p className={styles.error}>
					You don't have access to manage organizations.
				</p>
			)}
			{isLoading && <p>Loading...</p>}

			{data && (
				<OrganizationsTable
					data={data.data}
					page={Number(data.page)}
					pageCount={Number(data.pageCount)}
					sortDescending={sortDescending}
					onPageChange={setPage}
					onSortDescendingChange={handleSortDescendingChange}
				/>
			)}

			<CreateOrganizationDialog ref={dialogRef} />
		</main>
	);
}
