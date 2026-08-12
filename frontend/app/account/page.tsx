"use client";

import { useMutation } from "@tanstack/react-query";
import { BadgeAlert, ShieldCheck } from "lucide-react";
import { type FormEvent, useState } from "react";
import { useAuth, withAuthenticationRequired } from "react-oidc-context";
import {
	sendAccountVerificationEmailMutation,
	updateAccountEmailMutation,
	updateAccountNameMutation,
	updateAccountPasswordMutation,
} from "@/api/@tanstack/react-query.gen";
import { AdornedInput, Avatar, Button, FormField, Input } from "@/ui";
import styles from "./page.module.css";

export default withAuthenticationRequired(Account, {
	OnRedirecting: () => <div>Redirecting to the login page...</div>,
});

// Users on Auth0's database connection have a `sub` claim prefixed like this;
// social/enterprise connections don't have an Auth0-managed password to change.
const DATABASE_CONNECTION_SUBJECT_PREFIX = "auth0|";

function Account() {
	return (
		<main className={styles.main}>
			<Header />
			<NameForm />
			<EmailForm />
			<PasswordForm />
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

function NameForm() {
	const auth = useAuth();
	const { user } = auth;

	const [name, setName] = useState(user?.profile.name ?? "");
	const updateName = useMutation(updateAccountNameMutation());

	if (!user) return null;

	const nameChanged = name !== (user.profile.name ?? "");

	async function handleSubmit(e: FormEvent) {
		e.preventDefault();
		await updateName.mutateAsync({ body: { name } });
		// Refresh the ID token so the header reflects the new name.
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

			{updateName.isError && (
				<p className={styles.error}>Failed to update profile.</p>
			)}

			<Button type="submit" disabled={updateName.isPending || !nameChanged}>
				{updateName.isPending ? "Updating..." : "Update profile"}
			</Button>
		</form>
	);
}

function EmailForm() {
	const auth = useAuth();
	const { user } = auth;

	const [email, setEmail] = useState(user?.profile.email ?? "");
	const updateEmail = useMutation(updateAccountEmailMutation());
	const sendVerification = useMutation(sendAccountVerificationEmailMutation());

	if (!user) return null;

	const { profile } = user;
	const emailChanged = email !== (profile.email ?? "");

	async function handleSubmit(e: FormEvent) {
		e.preventDefault();
		// The backend sends a fresh verification email as part of this call.
		await updateEmail.mutateAsync({ body: { email } });
	}

	return (
		<form className={styles.form} onSubmit={handleSubmit}>
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

			{!profile.email_verified && !updateEmail.isSuccess && (
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

			{updateEmail.isError && (
				<p className={styles.error}>Failed to update email.</p>
			)}

			{updateEmail.isSuccess ? (
				<>
					<p className={styles.success}>
						Verification email sent to {email}. Please sign in again to confirm
						your new address.
					</p>
					<Button type="button" onClick={() => auth.signinRedirect()}>
						Sign in again
					</Button>
				</>
			) : (
				<Button type="submit" disabled={updateEmail.isPending || !emailChanged}>
					{updateEmail.isPending ? "Updating..." : "Update email"}
				</Button>
			)}
		</form>
	);
}

function PasswordForm() {
	const { user } = useAuth();

	const [password, setPassword] = useState("");
	const [confirmPassword, setConfirmPassword] = useState("");
	const updatePassword = useMutation(updateAccountPasswordMutation());

	if (!user?.profile.sub.startsWith(DATABASE_CONNECTION_SUBJECT_PREFIX)) {
		return null;
	}

	const mismatch = confirmPassword.length > 0 && password !== confirmPassword;

	async function handleSubmit(e: FormEvent) {
		e.preventDefault();
		await updatePassword.mutateAsync({ body: { password } });
		setPassword("");
		setConfirmPassword("");
	}

	return (
		<form className={styles.form} onSubmit={handleSubmit}>
			<FormField label="New password">
				<Input
					type="password"
					value={password}
					onChange={(e) => setPassword(e.target.value)}
					autoComplete="new-password"
				/>
			</FormField>

			<FormField label="Confirm new password">
				<Input
					type="password"
					value={confirmPassword}
					onChange={(e) => setConfirmPassword(e.target.value)}
					autoComplete="new-password"
				/>
			</FormField>

			{mismatch && <p className={styles.error}>Passwords do not match.</p>}
			{updatePassword.isError && (
				<p className={styles.error}>Failed to update password.</p>
			)}
			{updatePassword.isSuccess && (
				<p className={styles.success}>Password updated.</p>
			)}

			<Button
				type="submit"
				disabled={updatePassword.isPending || !password || mismatch}
			>
				{updatePassword.isPending ? "Updating..." : "Update password"}
			</Button>
		</form>
	);
}
