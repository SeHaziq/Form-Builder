const API_BASE_URL = "http://localhost:5018/api/v1";

let csrfToken: string | null = null;

async function getCsrfToken(): Promise<string> {
  const response = await fetch(`${API_BASE_URL}/auth/csrf-token`, {
    method: "GET",
    credentials: "include",
  });

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
    const token = await getCsrfToken();
    headers.set("X-CSRF-TOKEN", token);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    method,
    headers,
    credentials: "include",
  });

  if (!response.ok) {
    const contentType = response.headers.get("content-type");
    let message = `API request failed (${response.status}).`;

    if (contentType?.includes("application/json")) {
      const data = await response.json();

      if (typeof data.message === "string") {
        message = data.message;
      } else if (typeof data.title === "string") {
        message = data.title;
      } else if (data.errors) {
        message = Object.values(data.errors).flat().join(" ");
      }
    } else {
      const text = await response.text();

      if (text) {
        message = text;
      }
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}
