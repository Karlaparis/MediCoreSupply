import type { SVGProps } from 'react'

type IconProps = SVGProps<SVGSVGElement>

// Shared defaults for 24x24 line icons.
function LineIcon({ children, ...props }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.8}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      {...props}
    >
      {children}
    </svg>
  )
}

export function LogoMark(props: IconProps) {
  return (
    <svg viewBox="0 0 32 32" aria-hidden="true" {...props}>
      <rect width="32" height="32" rx="8" fill="currentColor" opacity="0.18" />
      <path d="M13 7h6v6h6v6h-6v6h-6v-6H7v-6h6z" fill="currentColor" />
      <circle cx="24" cy="24" r="4" fill="#1fb6a6" />
    </svg>
  )
}

export function SearchIcon(props: IconProps) {
  return (
    <LineIcon {...props}>
      <circle cx="11" cy="11" r="7" />
      <path d="m20 20-3.5-3.5" />
    </LineIcon>
  )
}

export function UserIcon(props: IconProps) {
  return (
    <LineIcon {...props}>
      <circle cx="12" cy="12" r="9.5" />
      <circle cx="12" cy="10" r="3.2" />
      <path d="M6.2 18.4a7 7 0 0 1 11.6 0" />
    </LineIcon>
  )
}

export function CartIcon(props: IconProps) {
  return (
    <LineIcon {...props}>
      <path d="M3 4h2.2l2.3 11h10.8l2-8H6.3" />
      <circle cx="9" cy="19.5" r="1.4" />
      <circle cx="17" cy="19.5" r="1.4" />
    </LineIcon>
  )
}

export function ShieldIcon(props: IconProps) {
  return (
    <LineIcon {...props}>
      <path d="M12 3 19 6v5c0 5-3.2 8.3-7 10-3.8-1.7-7-5-7-10V6z" />
      <path d="m9 12 2 2 4-4" />
    </LineIcon>
  )
}

export function BandageIcon(props: IconProps) {
  return (
    <LineIcon {...props}>
      <rect x="2.5" y="8.5" width="19" height="7" rx="3.5" transform="rotate(-45 12 12)" />
      <path d="M10 12h.01M12 10h.01M14 12h.01M12 14h.01" strokeWidth={2.4} />
    </LineIcon>
  )
}

export function StethoscopeIcon(props: IconProps) {
  return (
    <LineIcon {...props}>
      <path d="M5 3v5a4 4 0 0 0 8 0V3" />
      <path d="M9 12v2.5a5.5 5.5 0 0 0 11 0V13" />
      <circle cx="20" cy="11" r="2" />
    </LineIcon>
  )
}

export function MedicineBottleIcon(props: IconProps) {
  return (
    <LineIcon {...props}>
      <rect x="7" y="2.5" width="10" height="3.5" rx="1" />
      <path d="M8 6v1.5L6 10v10a1.5 1.5 0 0 0 1.5 1.5h9A1.5 1.5 0 0 0 18 20V10l-2-2.5V6" />
      <path d="M12 12.5v5M9.5 15h5" />
    </LineIcon>
  )
}

export function BoxIcon(props: IconProps) {
  return (
    <LineIcon {...props}>
      <path d="M3 7.5 12 3l9 4.5v9L12 21l-9-4.5z" />
      <path d="m3 7.5 9 4.5 9-4.5M12 12v9" />
    </LineIcon>
  )
}

// Picks an icon from the category name, since the API has no image field yet.
export function CategoryIcon({ categoryName, ...props }: IconProps & { categoryName: string }) {
  const name = categoryName.toLowerCase()
  if (name.includes('protective')) return <ShieldIcon {...props} />
  if (name.includes('wound')) return <BandageIcon {...props} />
  if (name.includes('diagnostic')) return <StethoscopeIcon {...props} />
  if (name.includes('pharma')) return <MedicineBottleIcon {...props} />
  return <BoxIcon {...props} />
}

// Hero illustration: a wheelchair on soft background shapes.
export function MobilityIllustration(props: IconProps) {
  return (
    <svg viewBox="0 0 400 320" aria-hidden="true" {...props}>
      <circle cx="300" cy="80" r="70" fill="#1fb6a6" opacity="0.25" />
      <circle cx="90" cy="250" r="90" fill="#ffffff" opacity="0.12" />
      <circle cx="330" cy="260" r="40" fill="#ffffff" opacity="0.18" />
      <g fill="none" stroke="#ffffff" strokeWidth="12" strokeLinecap="round" strokeLinejoin="round">
        {/* Backrest and push handle */}
        <path d="M150 60h-24l20 110" />
        {/* Seat and frame down to the front caster */}
        <path d="M146 170h92l30 60" />
        {/* Armrest */}
        <path d="M160 130h70" />
        {/* Footrest */}
        <path d="M238 170l18 46h26" />
        {/* Large rear wheel */}
        <circle cx="170" cy="226" r="58" />
        <circle cx="170" cy="226" r="8" fill="#ffffff" />
        {/* Front caster */}
        <circle cx="268" cy="250" r="16" />
      </g>
    </svg>
  )
}
