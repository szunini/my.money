import React, { createContext, useContext, useState } from "react";
//Guarda el token en localStorage para mantener la sesión incluso después de recargar la página
type AuthContextType = {
  token: string | null;
  setToken: (t: string | null) => void;
  logout: () => void;
};

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setTokenState] = useState<string | null>(() =>
    localStorage.getItem("access_token")
  );

  function setToken(t: string | null) {
    setTokenState(t);
    if (t) localStorage.setItem("access_token", t);
    else localStorage.removeItem("access_token");
  }

  function logout() {
    setToken(null);
  }

  return (
    <AuthContext.Provider value={{ token, setToken, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
