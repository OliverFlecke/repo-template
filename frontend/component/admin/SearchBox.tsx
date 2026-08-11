"use client";

import { useEffect, useRef, useState } from "react";
import { Input } from "@/ui/Input/Input";
import styles from "./SearchBox.module.css";

type Props = {
	value: string;
	onChange: (value: string) => void;
	placeholder?: string;
};

export default function SearchBox({ value, onChange, placeholder }: Props) {
	const [draft, setDraft] = useState(value);
	const onChangeRef = useRef(onChange);
	onChangeRef.current = onChange;

	useEffect(() => {
		const timeout = setTimeout(() => onChangeRef.current(draft), 300);
		return () => clearTimeout(timeout);
	}, [draft]);

	return (
		<Input
			type="search"
			className={styles.input}
			value={draft}
			onChange={(e) => setDraft(e.target.value)}
			placeholder={placeholder}
		/>
	);
}