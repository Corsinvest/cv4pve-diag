// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

export default defineConfig({
  site: 'https://corsinvest.github.io',
  base: '/cv4pve-diag',
  integrations: [
    starlight({
      title: 'cv4pve-diag',
      description: 'Health checks and diagnostics for Proxmox VE.',
      logo: {
        light: './src/assets/corsinvest-wordmark.svg',
        dark: './src/assets/corsinvest-wordmark-white.svg',
        alt: 'Corsinvest',
      },
      favicon: '/favicon.png',
      customCss: ['./src/styles/brand.css'],
      social: [
        { icon: 'github', label: 'GitHub', href: 'https://github.com/Corsinvest/cv4pve-diag' },
      ],
      editLink: {
        baseUrl: 'https://github.com/Corsinvest/cv4pve-diag/edit/master/docs/',
      },
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
        {
          label: 'Corsinvest',
          items: [
            { label: 'cv4pve suite', link: 'https://www.corsinvest.it/en/cv4pve/', attrs: { target: '_blank' } },
            { label: 'Professional support', link: 'https://www.corsinvest.it/en/contact/', attrs: { target: '_blank' } },
            { label: 'corsinvest.it', link: 'https://www.corsinvest.it/en/', attrs: { target: '_blank' } },
          ],
        },
      ],
    }),
  ],
});
