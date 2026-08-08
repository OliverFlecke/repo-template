"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { forwardRef, useImperativeHandle, useRef, useState } from "react";
import {
	getOrganizationsAdminQueryKey,
	postV1AdminOrganizationMutation,
} from "@/api/@tanstack/react-query.gen";
import { Button } from "@/ui/Button/Button";
import { FormField } from "@/ui/FormField/FormField";
import { Input } from "@/ui/Input/Input";
import styles from "./CreateOrganizationDialog.module.css";

export type CreateOrganizationDialogHandle = {
	open: () => void;
};

const CreateOrganizationDialog = forwardRef<CreateOrganizationDialogHandle>(
	function CreateOrganizationDialog(_props, ref) {
		const dialogRef = useRef<HTMLDialogElement>(null);
		const [name, setName] = useState("");
		const queryClient = useQueryClient();

		const { mutate, isPending, error, reset } = useMutation({
			...postV1AdminOrganizationMutation(),
			onSuccess: () => {
				queryClient.invalidateQueries({
					queryKey: getOrganizationsAdminQueryKey(),
				});
				setName("");
				dialogRef.current?.close();
			},
		});

		useImperativeHandle(ref, () => ({
			open: () => {
				reset();
				setName("");
				dialogRef.current?.showModal();
			},
		}));

		return (
			<dialog ref={dialogRef} className={styles.dialog}>
				<form
					className={styles.form}
					onSubmit={(e) => {
						e.preventDefault();
						mutate({ body: { name } });
					}}
				>
					<h2>Create organization</h2>

					<FormField label="Name" required>
						<Input
							type="text"
							value={name}
							onChange={(e) => setName(e.target.value)}
							required
						/>
					</FormField>

					{error && (
						<p className={styles.error}>Failed to create organization.</p>
					)}

					<div className={styles.actions}>
						<Button
							variant="outlined"
							color="secondary"
							onClick={() => dialogRef.current?.close()}
						>
							Cancel
						</Button>
						<Button type="submit" disabled={isPending}>
							{isPending ? "Creating..." : "Create"}
						</Button>
					</div>
				</form>
			</dialog>
		);
	},
);

export default CreateOrganizationDialog;
