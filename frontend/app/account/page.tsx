"use client";

import { BadgeAlert, ShieldCheck } from "lucide-react";
import { useAuth, withAuthenticationRequired } from "react-oidc-context";
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
	const { user } = useAuth();
	if (!user) return null;

	const { profile } = user;

	return (
		<>
			<FormField label="Name">
				<Input value={profile.name} readOnly title="Your full legal name" />
			</FormField>

			<FormField label="Email">
				<AdornedInput
					value={profile.email}
					readOnly
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

			<Button>Update profile</Button>
		</>
	);
}
