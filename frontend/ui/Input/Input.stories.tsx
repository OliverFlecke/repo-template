import type { Story } from "@ladle/react";
import { FormField } from "@/ui/FormField/FormField";
import { Input } from "./Input";

export const Gallery: Story = () => (
	<div
		style={{ display: "flex", flexDirection: "column", gap: 24, maxWidth: 320 }}
	>
		<FormField label="Default" helperText="Helper text">
			<Input placeholder="Type something" />
		</FormField>

		<FormField label="Disabled">
			<Input placeholder="Type something" disabled />
		</FormField>

		<FormField label="Invalid" errorText="This field has an error">
			<Input placeholder="Type something" defaultValue="Bad value" />
		</FormField>
	</div>
);

export const Playground: Story<{ placeholder: string; disabled: boolean }> = ({
	placeholder,
	disabled,
}) => <Input placeholder={placeholder} disabled={disabled} />;

Playground.args = { placeholder: "Type something", disabled: false };
