"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { forwardRef, useImperativeHandle, useRef, useState } from "react";
import {
	getOrganizationsAdminQueryKey,
	postV1AdminOrganizationMutation,
} from "@/api/@tanstack/react-query.gen";
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

					<label className={styles.field}>
						Name
						<input
							type="text"
							value={name}
							onChange={(e) => setName(e.target.value)}
							required
						/>
					</label>

					{error && (
						<p className={styles.error}>Failed to create organization.</p>
					)}

					<div className={styles.actions}>
						<button type="button" onClick={() => dialogRef.current?.close()}>
							Cancel
						</button>
						<button type="submit" disabled={isPending}>
							{isPending ? "Creating..." : "Create"}
						</button>
					</div>
				</form>
			</dialog>
		);
	},
);

export default CreateOrganizationDialog;
