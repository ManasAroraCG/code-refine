// src/components/ui/Card.jsx
import React from 'react';
import { cn } from '../../utils/cn';

export const Card = React.forwardRef(({ className, ...props }, ref) => (
  <div ref={ref} className={cn('rounded-lg border bg-white shadow-sm', className)} {...props} />
));
Card.displayName = 'Card';
