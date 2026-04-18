import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './styles/index.css'
import App from './app/App'
import { applyStoredCustomerPreferences } from './services/customerPreferences'

applyStoredCustomerPreferences()

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
