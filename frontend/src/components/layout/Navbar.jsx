import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';

export const Navbar = () => {
  const { user, logout } = useAuth();
  
  return (
    <nav className="bg-white border-b px-6 py-3 flex justify-between items-center shadow-sm">
      <div className="font-bold text-xl text-primary-600">
        <Link to="/">HackathonApp</Link>
      </div>
      <div>
        {user ? (
          <div className="flex gap-4 items-center">
            <span className="text-sm text-gray-600">Hello, {user.name}</span>
            <button onClick={logout} className="text-sm font-medium text-gray-500 hover:text-gray-900">Logout</button>
          </div>
        ) : (
          <Link to="/login" className="text-sm font-medium text-primary-600 hover:text-primary-700">Login</Link>
        )}
      </div>
    </nav>
  );
};
