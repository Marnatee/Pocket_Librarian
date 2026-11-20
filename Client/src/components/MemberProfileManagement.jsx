import { useEffect, useState } from "react";

const emptyProfile = {
    firstName: "",
    lastName: "",
    email: "",
    role: "",
};

async function getApiMessage(response, fallback) {
    const data = await response.json().catch(() => ({}));
    return data.message || fallback;
}

function MemberProfileManagement({ onProfileUpdated }) {
    const [profile, setProfile] = useState(null);
    const [form, setForm] = useState(emptyProfile);
    const [isEditing, setIsEditing] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [message, setMessage] = useState("");
    const [error, setError] = useState("");

    useEffect(() => {
        let isMounted = true;

        async function loadProfile() {
            try {
                const response = await fetch("/api/member-profile/me", {
                    credentials: "include",
                });

                if (!response.ok) {
                    throw new Error(await getApiMessage(
                        response,
                        "The member profile could not be loaded."
                    ));
                }

                const data = await response.json();

                if (isMounted) {
                    setProfile(data);
                    setForm({
                        firstName: data.firstName || "",
                        lastName: data.lastName || "",
                        email: data.email || "",
                        role: data.role || "",
                    });
                }
            } catch (loadError) {
                if (isMounted) {
                    setError(loadError.message);
                }
            } finally {
                if (isMounted) {
                    setIsLoading(false);
                }
            }
        }

        void loadProfile();

        return () => {
            isMounted = false;
        };
    }, []);

    function updateField(event) {
        const { name, value } = event.target;
        setForm((currentForm) => ({ ...currentForm, [name]: value }));
    }

    function cancelEditing() {
        setForm({
            firstName: profile?.firstName || "",
            lastName: profile?.lastName || "",
            email: profile?.email || "",
            role: profile?.role || "",
        });
        setIsEditing(false);
        setError("");
        setMessage("");
    }

    async function saveProfile(event) {
        event.preventDefault();
        setIsSaving(true);
        setError("");
        setMessage("");

        try {
            const response = await fetch("/api/member-profile/me", {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                credentials: "include",
                body: JSON.stringify({
                    firstName: form.firstName,
                    lastName: form.lastName,
                    email: form.email,
                }),
            });

            if (!response.ok) {
                throw new Error(await getApiMessage(
                    response,
                    "The member profile could not be updated."
                ));
            }

            const updatedProfile = await response.json();
            setProfile(updatedProfile);
            setForm({
                firstName: updatedProfile.firstName || "",
                lastName: updatedProfile.lastName || "",
                email: updatedProfile.email || "",
                role: updatedProfile.role || "",
            });
            onProfileUpdated?.(updatedProfile);
            setIsEditing(false);
            setMessage("Profile updated successfully.");
        } catch (saveError) {
            setError(saveError.message);
        } finally {
            setIsSaving(false);
        }
    }

    return (
        <section className="member-feature-card" aria-labelledby="profile-heading">
            <div className="member-feature-heading">
                <div>
                    <p className="page-eyebrow">Member account</p>
                    <h2 id="profile-heading">Member Profile</h2>
                </div>
                {profile && !isEditing && (
                    <button
                        type="button"
                        className="member-secondary-button"
                        onClick={() => {
                            setIsEditing(true);
                            setMessage("");
                            setError("");
                        }}
                    >
                        Edit profile
                    </button>
                )}
            </div>

            {isLoading && <p className="member-muted-text">Loading profile...</p>}
            {error && <p className="member-error" role="alert">{error}</p>}
            {message && <p className="member-success" role="status">{message}</p>}

            {!isLoading && profile && !isEditing && (
                <dl className="profile-details">
                    <div><dt>First name</dt><dd>{profile.firstName}</dd></div>
                    <div><dt>Last name</dt><dd>{profile.lastName}</dd></div>
                    <div><dt>Email</dt><dd>{profile.email}</dd></div>
                    <div><dt>Role</dt><dd>{profile.role}</dd></div>
                </dl>
            )}

            {isEditing && (
                <form className="member-form" onSubmit={saveProfile}>
                    <label>
                        First name
                        <input
                            name="firstName"
                            value={form.firstName}
                            onChange={updateField}
                            required
                        />
                    </label>
                    <label>
                        Last name
                        <input
                            name="lastName"
                            value={form.lastName}
                            onChange={updateField}
                            required
                        />
                    </label>
                    <label>
                        Email
                        <input
                            name="email"
                            type="email"
                            value={form.email}
                            onChange={updateField}
                            required
                        />
                    </label>
                    <div className="member-form-actions">
                        <button
                            type="submit"
                            className="member-primary-button"
                            disabled={isSaving}
                        >
                            {isSaving ? "Saving..." : "Save changes"}
                        </button>
                        <button
                            type="button"
                            className="member-secondary-button"
                            onClick={cancelEditing}
                            disabled={isSaving}
                        >
                            Cancel
                        </button>
                    </div>
                </form>
            )}
        </section>
    );
}

export default MemberProfileManagement;
