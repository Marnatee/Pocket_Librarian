// Presentation layer / UI
// Dedicated registration page for new Pocket Librarian accounts.

import { useState } from "react";

const initialForm = {
    firstName: "",
    lastName: "",
    email: "",
    password: "",
};

function SignUpPage({ onSubmit, onBack, message }) {
    const [form, setForm] = useState(initialForm);
    const [isSubmitting, setIsSubmitting] = useState(false);

    function updateField(event) {
        const { name, value } = event.target;
        setForm((currentForm) => ({ ...currentForm, [name]: value }));
    }

    async function submitForm(event) {
        event.preventDefault();
        setIsSubmitting(true);

        try {
            await onSubmit(form);
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <section className="page-view auth-page" aria-labelledby="sign-up-heading">
            <div className="page-introduction">
                <p className="page-eyebrow">New account</p>
                <h1 id="sign-up-heading">Sign up</h1>
                <p>Create a member account to reserve books, receive notifications, and submit donations.</p>
            </div>

            <form className="member-feature-card auth-card auth-form" onSubmit={submitForm}>
                <div className="auth-form-grid">
                    <label>
                        First name
                        <input
                            name="firstName"
                            type="text"
                            value={form.firstName}
                            onChange={updateField}
                            required
                            autoComplete="given-name"
                        />
                    </label>
                    <label>
                        Last name
                        <input
                            name="lastName"
                            type="text"
                            value={form.lastName}
                            onChange={updateField}
                            required
                            autoComplete="family-name"
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
                            autoComplete="email"
                        />
                    </label>
                    <label>
                        Password
                        <input
                            name="password"
                            type="password"
                            value={form.password}
                            onChange={updateField}
                            required
                            autoComplete="new-password"
                        />
                    </label>
                </div>

                {message && <p className="auth-form-message" role="status">{message}</p>}

                <div className="member-form-actions auth-form-actions">
                    <button
                        className="member-primary-button"
                        type="submit"
                        disabled={isSubmitting}
                    >
                        {isSubmitting ? "Creating account..." : "Create account"}
                    </button>
                    <button
                        className="member-secondary-button"
                        type="button"
                        onClick={onBack}
                    >
                        Back to sign in
                    </button>
                </div>
            </form>
        </section>
    );
}

export default SignUpPage;
