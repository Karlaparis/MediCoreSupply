import styles from './NavBar.module.css'

const links = [
  { label: 'Shop Supplies', href: '#categories' },
  { label: 'Mobility Aids', href: '#mobility' },
  { label: 'Deals', href: '#deals' },
  { label: 'Best Sellers', href: '#best-sellers' },
  { label: 'Hospitals & Clinics', href: '#institutions' },
  { label: 'Resources', href: '#resources' },
]

const valueProps = [
  'Hospital-grade supplies and equipment',
  'Serving hospitals, clinics and pharmacies',
  'Fast shipping from two distribution centers',
]

export default function NavBar() {
  return (
    <>
      <nav className={styles.nav} aria-label="Main">
        <ul className={`container ${styles.links}`}>
          {links.map((link) => (
            <li key={link.label}>
              <a href={link.href}>{link.label}</a>
            </li>
          ))}
        </ul>
      </nav>

      <div className={styles.valueStrip}>
        <ul className={`container ${styles.values}`}>
          {valueProps.map((text) => (
            <li key={text}>{text}</li>
          ))}
        </ul>
      </div>
    </>
  )
}
