import type { Story } from "@ladle/react";
import { useState } from "react";
import { useTable } from "@tanstack/react-table";
import { DataTable } from "./DataTable";
import { createTableColumnHelper, features } from "./tableFeatures";

type Person = { id: string; name: string; role: string };

const people: Person[] = [
	{ id: "1", name: "Ada Lovelace", role: "Admin" },
	{ id: "2", name: "Grace Hopper", role: "Member" },
	{ id: "3", name: "Alan Turing", role: "Member" },
];

const columnHelper = createTableColumnHelper<Person>();
const columns = columnHelper.columns([
	columnHelper.accessor("name", { header: "Name" }),
	columnHelper.accessor("role", { header: "Role" }),
]);

export const Gallery: Story = () => {
	const [desc, setDesc] = useState(false);
	const [page, setPage] = useState(1);

	const table = useTable({ features, columns, data: people });

	return (
		<div style={{ maxWidth: 480 }}>
			<DataTable
				table={table}
				sort={{ columnId: "name", desc, onToggle: () => setDesc((d) => !d) }}
				pagination={{ page, pageCount: 3, onPageChange: setPage }}
			/>
		</div>
	);
};

export const Empty: Story = () => {
	const table = useTable({ features, columns, data: [] });

	return (
		<div style={{ maxWidth: 480 }}>
			<DataTable table={table} emptyMessage="No people found." />
		</div>
	);
};
