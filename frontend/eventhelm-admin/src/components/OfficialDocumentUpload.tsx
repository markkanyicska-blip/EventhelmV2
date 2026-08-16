import { useState } from "react";
import type { ChangeEvent } from "react";

import {
    deleteOfficialDocument,
    uploadOfficialDocument,
} from "../api/uploads";

type Props = {
    label: string;
    value?: string | null;
    onChange: (value: string | null) => void;
    disabled?: boolean;
};

export default function OfficialDocumentUpload({
    label,
    value,
    onChange,
    disabled = false,
}: Props) {
    const [uploading, setUploading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    async function handleUpload(
        event: ChangeEvent<HTMLInputElement>,
    ) {
        const file = event.target.files?.[0];

        if (!file) {
            return;
        }

        try {
            setUploading(true);
            setError(null);

            const uploaded =
                await uploadOfficialDocument(file);

            onChange(uploaded.url);
        } catch (uploadError) {
            console.error(uploadError);
            setError(
                "A dokumentum feltöltése nem sikerült.",
            );
        } finally {
            setUploading(false);
            event.target.value = "";
        }
    }

    async function handleDelete() {
        if (!value) {
            return;
        }

        try {
            setUploading(true);
            setError(null);

            await deleteOfficialDocument(value);
            onChange(null);
        } catch (deleteError) {
            console.error(deleteError);
            setError(
                "A dokumentum törlése nem sikerült.",
            );
        } finally {
            setUploading(false);
        }
    }

    const documentUrl = value
        ? `http://localhost:5100${value}`
        : null;

    return (
        <div className="eh-official-document-upload">
            <label>
                {label}

                <input
                    type="file"
                    accept=".pdf,.jpg,.jpeg,.png"
                    disabled={disabled || uploading}
                    onChange={(event) =>
                        void handleUpload(event)
                    }
                />
            </label>

            {uploading && (
                <div>Feltöltés folyamatban...</div>
            )}

            {documentUrl && (
                <div
                    style={{
                        marginTop: 8,
                        display: "flex",
                        gap: 8,
                        alignItems: "center",
                    }}
                >
                    <a
                        href={documentUrl}
                        target="_blank"
                        rel="noreferrer"
                    >
                        Dokumentum megnyitása
                    </a>

                    <button
                        type="button"
                        disabled={uploading}
                        onClick={() =>
                            void handleDelete()
                        }
                    >
                        Törlés
                    </button>
                </div>
            )}

            {error && (
                <div role="alert">
                    {error}
                </div>
            )}
        </div>
    );
}