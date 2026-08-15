"use client";

import { useQuery } from "@tanstack/react-query";
import {
	getHealthOptions,
	listJobsOptions,
} from "@/api-flare/@tanstack/react-query.gen";
import ClientsTable from "./ClientsTable";
import styles from "./FlareStatusPage.module.css";
import JobsTable from "./JobsTable";
import ProvisionClientForm from "./ProvisionClientForm";
import SubmitJobForm from "./SubmitJobForm";

export default function FlareStatusPage() {
	const {
		data: health,
		isLoading: healthLoading,
		isError: healthError,
	} = useQuery(getHealthOptions());

	const {
		data: jobs,
		isLoading: jobsLoading,
		isError: jobsError,
	} = useQuery(listJobsOptions());

	return (
		<main className={styles.main}>
			<h1>Flare</h1>

			{healthError && (
				<p className={styles.error}>Could not reach the flare server.</p>
			)}
			{healthLoading && <p>Loading...</p>}

			{health && (
				<section className={styles.section}>
					<h2>Server status</h2>
					<p>
						<span className={styles.statusLabel}>
							{health.server_info.status ?? "unknown"}
						</span>
						{health.server_info.start_time !== null && (
							<span className={styles.since}>
								{" "}
								since{" "}
								{new Date(
									health.server_info.start_time * 1000,
								).toLocaleString()}
							</span>
						)}
					</p>
				</section>
			)}

			<section className={styles.section}>
				<h2>Connected clients</h2>
				<ClientsTable data={health?.client_info ?? []} />
			</section>

			<section className={styles.section}>
				<h2>Provision client</h2>
				<ProvisionClientForm />
			</section>

			<section className={styles.section}>
				<h2>Submit job</h2>
				<SubmitJobForm />
			</section>

			{jobsError && <p className={styles.error}>Could not load jobs.</p>}
			{jobsLoading && <p>Loading...</p>}

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
		</main>
	);
}
