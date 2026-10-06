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
      // Brand, product icon, GitHub link, the Corsinvest sidebar group and
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
          // Steps panel in the home hero: the same steps, in the same order and words, as
          // Getting started (CliGettingStarted). The commands are in the pages (CliInstall).
          steps: {
            items: [
              'Install cv4pve-diag',
              { text: 'Create an API token', href: 'permissions/#user-and-token' },
              'Run `cv4pve-diag execute`',
              'Read the report',
            ],
          },
        }),
      ],
      sidebar: [
        {
          label: 'Start here',
          items: ['getting-started', 'permissions', 'connection', 'reading-the-report', 'ai-agents', 'troubleshooting'],
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
