import { forwardRef, type LabelHTMLAttributes } from "react";
import { cx } from "@/ui/util/cx";
import styles from "./Label.module.css";

export type LabelProps = LabelHTMLAttributes<HTMLLabelElement>;

export const Label = forwardRef<HTMLLabelElement, LabelProps>(
	({ className, ...props }, ref) => {
		return (
			// biome-ignore lint/a11y/noLabelWithoutControl: association is provided by the consumer via htmlFor or by wrapping the control as children
			<label ref={ref} className={cx(styles.label, className)} {...props} />
		);
	},
);

Label.displayName = "Label";
