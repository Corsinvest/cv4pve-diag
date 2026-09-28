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
          // Visits, without cookies.
          matomo: { url: 'https://matomo.corsinvest.it/', siteId: 6 },
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
          items: ['getting-started', 'permissions', 'reading-the-report', 'troubleshooting'],
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
          label: 'Compliance',
          collapsed: true,
          items: [
            { label: 'Overview', slug: 'compliance' },
            'compliance/nis2',
            'compliance/nis2-ir',
            'compliance/acn',
            'compliance/dora',
            'compliance/iso-27001',
            'compliance/iso-27017',
            'compliance/iso-27018',
            'compliance/iso-22301',
            'compliance/pci-dss',
            'compliance/gdpr',
            'compliance/agid',
            'compliance/ens',
            'compliance/bsi-grundschutz',
            'compliance/c5',
            'compliance/soc-2',
            'compliance/nist-800-53',
            'compliance/nist-csf',
            'compliance/cis',
          ],
        },
        {
          label: 'Reference',
          items: ['settings', 'ignored-issues'],
        },
      ],
    }),
  ],
});
