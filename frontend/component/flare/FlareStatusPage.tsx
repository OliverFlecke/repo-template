"use client";

import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import {
	getHealthOptions,
	listClientsOptions,
	listJobsOptions,
} from "@/api-flare/@tanstack/react-query.gen";
import { Select } from "@/ui/Select/Select";
import ClientsTable from "./ClientsTable";
import styles from "./FlareStatusPage.module.css";
import JobsTable from "./JobsTable";
import ProvisionClientForm from "./ProvisionClientForm";
import SubmitJobForm from "./SubmitJobForm";

const REFRESH_OPTIONS_SECONDS = [1, 2, 5, 10, 30, 60];

export default function FlareStatusPage() {
	const [refreshSeconds, setRefreshSeconds] = useState(10);
	const refetchInterval = refreshSeconds > 0 ? refreshSeconds * 1000 : false;

	return (
		<main className={styles.main}>
			<div className={styles.header}>
				<h1>Flare</h1>

				<Select
					className={styles.refreshSelect}
					value={refreshSeconds}
					onChange={(e) => setRefreshSeconds(Number(e.target.value))}
					aria-label="Refresh interval"
				>
					<option value={0}>Auto-refresh off</option>
					{REFRESH_OPTIONS_SECONDS.map((seconds) => (
						<option key={seconds} value={seconds}>
							Refresh every {seconds}s
						</option>
					))}
				</Select>
			</div>

			<HealthSection refetchInterval={refetchInterval} />
			<ClientsSection refetchInterval={refetchInterval} />

			<section className={styles.section}>
				<h2>Provision client</h2>
				<ProvisionClientForm />
			</section>

			<section className={styles.section}>
				<h2>Submit job</h2>
				<SubmitJobForm />
			</section>

			<JobsSection refetchInterval={refetchInterval} />
		</main>
	);
}

function ClientsSection({
	refetchInterval,
}: {
	refetchInterval: number | false;
}) {
	const {
		data: clients,
		isLoading: clientsLoading,
		isError: clientsError,
	} = useQuery({ ...listClientsOptions(), refetchInterval });

	return (
		<section className={styles.section}>
			<h2>Clients</h2>
			{clientsError && <p className={styles.error}>Could not load clients.</p>}
			{clientsLoading && <p>Loading...</p>}
			{clients && <ClientsTable data={clients} />}
		</section>
	);
}

function JobsSection({ refetchInterval }: { refetchInterval: number | false }) {
	const {
		data: jobs,
		isLoading,
		isError,
	} = useQuery({ ...listJobsOptions(), refetchInterval });

	return (
		<>
			{isError && <p className={styles.error}>Could not load jobs.</p>}
			{isLoading && <p>Loading...</p>}

			{jobs && (
				<>
					<section className={styles.section}>
						<h2>Active jobs</h2>
						<JobsTable data={jobs.active} emptyMessage="No active jobs." />
					</section>

					<section className={styles.section}>
						<h2>Completed jobs</h2>
						<JobsTable
							data={jobs.completed}
							emptyMessage="No completed jobs."
						/>
					</section>
				</>
			)}
		</>
	);
}

function HealthSection({
	refetchInterval,
}: {
	refetchInterval: number | false;
}) {
	const { data, isLoading, isError } = useQuery({
		...getHealthOptions(),
		refetchInterval,
	});

	return (
		<>
			{isError && (
				<p className={styles.error}>Could not reach the flare server.</p>
			)}
			{isLoading && <p>Loading...</p>}

			{data && (
				<section className={styles.section}>
					<h2>Server status</h2>
					<p>
						<span className={styles.statusLabel}>
							{data.server_info.status ?? "unknown"}
						</span>
						{data.server_info.start_time !== null && (
							<span className={styles.since}>
								{" "}
								since{" "}
								{new Date(data.server_info.start_time * 1000).toLocaleString()}
							</span>
						)}
					</p>
				</section>
			)}
		</>
	);
}
