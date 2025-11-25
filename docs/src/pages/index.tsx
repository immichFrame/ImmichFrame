import type { ReactNode } from 'react';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';
import { discordPath, discordViewBox } from '@site/src/components/svg-paths';
import ThemedImage from '@theme/ThemedImage';
import Icon from '@mdi/react';

function HomepageHeader() {
  return (
    <header>
      <div className="h-3/4 w-full absolute -z-10">
        <img src={'img/immich-frame-logo.svg'} className="h-full w-full antialiased -z-10" alt="ImmichFrame logo" />
        <div className="w-full h-full absolute left-0 top-0 backdrop-blur-3xl bg-immich-bg/40 dark:bg-transparent"></div>
      </div>
      <section className="text-center pt-12 sm:pt-24">
        <ThemedImage
          sources={{ dark: 'img/immich-frame-logo.svg', light: 'img/immich-frame-logo.svg' }}
          className="h-[125px] w-[125px] antialiased rounded-none"
          alt="ImmichFrame logo"
        />

        <div className="mt-8">
          <p className="text-3xl md:text-5xl sm:leading-tight mb-1 font-extrabold px-4">
            An awesome way {' '}<span className="block"></span>to{' '}
            <span className="text--primary">
              display your photos {' '}
            </span>
            as
            <span className="block"></span>
            a digital photo frame<span className="block"></span>
          </p>
        </div>
        <div className="flex flex-col sm:flex-row place-items-center place-content-center mt-9 gap-4 ">
          <Link
            className="flex place-items-center place-content-center py-3 px-8 border rounded-xl no-underline hover:no-underline font-bold"
            to="/docs/overview"
          >
            Get Started
          </Link>

          <Link
            className="flex place-items-center place-content-center py-3 px-8 border bg--primary  rounded-xl hover:no-underline text-primary font-bold"
            to="https://demo.immichframe.dev/"
          >
            Open Demo
          </Link>
        </div>

        <div className="my-8 flex gap-1 font-medium place-items-center place-content-center">
          <Icon
            path={discordPath}
            viewBox={discordViewBox} /* viewBox may show an error in your IDE but it is normal. */
            size={1}
          />
          <Link to="https://discord.immich.app/">Join the immich Discord</Link>
        </div>
      </section>
    </header>
  );
}

export default function Home(): ReactNode {
  const { siteConfig } = useDocusaurusContext();
  return (
    <Layout
      title={`${siteConfig.title}`}
      noFooter={true}
      description="An awesome way to display your photos as a digital photo frame">
      <HomepageHeader />
      <main>
        <HomepageFeatures />
      </main>
    </Layout>
  );
}
