"use client";

import { client } from "@/api/client.gen";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PropsWithChildren } from "react";

/// Sets the configuration for the client, such as the base URL and the authorization.
/// Other headers or general request options can be provided here.
client.setConfig({
	baseUrl: process.env.NEXT_PUBLIC_API_HOST,
});

const queryClient = new QueryClient();

export default function QueryProvider({
	children,
}: Readonly<PropsWithChildren>) {
	return (
		<QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
	);
}
