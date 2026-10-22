
const API_BASE_URL = "http://localhost:5018/api/v1";

let csrfToken: string | null = null;

async function getCsrfToken(): Promise<string> {
    const response = await fetch(
        `${API_BASE_URL}/auth/csrf-token`,
        {
            method: "GET",
            credentials: "include",
        },
    );

    if (!response.ok) {
        throw new Error("Failed to obtain CSRF token.");
    }

    const data: { requestToken: string } = await response.json();

    csrfToken = data.requestToken;

    return csrfToken;
}

export async function apiRequest<T>(
    path: string,
    options: RequestInit = {},
): Promise<T> {
    const method = (options.method ?? "GET").toUpperCase();
    const needsCsrf = !["GET", "HEAD", "OPTIONS"].includes(method);

    const headers = new Headers(options.headers);

    if (options.body && !headers.has("Content-Type")) {
        headers.set("Content-Type", "application/json");
    }

    if (needsCsrf) {
        const token = csrfToken ?? await getCsrfToken();
        headers.set("X-CSRF-TOKEN", token);
    }

    const response = await fetch(`${API_BASE_URL}${path}`, {
        ...options,
        method,
        headers,
        credentials: "include",
    });

    if (!response.ok) {
        throw new Error(`API request failed (${response.status}).`);
    }

    if (response.status === 204) {
        return undefined as T;
    }

    return response.json() as Promise<T>;
}
