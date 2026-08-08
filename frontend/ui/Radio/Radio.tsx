import { forwardRef, type InputHTMLAttributes } from "react";
import { cx } from "@/ui/util/cx";
import styles from "./Radio.module.css";

export type RadioProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type">;

export const Radio = forwardRef<HTMLInputElement, RadioProps>(
	({ className, ...props }, ref) => {
		return (
			<input
				ref={ref}
				type="radio"
				className={cx(styles.radio, className)}
				{...props}
			/>
		);
	},
);

Radio.displayName = "Radio";
