import type { Story } from "@ladle/react";
import { ShieldCheck } from "lucide-react";
import { FormField } from "@/ui/FormField/FormField";
import { AdornedInput } from "./AdornedInput";

export const Gallery: Story = () => (
	<div
		style={{ display: "flex", flexDirection: "column", gap: 24, maxWidth: 320 }}
	>
		<FormField label="Email">
			<AdornedInput
				defaultValue="jane@example.com"
				readOnly
				icon={<ShieldCheck size={16} color="var(--color-success)" />}
				tooltip="Email verified"
			/>
		</FormField>
	</div>
);
