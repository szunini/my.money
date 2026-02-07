import React, { createContext, useContext, useState } from "react";

type AuthContextType = {
  token: string | null;
  isAuthenticated: boolean;
  setToken: (token: string | null) => void;
  logout: () => void;
};

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setTokenState] = useState<string | null>(() => {
    return localStorage.getItem("access_token");
  });

  function setToken(token: string | null) {
    setTokenState(token);
    if (token) localStorage.setItem("access_token", token);
    else localStorage.removeItem("access_token");
  }

  function logout() {
    setToken(null);
  }

  const value: AuthContextType = {
    token,
    isAuthenticated: !!token,
    setToken,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
