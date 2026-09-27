import { useEffect, useState } from 'react'

// Empty in development: requests go to /api/... and the Vite proxy forwards them.
// For a deployed frontend, set VITE_API_BASE_URL to the API's address.
const API_BASE_URL: string = import.meta.env.VITE_API_BASE_URL ?? ''

// These types mirror the API's response DTOs (CategoryResponse, ProductResponse).
export interface Category {
  id: number
  name: string
  description: string | null
}

export interface Product {
  id: number
  sku: string
  name: string
  description: string | null
  unitOfMeasure: string
  unitPrice: number
  requiresPrescription: boolean
  isActive: boolean
  categoryId: number
  categoryName: string
}

export interface ApiState<T> {
  data: T | null
  error: string | null
  loading: boolean
}

// Fetches JSON from the API and tracks loading and error state.
// The request is cancelled if the component unmounts or the path changes.
export function useApi<T>(path: string): ApiState<T> {
  const [state, setState] = useState<ApiState<T>>({ data: null, error: null, loading: true })

  useEffect(() => {
    const controller = new AbortController()

    fetch(`${API_BASE_URL}${path}`, { signal: controller.signal })
      .then((response) => {
        if (!response.ok) throw new Error(`Request failed with status ${response.status}`)
        return response.json() as Promise<T>
      })
      .then((data) => setState({ data, error: null, loading: false }))
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        setState({ data: null, error: error instanceof Error ? error.message : 'Request failed', loading: false })
      })

    return () => controller.abort()
  }, [path])

  return state
}
