import type { Story } from "@ladle/react";
import { FormField } from "@/ui/FormField/FormField";
import { Textarea } from "./Textarea";

export const Gallery: Story = () => (
	<div
		style={{ display: "flex", flexDirection: "column", gap: 24, maxWidth: 320 }}
	>
		<FormField label="Default" helperText="Helper text">
			<Textarea placeholder="Type something" />
		</FormField>

		<FormField label="Disabled">
			<Textarea placeholder="Type something" disabled />
		</FormField>

		<FormField label="Invalid" errorText="This field has an error">
			<Textarea placeholder="Type something" defaultValue="Bad value" />
		</FormField>
	</div>
);

export const Playground: Story<{ placeholder: string; disabled: boolean }> = ({
	placeholder,
	disabled,
}) => <Textarea placeholder={placeholder} disabled={disabled} />;

Playground.args = { placeholder: "Type something", disabled: false };
