"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { PropsWithChildren } from "react";
import { client } from "@/api/client.gen";
import { getUser } from "@/component/auth/config";

/// Sets the configuration for the client, such as the base URL and the authorization.
/// Other headers or general request options can be provided here.
client.setConfig({
	baseUrl: process.env.NEXT_PUBLIC_API_HOST,
	auth: () => getUser()?.access_token,
});

const queryClient = new QueryClient();

export default function QueryProvider({
	children,
}: Readonly<PropsWithChildren>) {
	return (
		<QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
	);
}
