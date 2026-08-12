import {
	cloneElement,
	type HTMLAttributes,
	isValidElement,
	type ReactElement,
	useId,
} from "react";
import { Label } from "@/ui/Label/Label";
import { cx } from "@/ui/util/cx";
import styles from "./FormField.module.css";
import { ShieldAlert } from "lucide-react";

export interface FormFieldProps
	extends Omit<HTMLAttributes<HTMLDivElement>, "children"> {
	label: string;
	helperText?: string;
	errorText?: string;
	required?: boolean;
	children: ReactElement<{
		id?: string;
		"aria-describedby"?: string;
		"aria-invalid"?: boolean;
	}>;
}

export function FormField({
	label,
	helperText,
	errorText,
	required,
	children,
	className,
	id,
	...props
}: FormFieldProps) {
	const generatedId = useId();
	const controlId = id ?? generatedId;
	const helperId = `${controlId}-helper`;
	const invalid = Boolean(errorText);

	const control = isValidElement(children)
		? cloneElement(children, {
				id: controlId,
				"aria-describedby": helperText || errorText ? helperId : undefined,
				"aria-invalid": invalid,
			})
		: children;

	return (
		<div className={cx(styles.field, className)} {...props}>
			<Label htmlFor={controlId}>
				{label}
				{required && <span className={styles.required}> *</span>}
			</Label>

			{control}

			{(helperText || errorText) && (
				<p
					id={helperId}
					className={cx(styles.helperText, invalid && styles.errorText)}
				>
					{errorText || helperText}
				</p>
			)}
		</div>
	);
}
