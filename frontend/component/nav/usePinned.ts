import { useEffect, useState } from "react";

const PINNED_KEY = "nav-pinned";

/** Whether the desktop sidebar is pinned open, persisted to localStorage. */
export function usePinned() {
	const [pinned, setPinned] = useState(false);

	// Read the persisted preference after mount to avoid an SSR/client mismatch.
	useEffect(() => {
		try {
			setPinned(localStorage.getItem(PINNED_KEY) === "true");
		} catch {
			// Storage may be unavailable (e.g. sandboxed iframes); fall back to unpinned.
		}
	}, []);

	const toggle = () => {
		const next = !pinned;
		try {
			localStorage.setItem(PINNED_KEY, String(next));
		} catch {
			// Storage may be unavailable; the preference just won't persist.
		}
		setPinned(next);
	};

	return [pinned, toggle] as const;
}
