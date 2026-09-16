import React from 'react';
import { AuthProvider } from './AuthContext';

export const ContextProvider = ({ children }) => (
  <AuthProvider>
    {children}
  </AuthProvider>
);
