"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense } from "react";
import { useAuth } from "react-oidc-context";
import {
	acceptInviteMutation,
	getInviteOptions,
	getMyOrganizationsQueryKey,
} from "@/api/@tanstack/react-query.gen";
import { useOrganization } from "@/component/organization/OrganizationProvider";
import { Button } from "@/ui/Button/Button";
import styles from "./JoinPage.module.css";

export default function JoinPage() {
	return (
		<Suspense fallback={<p>Loading...</p>}>
			<JoinContent />
		</Suspense>
	);
}

function JoinContent() {
	const token = useSearchParams().get("token") ?? "";
	const { isAuthenticated, signinRedirect } = useAuth();
	const { setCurrentOrganizationId } = useOrganization();
	const queryClient = useQueryClient();
	const router = useRouter();

	const {
		data: invite,
		isLoading,
		isError,
	} = useQuery({
		...getInviteOptions({ path: { token } }),
		enabled: token !== "",
	});

	const { mutate, isPending, error } = useMutation({
		...acceptInviteMutation(),
		onSuccess: () => {
			if (!invite) {
				return;
			}
			queryClient.invalidateQueries({
				queryKey: getMyOrganizationsQueryKey(),
			});
			setCurrentOrganizationId(invite.organizationId);
			router.push(`/organization/detail?id=${invite.organizationId}`);
		},
	});

	return (
		<main className={styles.main}>
			{token === "" && (
				<p className={styles.error}>This invite link is invalid.</p>
			)}
			{isLoading && <p>Loading...</p>}
			{isError && (
				<p className={styles.error}>
					This invite link is invalid or has expired.
				</p>
			)}

			{invite?.accepted && (
				<p className={styles.error}>This invite has already been used.</p>
			)}

			{invite && !invite.accepted && (
				<>
					<h1>You've been invited to join {invite.organizationName}</h1>

					{error && <p className={styles.error}>Failed to join.</p>}

					{isAuthenticated ? (
						<Button
							onClick={() => mutate({ path: { token } })}
							disabled={isPending}
						>
							{isPending ? "Joining..." : "Join"}
						</Button>
					) : (
						<Button
							onClick={() =>
								signinRedirect({
									state: {
										returnTo: `${window.location.pathname}${window.location.search}`,
									},
								})
							}
						>
							Sign in to join
						</Button>
					)}
				</>
			)}
		</main>
	);
}
