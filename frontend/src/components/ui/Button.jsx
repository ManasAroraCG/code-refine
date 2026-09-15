// src/components/ui/Button.jsx
import React from 'react';
import { cn } from '../../utils/cn';

export const Button = React.forwardRef(({ className, variant = 'primary', ...props }, ref) => {
  return (
    <button
      ref={ref}
      className={cn(
        'px-4 py-2 rounded-md font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-princeton-orange focus:ring-offset-2',
        variant === 'primary' && 'bg-deep-space-blue text-mint-cream hover:bg-deep-space-blue/90',
        variant === 'secondary' && 'bg-vanilla-custard text-deep-space-blue hover:bg-sunflower-gold',
        variant === 'outline' && 'border border-deep-space-blue bg-transparent hover:bg-mint-cream text-deep-space-blue',
        className
      )}
      {...props}
    />
  );
});
Button.displayName = 'Button';
