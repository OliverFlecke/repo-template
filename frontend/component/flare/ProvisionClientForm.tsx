"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import {
	listClientsQueryKey,
	provisionClientMutation,
} from "@/api-flare/@tanstack/react-query.gen";
import { Button } from "@/ui/Button/Button";
import { Input } from "@/ui/Input/Input";
import styles from "./ProvisionClientForm.module.css";

function downloadBlob(blob: Blob, filename: string) {
	const url = URL.createObjectURL(blob);
	const a = document.createElement("a");
	a.href = url;
	a.download = filename;
	a.click();
	URL.revokeObjectURL(url);
}

export default function ProvisionClientForm() {
	const [name, setName] = useState("");
	const queryClient = useQueryClient();

	const {
		mutate: provisionClient,
		isPending,
		error,
		isSuccess,
	} = useMutation({
		...provisionClientMutation(),
		onSuccess: (blob, variables) => {
			downloadBlob(blob, `${variables.path.name}.zip`);
			setName("");
			queryClient.invalidateQueries({ queryKey: listClientsQueryKey() });
		},
	});

	return (
		<form
			className={styles.form}
			onSubmit={(e) => {
				e.preventDefault();
				if (!name) return;
				provisionClient({ path: { name } });
			}}
		>
			<Input
				type="text"
				value={name}
				onChange={(e) => setName(e.target.value)}
				placeholder="Client name, e.g. site-6"
				required
			/>
			<Button type="submit" disabled={!name || isPending}>
				{isPending ? "Provisioning..." : "Provision & download"}
			</Button>
			{error && (
				<p className={styles.error}>
					Failed to provision client - name may already be in use.
				</p>
			)}
			{isSuccess && <p className={styles.success}>Startup kit downloaded.</p>}
		</form>
	);
}
