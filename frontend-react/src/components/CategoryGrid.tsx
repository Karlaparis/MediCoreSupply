import { useApi, type Category } from '../api'
import { CategoryIcon } from './icons'
import styles from './CategoryGrid.module.css'

export default function CategoryGrid() {
  const { data: categories, error, loading } = useApi<Category[]>('/api/categories')

  return (
    <section id="categories" className={`container ${styles.section}`} aria-labelledby="categories-title">
      <h2 id="categories-title" className="section-title">
        Shop by Category
      </h2>

      {error && <p className={styles.message}>We couldn't load categories right now. Please try again later.</p>}

      <ul className={styles.grid} aria-busy={loading}>
        {loading &&
          Array.from({ length: 4 }, (_, i) => (
            <li key={i} className={styles.card}>
              <div className={`${styles.tile} ${styles.placeholder}`} />
              <div className={styles.placeholderText} />
            </li>
          ))}

        {categories?.map((category) => (
          <li key={category.id} className={styles.card}>
            <a href={`#category-${category.id}`} className={styles.link}>
              <div className={styles.tile}>
                <CategoryIcon categoryName={category.name} className={styles.icon} />
              </div>
              <span className={styles.name}>{category.name}</span>
            </a>
          </li>
        ))}
      </ul>
    </section>
  )
}
