import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import { AuthProvider } from './auth/AuthContext'
import './styles.css'
import './soft-theme.css'
import './refined-theme.css'
import './orders-theme.css'
import './recharge-detail-theme.css'

ReactDOM.createRoot(document.getElementById('root')!).render(<React.StrictMode><AuthProvider><App /></AuthProvider></React.StrictMode>)
