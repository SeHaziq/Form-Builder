
import { useEffect, useState } from "react";
import {
    getCurrentUser,
    login,
    logout,
    register,
    type CurrentUser,
} from "../../api/auth";

type AuthMode = "login" | "register";

export default function AuthPage() {
    const [mode, setMode] = useState<AuthMode>("login");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [user, setUser] = useState<CurrentUser | null>(null);
    const [checkingSession, setCheckingSession] = useState(true);
    const [message, setMessage] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        getCurrentUser()
            .then(setUser)
            .catch(() => setUser(null))
            .finally(() => setCheckingSession(false));
    }, []);

    async function handleSubmit(
        event: React.FormEvent<HTMLFormElement>,
    ) {
        event.preventDefault();
        setMessage("");
        setError("");
        setLoading(true);

        try {
            if (mode === "login") {
                await login({ email, password });
                const currentUser = await getCurrentUser();
                setUser(currentUser);
            } else {
                const result = await register({ email, password });
                setMessage(result.message);
                setMode("login");
            }

            setPassword("");
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : "Something went wrong.",
            );
        } finally {
            setLoading(false);
        }
    }

    async function handleLogout() {
        setError("");
        setMessage("");
        setLoading(true);

        try {
            await logout();
            setUser(null);
            setEmail("");
            setPassword("");
            setMessage("You have logged out successfully.");
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : "Logout failed.",
            );
        } finally {
            setLoading(false);
        }
    }

    if (checkingSession) {
        return <main><p>Checking session...</p></main>;
    }

    if (user) {
        return (
            <main>
                <h1>Form Builder</h1>
                <h2>Dashboard</h2>
                <p>Welcome, {user.email ?? "user"}!</p>
                <p>You are signed in.</p>

                <button
                    type="button"
                    onClick={handleLogout}
                    disabled={loading}
                >
                    {loading ? "Please wait..." : "Log out"}
                </button>

                {error && <p role="alert">{error}</p>}
            </main>
        );
    }

    return (
        <main>
            <h1>Form Builder</h1>
            <h2>
                {mode === "login" ? "Welcome back" : "Create an account"}
            </h2>

            <form onSubmit={handleSubmit}>
                <label htmlFor="email">Email</label>
                <input
                    id="email"
                    type="email"
                    autoComplete="email"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    required
                />

                <label htmlFor="password">Password</label>
                <input
                    id="password"
                    type="password"
                    autoComplete={
                        mode === "login"
                            ? "current-password"
                            : "new-password"
                    }
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                    required
                />

                <button type="submit" disabled={loading}>
                    {loading
                        ? "Please wait..."
                        : mode === "login"
                          ? "Log in"
                          : "Register"}
                </button>
            </form>

            {message && <p role="status">{message}</p>}
            {error && <p role="alert">{error}</p>}

            <button
                type="button"
                onClick={() => {
                    setMode(mode === "login" ? "register" : "login");
                    setMessage("");
                    setError("");
                }}
            >
                {mode === "login"
                    ? "Need an account? Register"
                    : "Already have an account? Log in"}
            </button>
        </main>
    );
}
