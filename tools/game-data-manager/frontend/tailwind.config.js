/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // アークナイツ風カラー
        'ark-dark': '#1a1a2e',
        'ark-darker': '#0f0f1a',
        'ark-accent': '#ff6b35',
        'ark-blue': '#4a9eff',
        'ark-gold': '#ffd700',
      }
    },
  },
  plugins: [],
}
