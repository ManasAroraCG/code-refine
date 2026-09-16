import { createContext, useState } from 'react';

export const AppContext = createContext();

export const AppProvider = ({ children }) => {
  const [globalLoading, setGlobalLoading] = useState(false);

  return (
    <AppContext.Provider value={{ globalLoading, setGlobalLoading }}>
      {children}
    </AppContext.Provider>
  );
};