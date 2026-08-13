"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
	createContext,
	type PropsWithChildren,
	useContext,
	useEffect,
	useMemo,
	useState,
} from "react";
import { useAuth } from "react-oidc-context";
import {
	getMyOrganizationsOptions,
	getMyOrganizationsQueryKey,
} from "@/api/@tanstack/react-query.gen";
import type { OrganizationMembership } from "@/api/types.gen";

const STORAGE_KEY = "currentOrganizationId";

type OrganizationContextValue = {
	organizations: OrganizationMembership[];
	isLoading: boolean;
	currentOrganizationId: string | undefined;
	setCurrentOrganizationId: (id: string) => void;
};

const OrganizationContext = createContext<OrganizationContextValue | null>(
	null,
);

/** Fetches the current user's organizations and tracks which one is active,
 * persisting the selection to localStorage. Falls back to the first
 * organization if none is selected yet or the selected one is no longer
 * in the list. */
export default function OrganizationProvider({
	children,
}: Readonly<PropsWithChildren>) {
	const { isAuthenticated } = useAuth();
	const queryClient = useQueryClient();
	const [currentOrganizationId, setCurrentOrganizationId] = useState<
		string | undefined
	>(undefined);

	const { data, isLoading } = useQuery({
		...getMyOrganizationsOptions(),
		enabled: isAuthenticated,
	});
	const organizations = useMemo(() => data ?? [], [data]);

	useEffect(() => {
		if (!isAuthenticated) {
			// Otherwise a different user signing in on the same browser would
			// briefly see the previous user's organizations: a disabled query
			// keeps its last data rather than clearing it.
			queryClient.removeQueries({ queryKey: getMyOrganizationsQueryKey() });
		}
		setCurrentOrganizationId(
			isAuthenticated
				? (localStorage.getItem(STORAGE_KEY) ?? undefined)
				: undefined,
		);
	}, [isAuthenticated, queryClient]);

	useEffect(() => {
		if (
			organizations.length > 0 &&
			!organizations.some((o) => o.id === currentOrganizationId)
		) {
			setCurrentOrganizationId(organizations[0].id);
		}
	}, [organizations, currentOrganizationId]);

	function selectOrganization(id: string) {
		localStorage.setItem(STORAGE_KEY, id);
		setCurrentOrganizationId(id);
	}

	return (
		<OrganizationContext.Provider
			value={{
				organizations,
				isLoading,
				currentOrganizationId,
				setCurrentOrganizationId: selectOrganization,
			}}
		>
			{children}
		</OrganizationContext.Provider>
	);
}

export function useOrganization() {
	const context = useContext(OrganizationContext);
	if (!context) {
		throw new Error(
			"useOrganization must be used within an OrganizationProvider",
		);
	}

	return context;
}
