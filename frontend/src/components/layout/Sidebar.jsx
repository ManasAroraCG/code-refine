import React from 'react';
import { Link } from 'react-router-dom';

export const Sidebar = () => (
  <aside className="w-64 bg-gray-50 border-r h-[calc(100vh-60px)] p-4 hidden md:block">
    <nav className="space-y-2">
      <Link to="/dashboard" className="block px-4 py-2 rounded text-gray-700 hover:bg-gray-200">Dashboard</Link>
      <Link to="/" className="block px-4 py-2 rounded text-gray-700 hover:bg-gray-200">Home</Link>
    </nav>
  </aside>
);
