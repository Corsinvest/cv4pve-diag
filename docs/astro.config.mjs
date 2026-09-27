// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import corsinvestTheme from '@corsinvest/cv4pve-docs-theme';

export default defineConfig({
  site: 'https://corsinvest.github.io',
  base: '/cv4pve-diag',
  integrations: [
    starlight({
      title: 'cv4pve-diag',
      description: 'Health checks and diagnostics for Proxmox VE.',
      // Brand, logo, GitHub and "Edit page" links, the Corsinvest sidebar group and
      // external links in a new tab come from the shared cv4pve theme.
      plugins: [
        corsinvestTheme({
          repo: 'cv4pve-diag',
          // Product icon: favicon and header, dark variant for the dark theme.
          icon: { light: '/icon.svg', dark: '/icon-dark.svg' },
          // Banner on the home page: the same engine runs inside cv4pve-admin.
          admin: { module: 'diagnostics' },
          // Install-and-run panel in the home hero.
          install: {
            targets: ['linux', 'macos', 'windows'],
            run: ['--host=pve01', "--api-token='diag@pve!audit=…'", 'execute --full'],
            // Same counts as the report preview below the hero.
            output: [
              { text: '2 critical', tone: 'critical' },
              { text: '5 warning', tone: 'warning' },
              { text: '2 info', tone: 'info' },
              { text: '4 ok', tone: 'ok' },
            ],
          },
        }),
      ],
      lastUpdated: true,
      sidebar: [
        {
          label: 'Start here',
          items: ['getting-started', 'permissions'],
        },
        {
          label: 'Checks',
          items: [
            { label: 'Overview', slug: 'checks' },
            'checks/cluster',
            'checks/node',
            'checks/storage',
            'checks/vm',
            'checks/container',
          ],
        },
        {
          label: 'Reference',
          items: ['settings', 'ignored-issues', 'compliance'],
        },
      ],
    }),
  ],
});
