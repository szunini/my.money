import { http } from "./http";
//LoginService
export type AuthResponse = { accessToken: string };

export async function login(email: string, password: string): Promise<AuthResponse> {
  const res = await http.post<AuthResponse>("/api/auth/login", { email, password });
  return res.data;
}