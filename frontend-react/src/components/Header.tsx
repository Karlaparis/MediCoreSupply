import type { FormEvent } from 'react'
import { CartIcon, LogoMark, SearchIcon, UserIcon } from './icons'
import styles from './Header.module.css'

export default function Header() {
  // Search results page doesn't exist yet; keep the form from reloading the page.
  function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
  }

  return (
    <header className={styles.header}>
      <div className={styles.utilityBar}>
        <div className={`container ${styles.utilityInner}`}>
          <span>
            Questions? Call us: <a href="tel:18005550142">1-800-555-0142</a>
          </span>
          <a href="#contact">Contact Us</a>
        </div>
      </div>

      <div className={`container ${styles.main}`}>
        <a href="/" className={styles.logo} aria-label="MediCore Supply home">
          <LogoMark className={styles.logoMark} />
          <span>
            <span className={styles.logoStrong}>medicore</span>supply
          </span>
        </a>

        <form className={styles.search} role="search" onSubmit={handleSearch}>
          <label htmlFor="site-search" className="visually-hidden">
            Search products
          </label>
          <input id="site-search" type="search" placeholder="What are you looking for?" />
          <button type="submit" aria-label="Search">
            <SearchIcon className={styles.searchIcon} />
          </button>
        </form>

        <div className={styles.actions}>
          <a href="#account" className={styles.action}>
            <UserIcon className={styles.actionIcon} />
            <span className={styles.actionLabel}>Sign in</span>
          </a>
          <a href="#cart" className={styles.action} aria-label="Cart, 0 items">
            <span className={styles.cartIconWrap}>
              <CartIcon className={styles.actionIcon} />
              <span className={styles.cartCount}>0</span>
            </span>
            <span className={styles.actionLabel}>Cart</span>
          </a>
        </div>
      </div>
    </header>
  )
}
