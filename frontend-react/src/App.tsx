import BestSellers from './components/BestSellers'
import CategoryGrid from './components/CategoryGrid'
import Footer from './components/Footer'
import Header from './components/Header'
import HeroBanner from './components/HeroBanner'
import NavBar from './components/NavBar'
import PromoCards from './components/PromoCards'

function App() {
  return (
    <>
      <Header />
      <NavBar />
      <main>
        <HeroBanner />
        <PromoCards />
        <CategoryGrid />
        <BestSellers />
      </main>
      <Footer />
    </>
  )
}

export default App
