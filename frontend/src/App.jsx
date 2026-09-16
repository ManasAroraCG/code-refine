
import { ContextProvider } from './context/ContextProvider';
import AppRoutes from './routes/AppRoutes';

const App = () => {
  return (
    <ContextProvider>
      <div className="min-h-screen bg-white text-slate-950">
        <AppRoutes />
      </div>
    </ContextProvider>
  );
};

export default App;
