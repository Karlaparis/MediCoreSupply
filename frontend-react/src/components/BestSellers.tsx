import { useApi, type Product } from '../api'
import { CategoryIcon } from './icons'
import styles from './BestSellers.module.css'

const PRODUCTS_SHOWN = 4

const price = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

export default function BestSellers() {
  const { data: products, error, loading } = useApi<Product[]>('/api/products')

  // The API has no sales ranking yet, so this shows the first active products.
  const featured = products?.filter((product) => product.isActive).slice(0, PRODUCTS_SHOWN)

  return (
    <section id="best-sellers" className={`container ${styles.section}`} aria-labelledby="best-sellers-title">
      <h2 id="best-sellers-title" className="section-title">
        Shop Best Selling Products
      </h2>

      {error && <p className={styles.message}>We couldn't load products right now. Please try again later.</p>}

      <ul className={styles.grid} aria-busy={loading}>
        {loading &&
          Array.from({ length: PRODUCTS_SHOWN }, (_, i) => (
            <li key={i} className={styles.card}>
              <div className={`${styles.image} ${styles.placeholder}`} />
            </li>
          ))}

        {featured?.map((product) => (
          <li key={product.id} className={styles.card}>
            <div className={styles.image}>
              <CategoryIcon categoryName={product.categoryName} className={styles.icon} />
              {product.requiresPrescription && <span className={styles.badge}>Rx required</span>}
            </div>
            <div className={styles.body}>
              <p className={styles.category}>{product.categoryName}</p>
              <h3 className={styles.name}>{product.name}</h3>
              <p className={styles.sku}>SKU {product.sku}</p>
              <p className={styles.price}>
                {price.format(product.unitPrice)}
                <span className={styles.unit}> / {product.unitOfMeasure}</span>
              </p>
              <button type="button" className={styles.addButton}>
                Add to Cart
              </button>
            </div>
          </li>
        ))}
      </ul>
    </section>
  )
}
