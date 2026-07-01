import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      injectRegister: 'auto',
      registerType: 'autoUpdate',
      devOptions: {
        enabled: true,
      },
      manifest: {
        name: 'Match-3 Story Flow',
        short_name: 'Story Flow',
        description: 'Local story graph and script editor for Match-3 Adventure.',
        start_url: '/',
        scope: '/',
        theme_color: '#f3efe4',
        background_color: '#f8f5ec',
        display: 'standalone',
        icons: [
          {
            src: '/pwa-icon.svg',
            sizes: 'any',
            type: 'image/svg+xml',
          },
        ],
      },
    }),
  ],
});
