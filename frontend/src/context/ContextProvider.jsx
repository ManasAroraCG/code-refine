import React from 'react';
import { AuthProvider } from './AuthContext';
import { AppProvider } from './AppContext';

export const ContextProvider = ({ children }) => (
  <AppProvider>
    <AuthProvider>
      {children}
    </AuthProvider>
  </AppProvider>
);
