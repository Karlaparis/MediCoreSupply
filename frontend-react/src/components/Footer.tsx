import { LogoMark } from './icons'
import styles from './Footer.module.css'

const columns = [
  {
    title: 'Shop',
    links: ['Medical Supplies', 'Mobility Aids', 'Deals', 'Best Sellers'],
  },
  {
    title: 'Customer Service',
    links: ['Contact Us', 'Shipping Information', 'Returns', 'Track an Order'],
  },
  {
    title: 'For Institutions',
    links: ['Hospital Accounts', 'Clinic Accounts', 'Pharmacy Accounts', 'Bulk Ordering'],
  },
  {
    title: 'Company',
    links: ['About MediCore Supply', 'Careers', 'Privacy Policy', 'Terms of Use'],
  },
]

export default function Footer() {
  return (
    <footer id="contact" className={styles.footer}>
      <div className={`container ${styles.top}`}>
        <div className={styles.brand}>
          <div className={styles.logo}>
            <LogoMark className={styles.logoMark} />
            <span>
              <strong>medicore</strong>supply
            </span>
          </div>
          <p className={styles.tagline}>Medical supplies and equipment for hospitals, clinics and pharmacies.</p>
          <p>
            <a href="tel:18005550142">1-800-555-0142</a>
            <br />
            Mon–Fri, 8 AM–6 PM ET
          </p>
        </div>

        {columns.map((column) => (
          <nav key={column.title} aria-label={column.title}>
            <h2 className={styles.columnTitle}>{column.title}</h2>
            <ul className={styles.links}>
              {column.links.map((link) => (
                <li key={link}>
                  <a href="#">{link}</a>
                </li>
              ))}
            </ul>
          </nav>
        ))}
      </div>

      <div className={styles.bottom}>
        <div className="container">
          <p>© {new Date().getFullYear()} MediCore Supply. A portfolio demo project with synthetic data.</p>
        </div>
      </div>
    </footer>
  )
}
