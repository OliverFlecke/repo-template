"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "next/navigation";
import { Suspense, useState } from "react";
import { useAuth } from "react-oidc-context";
import {
	deleteApiV1OrganizationByIdMemberByUserIdMutation,
	getApiV1OrganizationByIdOptions,
	getApiV1OrganizationByIdQueryKey,
	postApiV1OrganizationByIdInviteMutation,
} from "@/api/@tanstack/react-query.gen";
import { Button } from "@/ui/Button/Button";
import { FormField } from "@/ui/FormField/FormField";
import { Input } from "@/ui/Input/Input";
import styles from "./OrganizationDetailPage.module.css";

export default function OrganizationDetailPage() {
	return (
		<Suspense fallback={<p>Loading...</p>}>
			<OrganizationDetailContent />
		</Suspense>
	);
}

function OrganizationDetailContent() {
	const id = useSearchParams().get("id") ?? "";
	const [email, setEmail] = useState("");
	const [inviteLink, setInviteLink] = useState<string | null>(null);
	const { user } = useAuth();
	const queryClient = useQueryClient();

	const {
		data: org,
		isLoading,
		isError,
	} = useQuery({
		...getApiV1OrganizationByIdOptions({ path: { id } }),
		enabled: id !== "",
	});

	const { mutate, isPending, error } = useMutation({
		...postApiV1OrganizationByIdInviteMutation(),
		onSuccess: (invite) => {
			setInviteLink(`${window.location.origin}/join?token=${invite.id}`);
			setEmail("");
		},
	});

	const { mutate: removeMember } = useMutation({
		...deleteApiV1OrganizationByIdMemberByUserIdMutation(),
		onSuccess: () => {
			queryClient.invalidateQueries({
				queryKey: getApiV1OrganizationByIdQueryKey({ path: { id } }),
			});
		},
	});

	const isAdmin = org?.members.some(
		(member) => member.userId === user?.profile.sub && member.role === "Admin",
	);

	return (
		<main className={styles.main}>
			{isError && (
				<p className={styles.error}>
					You don't have access to this organization.
				</p>
			)}
			{isLoading && <p>Loading...</p>}

			{org && (
				<>
					<h1>{org.name}</h1>

					<ul className={styles.list}>
						{org.members.map((member) => (
							<li key={member.userId} className={styles.item}>
								<span className={styles.name}>
									{member.name ?? member.userId}
								</span>
								<span className={styles.role}>{member.role}</span>
								{isAdmin && member.role !== "Admin" && (
									<Button
										type="button"
										variant="text"
										color="danger"
										size="sm"
										onClick={() =>
											removeMember({ path: { id, userId: member.userId } })
										}
									>
										Remove
									</Button>
								)}
							</li>
						))}
					</ul>

					<form
						className={styles.form}
						onSubmit={(e) => {
							e.preventDefault();
							mutate({ path: { id }, body: { email } });
						}}
					>
						<FormField label="Email" required>
							<Input
								type="email"
								value={email}
								onChange={(e) => setEmail(e.target.value)}
								required
							/>
						</FormField>

						{error && <p className={styles.error}>Failed to send invite.</p>}

						<Button type="submit" disabled={isPending}>
							{isPending ? "Inviting..." : "Invite member"}
						</Button>
					</form>

					{inviteLink && (
						<div className={styles.inviteLink}>
							<Input type="text" value={inviteLink} readOnly />
							<Button
								type="button"
								variant="outlined"
								color="secondary"
								onClick={() => navigator.clipboard.writeText(inviteLink)}
							>
								Copy link
							</Button>
						</div>
					)}
				</>
			)}
		</main>
	);
}
