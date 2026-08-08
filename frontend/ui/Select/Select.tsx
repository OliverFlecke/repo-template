import { forwardRef, type SelectHTMLAttributes } from "react";
import { cx } from "@/ui/util/cx";
import styles from "./Select.module.css";

export type SelectProps = SelectHTMLAttributes<HTMLSelectElement>;

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
	({ className, ...props }, ref) => {
		return (
			<select ref={ref} className={cx(styles.select, className)} {...props} />
		);
	},
);

Select.displayName = "Select";
