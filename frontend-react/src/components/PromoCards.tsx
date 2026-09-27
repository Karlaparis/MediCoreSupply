import styles from './PromoCards.module.css'

interface Promo {
  headline: string
  detail: string
  terms?: string
}

// Static marketing content for now; a promotions endpoint could replace this later.
const promos: Promo[] = [
  { headline: 'Free Shipping', detail: 'On orders of $150 or more with code SHIP150.', terms: 'Ends 10/31/26 at 11:59 PM ET.' },
  { headline: '10% off', detail: 'Wound care dressings and bandages.', terms: 'Ends 10/15/26 at 11:59 PM ET.' },
  { headline: '5% off', detail: 'Bulk PPE orders of 10 boxes or more.', terms: 'Ends 10/15/26 at 11:59 PM ET.' },
  { headline: '15% off', detail: 'Your first order as a new clinic account.', terms: 'One use per account.' },
]

export default function PromoCards() {
  return (
    <section id="deals" className={`container ${styles.section}`} aria-label="Current deals">
      <ul className={styles.grid}>
        {promos.map((promo) => (
          <li key={promo.detail} className={styles.card}>
            <h2 className={styles.headline}>{promo.headline}</h2>
            <p className={styles.detail}>{promo.detail}</p>
            {promo.terms && <p className={styles.terms}>{promo.terms}</p>}
          </li>
        ))}
        <li className={`${styles.card} ${styles.allDeals}`}>
          <a href="#deals">Shop All Deals</a>
        </li>
      </ul>
    </section>
  )
}
