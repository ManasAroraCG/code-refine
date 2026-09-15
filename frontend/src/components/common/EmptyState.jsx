import React from 'react';

export const EmptyState = ({ message = 'No data available' }) => (
  <div className="flex flex-col items-center justify-center py-12 text-gray-500">
    <p>{message}</p>
  </div>
);
