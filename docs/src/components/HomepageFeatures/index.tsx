
import React from 'react';
import styles from './styles.module.css';

function Feature({ icon, title, description }: { icon: string; title: string; description: string }) {
  return (
    <div className="col col--4">
      <div style={{ textAlign: 'center', padding: '1rem' }}>
        <div style={{ fontSize: '3rem' }}>{icon}</div>
        <h3>{title}</h3>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures() {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          <Feature
            icon="🖼️"
            title="Beautiful Slideshow"
            description="Display your Immich albums on a fullscreen frame-like view – perfect for living rooms, walls, or tablets." />
          <Feature
            icon="🖌️"
            title="Customizable"
            description="Customize your slideshow to your liking." />
          <Feature
            icon="🔌"
            title="Wide Device Support"
            description="ImmichFrame can run on any modern browser, Android, Android TV, Apple TV, etc." />
        </div>
      </div>
    </section>
  );
}
