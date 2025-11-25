import { themes as prismThemes } from 'prism-react-renderer';
import type { Config } from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

const config: Config = {
  title: 'ImmichFrame',
  tagline: 'An awesome way to display your photos as an digital photo frame',
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',
  favicon: 'img/favicon.png',

  url: 'https://immichframe.dev',
  baseUrl: '/',
  trailingSlash: false,

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'immichFrame', // Usually your GitHub org/user name.
  projectName: 'ImmichFrame', // Usually your repo name.
  deploymentBranch: 'main',

  plugins: ["./src/plugins/tailwind-config.js"],

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          // Please change this to your repo.
          // Remove this to remove the "edit this page" links.
          editUrl:
            'https://github.com/immichFrame/ImmichFrame/tree/main/docs/',
        },
        blog: {
          showReadingTime: true,
          feedOptions: {
            type: ['rss', 'atom'],
            xslt: true,
          },
          // Please change this to your repo.
          // Remove this to remove the "edit this page" links.
          editUrl:
            'https://github.com/immichFrame/ImmichFrame/tree/main/docs/',
          // Useful options to enforce blogging best practices
          onInlineTags: 'warn',
          onInlineAuthors: 'warn',
          onUntruncatedBlogPosts: 'warn',
        },
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    // Replace with your project's social card
    image: 'img/social-card.png',
    navbar: {
      title: 'ImmichFrame',
      logo: {
        alt: 'ImmichFrame Logo',
        src: 'img/immich-frame-logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Docs',
        },
        // { to: '/blog', label: 'Blog', position: 'left' },
        {
          href: 'https://demo.immichframe.dev',
          label: 'Demo',
          position: 'left',
        },
        {
          href: 'https://github.com/immichFrame/ImmichFrame',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Getting started',
          items: [
            {
              label: 'View Demo',
              to: 'https://demo.immichframe.dev',
            },
            {
              label: 'Docker Setup',
              to: '/docs/getting-started/installation/docker',
            },
            {
              label: 'Get immich',
              to: 'https://immich.app',
            },
          ],
        },
        {
          title: 'Community',
          items: [
            {
              label: 'Discord',
              href: 'https://discord.com/channels/979116623879368755/1217843270244372480',
            },
            {
              label: 'Github Discussions',
              href: 'https://github.com/immichFrame/ImmichFrame/discussions',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'Blog',
              to: '/blog',
            },
            {
              label: 'GitHub',
              href: 'https://github.com/immichFrame/ImmichFrame',
            },
          ],
        },
      ],
      // copyright: `Copyright © ${new Date().getFullYear()} ImmichFrame.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
