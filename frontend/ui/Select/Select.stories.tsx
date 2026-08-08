import type { Story } from "@ladle/react";
import { FormField } from "@/ui/FormField/FormField";
import { Select } from "./Select";

const Options = () => (
	<>
		<option value="a">Option A</option>
		<option value="b">Option B</option>
		<option value="c">Option C</option>
	</>
);

export const Gallery: Story = () => (
	<div
		style={{ display: "flex", flexDirection: "column", gap: 24, maxWidth: 320 }}
	>
		<FormField label="Default" helperText="Helper text">
			<Select defaultValue="a">
				<Options />
			</Select>
		</FormField>

		<FormField label="Disabled">
			<Select defaultValue="a" disabled>
				<Options />
			</Select>
		</FormField>

		<FormField label="Invalid" errorText="This field has an error">
			<Select defaultValue="a">
				<Options />
			</Select>
		</FormField>
	</div>
);

export const Playground: Story<{ disabled: boolean }> = ({ disabled }) => (
	<Select defaultValue="a" disabled={disabled}>
		<Options />
	</Select>
);

Playground.args = { disabled: false };
