import React from 'react';
import { ContextProvider } from './context/ContextProvider';
import { Navbar } from './components/layout/Navbar';
import { Footer } from './components/layout/Footer';
import AppRoutes from './routes/AppRoutes';

const App = () => {
  return (
    <ContextProvider>
      <div className="flex flex-col min-h-screen">
        <Navbar />
        <main className="flex-1">
          <AppRoutes />
        </main>
        <Footer />
      </div>
    </ContextProvider>
  );
};

export default App;
