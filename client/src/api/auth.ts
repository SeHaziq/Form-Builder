
import { apiRequest } from "./api";

export interface AuthResponse {
    message: string;
}

export interface RegisterRequest {
    email: string;
    password: string;
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface CurrentUser {
    userId: string;
    email: string | null;
}

export function register(
    data: RegisterRequest,
): Promise<AuthResponse> {
    return apiRequest<AuthResponse>("/auth/register", {
        method: "POST",
        body: JSON.stringify(data),
    });
}

export function login(
    data: LoginRequest,
): Promise<AuthResponse> {
    return apiRequest<AuthResponse>("/auth/login", {
        method: "POST",
        body: JSON.stringify(data),
    });
}

export function logout(): Promise<AuthResponse> {
    return apiRequest<AuthResponse>("/auth/logout", {
        method: "POST",
    });
}

export function getCurrentUser(): Promise<CurrentUser> {
    return apiRequest<CurrentUser>("/auth/me");
}
