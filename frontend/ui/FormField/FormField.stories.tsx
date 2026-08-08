import type { Story } from "@ladle/react";
import { Input } from "@/ui/Input/Input";
import { Select } from "@/ui/Select/Select";
import { Textarea } from "@/ui/Textarea/Textarea";
import { FormField } from "./FormField";

export const Gallery: Story = () => (
	<div
		style={{ display: "flex", flexDirection: "column", gap: 24, maxWidth: 320 }}
	>
		<FormField label="Name" helperText="Your full name">
			<Input placeholder="Ada Lovelace" />
		</FormField>

		<FormField label="Email" required helperText="We'll never share it">
			<Input type="email" placeholder="ada@example.com" />
		</FormField>

		<FormField label="Bio">
			<Textarea placeholder="Tell us about yourself" />
		</FormField>

		<FormField label="Role">
			<Select defaultValue="member">
				<option value="admin">Admin</option>
				<option value="member">Member</option>
			</Select>
		</FormField>

		<FormField
			label="Password"
			required
			errorText="Must be at least 8 characters"
		>
			<Input type="password" />
		</FormField>
	</div>
);
