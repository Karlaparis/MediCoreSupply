import { MobilityIllustration } from './icons'
import styles from './HeroBanner.module.css'

export default function HeroBanner() {
  return (
    <section id="mobility" className={`container ${styles.wrapper}`} aria-labelledby="hero-title">
      <div className={styles.hero}>
        <div className={styles.text}>
          <p className={styles.eyebrow}>Mobility Month</p>
          <h1 id="hero-title" className={styles.title}>
            Move with Confidence
          </h1>
          <p className={styles.body}>
            Wheelchairs, walkers and daily mobility aids that help patients stay safe, comfortable and independent at
            home and in care.
          </p>
          <a href="#mobility-aids" className={styles.cta}>
            Shop Mobility Aids
          </a>
        </div>
        <div className={styles.art}>
          <MobilityIllustration className={styles.illustration} />
        </div>
      </div>
    </section>
  )
}
