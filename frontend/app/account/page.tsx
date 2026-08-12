"use client";

import { useMutation } from "@tanstack/react-query";
import { BadgeAlert, ShieldCheck } from "lucide-react";
import { type FormEvent, useState } from "react";
import { useAuth, withAuthenticationRequired } from "react-oidc-context";
import {
	sendAccountVerificationEmailMutation,
	updateAccountEmailMutation,
	updateAccountNameMutation,
} from "@/api/@tanstack/react-query.gen";
import { AdornedInput, Avatar, Button, FormField, Input } from "@/ui";
import styles from "./page.module.css";

export default withAuthenticationRequired(Account, {
	OnRedirecting: () => <div>Redirecting to the login page...</div>,
});

function Account() {
	return (
		<main className={styles.main}>
			<Header />
			<ProfileForm />
		</main>
	);
}

function Header() {
	const { user } = useAuth();
	if (!user) return null;

	const { profile } = user;
	const name = profile.name ?? profile.email ?? "Account";

	return (
		<div className={styles.header}>
			<Avatar src={profile.picture} name={name} size="lg" />
			<div>
				<h1>{name}</h1>
				{profile.email && <p className={styles.email}>{profile.email}</p>}
			</div>
		</div>
	);
}

function ProfileForm() {
	const auth = useAuth();
	const { user } = auth;

	const [name, setName] = useState(user?.profile.name ?? "");
	const [email, setEmail] = useState(user?.profile.email ?? "");

	const updateName = useMutation(updateAccountNameMutation());
	const updateEmail = useMutation(updateAccountEmailMutation());
	const sendVerification = useMutation(sendAccountVerificationEmailMutation());

	if (!user) return null;

	const { profile } = user;
	const nameChanged = name !== (profile.name ?? "");
	const emailChanged = email !== (profile.email ?? "");
	const isPending = updateName.isPending || updateEmail.isPending;

	async function handleSubmit(e: FormEvent) {
		e.preventDefault();

		await Promise.all([
			nameChanged ? updateName.mutateAsync({ body: { name } }) : null,
			emailChanged ? updateEmail.mutateAsync({ body: { email } }) : null,
		]);

		// Refresh the ID token so profile/email_verified reflect what we just wrote.
		await auth.signinSilent();
	}

	return (
		<form className={styles.form} onSubmit={handleSubmit}>
			<FormField label="Name">
				<Input
					value={name}
					onChange={(e) => setName(e.target.value)}
					title="Your full legal name"
				/>
			</FormField>

			<FormField label="Email">
				<AdornedInput
					type="email"
					value={email}
					onChange={(e) => setEmail(e.target.value)}
					title="Your email address"
					icon={
						profile.email_verified ? (
							<ShieldCheck size={16} color="var(--color-success)" />
						) : (
							<BadgeAlert size={16} color="var(--color-warning)" />
						)
					}
					tooltip={
						profile.email_verified ? "Email verified" : "Email not verified"
					}
				/>
			</FormField>

			{!profile.email_verified && (
				<Button
					type="button"
					variant="text"
					size="sm"
					className={styles.resendButton}
					disabled={sendVerification.isPending}
					onClick={() => sendVerification.mutate({})}
				>
					{sendVerification.isPending
						? "Sending..."
						: sendVerification.isSuccess
							? "Verification email sent"
							: "Resend verification email"}
				</Button>
			)}

			{(updateName.isError || updateEmail.isError) && (
				<p className={styles.error}>Failed to update profile.</p>
			)}

			<Button
				type="submit"
				disabled={isPending || (!nameChanged && !emailChanged)}
			>
				{isPending ? "Updating..." : "Update profile"}
			</Button>
		</form>
	);
}
