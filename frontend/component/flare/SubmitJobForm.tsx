"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import {
	listJobDefinitionsOptions,
	listJobsQueryKey,
	submitJobMutation,
} from "@/api-flare/@tanstack/react-query.gen";
import { Button } from "@/ui/Button/Button";
import { Select } from "@/ui/Select/Select";
import styles from "./SubmitJobForm.module.css";

export default function SubmitJobForm() {
	const [selected, setSelected] = useState("");
	const queryClient = useQueryClient();

	const { data: jobDefinitions, isLoading } = useQuery(
		listJobDefinitionsOptions(),
	);

	const {
		mutate: submitJob,
		isPending,
		error,
	} = useMutation({
		...submitJobMutation(),
		onSuccess: () => {
			setSelected("");
			queryClient.invalidateQueries({ queryKey: listJobsQueryKey() });
		},
	});

	return (
		<form
			className={styles.form}
			onSubmit={(e) => {
				e.preventDefault();
				if (!selected) return;
				submitJob({ path: { job_name: selected } });
			}}
		>
			<Select
				value={selected}
				onChange={(e) => setSelected(e.target.value)}
				disabled={isLoading || jobDefinitions?.length === 0}
				required
			>
				<option value="" disabled>
					{jobDefinitions?.length === 0
						? "No job definitions available"
						: "Select a job..."}
				</option>
				{jobDefinitions?.map((name) => (
					<option key={name} value={name}>
						{name}
					</option>
				))}
			</Select>
			<Button type="submit" disabled={!selected || isPending}>
				{isPending ? "Submitting..." : "Submit"}
			</Button>
			{error && <p className={styles.error}>Failed to submit job.</p>}
		</form>
	);
}
