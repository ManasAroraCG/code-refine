import React from 'react';
import { Link } from 'react-router-dom';

const NotFound = () => {
  return (
    <div className="flex flex-col items-center justify-center min-h-[70vh] text-center p-8">
      <h1 className="text-6xl font-bold text-primary-600">404</h1>
      <p className="mt-4 text-gray-600">Page not found.</p>
      <Link to="/" className="mt-6 text-primary-600 hover:text-primary-700 font-medium">Go back home</Link>
    </div>
  );
};

export default NotFound;
