"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useState } from "react";
import {
	getApiV1OrganizationQueryKey,
	postApiV1OrganizationMutation,
} from "@/api/@tanstack/react-query.gen";
import { Button } from "@/ui/Button/Button";
import { FormField } from "@/ui/FormField/FormField";
import { Input } from "@/ui/Input/Input";
import { useOrganization } from "./OrganizationProvider";
import styles from "./OrganizationsPage.module.css";

export default function OrganizationsPage() {
	const {
		organizations,
		isLoading,
		currentOrganizationId,
		setCurrentOrganizationId,
	} = useOrganization();
	const [name, setName] = useState("");
	const queryClient = useQueryClient();

	const { mutate, isPending, error } = useMutation({
		...postApiV1OrganizationMutation(),
		onSuccess: (org) => {
			queryClient.invalidateQueries({
				queryKey: getApiV1OrganizationQueryKey(),
			});
			setCurrentOrganizationId(org.id);
			setName("");
		},
	});

	return (
		<main className={styles.main}>
			<h1>Organizations</h1>

			<form
				className={styles.form}
				onSubmit={(e) => {
					e.preventDefault();
					mutate({ body: { name } });
				}}
			>
				<FormField label="Name" required>
					<Input
						type="text"
						value={name}
						onChange={(e) => setName(e.target.value)}
						required
					/>
				</FormField>

				{error && (
					<p className={styles.error}>Failed to create organization.</p>
				)}

				<Button type="submit" disabled={isPending}>
					{isPending ? "Creating..." : "Create organization"}
				</Button>
			</form>

			{isLoading && <p>Loading...</p>}

			{!isLoading && organizations.length === 0 && (
				<p>You're not a member of any organization yet.</p>
			)}

			{organizations.length > 0 && (
				<ul className={styles.list}>
					{organizations.map((org) => (
						<li key={org.id} className={styles.item}>
							<Link
								href={`/organization/detail?id=${org.id}`}
								className={styles.name}
							>
								{org.name}
							</Link>
							<span className={styles.role}>{org.role}</span>
							{org.id === currentOrganizationId ? (
								<span className={styles.current}>Current</span>
							) : (
								<Button
									variant="outlined"
									color="secondary"
									size="sm"
									onClick={() => setCurrentOrganizationId(org.id)}
								>
									Switch
								</Button>
							)}
						</li>
					))}
				</ul>
			)}
		</main>
	);
}
