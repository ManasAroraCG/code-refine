import React from 'react';

export const ErrorBanner = ({ message }) => (
  <div className="bg-red-50 border-l-4 border-red-500 p-4 mb-4">
    <p className="text-red-700">{message}</p>
  </div>
);
