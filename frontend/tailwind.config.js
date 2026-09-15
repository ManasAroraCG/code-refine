/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#eff6ff',
          100: '#dbeafe',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
        },
        'deep-space-blue': '#003049',
        'mint-cream': '#f4fff8',
        'flag-red': '#d62828',
        'princeton-orange': '#f77f00',
        'sunflower-gold': '#fcbf49',
        'vanilla-custard': '#eae2b7',
      }
    },
  },
  plugins: [],
}
